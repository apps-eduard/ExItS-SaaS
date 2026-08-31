# POS-MULTI-BRANCH-V2-MB2-01A-PRODUCT-GOVERNANCE-DATA-FOUNDATION

TASK=POS-MULTI-BRANCH-V2-MB2-01A-PRODUCT-GOVERNANCE-DATA-FOUNDATION
PROGRAM=POS-MULTI-BRANCH-COMMERCE-V2
PACKAGE=MB2-01A

START_SHA=3c2d360818f8e1accfe735a8b940d853869ca76d
IMPLEMENTATION_SHA=47c8b66704c735ea98ba8177bc3fd3ca64617834

PRODUCT_SCOPE_MODEL=CatalogProductScope
ORGANIZATION_STANDARD=OrganizationStandard
BRANCH_LOCAL=BranchLocal
ORIGIN_BRANCH_ID=PosBranchId? (opaque Platform branch id; no POS/Platform FK)

EXISTING_PRODUCTS_BACKFILLED_STANDARD=YES
PRODUCT_IDS_REWRITTEN=NO

BRANCH_PRODUCT_AVAILABILITY_TABLE=pos.branch_product_availabilities
AVAILABILITY_STORAGE=SPARSE_OVERRIDE
EXISTING_AVAILABILITY_ROWS_CREATED=0

AVAILABILITY_UNIQUE_KEY=(organization_id, branch_id, product_id) PK
AVAILABILITY_CONCURRENCY_MODEL=PostgreSQL xmin (xid concurrency token)

PLATFORM_BRANCH_CROSS_DB_FK_ADDED=NO
PRODUCT_FK_MODEL=composite (product_id, organization_id) → AK_products_id_organization_id Restrict

CURRENT_CREATE_DEFAULT_SCOPE=OrganizationStandard
CURRENT_IMPORT_DEFAULT_SCOPE=OrganizationStandard

AUTHORIZATION_BEHAVIOR_CHANGED=NO
COMMERCIAL_AVAILABILITY_BEHAVIOR_CHANGED=NO
PRICING_BEHAVIOR_CHANGED=NO
INVENTORY_BEHAVIOR_CHANGED=NO
REACT_PRODUCTION_CHANGED=NO

MIGRATION=20260831200135_AddCatalogProductGovernanceFoundation
MIGRATION_APPLY=YES (integration tested)
MIGRATION_BACKFILL=all existing products → OrganizationStandard; origin_branch_id NULL
MIGRATION_DOWN=YES (drops availability table + scope/origin columns/constraints)
MIGRATION_UNRELATED_SCHEMA_DRIFT=NO
MIGRATION_PRODUCT_ID_REWRITE=NO
MIGRATION_SELLING_PRICE_CHANGE=NO
MIGRATION_HISTORY_REWRITE=NO
MIGRATION_AUTO_AVAILABILITY_ROWS=NO

DOMAIN_TESTS=PASS (PGDF-DOM-01…08 + scope codes; Catalog filter 193 passed)
PERSISTENCE_TESTS=PASS (PGDF-DB-01…08 + DB constraint checks)
MIGRATION_TESTS=PASS (PGDF-MIG-01…12)
ARCHITECTURE_TESTS=PASS (PosProductGovernanceArchitectureTests ×6)
BROADER_POS_TESTS=Catalog unit filter PASS (193)

P0=0
P1=0
P2=0 (pre-existing PosCatalogScopeArchitectureTests CostPrice string match in CatalogImportContracts — not introduced by MB2-01A)
P0_UNRESOLVED=0
P1_UNRESOLVED=0

MB2_01A_STATUS=COMPLETE_VALIDATED_FOUNDATION
NEXT=MB2_01B

HARD STOP: Do not start MB2-01B until explicitly authorized.
No recursive SHA-stamp commit for this report.

## Schema added

### pos.products
- `scope` varchar(32) NOT NULL — check `ck_products_scope`
- `origin_branch_id` uuid NULL — check `ck_products_branch_local_origin` (BranchLocal ⇒ NOT NULL)

### pos.branch_product_availabilities
- PK (organization_id, branch_id, product_id)
- is_offered, created_at_utc, updated_at_utc, updated_by_actor_id, xmin
- FK Restrict to products alternate key
- ix_branch_product_availabilities_org_branch_offered

## Behavior

Create / CreateImportedSnapshot remain OrganizationStandard.
No Sell/storefront/availability enforcement.
No React / pricing / inventory / auth changes.
