#!/bin/bash
# Daily Sentry auto-fix: checks for new issues, invokes Claude only if needed.
# Reads SENTRY_AUTH_TOKEN from .env in the repo root.
# Run via cron: 0 9 * * * /path/to/repo/scripts/sentry-autofix.sh

set -uo pipefail

REPO_DIR="$(cd "$(dirname "$0")/.." && pwd)"
LOG_FILE="${REPO_DIR}/.sentry-autofix.log"
SEEN_FILE="${REPO_DIR}/.sentry-seen-issues.json"
SENTRY_ORG="gamarr"

log() { echo "$(date): $1" >> "$LOG_FILE"; }

cd "$REPO_DIR" || { log "ERROR: Cannot cd to $REPO_DIR"; exit 1; }

log "=== Starting Sentry check ==="

# Pull latest
if ! git pull --rebase origin main >> "$LOG_FILE" 2>&1; then
    log "WARNING: git pull failed, continuing with local state"
fi

# Load credentials
if [ ! -f "$REPO_DIR/.env" ]; then
    log "ERROR: .env file not found at $REPO_DIR/.env"
    exit 1
fi
set -a
source "$REPO_DIR/.env"
set +a

if [ -z "${SENTRY_AUTH_TOKEN:-}" ]; then
    log "ERROR: SENTRY_AUTH_TOKEN not set in .env"
    exit 1
fi

log "Fetching unresolved issues from Sentry..."

# Fetch unresolved issues
HTTP_CODE=$(curl -s -o /tmp/sentry-issues.json -w "%{http_code}" \
    -H "Authorization: Bearer ${SENTRY_AUTH_TOKEN}" \
    "https://sentry.io/api/0/organizations/${SENTRY_ORG}/issues/?query=is:unresolved&sort=date&limit=10")

if [ "$HTTP_CODE" != "200" ]; then
    log "ERROR: Sentry API returned HTTP $HTTP_CODE"
    [ -f /tmp/sentry-issues.json ] && cat /tmp/sentry-issues.json >> "$LOG_FILE"
    exit 1
fi

ISSUES=$(cat /tmp/sentry-issues.json)
ISSUE_COUNT=$(echo "$ISSUES" | python3 -c "import json,sys; print(len(json.load(sys.stdin)))" 2>&1)
log "Fetched $ISSUE_COUNT unresolved issues"

# Load seen IDs
if [ -f "$SEEN_FILE" ]; then
    SEEN=$(cat "$SEEN_FILE")
    SEEN_COUNT=$(echo "$SEEN" | python3 -c "import json,sys; print(len(json.load(sys.stdin)))" 2>&1)
    log "Loaded $SEEN_COUNT seen issue IDs"
else
    SEEN="[]"
    log "No seen issues file, starting fresh"
fi

# Find new error-level issues not in seen list
NEW_ISSUES=$(echo "$ISSUES" | python3 -c "
import json, sys
issues = json.load(sys.stdin)
seen = json.loads('''${SEEN}''')
seen_ids = set(str(s) for s in seen)
new = [i for i in issues if str(i['id']) not in seen_ids and i['level'] == 'error']
if not new:
    sys.exit(1)
for i in new:
    print(f\"  - [{i['id']}] {i['title']} (count: {i['count']})\", file=sys.stderr)
print(json.dumps([{'id': i['id'], 'title': i['title'], 'count': i['count'], 'firstSeen': i['firstSeen']} for i in new]))
" 2>> "$LOG_FILE") || {
    log "No new issues. Done."
    exit 0
}

NEW_COUNT=$(echo "$NEW_ISSUES" | python3 -c "import json,sys; print(len(json.load(sys.stdin)))")
log "Found $NEW_COUNT new issue(s), fetching stack traces..."

# Fetch stack traces for each new issue
DETAILS=$(echo "$NEW_ISSUES" | python3 -c "
import json, sys, subprocess
issues = json.load(sys.stdin)
for issue in issues:
    iid = issue['id']
    result = subprocess.run(
        ['curl', '-sf', '-H', 'Authorization: Bearer ${SENTRY_AUTH_TOKEN}',
         'https://sentry.io/api/0/organizations/${SENTRY_ORG}/issues/' + str(iid) + '/events/latest/'],
        capture_output=True, text=True)
    if result.returncode == 0:
        issue['event'] = json.loads(result.stdout)
        print(f'Fetched event for issue {iid}', file=sys.stderr)
    else:
        print(f'WARNING: Failed to fetch event for issue {iid} (exit {result.returncode})', file=sys.stderr)
print(json.dumps(issues, indent=2))
" 2>> "$LOG_FILE")

if [ -z "$DETAILS" ]; then
    log "ERROR: Failed to fetch issue details"
    exit 1
fi

log "Invoking Claude to analyze and fix..."

CLAUDE_OUTPUT=$(claude -p --model opus "New Sentry issues found. Here are the details:

$DETAILS

For each issue:
1. Determine if it's a real code bug (crash, type error, overflow, null ref) or a user/environment issue (bad config, transient network)
2. If it's a code bug: find the source, implement a minimal fix, build with 'dotnet build src/Gamarr.sln', test with 'dotnet test src/Gamarr.sln --filter Category!=AutomationTest'
3. If not a code bug: skip it

After processing, update .sentry-seen-issues.json — append all new issue IDs (both fixed and skipped).
Commit all changes and push to main.
Keep fixes minimal. Do not refactor surrounding code." 2>&1) || {
    log "ERROR: Claude exited with code $?"
    echo "$CLAUDE_OUTPUT" >> "$LOG_FILE"
    exit 1
}

echo "$CLAUDE_OUTPUT" >> "$LOG_FILE"
log "=== Done ==="
