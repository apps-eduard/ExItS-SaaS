# POS-MULTI-BRANCH-V2-DOCUMENTATION-AND-ARCHITECTURE-LOCK-01

TASK=POS-MULTI-BRANCH-V2-DOCUMENTATION-AND-ARCHITECTURE-LOCK-01
PROGRAM=POS-MULTI-BRANCH-COMMERCE-V2
PACKAGE=MB2-00

START_SHA=dcc2b268894feb84eb742c3f26a0f855e5d330d9
DOCS_SHA=(pending)
FINAL_SHA=(pending)
REMOTE_SHA=(pending)

PRODUCTION_CODE_CHANGED=NO
MIGRATION_CREATED=NO

## Audit

CURRENT_ARCHITECTURE_AUDITED=YES
CURRENT_VS_TARGET_MATRIX=YES (master doc §6)
DATA_OWNERSHIP_LOCKED=YES
PRODUCT_SCOPE_LOCKED=YES (OrganizationStandard / BranchLocal)
PRODUCT_AVAILABILITY_LOCKED=YES (default + BranchProductAvailability)
PROMOTION_MODEL_LOCKED=YES (Local→Standard, same ProductId, one-way V1)
INVENTORY_TARGET_LOCKED=YES (branch display; org aggregate)
PRICING_TARGET_LOCKED=YES (override ?? org default)
CUSTOMER_PRIVACY_LOCKED=YES
SUPPLIER_PRIVACY_LOCKED=YES
BRANCH_SETUP_FLOW_LOCKED=YES
OFFLINE_IMPACT_DOCUMENTED=YES
MIGRATION_STRATEGY_DOCUMENTED=YES
SECURITY_THREAT_REVIEW=YES
PACKAGE_DEPENDENCY_GRAPH=YES

OPEN_DECISIONS_COUNT=5
OPEN_DECISIONS=OD-01 availability+stock warn vs block; OD-02 party backfill provenance; OD-03 setup progress storage; OD-04 inventory API shape; OD-05 offline cache key timing

## Package readiness (docs)

MB2_01_READY=YES (owner review first)
MB2_02_READY=YES (after MB2-01 preferred)
MB2_03_READY=YES (after MB2-01)
MB2_04_READY=CONDITIONAL (OD-02)
MB2_05_READY=YES (after MB2-01…04)
MB2_06_READY=YES (after MB2-05)
MB2_07_READY=YES (terminal)

ARCHITECTURE_STATUS=DOCUMENTED_READY_FOR_OWNER_REVIEW

NEXT=OWNER_REVIEW_BEFORE_MB2_01

## Documents created

- docs/Mobile-React/Authoritative/POS/multi-branch-commerce-v2.md
- docs/Mobile-React/Authoritative/POS/product-governance-and-branch-assortment.md
- docs/Mobile-React/Authoritative/POS/branch-inventory-authority.md
- docs/Mobile-React/Authoritative/POS/branch-pricing-and-effective-price.md
- docs/Mobile-React/Authoritative/POS/branch-customer-supplier-access.md
- docs/Mobile-React/Authoritative/POS/branch-guided-setup.md
- docs/Mobile-React/Implementation-Readiness/POS-MULTI-BRANCH-V2-IMPLEMENTATION-PLAN.md

## Documents updated (evolution pointers)

- docs/engineering/organization-branches-and-fulfillment-locations.md
- docs/engineering/organization-branch-capability-matrix.md (also SUPERSEDED staff-ACL line fixed)
- docs/engineering/data-ownership.md
- docs/Mobile-React/Authoritative/POS/pricing-and-price-authority.md

## Major CURRENT→TARGET gaps (summary)

1. No product scope / Local / availability
2. Inventory list often shows org pool; receive/open often skip branch overlay
3. No branch price overrides; offline price source org-only
4. No customer/supplier branch access (privacy gap for multi-branch staff)
5. No guided resumable branch setup wizard

## Recommended MB2-01 scope

Product scope + OriginBranchId + availability + Standard backfill + Local create/edit authority + promotion Local→Standard (same ProductId) + barcode/SKU protections + API/React foundation + migration — **no wizard, no pricing overrides, no party ACL**.

## HARD STOP

Do **not** start MB2-01 production implementation until owner review.
