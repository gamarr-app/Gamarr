#!/bin/bash
# Fail if a personal email address has been committed into file *contents*.
#
# This exists because of contrib/prowlarr/0001-New-Gamarr-application-support.patch,
# which carried "From: Claude <a personal address>" for two months. Nothing caught
# it: the commit that added the file was correctly authored as noreply@anthropic.com,
# so every author-identity check passed. `git format-patch` writes the *original
# commit's author* into the From: header, so a commit made elsewhere under the wrong
# identity gets carried in as file content — and no hook, no secret scanner and no CI
# step was looking at contents. Purging it afterwards meant rewriting 185 commits and
# force-pushing 99 tags.
#
# This is an ALLOWLIST, not a denylist. A denylist only blocks addresses someone
# already thought to name — it has to store the very string it is protecting, and
# it lets the next unrelated address straight through. The allowlist inverts that:
# every address in the tree is known-good and anything new fails, so no personal
# address needs to be recorded here at all.
#
# Entries live in scripts/allowed-emails.sha256 as SHA-256 of the lowercased
# address. Writing addresses in clear would make that file the largest collection
# of personal data in the repo — and the first version of this script did exactly
# that with a denylist, re-committing the address it was meant to keep out and
# tripping its own check.
#
# Two rules:
#   1. Every address in file contents must be on the allowlist.
#   2. Identity headers inside vendored *.patch/*.diff must additionally use the
#      project address. That is the exact shape of the leak: a patch body is file
#      content, so a wrongly-authored commit exported by `git format-patch` walks
#      straight past every author-identity check.
#
# Address extraction is one `git grep` over the index and one hash per distinct
# address; a per-file loop over this tree takes minutes and was too slow for CI.
#
# Whole tree by default; --staged checks only pending added lines, which is how
# scripts/hooks/pre-commit calls it.

set -uo pipefail

ALLOWLIST_FILE="scripts/allowed-emails.sha256"
ALLOWED_PATCH_ADDRESS="noreply@anthropic.com"
EMAIL_RE='[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}'
IDENTITY_HEADER='^\+?(From|Author|Signed-off-by|Co-authored-by):.*<[^>]+@[^>]+>'

cd "$(git rev-parse --show-toplevel)" || exit 1

if command -v sha256sum >/dev/null 2>&1; then
    sha256() { printf '%s' "$1" | sha256sum | cut -d' ' -f1; }
else
    sha256() { printf '%s' "$1" | shasum -a 256 | cut -d' ' -f1; }
fi

status=0
fail() {
    echo "ERROR: $1" >&2
    status=1
}

staged_mode=0
[ "${1:-}" = "--staged" ] && staged_mode=1

if [ "$staged_mode" -eq 1 ]; then
    added=$(git diff --cached -U0 --diff-filter=ACMR | grep '^+' | grep -v '^+++')
    addresses=$(grep -oE "$EMAIL_RE" <<< "$added" | tr 'A-Z' 'a-z' | sort -u)
    patch_lines=$(git diff --cached -U0 --diff-filter=ACMR -- '*.patch' '*.diff' \
                  | grep '^+' | grep -v '^+++')
else
    addresses=$(git grep -hoIE "$EMAIL_RE" -- . 2>/dev/null | tr 'A-Z' 'a-z' | sort -u)
fi

if [ ! -f "$ALLOWLIST_FILE" ]; then
    echo "ERROR: $ALLOWLIST_FILE is missing; cannot verify addresses." >&2
    exit 1
fi
allowed=$(grep -oE '^[0-9a-f]{64}' "$ALLOWLIST_FILE")

while IFS= read -r addr; do
    [ -z "$addr" ] && continue
    hash=$(sha256 "$addr")
    if ! grep -qxF "$hash" <<< "$allowed"; then
        fail "an address that is not on the allowlist appears in:"
        # Filenames and the hash only, never the address. CI logs for this repo
        # are public, so echoing the matched line would publish the address on
        # the very run that is complaining about it.
        if [ "$staged_mode" -eq 1 ]; then
            git diff --cached --name-only -G"$(sed 's/[.[\*^$]/\\&/g' <<< "$addr")" \
                | head -20 | sed 's/^/    /' >&2
        else
            git grep -lIF "$addr" -- . 2>/dev/null | head -20 | sed 's/^/    /' >&2
        fi
        echo "    (address withheld; sha256 $hash)" >&2
        echo "    If it belongs here, add that hash to $ALLOWLIST_FILE." >&2
    fi
done <<< "$addresses"

if [ "$staged_mode" -eq 1 ]; then
    bad=$(grep -inE "$IDENTITY_HEADER" <<< "$patch_lines" | grep -vF "$ALLOWED_PATCH_ADDRESS")
else
    bad=$(git grep -inIE "$IDENTITY_HEADER" -- '*.patch' '*.diff' 2>/dev/null \
          | grep -vF "$ALLOWED_PATCH_ADDRESS")
fi

if [ -n "$bad" ]; then
    fail "a vendored patch has an identity header that is not <$ALLOWED_PATCH_ADDRESS>:"
    # Same reason as above — show which header is wrong, not the address in it.
    head -20 <<< "$bad" | sed -E 's/<[^>]*@[^>]*>/<redacted>/g; s/^/    /' >&2
    echo "    Regenerate with: git -c user.email=$ALLOWED_PATCH_ADDRESS format-patch ..." >&2
fi

if [ "$status" -ne 0 ]; then
    echo >&2
    echo "Personal data must not be committed. Fix the file above and re-stage." >&2
fi

exit "$status"
