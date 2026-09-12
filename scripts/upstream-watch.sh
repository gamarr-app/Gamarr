#!/bin/bash
# Upstream watch: cheap check for Radarr/Sonarr commits worth porting into Gamarr.
# Like sentry-watch.sh it runs no LLM of its own — it detects and reports, and the
# agent in the Discord channel does the judging, in the open, where it can be
# argued with.
#
# Gamarr's history was rewritten, so there is no merge-base with either upstream
# and `git log HEAD..radarr/develop` is meaningless. State is therefore a
# watermark: the last upstream SHA we have already reported, per remote.
#
# Run via cron, weekly:  0 9 * * 1 /path/to/repo/scripts/upstream-watch.sh
# Manual backfill:       scripts/upstream-watch.sh --since 2026-07-16 --dry-run

set -uo pipefail

# cron runs with PATH=/usr/bin:/bin. Same trap as sentry-watch.sh.
export DOTNET_ROOT="${DOTNET_ROOT:-$HOME/.dotnet}"
export PATH="$HOME/.local/bin:$DOTNET_ROOT:$PATH"

REPO_DIR="$(cd "$(dirname "$0")/.." && pwd)"
LOG_FILE="${REPO_DIR}/.upstream-watch.log"
STATE_FILE="${UPSTREAM_SEEN_FILE:-$HOME/.local/state/gamarr-upstream-seen.json}"

WAKE="${WAKE_SCRIPT:-$HOME/github/household/tools/wake.sh}"
CHANNEL="${UPSTREAM_WAKE_CHANNEL:-1533572556018290958}" # #gamarr

REMOTES="${UPSTREAM_REMOTES:-radarr sonarr}"
BRANCH="${UPSTREAM_BRANCH:-develop}"

SINCE=""
DRY_RUN=0
while [ $# -gt 0 ]; do
    case "$1" in
        --since)   SINCE="$2"; shift 2 ;;
        --dry-run) DRY_RUN=1; shift ;;
        *) echo "unknown arg: $1" >&2; exit 2 ;;
    esac
done

log() { echo "$(date): $1" >> "$LOG_FILE"; }

notify() {
    if [ -x "$WAKE" ]; then
        "$WAKE" -c "$CHANNEL" "$1" >> "$LOG_FILE" 2>&1 || log "WARNING: wake.sh failed"
    else
        log "WARNING: no wake script at $WAKE, cannot notify"
    fi
}

# Same reasoning as sentry-watch.sh: a broken watcher should say so once, then go
# quiet, rather than shout on every run. At weekly cadence one week is the right
# quiet window.
FAIL_QUIET_HOURS="${UPSTREAM_FAIL_QUIET_HOURS:-168}"

notify_failure() {
    local key="$1" msg="$2"
    local stamp
    stamp="$(dirname "$STATE_FILE")/gamarr-upstream-fail-${key}.stamp"
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

cd "$REPO_DIR" || { log "ERROR: Cannot cd to $REPO_DIR"; exit 1; }

log "=== Starting upstream check ==="

# --filter=blob:none keeps this to commit and tree objects. Both remotes are
# already partial clones; the apply-check below is what pulls the few blobs it
# actually needs.
for r in $REMOTES; do
    if ! git fetch --quiet --filter=blob:none "$r" "$BRANCH" 2>>"$LOG_FILE"; then
        log "ERROR: fetch failed for $r"
        notify_failure "fetch-$r" "Upstream watch could not fetch $r/$BRANCH. Tail $LOG_FILE on gamarr."
        exit 1
    fi
done

rm -f "$(dirname "$STATE_FILE")"/gamarr-upstream-fail-*.stamp
mkdir -p "$(dirname "$STATE_FILE")"

# The apply-check below tests patches against the working tree, so a dirty tree
# can flip a commit between the "clean" and "needs a port" bucket. Read-only
# either way — just worth knowing when reading the log afterwards.
if [ -n "$(git status --porcelain 2>/dev/null)" ]; then
    log "NOTE: working tree is dirty; apply-check results may be off."
fi

SUMMARY=$(SINCE="$SINCE" DRY_RUN="$DRY_RUN" REMOTES="$REMOTES" BRANCH="$BRANCH" \
    python3 - "$STATE_FILE" <<'PY' 2>>"$LOG_FILE"
import json, os, re, subprocess, sys

state_file = sys.argv[1]
since   = os.environ.get('SINCE', '')
dry_run = os.environ.get('DRY_RUN') == '1'
remotes = os.environ['REMOTES'].split()
branch  = os.environ['BRANCH']


def git(*args):
    return subprocess.run(['git', *args], capture_output=True, text=True).stdout


state = {}
if os.path.exists(state_file):
    state = json.load(open(state_file))

# Paths as they exist in Gamarr right now. A commit touching a path we do not
# have is one we renamed (Movie -> Game) or dropped, so it needs a human port
# rather than a patch, and is reported separately at most as a one-line mention.
gamarr_paths = set(git('ls-tree', '-r', '--name-only', 'HEAD').split('\n'))

# Noise we never want to hear about. Weblate churns dozens of files a week,
# upstream's own version bumps are meaningless here, and Gamarr has Dependabot
# for its own dependency tree.
SKIP_SUBJECT = re.compile(
    r'^(Multiple )?Translations? updated'
    r'|^Bump (to |[\w.@/-]+ to )'
    r'|^Bump .* dependencies'
    r'|^Update major version'
    r'|^New translations '
    r'|^Automated .*(translation|weblate)',
    re.I)


def interesting(remote, sha, subject, author, files):
    if SKIP_SUBJECT.search(subject) or 'weblate' in author.lower():
        return None
    files = [f for f in files if f]
    if not files:
        return None
    present = [f for f in files if f in gamarr_paths]
    if not present:
        return None
    return {
        'remote': remote, 'sha': sha, 'subject': subject,
        'n': len(files), 'hit': len(present), 'clean': None,
    }


def applies_cleanly(remote, sha):
    """Does upstream's patch still apply to our tree as-is? This is the single
    strongest cheap signal: a clean apply means we never diverged on those lines,
    so taking the fix is a cherry-pick rather than a rewrite."""
    patch = subprocess.run(
        ['git', 'format-patch', '-1', '--stdout', sha],
        capture_output=True, text=True)
    if patch.returncode != 0 or not patch.stdout:
        return None
    check = subprocess.run(
        ['git', 'apply', '--check', '-'],
        input=patch.stdout, capture_output=True, text=True)
    return check.returncode == 0


found = []
new_state = dict(state)

for remote in remotes:
    ref = f'{remote}/{branch}'
    tip = git('rev-parse', ref).strip()
    if not tip:
        continue

    if since:
        rng = ['--since', since, ref]
    elif state.get(remote):
        rng = [f'{state[remote]}..{ref}']
    else:
        # First ever run with no watermark: look back a fortnight rather than
        # dumping the entire upstream history into Discord.
        rng = ['--since', '2 weeks ago', ref]

    # --name-only puts a blank line between the header and the file list, so the
    # blank cannot be the record separator — flush on the next header instead.
    raw = git('log', '--format=%H%x01%s%x01%an', '--name-only', *rng)
    cur = None

    def flush(c):
        if c is None:
            return
        hit = interesting(*c)
        if hit:
            found.append(hit)

    for line in raw.split('\n'):
        if '\x01' in line:
            flush(cur)
            sha, subject, author = line.split('\x01')
            cur = (remote, sha, subject, author, [])
        elif cur is not None and line.strip():
            cur[4].append(line.strip())
    flush(cur)

    new_state[remote] = tip

# Only the survivors pay for blob fetches.
for c in found:
    c['clean'] = applies_cleanly(c['remote'], c['sha'])

if not found:
    sys.exit(1)

# Radarr and Sonarr land the same infrastructure fix under the same subject within
# days of each other. Collapsing those is both less noise and a signal in itself:
# a change both upstreams made is shared plumbing, not domain-specific feature work.
merged = {}
for c in found:
    key = re.sub(r'\W+', ' ', c['subject']).strip().lower()
    if key in merged:
        m = merged[key]
        m['remote'] = f"{m['remote']}+{c['remote']}"
        # Prefer whichever upstream we can take the patch from directly.
        if c['clean'] and not m['clean']:
            m['sha'], m['clean'], m['hit'], m['n'] = c['sha'], True, c['hit'], c['n']
    else:
        merged[key] = dict(c)
found = list(merged.values())

for c in found:
    c['ratio'] = c['hit'] / c['n'] if c['n'] else 0

clean = [c for c in found if c['clean']]
# A commit where most of the touched files do not exist here is an upstream domain
# feature wearing a few shared files — worth a count, not eight lines of Discord.
dirty = sorted((c for c in found if not c['clean']),
               key=lambda c: -c['ratio'])
near   = [c for c in dirty if c['ratio'] >= 0.5]
far    = [c for c in dirty if c['ratio'] < 0.5]

lines = [f"Upstream: {len(clean) + len(near)} commit(s) in Radarr/Sonarr worth a look."]
if clean:
    lines.append(f"  Applies cleanly to gamarr as-is ({len(clean)}):")
    for c in clean:
        lines.append(f"    - {c['remote']} {c['sha'][:8]}  {c['subject'][:80]}")
if near:
    lines.append(f"  Mostly our files, needs a port ({len(near)}):")
    for c in near:
        lines.append(
            f"    - {c['remote']} {c['sha'][:8]}  {c['subject'][:80]}"
            f"  [{c['hit']}/{c['n']} paths]")
if far:
    lines.append(
        f"  Plus {len(far)} commit(s) that are mostly upstream-only paths "
        f"(likely movie/series features) — see {os.path.basename(state_file)} range "
        f"if you want them.")
lines.append(
    "Review each: decide whether it is relevant to a game PVR at all, cherry-pick "
    "or port the ones that are, and say which you skipped and why. "
    "`git show <sha>` has the diff.")

if not dry_run:
    with open(state_file, 'w') as f:
        json.dump(new_state, f, indent=2)

print("\n".join(lines))
PY
) || { log "Nothing new upstream. Done."; exit 0; }

log "Found upstream candidate(s):"
echo "$SUMMARY" >> "$LOG_FILE"

if [ "$DRY_RUN" = "1" ]; then
    echo "$SUMMARY"
    log "=== Dry run, not notifying ==="
    exit 0
fi

notify "$SUMMARY"
log "=== Done ==="
