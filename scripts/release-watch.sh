#!/bin/bash
# Release watch: says so in Discord when the Release workflow fails.
#
# Why this exists: CI's `yarn build` is a *development* build, while the
# release runs `yarn build --env production`. Anything that only breaks under
# minification therefore passes CI and dies in Release — and because Release is
# a separate workflow triggered after CI's green tick, its failure puts no red
# mark on the commit. That is exactly how main sat unable to publish an image
# from 21 Sep to 23 Sep 2026 with nobody noticing. Adding the production build
# to CI was considered and deliberately rejected; this is the compensating
# control, so it has to be loud.
#
# Like sentry-watch.sh it runs no LLM of its own: it reports, and the agent in
# the channel does the diagnosis in the open.
#
# Run via cron: */15 * * * * /path/to/repo/scripts/release-watch.sh

set -uo pipefail

# cron runs with PATH=/usr/bin:/bin, which does not include gh.
export PATH="/usr/local/bin:/opt/homebrew/bin:$HOME/.local/bin:$PATH"

REPO_DIR="$(cd "$(dirname "$0")/.." && pwd)"
LOG_FILE="${REPO_DIR}/.release-watch.log"

# State lives outside the repo: a cron job writing a tracked file leaves a
# dirty tree under whoever is working here.
STATE_FILE="${RELEASE_SEEN_FILE:-$HOME/.local/state/gamarr-release-seen.json}"

WAKE="${WAKE_SCRIPT:-$HOME/github/household/tools/wake.sh}"
CHANNEL="${RELEASE_WAKE_CHANNEL:-1533572556018290958}" # #gamarr

log() { echo "$(date): $1" >> "$LOG_FILE"; }

notify() {
    if [ -x "$WAKE" ]; then
        "$WAKE" -c "$CHANNEL" "$1" >> "$LOG_FILE" 2>&1 || log "WARNING: wake.sh failed"
    else
        log "WARNING: no wake script at $WAKE, cannot notify"
    fi
}

# The failure paths have no dedup of their own and would otherwise fire on
# every run — 96 messages a day at */15. Announce once, go quiet, speak up
# again only if still broken after FAIL_QUIET_HOURS.
FAIL_QUIET_HOURS="${RELEASE_FAIL_QUIET_HOURS:-6}"

notify_failure() {
    local key="$1" msg="$2"
    local stamp
    stamp="$(dirname "$STATE_FILE")/gamarr-release-fail-${key}.stamp"
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

clear_failure_stamps() {
    rm -f "$(dirname "$STATE_FILE")"/gamarr-release-fail-*.stamp
}

cd "$REPO_DIR" || { log "ERROR: Cannot cd to $REPO_DIR"; exit 1; }

log "=== Starting Release check ==="

if ! command -v gh >/dev/null; then
    log "ERROR: gh not on PATH"
    notify_failure gh "Release watch is broken: gh is not on PATH under cron, so no release is being checked."
    exit 1
fi

if ! gh run list --workflow Release --limit 20 \
        --json databaseId,headSha,headBranch,conclusion,status,createdAt,url \
        > /tmp/gamarr-release-runs.json 2>>"$LOG_FILE"; then
    log "ERROR: gh run list failed"
    notify_failure api "Release watch could not reach GitHub. Tail $LOG_FILE on gamarr."
    exit 1
fi

clear_failure_stamps

mkdir -p "$(dirname "$STATE_FILE")"

# Diff failed runs against what we have already reported. First run seeds the
# state silently, so switching this on does not replay every historic failure.
SUMMARY=$(python3 - "$STATE_FILE" <<'PY' 2>>"$LOG_FILE"
import json, os, sys

state_file = sys.argv[1]
runs = json.load(open('/tmp/gamarr-release-runs.json'))

# 'cancelled' is usually the concurrency group superseding a run, not a break.
BAD = {'failure', 'timed_out', 'startup_failure'}
bad = [r for r in runs
       if r['status'] == 'completed' and r['conclusion'] in BAD]

seeding = not os.path.exists(state_file)
seen = set()
if not seeding:
    seen = {str(s) for s in json.load(open(state_file))}

new = [r for r in bad if str(r['databaseId']) not in seen]

if new or seeding:
    with open(state_file, 'w') as f:
        json.dump(sorted(seen | {str(r['databaseId']) for r in bad}), f, indent=2)

if seeding or not new:
    sys.exit(1)

lines = [f"Release FAILED: {len(new)} run(s) on gamarr did not publish an image."]
for r in new:
    lines.append(
        f"  - {r['headBranch']} {r['headSha'][:9]} — {r['conclusion']} "
        f"at {r['createdAt']}\n    {r['url']}"
    )
lines.append(
    "Nothing shipped from those commits. Note CI can be green here: CI builds the "
    "frontend in development mode and only Release builds it for production. "
    "Run `gh run view <id> --log-failed` and fix it — main cannot publish until you do."
)
print("\n".join(lines))
PY
) || { log "No new Release failures. Done."; exit 0; }

log "Found new Release failure(s), notifying Discord:"
echo "$SUMMARY" >> "$LOG_FILE"
notify "$SUMMARY"
log "=== Done ==="
