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
# The denylist is SHA-256 of the lowercased address, never the address itself.
# Writing it in clear here would re-commit the very string this is meant to keep
# out — the first version of this script did exactly that and tripped its own check.
# Add an entry with:
#   printf '%s' 'someone@example.com' | shasum -a 256
#
# Two rules, deliberately narrow so this stays quiet:
#   1. No address whose hash is on the denylist, anywhere.
#   2. Identity headers inside vendored *.patch/*.diff must use the project
#      address. That is the exact shape of the leak, and scoping it to patches
#      avoids tripping over the real contributor addresses that legitimately live
#      in test fixtures (ExtraTorrents.xml) and in
#      Datastore/Migration/Framework/TableDefinition.cs.
#
# Address extraction is one `git grep` over the index and one hash per distinct
# address; a per-file loop over this tree takes minutes and was too slow for CI.
#
# Whole tree by default; --staged checks only pending added lines, which is how
# scripts/hooks/pre-commit calls it.

set -uo pipefail

DENY_SHA256=(
    "2abaad518817b90fc477134079b9ce0c6292e0993d7a0284a09ffc1cef0b1df6"
)
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

while IFS= read -r addr; do
    [ -z "$addr" ] && continue
    hash=$(sha256 "$addr")
    for deny in "${DENY_SHA256[@]}"; do
        if [ "$hash" = "$deny" ]; then
            fail "a denylisted personal address appears in:"
            # File and line number only. CI logs for this repo are public, so
            # echoing the matched line back would publish the address a second
            # time on the very run that is complaining about it.
            git grep -lIF "$addr" -- . 2>/dev/null | head -20 | sed 's/^/    /' >&2
            echo "    (address withheld; sha256 $deny)" >&2
        fi
    done
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
