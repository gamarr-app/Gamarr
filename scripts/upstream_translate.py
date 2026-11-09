#!/usr/bin/env python3
"""Translate a Radarr/Sonarr commit into Gamarr's vocabulary, then test or apply it.

Most upstream commits used to land in "needs a port" because Gamarr renamed things
when it forked: Movie->Game, Radarr->Gamarr (namespaces Radarr.Api.V3 ->
Gamarr.Api.V3, paths frontend/src/Movie -> frontend/src/Game, translation keys
MovieFiles -> GameFiles). A plain `git apply` treats each rename as a conflict. This
module rewrites the patch first (paths and content lines alike), and then checks it
file by file against a throwaway index, so the working tree is never touched.

It is used by scripts/upstream-watch.sh (bucketing) and scripts/upstream-port.sh
(applying). Keep it runnable on macOS's /usr/bin/python3 (3.9): cron has a bare PATH.

    upstream_translate.py patch  <remote> <sha>            translated patch on stdout
    upstream_translate.py check  [--base REF] [--json] <remote> <sha>...
    upstream_translate.py apply  <remote> <sha>            apply to the work tree

Remote is the upstream's git remote name; the token map is picked by whether it
contains "sonarr" (else Radarr's map is used).
"""
import json
import os
import re
import subprocess
import sys
import tempfile

HERE = os.path.dirname(os.path.abspath(__file__))
MAP_FILE = os.path.join(HERE, 'upstream-rename-map.json')


# --------------------------------------------------------------------------- maps

def load_map(remote):
    with open(MAP_FILE) as f:
        m = json.load(f)
    key = 'sonarr' if 'sonarr' in remote.lower() else 'radarr'
    tokens = [tuple(t) for t in m['common'] + m[key]]
    paths = [tuple(p) for p in m.get('paths_' + key, []) + m.get('paths_common', [])]
    # Longest token first so "Movies" wins over "Movie".
    tokens.sort(key=lambda t: -len(t[0]))
    pattern = re.compile('|'.join(re.escape(a) for a, _ in tokens)) if tokens else None
    table = dict(tokens)
    return {'pattern': pattern, 'table': table, 'paths': paths}


def tr(text, m):
    if m['pattern'] is None:
        return text
    return m['pattern'].sub(lambda x: m['table'][x.group(0)], text)


# --------------------------------------------------------------------------- git

def git(*args, **kw):
    inp = kw.pop('input', None)
    env = kw.pop('env', None)
    r = subprocess.run(['git'] + list(args), input=inp, capture_output=True,
                       env=env, text=kw.pop('text', True))
    return r


class Tree(object):
    """Path set and throwaway-index helpers for one base commit."""

    def __init__(self, base):
        self.base = git('rev-parse', base).stdout.strip()
        self.paths = set(p for p in git('ls-tree', '-r', '--name-only', self.base)
                         .stdout.split('\n') if p)
        self.stems = {}
        self.names = {}
        for p in self.paths:
            self.stems.setdefault(os.path.splitext(p)[0], p)
            self.names.setdefault(os.path.basename(p), []).append(p)
        self._idx = None

    def _index(self):
        if self._idx is None:
            fd, self._idx = tempfile.mkstemp(prefix='gamarr-port-idx-')
            os.close(fd)
            env = dict(os.environ, GIT_INDEX_FILE=self._idx)
            git('read-tree', self.base, env=env)
            with open(self._idx, 'rb') as f:
                self._pristine = f.read()
        else:
            with open(self._idx, 'wb') as f:
                f.write(self._pristine)
        return dict(os.environ, GIT_INDEX_FILE=self._idx)

    def applies(self, patch, three_way=False, reverse=False):
        args = ['apply', '--cached']
        if three_way:
            args.append('--3way')
        else:
            args.append('--check')
        if reverse:
            args.append('-R')
        r = git(*args, input=patch, env=self._index())
        return r.returncode == 0

    def cleanup(self):
        if self._idx and os.path.exists(self._idx):
            os.unlink(self._idx)


# --------------------------------------------------------------------------- patch

def commit_patch(sha):
    r = git('format-patch', '-1', '--stdout', '--full-index', '--no-signature', sha)
    if r.returncode != 0:
        return None
    return r.stdout


def split_patch(text):
    """Return (preamble, [file sections]). Sections start at 'diff --git'."""
    parts = re.split(r'(?m)^(?=diff --git )', text)
    pre = parts[0]
    files = parts[1:]
    # format-patch appends a trailing "-- \n2.x" signature unless --no-signature;
    # strip anything after the last hunk that is not diff content.
    return pre, files


HDR = re.compile(r'^diff --git a/(.*?) b/(.*)$')


def section_paths(sec):
    first = sec.split('\n', 1)[0]
    m = HDR.match(first)
    old, new = (m.group(1), m.group(2)) if m else (None, None)
    if re.search(r'(?m)^new file mode', sec):
        old = None
    if re.search(r'(?m)^deleted file mode', sec):
        new = None
    return old, new


def resolve(path, tree, m):
    """Map an upstream path to Gamarr. Returns (path, how) where how is one of
    'same', 'renamed', 'mapped', 'rewritten' (same stem, other extension, e.g. a
    .js file Gamarr converted to .tsx), 'moved' (unique basename elsewhere),
    'missing'."""
    if path in tree.paths:
        return path, 'same'
    for a, b in m['paths']:
        if path.startswith(a):
            q = b + path[len(a):]
            q2 = tr(q, m)
            for cand in (q, q2):
                if cand in tree.paths:
                    return cand, 'mapped'
    t = tr(path, m)
    if t in tree.paths:
        return t, 'renamed'
    for cand in (os.path.splitext(path)[0], os.path.splitext(t)[0]):
        if cand in tree.stems:
            return tree.stems[cand], 'rewritten'
    hits = tree.names.get(os.path.basename(t)) or tree.names.get(os.path.basename(path))
    if hits and len(hits) == 1:
        return hits[0], 'moved'
    return t, 'missing'


def rewrite_section(sec, old_to, new_to, translate_body, m, three_way_blobs=False):
    """Rewrite one file section's paths (and optionally its hunk bodies)."""
    out = []
    in_hunks = False
    old_from, new_from = section_paths(sec)
    for line in sec.split('\n'):
        if not in_hunks:
            if line.startswith('diff --git '):
                a = old_to if old_to else new_to
                b = new_to if new_to else old_to
                line = 'diff --git a/%s b/%s' % (a, b)
            elif line.startswith('--- a/') and old_to:
                line = '--- a/' + old_to
            elif line.startswith('+++ b/') and new_to:
                line = '+++ b/' + new_to
            elif line.startswith('rename from ') and old_to:
                line = 'rename from ' + old_to
            elif line.startswith('rename to ') and new_to:
                line = 'rename to ' + new_to
            elif line.startswith('index ') and translate_body and three_way_blobs:
                line = _translate_index_line(line, m)
            elif line.startswith('@@'):
                in_hunks = True
        if in_hunks:
            if line.startswith('@@'):
                if translate_body:
                    # Keep the range header, translate only the trailing context.
                    hm = re.match(r'^(@@ [^@]* @@)(.*)$', line)
                    if hm:
                        line = hm.group(1) + tr(hm.group(2), m)
            elif translate_body and line[:1] in (' ', '+', '-'):
                line = line[0] + tr(line[1:], m)
        out.append(line)
    return '\n'.join(out)


def _translate_index_line(line, m):
    """--3way finds the merge base through the preimage blob id on the index line.
    After translating the hunk, that blob no longer matches, so store a translated
    copy of the upstream preimage and point at it instead."""
    im = re.match(r'^index ([0-9a-f]+)\.\.([0-9a-f]+)(.*)$', line)
    if not im or set(im.group(1)) == {'0'}:
        return line
    blob = git('cat-file', 'blob', im.group(1), text=False)
    if blob.returncode != 0:
        return line
    try:
        txt = blob.stdout.decode('utf-8')
    except UnicodeDecodeError:
        return line
    h = git('hash-object', '-w', '--stdin', input=tr(txt, m).encode('utf-8'), text=False)
    if h.returncode != 0:
        return line
    return 'index %s..%s%s' % (h.stdout.decode().strip(), im.group(2), im.group(3))


# --------------------------------------------------------------------------- en.json

LOCALE_FILE = 'src/NzbDrone.Core/Localization/Core/en.json'
KV = re.compile(r'^\s*"((?:[^"\\]|\\.)*)"\s*:\s*(.*?),?\s*$')


def merge_locale(sec, tree, m):
    """en.json is where hand ports went to die: nearly every UI commit adds a key,
    and the context lines around it are upstream keys Gamarr renamed or never had,
    so the hunk never applies. Merge by key instead: removed keys are dropped,
    added keys are inserted in sorted position (translated like code), changed keys
    are updated if Gamarr still has upstream's old value. Returns (section, ok)."""
    import collections
    olds, news = collections.OrderedDict(), collections.OrderedDict()
    for line in sec.split('\n'):
        if line[:1] in ('+', '-') and not line.startswith(('+++', '---')):
            kv = KV.match(line[1:])
            if kv:
                (news if line[0] == '+' else olds)[kv.group(1)] = json.loads(kv.group(2))
    text = git('show', '%s:%s' % (tree.base, LOCALE_FILE)).stdout
    if not text:
        return None, False
    d = json.loads(text, object_pairs_hook=collections.OrderedDict)
    ok = True

    def key_for(k):
        return k if k in d else tr(k, m)

    for k, old in olds.items():
        if k in news:
            continue
        gk = key_for(k)
        if gk not in d:
            continue
        # Upstream drops keys its own code stopped using; Gamarr's code may still
        # use them (it kept health checks Radarr removed, say). Keep those.
        used = git('grep', '-q', '-F', '-w', gk, tree.base, '--', 'src', 'frontend/src',
                   ':(exclude)src/NzbDrone.Core/Localization')
        if used.returncode == 0:
            continue
        d.pop(gk, None)
    for k, new in news.items():
        gk = key_for(k)
        if gk in d:
            old = olds.get(k)
            if d[gk] in (new, tr(new, m)):
                continue
            if old is not None and d[gk] in (old, tr(old, m)):
                d[gk] = tr(new, m)
            elif old is not None:
                ok = False
            continue
        d[gk] = tr(new, m)
    keys = list(d.keys())
    # Re-sort only the keys we inserted; the existing order is left alone.
    had = set(json.loads(text))
    inserted = [k for k in keys if k not in had]
    base_keys = [k for k in keys if k not in inserted]
    for k in inserted:
        pos = next((i for i, b in enumerate(base_keys) if b > k), len(base_keys))
        base_keys.insert(pos, k)
    out = json.dumps(collections.OrderedDict((k, d[k]) for k in base_keys),
                     indent=2, ensure_ascii=False) + '\n'
    if out == text:
        return None, ok
    import difflib
    diff = difflib.unified_diff(text.splitlines(True), out.splitlines(True),
                                'a/' + LOCALE_FILE, 'b/' + LOCALE_FILE, n=3)
    body = ''.join(diff)
    return 'diff --git a/%s b/%s\n%s' % (LOCALE_FILE, LOCALE_FILE, body), ok


# --------------------------------------------------------------------------- analysis

def analyse(remote, sha, tree, m, three_way=True):
    """Per-file verdicts plus the best translated patch.

    File verdict: 'raw' (applies untouched), 'paths' (applies once paths are
    mapped), 'tokens' (applies once identifiers are translated too), '3way'
    (translated patch merges cleanly), 'json' (en.json merged by key), 'present' (already in the tree), 'missing'
    / 'rewritten' (Gamarr has no such file / rewrote it), 'conflict' (drift).
    """
    text = commit_patch(sha)
    if text is None:
        return None
    pre, secs = split_patch(text)
    files = []
    if len(secs) > MAX_FILES:
        # Mass edits (an upstream-wide reformat, say) would cost thousands of git
        # calls file by file. One whole-patch check is enough to bucket them.
        whole = ''.join(rewrite_section(s, *([resolve(p, tree, m)[0] if p else None
                                              for p in section_paths(s)] + [True, m]))
                        for s in secs)
        v = 'tokens' if tree.applies(pre + whole) else 'conflict'
        return {'remote': remote, 'sha': sha, 'preamble': pre, 'too_big': True,
                'files': [{'path': '(%d files)' % len(secs), 'to': None, 'how': 'bulk',
                           'verdict': v, 'patch': pre + whole}]}
    for sec in secs:
        old, new = section_paths(sec)
        upath = old or new
        if old:
            old_to, how = resolve(old, tree, m)
        else:
            old_to, how = None, 'new'
        if new:
            new_to = resolve(new, tree, m)[0] if new != old else old_to
            if how == 'new':
                new_to = tr(new, m) if tr(new, m) != new else new
        else:
            new_to = None
        rec = {'path': upath, 'to': old_to or new_to, 'how': how}
        if how in ('missing',):
            rec['verdict'] = 'missing'
            rec['patch'] = None
            files.append(rec)
            continue
        candidates = []
        if how == 'same':
            candidates.append(('raw', rewrite_section(sec, old_to, new_to, False, m)))
        elif how == 'new':
            candidates.append(('raw', sec))
        else:
            candidates.append(('paths', rewrite_section(sec, old_to, new_to, False, m)))
        candidates.append(('tokens', rewrite_section(sec, old_to, new_to, True, m)))
        verdict, chosen = None, None
        for name, p in candidates:
            if tree.applies(p):
                verdict, chosen = name, p
                break
        if verdict is None and rec['to'] == LOCALE_FILE:
            merged, ok = merge_locale(sec, tree, m)
            if merged is None and ok:
                verdict = 'present'
            elif merged is not None and ok and tree.applies(merged):
                verdict, chosen = 'json', merged
        if verdict is None:
            tp = candidates[-1][1]
            if tree.applies(tp, reverse=True) or tree.applies(candidates[0][1], reverse=True):
                verdict = 'present'
            elif three_way and how != 'new':
                p3 = rewrite_section(sec, old_to, new_to, True, m, three_way_blobs=True)
                if tree.applies(p3, three_way=True):
                    verdict, chosen = '3way', p3
                else:
                    raw3 = candidates[0][1]
                    if tree.applies(raw3, three_way=True):
                        verdict, chosen = '3way', raw3
            if verdict is None:
                verdict = 'rewritten' if how == 'rewritten' else 'conflict'
                chosen = tp
        rec['verdict'] = verdict
        rec['patch'] = chosen
        files.append(rec)
    return {'remote': remote, 'sha': sha, 'preamble': pre, 'files': files}


MAX_FILES = 80

ORDER = ['raw', 'paths', 'tokens', 'json', '3way']


def summarise(res):
    """Commit-level verdict: the worst file verdict, ignoring files already present.
    'partial' means every file Gamarr has applies, but some upstream files don't
    exist here (usually Movie-only features, migrations or upstream CI)."""
    fs = [f for f in res['files'] if f['verdict'] != 'present']
    if not fs:
        return 'present'
    have = [f for f in fs if f['verdict'] not in ('missing',)]
    bad = [f for f in have if f['verdict'] in ('conflict', 'rewritten')]
    if bad:
        return 'conflict'
    if not have:
        return 'missing'
    worst = max(ORDER.index(f['verdict']) for f in have)
    v = ORDER[worst]
    if len(have) < len(fs):
        return 'partial-' + v
    return v


def build_patch(res, include=('raw', 'paths', 'tokens', 'json', '3way', 'conflict', 'rewritten')):
    secs = [f['patch'] for f in res['files'] if f['patch'] and f['verdict'] in include]
    body = ''.join(s if s.endswith('\n') else s + '\n' for s in secs)
    return res['preamble'] + body


# --------------------------------------------------------------------------- cli

def resolve_sha(remote, sha):
    r = git('rev-parse', '--verify', '--quiet', sha + '^{commit}')
    return r.stdout.strip() or None


def cmd_check(args):
    base = 'HEAD'
    as_json = False
    three_way = True
    while args and args[0].startswith('--'):
        a = args.pop(0)
        if a == '--base':
            base = args.pop(0)
        elif a == '--json':
            as_json = True
        elif a == '--no-3way':
            three_way = False
    remote = args.pop(0)
    tree = Tree(base)
    m = load_map(remote)
    out = []
    try:
        for s in args:
            full = resolve_sha(remote, s)
            res = analyse(remote, full, tree, m, three_way) if full else None
            if res is None:
                out.append({'sha': s, 'verdict': 'unknown', 'files': []})
                continue
            out.append({
                'sha': full, 'verdict': summarise(res),
                'files': [{k: f[k] for k in ('path', 'to', 'how', 'verdict')}
                          for f in res['files']]})
    finally:
        tree.cleanup()
    if as_json:
        json.dump(out, sys.stdout, indent=1)
        print()
    else:
        for o in out:
            print('%s %s' % (o['sha'][:10], o['verdict']))
            for f in o['files']:
                arrow = '' if f['to'] == f['path'] else ' -> ' + str(f['to'])
                print('    %-9s %s%s' % (f['verdict'], f['path'], arrow))
    return 0


def cmd_patch(args):
    remote, sha = args[0], args[1]
    tree = Tree('HEAD')
    try:
        res = analyse(remote, resolve_sha(remote, sha), tree, load_map(remote), False)
    finally:
        tree.cleanup()
    sys.stdout.write(build_patch(res))
    return 0


def cmd_apply(args):
    """Apply what applies to the work tree and index; report the rest. Files that
    conflict are applied with --3way so the conflict markers land in the file."""
    remote, sha = args[0], args[1]
    full = resolve_sha(remote, sha)
    if not full:
        print('unknown commit %s (git fetch %s?)' % (sha, remote), file=sys.stderr)
        return 2
    m = load_map(remote)
    tree = Tree('HEAD')
    try:
        res = analyse(remote, full, tree, m, True)
    finally:
        tree.cleanup()
    rc = 0
    for f in res['files']:
        v = f['verdict']
        if v in ('missing', 'present'):
            print('  skip     %-9s %s' % (v, f['path']))
            continue
        patch = res['preamble'] + f['patch'] + ('' if f['patch'].endswith('\n') else '\n')
        r = git('apply', '--index', '--3way', input=patch) if v in ('3way', 'conflict', 'rewritten') \
            else git('apply', '--index', input=patch)
        ok = r.returncode == 0
        if not ok:
            rc = 1
        label = v if ok else v + '!'
        print('  %-8s %s%s' % (label, f['to'],
                               '' if f['to'] == f['path'] else '   (upstream: %s)' % f['path']))
        if not ok and r.stderr.strip():
            for ln in r.stderr.strip().split('\n')[:4]:
                print('           ' + ln)
    print(summarise(res))
    return rc


def main(argv):
    if len(argv) < 2 or argv[1] in ('-h', '--help'):
        print(__doc__)
        return 0
    cmd, args = argv[1], argv[2:]
    if cmd == 'check':
        return cmd_check(args)
    if cmd == 'patch':
        return cmd_patch(args)
    if cmd == 'apply':
        return cmd_apply(args)
    print('unknown command %s' % cmd, file=sys.stderr)
    return 2


if __name__ == '__main__':
    sys.exit(main(sys.argv))
