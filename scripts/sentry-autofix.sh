#!/bin/bash
# Daily Sentry auto-fix: checks for new issues, invokes Claude only if needed.
# Reads SENTRY_AUTH_TOKEN from .env in the repo root.
# Run via cron: 0 9 * * * /path/to/repo/scripts/sentry-autofix.sh

set -euo pipefail

REPO_DIR="$(cd "$(dirname "$0")/.." && pwd)"
LOG_FILE="${REPO_DIR}/.sentry-autofix.log"
SEEN_FILE="${REPO_DIR}/.sentry-seen-issues.json"
SENTRY_ORG="gamarr"

cd "$REPO_DIR"
git pull --rebase origin main 2>/dev/null || true

# Load credentials
set -a
source "$REPO_DIR/.env"
set +a

: "${SENTRY_AUTH_TOKEN:?Set SENTRY_AUTH_TOKEN in .env}"

log() { echo "$(date): $1" >> "$LOG_FILE"; }
log "Checking Sentry for new issues..."

# Fetch unresolved issues
ISSUES=$(curl -sf -H "Authorization: Bearer ${SENTRY_AUTH_TOKEN}" \
  "https://sentry.io/api/0/organizations/${SENTRY_ORG}/issues/?query=is:unresolved&sort=date&limit=10")

# Load seen IDs
if [ -f "$SEEN_FILE" ]; then
  SEEN=$(cat "$SEEN_FILE")
else
  SEEN="[]"
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
print(json.dumps([{'id': i['id'], 'title': i['title'], 'count': i['count'], 'firstSeen': i['firstSeen']} for i in new]))
" 2>/dev/null) || {
    log "No new issues."
    exit 0
}

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
print(json.dumps(issues, indent=2))
")

log "New issues found, invoking Claude..."
echo "$DETAILS" >> "$LOG_FILE"

claude -p --model opus "New Sentry issues found. Here are the details:

$DETAILS

For each issue:
1. Determine if it's a real code bug (crash, type error, overflow, null ref) or a user/environment issue (bad config, transient network)
2. If it's a code bug: find the source, implement a minimal fix, build with 'dotnet build src/Gamarr.sln', test with 'dotnet test src/Gamarr.sln --filter Category!=AutomationTest'
3. If not a code bug: skip it

After processing, update .sentry-seen-issues.json — append all new issue IDs (both fixed and skipped).
Commit all changes and push to main.
Keep fixes minimal. Do not refactor surrounding code." >> "$LOG_FILE" 2>&1

log "Done."
