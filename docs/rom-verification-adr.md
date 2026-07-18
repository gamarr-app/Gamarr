# ADR, No-Intro ROM verification architecture contract

## Status

Approved for Todo 1 planning and implementation follow-up.

## Context

Gamarr needs a generic No-Intro verification design for ROM libraries that works across No-Intro-backed systems without bending current game file behavior into ROM-specific rules. The architecture scope is generic No-Intro systems, with Game Boy Advance and Nintendo DS used as the first validation examples because they already show the ZIP-heavy and mixed ZIP plus raw shapes that v1 must handle. Switch is deferred because its TitleID-driven metadata model is a separate problem.

## Decision summary

Gamarr will add a separate ROM inventory and verification aggregate for No-Intro-backed systems. It will not implicitly reuse `GameFileId`, `HasFile`, or existing wanted semantics. This ADR acts as the migration guard for those legacy concepts. ZIP verification is read-only and does not imply extraction or import overhaul. Catalog truth comes from automatic pinned upstream DAT or catalog sync with visible source, version, and last-sync metadata, plus a manual refresh action.

## Data model boundary

The No-Intro feature owns a separate ROM inventory and verification aggregate.

That aggregate is responsible for:

- catalog source identity, pinned upstream release metadata, sync timestamps, and sync failure state
- canonical catalog entries and their hash truth
- managed library membership for verification roots and system scope
- per-library-file verification results for raw files and ZIP-contained ROM payloads
- orthogonal duplicate and missing-set reporting
- expected filename resolution for the selected rename profile

The existing game and game-file model remains separate.

- Do not implicitly reuse `GameFileId` as the identity of a verified ROM record.
- Do not implicitly reuse `HasFile` as proof that a ROM is verified or present in a managed verification set.
- Do not reuse existing wanted semantics. `Missing` for ROM verification is a separate verification-set concept and not a replacement for existing wanted or monitored flows.

If later implementation needs any compatibility bridge, it must be an explicit migration path, not hidden semantic reuse.

## Verification status table

| Status | Scope | Meaning | Notes |
| --- | --- | --- | --- |
| Verified | per file | The ROM payload hash matches a loaded No-Intro catalog entry and the filename matches the selected rename profile | Name checks are profile-relative |
| Name mismatch | per file | The ROM payload hash matches a loaded No-Intro catalog entry, but the filename or path shape differs from the selected rename profile | `Name mismatch` is relative to the selected rename profile |
| Unknown | per file | The ROM payload hash does not match any loaded No-Intro catalog entry | Not a naming issue |
| Bad dump | per file | The ROM payload hash matches a known bad, overdump, headered, or otherwise non-good catalog entry, or a deterministic bad-dump rule | Takes precedence over Verified in summary views |
| Duplicate | orthogonal flag | More than one managed library file resolves to the same canonical No-Intro entry | Does not replace the per-file verification state |
| Missing | verification set | A catalog entry expected by the managed verification set is absent from the managed library scope | `Missing` is scoped to the managed verification set |

Summary precedence for the primary per-file state is: Bad dump, Unknown, Name mismatch, Verified. Duplicate and Missing remain orthogonal signals.

## ZIP/raw matching rules

The authoritative verification unit is the normalized catalog ROM entry derived from raw ROM bytes.

- Loose files are hashed from the raw ROM payload.
- ZIP verification is read-only and does not imply extraction, import, or archive-management overhaul.
- ZIP-contained matching hashes the selected ROM payload inside the archive against the same catalog truth used for loose files.
- Raw and ZIP matches converge on the same canonical ROM entry when the payload bytes are the same.
- V1 must classify ambiguous or unsupported multi-member archives deterministically instead of guessing.
- A successful ZIP match does not grant any separate import semantics, extracted-file lifecycle, or ownership change.

## V1 enablement matrix

| Area | V1 decision |
| --- | --- |
| Architecture scope | Generic No-Intro systems architecture scope |
| Initial validation examples | GBA and Nintendo DS |
| Catalog source | Automatic pinned upstream DAT or catalog sync |
| Catalog visibility | Show source, version, last-sync, last-attempt, and refresh failure state |
| Refresh behavior | Keep prior good catalog data visible on failed refresh, allow manual refresh |
| Verification inputs | Raw ROM files and ZIP-contained ROM payloads |
| Naming behavior | Keep current Gamarr naming as default, add No-Intro-aware profile evaluation |
| Missing scope | Managed verification set only |
| ZIP behavior | Read-only verification only |
| Switch | Deferred |

## Automatic upstream catalog policy

The approved owner decision is automatic pinned upstream DAT or catalog sync, not an unresolved default.

- The system stores the upstream source identity, pinned version or revision, last successful sync, last attempted sync, and failure state.
- The UI and API must expose visible source, version, and last-sync details.
- Manual refresh is available.
- Failed refresh attempts must not silently replace or clear the prior good catalog snapshot.

## Validation examples

Game Boy Advance and Nintendo DS are the first validation examples for v1 because they prove the generic design against real library shapes.

- GBA validates ZIP-heavy libraries, numbered by-id naming, and non-retail subsets.
- Nintendo DS validates mixed raw `.nds` and ZIP libraries plus additional subset folders.
- These examples validate the architecture. They do not narrow the architecture to only those two systems.

## Explicit non-goals

- No implicit reuse of `GameFileId`, `HasFile`, or existing wanted semantics.
- No archive extraction workflow change.
- No ROM import overhaul.
- No forced bulk rename of existing ROM libraries.
- No assumption that current folder names are authoritative catalog truth.
- No Switch implementation in v1. Switch is deferred.

## Consequences

This boundary keeps today’s game and file behavior stable while making room for ROM-specific verification truth, duplicate detection, missing-set reporting, and profile-relative naming checks. It also keeps ZIP support narrow and safe, because verification stays read-only in v1.
