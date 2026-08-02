# P16-WP11 — Local Validation dataset reset (ABC / XYZ)

**Status:** Open (P16-WP11 In Progress)  
**Phase:** Phase 16 — Implementation Complete, Under Validation  
**Date:** 2026-08-02

## Title

Replace Local Validation seed with deterministic ABC/XYZ organizations and eight single-scope identities

## Change summary

- Organizations: **ABC Sari-Sari Store** (`abc-sari-sari`), **XYZ Mini Grocery** (`xyz-mini-grocery`)
- Identities: 2 Platform + 4 Organization + 2 Personal (unchanged people; remapped orgs)
- Obsolete orgs closed on seed: `sampaguita-store`, `mabuhay-mini-mart`, `phase16-seed-org`
- Obsolete `.exits.test` identities decommissioned
- Personal Utang seed: Luis↔Sofia ledger + reminders
- Operator reset: `tools/Reset-LocalValidation.ps1 -ConfirmReset` (guarded; not automatic startup wipe)

## Dataset version

`2026-08-02-abc-xyz-v1`

## Safety

Reset requires `-ConfirmReset`, rejects Production environment / Production-looking connection strings, and removes only Local Validation named volumes.

## Remaining

- P16-WP11 In Progress
- P16-WP12 Not Started
- Phase 16 not closed
