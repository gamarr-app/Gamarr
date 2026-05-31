# Upstream sync

System for pulling fixes from upstream Radarr and Sonarr into Gamarr.

## How it works

For each new commit on `radarr/develop` and `sonarr/develop` since the
recorded cursor in `state.json`:

1. **Skip-patterns** — subject matched against regexes in
   `skip-patterns.txt` (version bumps, weblate translations, anime, etc.).
2. **Skip-paths** — if every file the commit touches matches a glob in
   `skip-paths.txt` (movie/TV/series/episode/TMDB/TVDB code with no
   game-domain analogue), skip.
3. **Semantic apply** — `catchup.py` hands the surviving commit to Claude.
   The model reads the upstream diff, decides whether it applies to
   Gamarr, finds the renamed Gamarr files (Movie→Game, Radarr→Gamarr,
   etc.), and writes the equivalent change. Pure cherry-pick is rarely
   right because of the rename, so this is the load-bearing step.
4. **Build gate** — `dotnet build src/Gamarr.sln` must pass; if it
   doesn't, the change is reverted and logged as `BUILD-FAILED`.
5. **Commit** — clean wins are committed locally with a footer pointing
   back at the upstream SHA. The cursor in `state.json` always advances
   past a processed commit (clean, skipped, or build-failed) so reruns
   don't re-pick the same commit.

## Running it

```bash
# Requires ANTHROPIC_API_KEY in env.
pip install anthropic   # one-time

# Dry run (filter only, no LLM calls, no commits)
./.github/upstream-sync/catchup.py both --dry-run --max 100

# For real, capped at 25 commits per upstream per run
./.github/upstream-sync/catchup.py both --max 25

# Just one upstream
./.github/upstream-sync/catchup.py radarr --max 10
./.github/upstream-sync/catchup.py sonarr --max 10
```

After it runs, eyeball `sync.log`, push the new commits, and review the
state.json change in the same push so the cursor advance is captured.

## When to extend the filters

- New noisy commit category appears (e.g. a new dependabot pattern):
  add a line to `skip-patterns.txt`.
- New movie/TV path lands in upstream that we don't have:
  add a glob to `skip-paths.txt`.
- A commit you'd want to take got skipped: remove or narrow the rule.

## Tuning

`catchup.py` defaults to Sonnet (`claude-sonnet-4-6`). For tricky
backports you can pass `--model claude-opus-4-7` for higher fidelity at
higher cost. The agent loop is bounded at 40 tool uses per commit; raise
it in the script if a particular commit needs more exploration.

## State

- `state.json` — per-upstream cursor (committed)
- `sync.log` — append-only audit log (committed)
- `skip-paths.txt`, `skip-patterns.txt` — filters (committed)
- `catchup.py` — the runner (committed)
