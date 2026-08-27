# Pinoy Pawn Manager — Security Folder

> Parent overview: [../security.md](../security.md)  
> Authorization matrix: [../authorization-matrix.md](../authorization-matrix.md)

| Field | Value |
|---|---|
| Status | PPM-00 documentation foundation |
| Implementation | None |
| Last updated | 2026-08-27 |
| Legal claim | `LEGAL_AUTHORIZATION_CLAIMED` = **NO** |

## Purpose

Security planning for PPM: product-local grants, custody controls, audit/history events, and privacy for sensitive collateral evidence.

## Documents

| Doc | Focus |
|---|---|
| [role-and-grant-baseline.md](role-and-grant-baseline.md) | Presets vs grants; least privilege |
| [custody-security.md](custody-security.md) | Vault, movement, release controls |
| [audit-and-history.md](audit-and-history.md) | Required operational event classes |
| [privacy-and-sensitive-data.md](privacy-and-sensitive-data.md) | Evidence minimization; DPA caution |

## Hard principles

| Principle | Value |
|---|---|
| Least privilege via grants | YES (not role-name hard-coding) |
| Org / branch isolation | YES |
| `CUSTODY_HISTORY_REQUIRED` | YES |
| `PHYSICAL_RELEASE_SEPARATE_FROM_PAYMENT` | YES |
| Default PHI | None unless separately authorized |
| `LEGAL_AUTHORIZATION_CLAIMED` | NO |

## Related Open decisions

- **PPM-D-00-13** authorized representative redemption  
- **PPM-D-00-18** grant identifier finalization  
- **PPM-D-00-19** retention / deletion / export  
- Portfolio **R-091** production authentication maturity  

## Non-goals

- Implementing auth middleware or crypto in PPM-00  
- Claiming AML/KYC completeness  
- Inventing biometric mandates  
