#!/bin/bash
# Resolve Sentry issues so that events from OLDER releases do not reopen them.
#
#   scripts/sentry-resolve.sh <issue-id...>
#
# A plain resolve reopens the moment an old install reports the same error
# again. Resolving "in the next release" only reopens on a release newer than
# the fix. Tries, in order:
#   1. {"status":"resolved","statusDetails":{"inNextRelease":true}}
#   2. inRelease with the org's latest release, if Sentry rejects (1) for the project
#   3. a plain resolve
# Prints the issue id and which form took. Never prints the token.
#
# Reads SENTRY_AUTH_TOKEN from .env in the repo root (or ~/github/gamarr/.env).

set -uo pipefail

REPO_DIR="$(cd "$(dirname "$0")/.." && pwd)"
SENTRY_ORG="${SENTRY_ORG:-gamarr}"
API="https://sentry.io/api/0"

if [ $# -eq 0 ]; then
    echo "usage: $0 <issue-id...>" >&2
    exit 2
fi
# .env is untracked, so a worktree has none: fall back to the main checkout's.
ENV_FILE="$REPO_DIR/.env"
[ -f "$ENV_FILE" ] || ENV_FILE="$HOME/github/gamarr/.env"
if [ ! -f "$ENV_FILE" ]; then
    echo "no .env at $REPO_DIR or ~/github/gamarr" >&2
    exit 1
fi
set -a
# shellcheck disable=SC1090
. "$ENV_FILE" >/dev/null 2>&1
set +a
if [ -z "${SENTRY_AUTH_TOKEN:-}" ]; then
    echo "SENTRY_AUTH_TOKEN is missing from the .env" >&2
    exit 1
fi

OUT="$(mktemp)"
trap 'rm -f "$OUT"' EXIT

# put_issue <id> <json body>: prints the HTTP code, body lands in $OUT.
put_issue() {
    curl -s --connect-timeout 15 --max-time 60 -o "$OUT" -w "%{http_code}" -X PUT \
        -H "Authorization: Bearer ${SENTRY_AUTH_TOKEN}" \
        -H "Content-Type: application/json" \
        -d "$2" "${API}/organizations/${SENTRY_ORG}/issues/$1/"
}

LATEST_RELEASE=""
latest_release() {
    [ -n "$LATEST_RELEASE" ] && return
    LATEST_RELEASE=$(curl -s --connect-timeout 15 --max-time 60 \
        -H "Authorization: Bearer ${SENTRY_AUTH_TOKEN}" \
        "${API}/organizations/${SENTRY_ORG}/releases/?per_page=1" |
        python3 -c 'import json,sys; r=json.load(sys.stdin); print(r[0]["version"] if r else "")' 2>/dev/null)
}

rc=0
for id in "$@"; do
    code=$(put_issue "$id" '{"status":"resolved","statusDetails":{"inNextRelease":true}}')
    if [ "$code" = "200" ]; then
        echo "$id resolved (inNextRelease)"
        continue
    fi
    case "$code" in 401|403|404|429|5??|000)
        echo "$id FAILED: HTTP $code" >&2; rc=1; continue ;;
    esac

    # 400: this project does not take inNextRelease (no release tracking).
    latest_release
    if [ -n "$LATEST_RELEASE" ]; then
        body=$(python3 -c 'import json,sys; print(json.dumps({"status":"resolved","statusDetails":{"inRelease":sys.argv[1]}}))' "$LATEST_RELEASE")
        code=$(put_issue "$id" "$body")
        if [ "$code" = "200" ]; then
            echo "$id resolved (inRelease $LATEST_RELEASE; inNextRelease unsupported)"
            continue
        fi
    fi

    code=$(put_issue "$id" '{"status":"resolved"}')
    if [ "$code" = "200" ]; then
        echo "$id resolved (plain; will reopen on old-release events)"
    else
        echo "$id FAILED: HTTP $code" >&2; rc=1
    fi
done
exit "$rc"
