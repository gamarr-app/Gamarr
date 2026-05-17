# Claude Code Notes

## What Gamarr is

Fork of Radarr (which shares roots with Sonarr/Lidarr/Readarr), repurposed as a
"PVR for games." Same architecture as Radarr — monitors RSS feeds and indexers,
hands grabs to download clients, imports/renames finished files — but the
managed entity is games (Steam/IGDB/RAWG metadata) instead of movies. Backend
namespace is still `NzbDrone.*` per `Directory.Build.props`; assemblies and
binaries are renamed to `Gamarr.*`.

## Stack

- Backend: .NET 10 (LTS), ASP.NET Core, SignalR, DryIoc, FluentMigrator, Dapper,
  SQLite (default) / Postgres (optional), NLog 6, Sentry 6, Swashbuckle 8.x.
- Frontend: React 19, TypeScript 6, Redux 5 + redux-actions 3, react-router 7,
  @tanstack/react-query 5, webpack 5, ESLint 10 (flat config), Prettier 3,
  stylelint 17, css-modules + PostCSS, @sentry/browser 10.
- Tooling: Node 22 (Volta-pinned), Yarn 1.22, Jest 30, NUnit 4.

## .NET SDK

The .NET 10 SDK is installed at `~/.dotnet`. Add to PATH if needed:

```bash
export DOTNET_ROOT="$HOME/.dotnet"
export PATH="$DOTNET_ROOT:$PATH"
```

`global.json` pins SDK 8.0.405 with `rollForward: latestMajor`, so the
installed .NET 10 satisfies it.

## Build Commands

```bash
# Backend
dotnet build src/Gamarr.sln

# Frontend
yarn install
yarn build

# Run tests (skip slow Playwright-based automation tests)
dotnet test src/Gamarr.sln --filter "Category!=AutomationTest"

# Frontend lint / format / unit tests
yarn lint
yarn format:check
yarn test
```

## Folder Layout

- `src/` — .NET backend. Project dirs still named `NzbDrone.*` (fork legacy);
  assemblies build as `Gamarr.*` via `Directory.Build.props`. Entry point is
  `src/NzbDrone/` (assembly `Gamarr`).
- `frontend/` — React/TypeScript UI. Entry: `frontend/src/index.tsx`. Webpack
  config at `frontend/build/webpack.config.js`. ESLint flat config at
  `frontend/eslint.config.mjs`.
- `_output/` — Backend build artifacts. `_output/net10.0/Gamarr` is the
  runnable binary on macOS/Linux (use `_output/net10.0/Gamarr.exe` on Windows).
  `_output/UI/` is the built frontend; the backend serves it from there.
- `_tests/` — Test assemblies. CI references `_tests/net10.0/*.dll`.
- `_temp/` — MSBuild `obj/` and intermediate Release `bin/` outputs (per
  `Directory.Build.props`). Safe to nuke.
- `docker/` — Linuxserver-style s6 service scripts (`docker/root/etc/...`).
  `Dockerfile` at repo root builds on `baseimage-alpine:3.23`.
- `distribution/` — Platform-specific packaging: `distribution/osx/Gamarr.app`
  bundle template, `distribution/windows/setup` Inno Setup scripts.
- `schemas/`, `scripts/`, `tools/` — codegen, helper scripts (incl.
  `scripts/sentry-autofix.sh`), and vendored build tools.

## How to run locally

```bash
# 1. Build backend + frontend
dotnet build src/Gamarr.sln
yarn build

# 2. Start with a throwaway data dir (no system browser opened)
_output/net10.0/Gamarr -nobrowser -data=/tmp/gamarr-test-data
```

On first launch Gamarr writes `config.xml` + `gamarr.db` to the `-data` dir.
Default HTTP port is `6767` (`<Port>` in `config.xml`); change it there before
the second start if `6767` is busy. The API key is auto-generated and lives in
the `<ApiKey>` element of the same `config.xml` — read it from there for any
REST/curl call.

## Known Gotchas

- **Swashbuckle pinned to 8.x.** 10.x depends on `Microsoft.OpenApi` v2, which
  moved types out of `Microsoft.OpenApi.Models`. Our annotations still target
  v1. Don't bump until annotations are migrated. See
  `src/Gamarr.Api.V3/Gamarr.Api.V3.csproj` and commit `713c9329e3`.
- **`*.css.d.ts` files are generated** by `css-modules-typescript-loader`.
  Don't run Prettier on them (it strips the quoted property names the loader
  emits). They're listed in `.prettierignore` / `frontend/.prettierignore`.
- **css-loader 7 changed `namedExport` defaults.** Webpack config requires
  `modules: { namedExport: false, exportLocalsConvention: 'asIs' }` to keep
  css-modules-typescript-loader output stable
  (`frontend/build/webpack.config.js`).
- **Steam Store API throttling.** `appdetails` bursts trigger undocumented 403s.
  `SteamStoreProxy` throttles to 1 req/sec. Never put per-item Steam fetches
  inside loops driven by user search/typeahead — see commit `550b659407` for
  the kind of N+1 we already had to fix.
- **ClamAV daemon disabled by default** to save ~1 GB RAM. We invoke
  `clamscan` standalone. Daemon mode is gated behind the `CLAMAV_DAEMON` env
  var. See `src/NzbDrone.Core/MediaFiles/VirusScanning/ClamAvScannerService.cs`
  and commits `036f7fae09` / `0d0b56b178`.
- **`Environment.UserInteractive` returns true on headless Linux.** The
  browser-launch path in `src/NzbDrone.Host/BrowserService.cs` additionally
  checks `DISPLAY` / `WAYLAND_DISPLAY` before opening a browser.
- **Sentry token lives in `.env`** (gitignored). GitHub push protection blocks
  any commit with the literal token. Scripts (e.g. `scripts/sentry-autofix.sh`)
  should `source .env` rather than hardcode the value.
- **Namespace mismatch is intentional.** `Directory.Build.props` rewrites
  `Gamarr*` project root namespaces to `NzbDrone*`. Don't "fix" it.
- **Don't `--amend`** failed-hook commits — see the harness rules; create a
  new commit instead.

## CI quirks

- `actions/download-artifact` is pinned to **v4**. v8 has a Linux
  digest-mismatch regression (commit `29a9f4d160`).
- Integration / automation tests reference `_tests/net10.0/*.dll`. If you
  retarget the runtime, update the workflow paths too (commit `1648d343d1`).
- Dependabot minor/patch PRs are auto-merged; majors are split into individual
  PRs by severity (`.github/workflows/dependabot-automerge.yml`).

## Git Workflow

Worktree/agent branches: open a PR rather than pushing to `main`. Direct push
shortcut (for non-PR work) is:

```bash
git push origin gamarr3-work:main
```

## Common Commands

```bash
# GitHub
gh pr list
gh pr create --fill
gh release list

# Sentry (token from .env; never inline it in commands you commit)
set -a; source .env; set +a
curl -s -H "Authorization: Bearer $SENTRY_AUTH_TOKEN" \
  "https://sentry.io/api/0/projects/gamarr/gamarr/issues/?query=is:unresolved&limit=25"
```
