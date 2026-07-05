# Prowlarr application support for Gamarr

`0001-New-Gamarr-application-support.patch` adds a **Gamarr** application type
to Prowlarr, so Prowlarr can push its indexers to Gamarr the same way it does
for Radarr/Sonarr/Whisparr (Settings → Apps → + → Gamarr).

It is six self-contained files under
`src/NzbDrone.Core/Applications/Gamarr/`, modeled on the Whisparr
implementation, with game-appropriate default sync categories
(Console 1000–1180, PC 4000–4050). No Prowlarr frontend changes are needed —
the application settings UI is schema-driven.

## Verified

Tested end-to-end on 2026-07-04 against Prowlarr `develop` (10.0.0.36977)
and a local Gamarr build:

- Gamarr appears in `GET /api/v1/applications/schema`
- `POST /api/v1/applications/test` → 200 (Prowlarr fetches Gamarr's indexer
  schema and Gamarr accepts the test indexer via `POST /api/v3/indexer/test`)
- Application add + `ApplicationIndexerSync` command complete cleanly

## Submitting upstream

```bash
git clone https://github.com/Prowlarr/Prowlarr.git
cd Prowlarr
git checkout -b gamarr-application develop
git am /path/to/0001-New-Gamarr-application-support.patch
# push to your fork and open a PR against Prowlarr's develop branch
```

PR title suggestion: `New: Gamarr application support`. Servarr projects use
the `New:` prefix for features. Expect maintainers to ask what Gamarr is —
link the repo and note the indexer API is contract-compatible with Radarr v3.

Once (if) the PR merges, this directory can be deleted.
