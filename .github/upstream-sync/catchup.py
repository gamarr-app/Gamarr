#!/usr/bin/env python3
"""Pull upstream Radarr / Sonarr commits into Gamarr.

For each new commit since the last recorded SHA:
  1. Skip if its subject matches skip-patterns.txt
  2. Skip if every file it touches matches a glob in skip-paths.txt
  3. Ask Claude to semantically apply the change in Gamarr's codebase
     (Movie->Game, Radarr->Gamarr, etc. — the LLM does the translation)
  4. Build. If green, commit + push. If not, revert.

State (last processed SHA per upstream) is persisted to state.json and
committed alongside the backports. A running log goes to sync.log.

Requires ANTHROPIC_API_KEY in the environment.

Usage:
  catchup.py radarr [--max N] [--dry-run] [--model MODEL]
  catchup.py sonarr [--max N] [--dry-run] [--model MODEL]
  catchup.py both   [--max N] [--dry-run] [--model MODEL]
"""

import argparse
import fnmatch
import json
import os
import re
import subprocess
import sys
import time
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[2]
SYNC_DIR = REPO_ROOT / ".github" / "upstream-sync"
STATE_FILE = SYNC_DIR / "state.json"
SKIP_PATHS_FILE = SYNC_DIR / "skip-paths.txt"
SKIP_PATTERNS_FILE = SYNC_DIR / "skip-patterns.txt"
LOG_FILE = SYNC_DIR / "sync.log"

DEFAULT_MODEL = "claude-sonnet-4-6"

SYSTEM_PROMPT = """\
You are backporting a single commit from an upstream project (Radarr or
Sonarr — both are PVR applications written in C# / .NET + React/TypeScript)
into Gamarr, which was forked from Radarr and repurposed as a "PVR for
games". Key differences:

- Domain entities are renamed: Movie -> Game, MovieFile -> GameFile,
  Collection (movie) -> doesn't exist, Series/Episode/Season (Sonarr) -> Game.
- Some assemblies are renamed: Radarr.Http / Sonarr.Http -> Gamarr.Http,
  Radarr.Api.V3 / Sonarr.Api.V3 -> Gamarr.Api.V3, Radarr.Test.Common -> Gamarr.Test.Common.
- Backend project directories and namespaces are STILL `NzbDrone.*` (do
  not rename those — Directory.Build.props remaps assemblies at build time).
- Metadata sources are Steam/IGDB/RAWG (not TMDB/IMDB/TVDB).
- Many movie/tv-specific paths don't exist (Movies/, Series/, Episode*,
  Tmdb*, Tvdb*, Plex/, Trakt/, Subtitles/, Languages/).

Your job, per commit:
1. Read the upstream commit (subject + body + full diff).
2. Decide if it applies to Gamarr. A movie-only or TV-only feature with
   no game-domain analogue should be SKIPPED.
3. If it applies: produce the equivalent change in the Gamarr codebase.
   You may need to find the renamed Gamarr file (e.g. MoviesController.cs
   -> GameController.cs), translate the patch (Movie -> Game, etc.), or
   adapt it to the slightly different surrounding code.
4. Use the file tools to read existing Gamarr files and write the changes.
5. When done, call the `apply` tool with a one-line commit message, or
   call the `skip` tool with a short reason.

Constraints:
- Only modify files that are clearly the Gamarr equivalent of files the
  upstream commit modified. Do not "improve" unrelated code.
- Never create a new top-level project, migration, or feature flag.
- If the change adds a new file, only create it if its Gamarr equivalent
  obviously belongs (e.g. a new bug-fix helper).
- If you're not confident the patch makes sense in Gamarr, SKIP with
  reason rather than guess.
"""


def log(msg: str) -> None:
    line = f"[{time.strftime('%Y-%m-%dT%H:%M:%SZ', time.gmtime())}] {msg}"
    print(line)
    with open(LOG_FILE, "a") as f:
        f.write(line + "\n")


def sh(*args: str, check: bool = True, capture: bool = True) -> subprocess.CompletedProcess:
    return subprocess.run(
        args, cwd=REPO_ROOT, check=check,
        stdout=subprocess.PIPE if capture else None,
        stderr=subprocess.PIPE if capture else None,
        text=True,
    )


def load_state() -> dict:
    return json.loads(STATE_FILE.read_text())


def save_state(state: dict) -> None:
    STATE_FILE.write_text(json.dumps(state, indent=2) + "\n")


def read_skip_patterns() -> list[re.Pattern]:
    out = []
    for line in SKIP_PATTERNS_FILE.read_text().splitlines():
        line = line.strip()
        if line and not line.startswith("#"):
            out.append(re.compile(line, re.IGNORECASE))
    return out


def read_skip_paths() -> list[str]:
    out = []
    for line in SKIP_PATHS_FILE.read_text().splitlines():
        line = line.strip()
        if line and not line.startswith("#"):
            out.append(line)
    return out


def matches_any(text: str, patterns: list[re.Pattern]) -> re.Pattern | None:
    for p in patterns:
        if p.search(text):
            return p
    return None


def all_paths_skipped(paths: list[str], globs: list[str]) -> bool:
    if not paths:
        return False
    return all(any(fnmatch.fnmatch(p, g) for g in globs) for p in paths)


def ensure_remote(name: str, url: str) -> None:
    try:
        sh("git", "remote", "get-url", name)
    except subprocess.CalledProcessError:
        sh("git", "remote", "add", name, url)
    sh("git", "fetch", "--quiet", name, "develop", "--depth=500")


def commit_range(name: str, start: str | None, limit: int) -> list[str]:
    if start:
        rev = f"{start}..{name}/develop"
    else:
        base = sh("git", "merge-base", "HEAD", f"{name}/develop").stdout.strip()
        if not base:
            log(f"{name}: no merge base with {name}/develop")
            return []
        log(f"{name}: first run; starting from merge-base {base[:12]}")
        rev = f"{base}..{name}/develop"
    res = sh("git", "log", "--reverse", "--no-merges", "--pretty=format:%H", rev)
    shas = res.stdout.split()
    return shas[:limit] if limit else shas


def commit_subject(sha: str) -> str:
    return sh("git", "log", "-1", "--pretty=%s", sha).stdout.strip()


def commit_files(sha: str) -> list[str]:
    out = sh("git", "show", "--name-only", "--pretty=format:", sha).stdout
    return [l for l in out.splitlines() if l.strip()]


def commit_full(sha: str) -> str:
    """Return subject + body + full diff for a SHA."""
    return sh("git", "show", "--no-color", "--unified=10", sha).stdout


# ---------------------------------------------------------------------- LLM

def llm_apply(sha: str, upstream: str, model: str, dry_run: bool) -> tuple[str, str]:
    """Ask Claude to semantically apply this upstream commit. Returns
    ('PICKED'|'SKIPPED'|'BUILD-FAILED', message).
    """
    try:
        import anthropic
    except ImportError:
        log("ERROR: anthropic SDK not installed. Run: pip install anthropic")
        sys.exit(1)

    api_key = os.environ.get("ANTHROPIC_API_KEY")
    if not api_key:
        log("ERROR: ANTHROPIC_API_KEY not set")
        sys.exit(1)

    client = anthropic.Anthropic(api_key=api_key)
    full = commit_full(sha)
    subject = commit_subject(sha)

    # Tool definitions: read_file, write_file, apply, skip
    tools = [
        {
            "name": "read_file",
            "description": "Read a file from the Gamarr repo. Use this to inspect the current Gamarr code before deciding what to change.",
            "input_schema": {
                "type": "object",
                "properties": {"path": {"type": "string", "description": "Repo-relative path"}},
                "required": ["path"],
            },
        },
        {
            "name": "grep",
            "description": "ripgrep search for a literal or regex in the Gamarr repo. Use to find renamed Gamarr files (e.g. find 'GameController' when upstream touches MoviesController).",
            "input_schema": {
                "type": "object",
                "properties": {
                    "pattern": {"type": "string"},
                    "path": {"type": "string", "description": "optional path scope, default whole repo"},
                },
                "required": ["pattern"],
            },
        },
        {
            "name": "write_file",
            "description": "Write a file in the Gamarr repo (creates or overwrites). Use to apply the equivalent change.",
            "input_schema": {
                "type": "object",
                "properties": {
                    "path": {"type": "string"},
                    "content": {"type": "string"},
                },
                "required": ["path", "content"],
            },
        },
        {
            "name": "apply",
            "description": "Finalize: keep all written files, build, and commit. Provide a concise commit message (the upstream SHA is appended automatically).",
            "input_schema": {
                "type": "object",
                "properties": {"message": {"type": "string"}},
                "required": ["message"],
            },
        },
        {
            "name": "skip",
            "description": "Skip this commit. Provide a short reason (movie-only feature, doesn't apply to Gamarr, etc.). Any files written so far will be discarded.",
            "input_schema": {
                "type": "object",
                "properties": {"reason": {"type": "string"}},
                "required": ["reason"],
            },
        },
    ]

    user_msg = (
        f"Upstream: {upstream}\n"
        f"Commit: {sha}\n\n"
        f"--- COMMIT (subject + body + full diff) ---\n{full}\n--- END COMMIT ---\n\n"
        "Decide whether to apply this to Gamarr. If yes, make the necessary writes "
        "and then call `apply`. If no, call `skip` with a brief reason."
    )

    if dry_run:
        log(f"{upstream} {sha[:12]} DRY-RUN       {subject}")
        return ("SKIPPED", "dry-run")

    messages = [{"role": "user", "content": user_msg}]
    written: dict[str, str] = {}

    for _ in range(40):  # bound the agent loop
        resp = client.messages.create(
            model=model,
            max_tokens=8192,
            system=SYSTEM_PROMPT,
            tools=tools,
            messages=messages,
        )
        if resp.stop_reason != "tool_use":
            return ("SKIPPED", f"LLM stopped without decision (stop_reason={resp.stop_reason})")

        tool_results = []
        decision: tuple[str, str] | None = None
        for block in resp.content:
            if block.type != "tool_use":
                continue
            name, args = block.name, block.input
            if name == "read_file":
                p = REPO_ROOT / args["path"]
                try:
                    content = p.read_text()
                    if len(content) > 80_000:
                        content = content[:80_000] + "\n...[truncated]..."
                    tool_results.append({"type": "tool_result", "tool_use_id": block.id, "content": content})
                except Exception as e:
                    tool_results.append({"type": "tool_result", "tool_use_id": block.id, "content": f"ERROR: {e}", "is_error": True})
            elif name == "grep":
                pattern = args["pattern"]
                scope = args.get("path") or "."
                try:
                    out = subprocess.run(["rg", "-n", "--no-heading", pattern, scope], cwd=REPO_ROOT,
                                         capture_output=True, text=True, timeout=15)
                    text = (out.stdout or "(no matches)")[:20_000]
                    tool_results.append({"type": "tool_result", "tool_use_id": block.id, "content": text})
                except Exception as e:
                    tool_results.append({"type": "tool_result", "tool_use_id": block.id, "content": f"ERROR: {e}", "is_error": True})
            elif name == "write_file":
                path, content = args["path"], args["content"]
                written[path] = content
                tool_results.append({"type": "tool_result", "tool_use_id": block.id, "content": "ok"})
            elif name == "apply":
                decision = ("APPLY", args["message"])
                tool_results.append({"type": "tool_result", "tool_use_id": block.id, "content": "ok"})
            elif name == "skip":
                decision = ("SKIP", args["reason"])
                tool_results.append({"type": "tool_result", "tool_use_id": block.id, "content": "ok"})

        messages.append({"role": "assistant", "content": resp.content})
        messages.append({"role": "user", "content": tool_results})

        if decision is None:
            continue

        kind, payload = decision
        if kind == "SKIP":
            return ("SKIPPED", payload)

        # APPLY: flush written files, build, commit or revert.
        if not written:
            return ("SKIPPED", "apply called but no files written")

        for path, content in written.items():
            full_path = REPO_ROOT / path
            full_path.parent.mkdir(parents=True, exist_ok=True)
            full_path.write_text(content)

        # Stage and try to build.
        sh("git", "add", *list(written.keys()))
        build = subprocess.run(
            ["dotnet", "build", "src/Gamarr.sln", "-clp:ErrorsOnly"],
            cwd=REPO_ROOT, capture_output=True, text=True, timeout=600,
        )
        if build.returncode != 0:
            tail = (build.stdout + build.stderr)[-2000:]
            sh("git", "checkout", "--", ".")
            sh("git", "clean", "-fd", *list(written.keys()), check=False)
            return ("BUILD-FAILED", f"{payload} | build error: {tail}")

        msg = (
            f"{payload}\n\n"
            f"Backported from {upstream}@{sha[:12]} \"{subject}\"\n"
            f"Origin: https://github.com/{'Radarr/Radarr' if upstream == 'radarr' else 'Sonarr/Sonarr'}/commit/{sha}\n"
        )
        sh("git", "commit", "-m", msg)
        return ("PICKED", payload)

    return ("SKIPPED", "agent loop exhausted")


# ---------------------------------------------------------------------- main

def process_upstream(name: str, url: str, max_n: int, dry_run: bool, model: str) -> dict:
    ensure_remote(name, url)
    state = load_state()
    last = state[name].get("last_sha")
    shas = commit_range(name, last, max_n)
    if not shas:
        log(f"{name}: nothing new")
        return {"picked": 0, "skipped": 0, "build_failed": 0}

    skip_patterns = read_skip_patterns()
    skip_paths = read_skip_paths()
    counts = {"picked": 0, "skipped": 0, "build_failed": 0}

    for sha in shas:
        subject = commit_subject(sha)

        if (m := matches_any(subject, skip_patterns)):
            log(f"{name} {sha[:12]} SKIP-PATTERN  /{m.pattern}/  {subject}")
            counts["skipped"] += 1
            if not dry_run:
                state[name]["last_sha"] = sha
                save_state(state)
            continue

        files = commit_files(sha)
        if all_paths_skipped(files, skip_paths):
            log(f"{name} {sha[:12]} SKIP-PATH     {subject}")
            counts["skipped"] += 1
            if not dry_run:
                state[name]["last_sha"] = sha
                save_state(state)
            continue

        kind, msg = llm_apply(sha, name, model, dry_run)
        log(f"{name} {sha[:12]} {kind:12}  {subject}  ::  {msg[:120]}")
        if kind == "PICKED":
            counts["picked"] += 1
        elif kind == "BUILD-FAILED":
            counts["build_failed"] += 1
        else:
            counts["skipped"] += 1

        if not dry_run:
            state[name]["last_sha"] = sha
            save_state(state)

    log(f"{name} SUMMARY: {counts}")
    return counts


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("upstream", choices=["radarr", "sonarr", "both"])
    parser.add_argument("--max", type=int, default=50)
    parser.add_argument("--dry-run", action="store_true")
    parser.add_argument("--model", default=DEFAULT_MODEL)
    args = parser.parse_args()

    targets = []
    if args.upstream in ("radarr", "both"):
        targets.append(("radarr", "https://github.com/Radarr/Radarr.git"))
    if args.upstream in ("sonarr", "both"):
        targets.append(("sonarr", "https://github.com/Sonarr/Sonarr.git"))

    for name, url in targets:
        process_upstream(name, url, args.max, args.dry_run, args.model)


if __name__ == "__main__":
    main()
