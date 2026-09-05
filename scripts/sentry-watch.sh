#!/bin/bash
# Sentry watch: cheap check for new unresolved issues. If it finds any, it says
# so in Discord and stops there — the agent in that channel does the triage and
# the fixing, in the open, where it can be seen and argued with.
#
# It deliberately runs no LLM of its own. The version this replaced shelled out
# to `claude -p` on every hit: that spends inference unattended, and lands fixes
# in main that nobody watched happen.
#
# Reads SENTRY_AUTH_TOKEN from .env in the repo root.
# Run via cron: */15 * * * * /path/to/repo/scripts/sentry-watch.sh

set -uo pipefail

# cron runs with PATH=/usr/bin:/bin, which has neither the tools in ~/.local/bin
# nor the .NET SDK. Without this the script dies at whatever step first needs one.
export DOTNET_ROOT="${DOTNET_ROOT:-$HOME/.dotnet}"
export PATH="$HOME/.local/bin:$DOTNET_ROOT:$PATH"

REPO_DIR="$(cd "$(dirname "$0")/.." && pwd)"
LOG_FILE="${REPO_DIR}/.sentry-autofix.log"
SENTRY_ORG="gamarr"

# Live state lives outside the repo: .sentry-seen-issues.json is tracked, and a
# cron job that writes a tracked file leaves a dirty tree under whoever is
# working here. That file is now only ever read, as the initial seed.
SEED_FILE="${REPO_DIR}/.sentry-seen-issues.json"
STATE_FILE="${SENTRY_SEEN_FILE:-$HOME/.local/state/gamarr-sentry-seen.json}"

WAKE="${WAKE_SCRIPT:-$HOME/github/household/tools/wake.sh}"
CHANNEL="${SENTRY_WAKE_CHANNEL:-1533572556018290958}" # #gamarr

log() { echo "$(date): $1" >> "$LOG_FILE"; }

# Every failure below is worth saying out loud. This job spent five months
# failing silently once already.
notify() {
    if [ -x "$WAKE" ]; then
        "$WAKE" -c "$CHANNEL" "$1" >> "$LOG_FILE" 2>&1 || log "WARNING: wake.sh failed"
    else
        log "WARNING: no wake script at $WAKE, cannot notify"
    fi
}

# Waking the channel is the one expensive thing this script does — an agent
# reads it, and that is inference. The new-issue path pays for itself, because
# the seen-issue state means it only ever fires on something genuinely new.
# The failure paths had no such dedup: they fire on every run, which was one
# message a day at the old daily cadence and is 96 a day at */15. So a missing
# token or a Sentry outage now says so once, goes quiet for FAIL_QUIET_HOURS,
# and speaks up again only if it is still broken after that.
FAIL_QUIET_HOURS="${SENTRY_FAIL_QUIET_HOURS:-6}"

notify_failure() {
    local key="$1" msg="$2"
    local stamp
    stamp="$(dirname "$STATE_FILE")/gamarr-sentry-fail-${key}.stamp"
    mkdir -p "$(dirname "$stamp")"

    if [ -f "$stamp" ]; then
        local age=$(( $(date +%s) - $(stat -f %m "$stamp" 2>/dev/null || echo 0) ))
        if [ "$age" -lt $(( FAIL_QUIET_HOURS * 3600 )) ]; then
            log "Still failing ($key), but notified ${age}s ago — staying quiet."
            return
        fi
    fi

    date +%s > "$stamp"
    notify "$msg"
}

# Once a run gets through cleanly, forget the failures so the next outage is
# announced immediately rather than swallowed by a stale stamp.
clear_failure_stamps() {
    rm -f "$(dirname "$STATE_FILE")"/gamarr-sentry-fail-*.stamp
}

cd "$REPO_DIR" || { log "ERROR: Cannot cd to $REPO_DIR"; exit 1; }

log "=== Starting Sentry check ==="

if [ ! -f "$REPO_DIR/.env" ]; then
    log "ERROR: .env file not found at $REPO_DIR/.env"
    notify_failure env "Sentry watch is broken: there is no .env at $REPO_DIR, so it has no token and is checking nothing."
    exit 1
fi
set -a
source "$REPO_DIR/.env"
set +a

if [ -z "${SENTRY_AUTH_TOKEN:-}" ]; then
    log "ERROR: SENTRY_AUTH_TOKEN not set in .env"
    notify_failure token "Sentry watch is broken: SENTRY_AUTH_TOKEN is missing from $REPO_DIR/.env, so nothing is being checked."
    exit 1
fi

log "Fetching unresolved issues from Sentry..."

HTTP_CODE=$(curl -s -o /tmp/sentry-issues.json -w "%{http_code}" \
    -H "Authorization: Bearer ${SENTRY_AUTH_TOKEN}" \
    "https://sentry.io/api/0/organizations/${SENTRY_ORG}/issues/?query=is:unresolved&sort=date&limit=25")

if [ "$HTTP_CODE" != "200" ]; then
    log "ERROR: Sentry API returned HTTP $HTTP_CODE"
    [ -f /tmp/sentry-issues.json ] && cat /tmp/sentry-issues.json >> "$LOG_FILE"
    notify_failure "http" "Sentry watch could not reach Sentry: HTTP $HTTP_CODE. Tail $LOG_FILE on gamarr for the body."
    exit 1
fi

clear_failure_stamps

mkdir -p "$(dirname "$STATE_FILE")"
if [ ! -f "$STATE_FILE" ] && [ -f "$SEED_FILE" ]; then
    cp "$SEED_FILE" "$STATE_FILE"
    log "Seeded state from $SEED_FILE"
fi

# Diff against what we have already reported, write the new state, and print a
# human-readable summary for Discord. Exits 1 when there is nothing new.
SUMMARY=$(python3 - "$STATE_FILE" <<'PY' 2>>"$LOG_FILE"
import json, os, sys

state_file = sys.argv[1]
issues = json.load(open('/tmp/sentry-issues.json'))
seen = set()
if os.path.exists(state_file):
    seen = {str(s) for s in json.load(open(state_file))}

new = [i for i in issues if str(i['id']) not in seen and i['level'] == 'error']
if not new:
    sys.exit(1)

lines = [f"Sentry: {len(new)} new unresolved issue(s) in gamarr."]
for i in new:
    lines.append(
        f"  - {i['shortId']} [{i['id']}] {i['title']} "
        f"({i['count']} event(s), first seen {i['firstSeen']})"
    )
lines.append(
    "Token is in ~/github/gamarr/.env. Pull each one's latest event, work out "
    "whether it is a real bug or environment noise, fix what is real, and resolve them."
)

with open(state_file, 'w') as f:
    json.dump(sorted(seen | {str(i['id']) for i in new}), f, indent=2)

print("\n".join(lines))
PY
) || { log "No new issues. Done."; exit 0; }

log "Found new issue(s), notifying Discord:"
echo "$SUMMARY" >> "$LOG_FILE"
notify "$SUMMARY"
log "=== Done ==="
