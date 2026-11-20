#!/bin/bash
# Register a one-shot background wake for the moment the CI workflow run for a
# commit finishes, pass or fail, so nobody polls with sleep loops.
#
#   scripts/dev/ci-wait.sh <full-sha>
#
# Needs the FULL 40-char SHA: `gh run list --commit` silently returns [] for a
# short one. The run may not exist yet (push just landed), so the watcher first
# waits for `gh run list --commit <sha> --workflow CI` to return one, then runs
# `gh run watch` on it. Its output ends with the run id and the conclusion.
#
# Built on household's track-job.sh, --quiet: the end is filed as a note the
# room reads on its next turn. To be woken outright, name the label
# (ci-<sha8>) as the `on` of a blocked set_status step.
#
# Env: TRACK_JOB (path to track-job.sh), CI_WAIT_FIND_TIMEOUT (seconds to wait
# for the run to appear, default 600), CI_WAIT_REPO (owner/name, default gh's).

set -uo pipefail

TRACK_JOB="${TRACK_JOB:-$HOME/github/household/tools/track-job.sh}"
FIND_TIMEOUT="${CI_WAIT_FIND_TIMEOUT:-600}"
# gh reads GH_REPO; an empty --repo array would trip bash 3.2 under set -u.
[ -n "${CI_WAIT_REPO:-}" ] && export GH_REPO="$CI_WAIT_REPO"

INNER=0
if [ "${1:-}" = "--inner" ]; then INNER=1; shift; fi
SHA="${1:-}"

if ! [[ "$SHA" =~ ^[0-9a-f]{40}$ ]]; then
    echo "usage: $0 <full-40-char-sha>  (short SHAs make gh run list return [])" >&2
    exit 2
fi

if [ "$INNER" = "0" ]; then
    [ -x "$TRACK_JOB" ] || { echo "no track-job.sh at $TRACK_JOB" >&2; exit 1; }
    exec "$TRACK_JOB" --quiet here --run "ci-${SHA:0:8}" -- \
        "$(cd "$(dirname "$0")" && pwd)/$(basename "$0")" --inner "$SHA"
fi

# Inner half: runs detached under track-job.sh.
deadline=$(( $(date +%s) + FIND_TIMEOUT ))
RUN_ID=""
while :; do
    RUN_ID=$(gh run list --commit "$SHA" --workflow CI \
        --json databaseId --jq '.[0].databaseId // empty' 2>/dev/null)
    [ -n "$RUN_ID" ] && break
    if [ "$(date +%s)" -ge "$deadline" ]; then
        echo "no CI run appeared for $SHA within ${FIND_TIMEOUT}s"
        exit 1
    fi
    sleep 15
done

# gh run watch exits non-zero on a failed run only with --exit-status, and can
# itself drop on a network blip, so the verdict is read back from the run.
gh run watch "$RUN_ID" --interval 20 >/dev/null 2>&1
while :; do
    CONCLUSION=$(gh run view "$RUN_ID" --json status,conclusion \
        --jq 'select(.status == "completed") | .conclusion' 2>/dev/null)
    [ -n "$CONCLUSION" ] && break
    sleep 20
done

echo "CI run $RUN_ID for $SHA: $CONCLUSION"
echo "run id: $RUN_ID"
echo "conclusion: $CONCLUSION"
[ "$CONCLUSION" = "success" ]
