#!/usr/bin/env bash
# Prune git worktrees and local branches whose work is already on origin/main.
#
# A worktree is removed only if ALL hold:
#   - it is clean (no uncommitted or untracked files)
#   - every commit it has beyond origin/main has a subject that exists on
#     origin/main (main was history-rewritten, so SHAs cannot be compared)
#   - nothing in it was modified in the last day
# Local branches are deleted under the same merged test (ancestor of
# origin/main, or every commit subject present on origin/main), but never the
# current branch, main, or a branch checked out in a kept worktree.
# Remote branches are never touched.
#
# Usage: scripts/dev/prune-worktrees.sh [--dry-run]
set -u

dry=0
case "${1:-}" in
  --dry-run) dry=1 ;;
  "") ;;
  *) echo "usage: $0 [--dry-run]" >&2; exit 2 ;;
esac

repo=$(git rev-parse --show-toplevel) || exit 1
cd "$repo" || exit 1
main_wt=$(git worktree list --porcelain | awk '/^worktree /{print $2; exit}')
git fetch -q origin || echo "warning: fetch failed, using existing origin/main" >&2

subjects=$(mktemp)
trap 'rm -f "$subjects"' EXIT
git log origin/main --format=%s > "$subjects"

# True if ref $1 (run in dir $2) has no commit beyond origin/main with an unknown subject.
merged() {
  git -C "$2" merge-base --is-ancestor "$1" origin/main 2>/dev/null && return 0
  [ "$(git -C "$2" log "origin/main..$1" --format=%s | grep -cvxFf "$subjects")" -eq 0 ]
}

act() {
  if [ "$dry" -eq 1 ]; then echo "[dry-run] would: $*"; else echo "$*"; "$@"; fi
}

kept_branches=" main "
cur=$(git -C "$main_wt" symbolic-ref -q --short HEAD || true)
[ -n "$cur" ] && kept_branches="$kept_branches$cur "

path=""
while read -r key val; do
  case "$key" in
    worktree) path=$val ;;
    "")
      if [ -n "$path" ] && [ "$path" != "$main_wt" ]; then
        br=$(git -C "$path" symbolic-ref -q --short HEAD || true)
        reason=""
        if [ ! -d "$path" ]; then reason="missing (will git worktree prune)"
        elif [[ "$path" == */gamarr-upstream-watch ]]; then reason="in use by the weekly upstream-watch cron"
        elif [ -n "$(git -C "$path" status --porcelain 2>/dev/null)" ]; then reason="dirty"
        elif ! merged HEAD "$path"; then reason="has commits not on origin/main"
        elif [ -n "$(find "$path" -path '*/node_modules' -prune -o -path "$path/.git" -prune -o -mtime -1 -print -quit 2>/dev/null)" ]; then reason="modified within 1 day"
        fi
        if [ -z "$reason" ]; then act git worktree remove "$path"
        elif [ "$reason" = "${reason#missing}" ]; then
          echo "keep worktree $path: $reason"
          [ -n "$br" ] && kept_branches="$kept_branches$br "
        fi
      fi
      path="" ;;
  esac
done < <(git worktree list --porcelain; echo)

[ "$dry" -eq 1 ] || git worktree prune

for b in $(git for-each-ref refs/heads --format='%(refname:short)'); do
  case "$kept_branches" in *" $b "*) continue ;; esac
  # In dry-run, worktrees were not removed, so skip branches still checked out.
  if git worktree list --porcelain | grep -qx "branch refs/heads/$b"; then
    [ "$dry" -eq 1 ] || echo "keep branch $b: checked out"
    continue
  fi
  if merged "$b" "$repo"; then act git branch -D "$b"; else echo "keep branch $b: has commits not on origin/main"; fi
done
