#!/bin/bash
# Port one Radarr/Sonarr commit into the current checkout.
#
#   scripts/upstream-port.sh [--check] [--commit] <remote> <sha>
#
# The upstream patch is rewritten into Gamarr's vocabulary first (Movie->Game,
# Radarr->Gamarr, Series->Game, ... — see scripts/upstream-rename-map.json), then
# applied file by file: plain apply where it fits, en.json merged by key, and
# `git apply --3way` for the rest, which leaves conflict markers to resolve by hand.
# Files Gamarr does not have (Movie-only features, old migrations) and changes
# already present are skipped and listed.
#
#   --check   report per-file verdicts against HEAD, change nothing
#   --commit  if every file applied cleanly, commit as
#             "<upstream subject> (upstream <remote> <sha8>)"
#
# Always read the staged diff before committing: the token map is mechanical, and
# a "movie" in a user-facing string or comment may need a better word than "game".
set -uo pipefail

REPO_DIR="$(cd "$(dirname "$0")/.." && pwd)"
CHECK=0
COMMIT=0
while [ $# -gt 0 ]; do
    case "$1" in
        --check)  CHECK=1; shift ;;
        --commit) COMMIT=1; shift ;;
        -h|--help) sed -n '2,20p' "$0"; exit 0 ;;
        -*) echo "unknown flag: $1" >&2; exit 2 ;;
        *) break ;;
    esac
done
if [ $# -ne 2 ]; then
    echo "usage: $0 [--check] [--commit] <remote> <sha>" >&2
    exit 2
fi
REMOTE="$1"
SHA="$2"

cd "$REPO_DIR" || exit 1
if ! git rev-parse --verify --quiet "${SHA}^{commit}" >/dev/null; then
    git fetch --quiet --filter=blob:none "$REMOTE" || { echo "fetch $REMOTE failed" >&2; exit 2; }
fi
FULL="$(git rev-parse --verify --quiet "${SHA}^{commit}")" || { echo "unknown commit $SHA" >&2; exit 2; }

if [ "$CHECK" = 1 ]; then
    exec python3 -B "$REPO_DIR/scripts/upstream_translate.py" check "$REMOTE" "$FULL"
fi

if [ -n "$(git diff --cached --name-only)" ]; then
    echo "index has staged changes; commit or unstage them first" >&2
    exit 2
fi

echo "Porting $REMOTE ${FULL:0:8}: $(git log -1 --format=%s "$FULL")"
python3 -B "$REPO_DIR/scripts/upstream_translate.py" apply "$REMOTE" "$FULL"
rc=$?

if [ "$rc" -ne 0 ]; then
    echo
    echo "Some files need a hand port: resolve the conflict markers (git diff), then commit."
    exit 1
fi
if [ -z "$(git diff --cached --name-only)" ]; then
    echo "Nothing to change: already present, or only touches files gamarr lacks."
    exit 0
fi
if [ "$COMMIT" = 1 ]; then
    SUBJECT="$(git log -1 --format=%s "$FULL")"
    git commit -q -m "$SUBJECT (upstream $REMOTE ${FULL:0:8})" \
        -m "Ported with scripts/upstream-port.sh from $REMOTE $FULL." && git log -1 --oneline
else
    git diff --cached --stat
    echo "Staged. Review with 'git diff --cached', then commit as"
    echo "  \"$(git log -1 --format=%s "$FULL") (upstream $REMOTE ${FULL:0:8})\""
fi
