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

# Patches are checked against origin/main's committed tree through a throwaway
# index, not the working tree, so whatever branch or dirt the checkout carries
# does not move commits between buckets. A failed origin fetch is not fatal: the
# last-fetched origin/main is still a fine base.
git fetch --quiet origin main 2>>"$LOG_FILE" || log "NOTE: origin fetch failed; using last-fetched origin/main."
BASE="${UPSTREAM_BASE:-origin/main}"
git rev-parse --verify --quiet "$BASE" >/dev/null || BASE=HEAD

rm -f "$(dirname "$STATE_FILE")"/gamarr-upstream-fail-*.stamp
mkdir -p "$(dirname "$STATE_FILE")"

SUMMARY=$(SINCE="$SINCE" DRY_RUN="$DRY_RUN" REMOTES="$REMOTES" BRANCH="$BRANCH" \
    BASE="$BASE" SCRIPTS_DIR="$REPO_DIR/scripts" \
    python3 - "$STATE_FILE" <<'PY' 2>>"$LOG_FILE"
import json, os, re, subprocess, sys

state_file = sys.argv[1]
since   = os.environ.get('SINCE', '')
dry_run = os.environ.get('DRY_RUN') == '1'
remotes = os.environ['REMOTES'].split()
branch  = os.environ['BRANCH']
sys.dont_write_bytecode = True
sys.path.insert(0, os.environ['SCRIPTS_DIR'])
import upstream_translate as ut  # noqa: E402

tree = ut.Tree(os.environ['BASE'])
maps = {r: ut.load_map(r) for r in remotes}


def git(*args):
    return subprocess.run(['git', *args], capture_output=True, text=True).stdout


state = {}
if os.path.exists(state_file):
    state = json.load(open(state_file))

# A path "is ours" if it exists here as-is or after the Movie->Game / Series->Game
# translation in upstream-rename-map.json. Matching raw paths only used to drop
# every commit under frontend/src/Movie or Radarr.Api.V3 on the floor unseen.
def ours(remote, path):
    return ut.resolve(path, tree, maps[remote])[1] in ('same', 'mapped', 'renamed')

# Noise we never want to hear about. Weblate churns dozens of files a week,
# upstream's own version bumps are meaningless here, and Gamarr has Dependabot
# for its own dependency tree.
SKIP_SUBJECT = re.compile(
    r'^(Multiple )?Translations? updated'
    r'|^Bump (to |[\w.@/-]+ to )'
    r'|^Bump .* dependencies'
    r'|^Update major version'
    r'|^New translations '
    r'|^Automated .*(translation|weblate)'
    r'|^Automated API Docs update',
    re.I)


def interesting(remote, sha, subject, author, files):
    if SKIP_SUBJECT.search(subject) or 'weblate' in author.lower():
        return None
    files = [f for f in files if f]
    if not files:
        return None
    present = [f for f in files if ours(remote, f)]
    if not present:
        return None
    return {
        'remote': remote, 'sha': sha, 'subject': subject,
        'n': len(files), 'hit': len(present), 'clean': None,
    }


def verdict(remote, sha):
    """How does upstream's patch land here? 'raw' is a plain cherry-pick; 'paths',
    'tokens', 'json' and '3way' apply once upstream_translate.py has rewritten it
    (scripts/upstream-port.sh does that); 'present' is already in; 'partial-*'
    applies to every file we have and the rest are upstream-only; 'conflict' is a
    hand port. See upstream_translate.analyse for the per-file details."""
    res = ut.analyse(remote, sha, tree, maps[remote], three_way=True)
    return ut.summarise(res) if res else 'conflict'


# Sonarr changes often reach Radarr later under the same subject, and Gamarr's
# shared files descend from Radarr, so Radarr's version is usually the one that
# applies. Look further back than this run's range for a twin.
def norm_subject(s):
    s = re.sub(r'(?i)\b(sonarr|radarr)\b|\(#\d+\)', '', s)
    return re.sub(r'\W+', ' ', s).strip().lower()


radarr_subjects = {}
if 'radarr' in remotes:
    for ln in git('log', '--since', '6 months ago', '--format=%H%x01%s',
                  f'radarr/{branch}').split('\n'):
        if '\x01' in ln:
            h, s = ln.split('\x01', 1)
            radarr_subjects.setdefault(norm_subject(s), h)


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
    c['verdict'] = verdict(c['remote'], c['sha'])
    c['clean'] = c['verdict'] == 'raw'
    c['twin'] = (radarr_subjects.get(norm_subject(c['subject']))
                 if c['remote'] == 'sonarr' else None)
tree.cleanup()

RANK = {'raw': 0, 'paths': 1, 'tokens': 1, 'json': 1, '3way': 2,
        'present': 3, 'conflict': 5, 'missing': 6}


def rank(v):
    return RANK.get(v.replace('partial-', ''), 5) + (0.5 if v.startswith('partial-') else 0)

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
        # Prefer whichever upstream we can take the patch from most directly.
        if rank(c['verdict']) < rank(m['verdict']):
            for k in ('sha', 'clean', 'hit', 'n', 'verdict', 'twin'):
                m[k] = c[k]
            m['remote'] = f"{c['remote']}+{m['remote'].split('+')[0]}"
    else:
        merged[key] = dict(c)
found = list(merged.values())

for c in found:
    c['ratio'] = c['hit'] / c['n'] if c['n'] else 0

def line(c, extra=''):
    t = (f"  (radarr twin {c['twin'][:8]})"
         if c.get('twin') and 'radarr' not in c['remote'] else '')
    return f"    - {c['remote']} {c['sha'][:8]}  {c['subject'][:80]}{extra}{t}"


clean   = [c for c in found if c['verdict'] == 'raw']
xlated  = [c for c in found if c['verdict'] in ('paths', 'tokens', 'json', '3way')
           or (c['verdict'].startswith('partial-') and c['ratio'] >= 0.5)]
present = [c for c in found if c['verdict'] == 'present']
rest    = [c for c in found if c not in clean and c not in xlated and c not in present]
# A commit where most of the touched files do not exist here is an upstream domain
# feature wearing a few shared files — worth a count, not eight lines of Discord.
rest.sort(key=lambda c: -c['ratio'])
near = [c for c in rest if c['ratio'] >= 0.5]
far  = [c for c in rest if c['ratio'] < 0.5]

lines = [f"Upstream: {len(clean) + len(xlated) + len(near)} commit(s) in Radarr/Sonarr worth a look."]
if clean:
    lines.append(f"  Applies cleanly to gamarr as-is ({len(clean)}):")
    lines += [line(c) for c in clean]
if xlated:
    lines.append(f"  Applies after Movie->Game translation / en.json merge / 3-way "
                 f"({len(xlated)}) — `scripts/upstream-port.sh <remote> <sha>`:")
    lines += [line(c, f"  [{c['verdict']}]") for c in xlated]
if near:
    lines.append(f"  Mostly our files, needs a hand port ({len(near)}):")
    lines += [line(c, f"  [{c['hit']}/{c['n']} paths]") for c in near]
if present:
    lines.append(f"  Already in gamarr, nothing to do ({len(present)}): "
                 + ", ".join(f"{c['remote']} {c['sha'][:8]}" for c in present))
if far:
    lines.append(
        f"  Plus {len(far)} commit(s) that are mostly upstream-only paths "
        f"(likely movie/series features) — see {os.path.basename(state_file)} range "
        f"if you want them.")
lines.append(
    "Review each: decide whether it is relevant to a game PVR at all, cherry-pick "
    "or port the ones that are, and say which you skipped and why. "
    "`scripts/upstream-port.sh <remote> <sha>` applies what it can and leaves "
    "conflict markers for the rest; for a Sonarr commit with a radarr twin, port the "
    "twin — gamarr's shared files descend from Radarr.")

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
