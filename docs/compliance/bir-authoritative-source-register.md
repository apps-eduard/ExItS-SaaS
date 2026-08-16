# BIR Authoritative Source Register

> Engineering reference only. Entries record what ExItS has **accessed** and what each source **does / does not confirm**.  
> This register is **not** legal advice, **not** BIR accreditation, and **not** a claim that ExItS is BIR compliant or certified.

Accessed date for this register revision: **2026-08-16**.

Related: [BIR activation roadmap](bir-compliance-activation-roadmap.md) ·
[Registration readiness design](../engineering/bir-registration-readiness-and-activation.md) ·
[Phase 26](../phases/phase-26-sales-documents-compliance-readiness.md)

---

## Status vocabulary

| Marker | Meaning |
|---|---|
| **CONFIRMED (source existence)** | Official URL / portal confirmed reachable as BIR-owned or Republic Act / RMC reference |
| **SECONDARY SUMMARY** | Non-BIR summary of an official issuance; cite carefully; prefer primary when available |
| **UNCONFIRMED** | Not yet confirmed for ExItS product requirements |
| **DOES NOT CONFIRM** | Explicitly out of scope for what ExItS may claim from that source |

---

## Register

### SRC-BIR-001 — Official BIR website / EOPT landing

| Field | Value |
|---|---|
| **ID** | SRC-BIR-001 |
| **Title** | Bureau of Internal Revenue official site (incl. Ease of Paying Taxes / RA 11976 landing context) |
| **Authority** | Bureau of Internal Revenue (Philippines) |
| **URL** | https://www.bir.gov.ph/ |
| **Accessed** | 2026-08-16 |
| **What confirms** | Official BIR digital presence; public navigation to services, issuances, and taxpayer information. Useful as the primary entry point for further official sources. |
| **What does NOT confirm** | Invoice layout; OR/SI numbering series rules for ExItS; fiscal memory requirements; that ExItS or any merchant is accredited/certified; TaxDocument field schemas for ExItS. |

### SRC-BIR-002 — BIR eAccReg portal (CRM/POS accreditation & PTU-related workflows)

| Field | Value |
|---|---|
| **ID** | SRC-BIR-002 |
| **Title** | BIR eAccReg (Electronic Accreditation / Registration) |
| **Authority** | Bureau of Internal Revenue (Information Systems Group branding on portal help) |
| **URL** | https://eaccreg.bir.gov.ph |
| **Help** | https://eaccreg.bir.gov.ph/ACCREG/help.html |
| **Accessed** | 2026-08-16 |
| **What confirms** | Official BIR portal surface for CRM/POS machine and sales-machine related accreditation/registration templates and help (including “Uploading Registration of Permit to Use” instructional materials on the help page). |
| **What does NOT confirm** | That ExItS is accredited; that PTU alone equals CAS registration; invoice/OR layout; numbering; fiscal memory; EIS PTT issuance for ExItS; product readiness. |
| **Wording caution** | Prefer “BIR eAccReg portal for CRM/POS accreditation / PTU-related registration workflows” — do **not** claim ExItS holds a PTU or accreditation. |

### SRC-BIR-003 — CAS registration vs PTU (RMC No. 5-2021) — secondary summary

| Field | Value |
|---|---|
| **ID** | SRC-BIR-003 |
| **Title** | RMC No. 5-2021 — simplified CAS/CBA/system registration policies (PTU requirement removed for covered systems; register instead) |
| **Authority** | Bureau of Internal Revenue (Revenue Memorandum Circular) |
| **Primary preference** | Official BIR issuances / RMC PDF when available from bir.gov.ph issuances channels |
| **Secondary summaries accessed** | Professional firm alerts summarizing RMC 5-2021 (e.g. PwC / KPMG / Grant Thornton public tax alerts) — **SECONDARY SUMMARY only** |
| **Accessed** | 2026-08-16 |
| **What confirms (as secondary summary of RMC 5-2021)** | For covered Computerized Accounting System / related systems, taxpayers register the system with the RDO (documentary checklist); securing a new PTU for CAS is not the post–RMC 5-2021 path described in those summaries. Existing PTUs may remain valid subject to stated exceptions (revocation / major enhancement). |
| **What does NOT confirm** | Exact ExItS product classification as CAS vs POS machine vs other; documentary checklist contents for ExItS; that ExItS registration is complete; invoice layout; numbering; EIS obligations. |
| **ExItS product note** | Domain registration types keep both `PosPermitToUse` and `CasRegistration` as **distinct readiness evidence types**. Do not collapse them in UI wording. |

### SRC-BIR-004 — EIS certification / Permit to Transmit (reference types only)

| Field | Value |
|---|---|
| **ID** | SRC-BIR-004 |
| **Title** | Electronic Invoicing System (EIS) certification / Permit to Transmit (PTT) — reference types |
| **Authority** | Bureau of Internal Revenue (concept referenced for future readiness evidence typing) |
| **URL** | Prefer official BIR EIS / eServices pages under https://www.bir.gov.ph/ when locating current program pages |
| **Accessed** | 2026-08-16 |
| **What confirms** | EIS certification and PTT are recognized **reference registration types** ExItS may record when an organization provides evidence (`EisCertification`, `EisPermitToTransmit`). |
| **What does NOT confirm** | That ExItS is EIS-certified; that merchants must use EIS via ExItS today; transmission formats; invoice layout; TaxDocument runtime availability. |

### SRC-BIR-005 — Secondary registration / citizen-facing PTU pages

| Field | Value |
|---|---|
| **ID** | SRC-BIR-005 |
| **Title** | Citizen-facing secondary pages describing secondary registration / PTU concepts |
| **Authority** | Mixed — prefer bir.gov.ph eServices; treat non-BIR citizen portals as **SECONDARY** |
| **URL** | Prefer https://www.bir.gov.ph/ eServices and https://eaccreg.bir.gov.ph over third-party “howto” sites |
| **Accessed** | 2026-08-16 |
| **What confirms** | Directional awareness that PTU / machine registration concepts exist in taxpayer workflows. |
| **What does NOT confirm** | Binding ExItS obligations; exact forms; timelines; that recording a reference number in ExItS equals BIR acceptance. |

---

## Explicit non-confirmations (portfolio-wide)

Regardless of source above, ExItS **does not** treat any register entry as confirming:

- Official Receipt / Sales Invoice layout for ExItS TaxDocument
- Numbering / series generators
- Fiscal memory / grand total memory requirements
- Automatic TaxDocument issuance
- “BIR Compliant”, “BIR Certified”, or “BIR Accredited” product status
- NPC / privacy legal compliance

`TaxDocumentIssuanceRuntime.ImplementationAvailable` remains **false** until a future authorized work package implements and validates issuance.
