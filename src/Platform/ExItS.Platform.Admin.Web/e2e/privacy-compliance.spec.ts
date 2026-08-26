import { expect, test, type Page } from "@playwright/test";

const sampleRequirement = {
  id: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
  code: "DOC-1",
  title: "Privacy Notice",
  category: "CustomerFacing",
  description: "Customer-facing privacy notice",
  requirementLevel: "Required",
  status: "NotStarted",
  ownerRole: "Platform",
  version: "1.0",
  effectiveDate: null,
  lastReviewedDate: null,
  nextReviewDate: null,
  notes: null,
  sourceReference: null,
  requiresDpoLegalVerification: false,
  createdAtUtc: "2026-01-01T00:00:00Z",
  updatedAtUtc: "2026-01-01T00:00:00Z",
  evidenceCount: 1,
};

const sampleOverview = {
  totalRequirements: 1,
  totalSystems: 1,
  totalEvidence: 1,
  requirementsByStatus: { NotStarted: 1 },
  requirementsByCategory: { CustomerFacing: 1 },
  lastUpdatedUtc: "2026-08-19T12:00:00Z",
  overallReadiness: "ActionNeeded",
  readyCount: 0,
  actionNeededCount: 1,
  externalLegalReviewCount: 0,
  requirementsWithEvidenceCount: 1,
  technicalSafeguardsSummary: "Partial",
  governanceDocumentationSummary: "Unavailable",
  legalReviewSummary: "Required",
  npcVerificationSummary: "NotVerified",
  categorySummaries: [
    {
      group: "Documents",
      detailRoute: "/admin/privacy-compliance/documents",
      requirementCount: 1,
      readyCount: 0,
      actionNeededCount: 1,
      evidenceCoveredCount: 1,
      lastReviewedDate: null,
      status: "ActionNeeded",
      hasActionNeeded: true,
    },
  ],
  privacyImpactFollowUps: [
    {
      code: "PIA-1",
      title: "Core processing PIA",
      status: "NeedsUpdate",
      requiresDpoLegalVerification: true,
      evidenceCount: 0,
      lastReviewedDate: null,
    },
  ],
};

const sampleSystem = {
  id: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
  code: "SYS-POS",
  systemName: "POS Core",
  purpose: "Sales",
  dataSubjects: "Customers",
  personalDataCategories: "Contact",
  sensitiveDataCategories: null,
  storageLocation: "PH",
  recipientsProcessors: null,
  retentionSummary: "7y",
  securityControls: "TLS",
  owner: "Platform",
  piaStatus: "Required",
  createdAtUtc: "2026-01-01T00:00:00Z",
  updatedAtUtc: "2026-01-01T00:00:00Z",
};

const sampleEvidence = {
  id: "cccccccc-cccc-cccc-cccc-cccccccccccc",
  requirementId: sampleRequirement.id,
  kind: "Report",
  label: "Readiness report",
  referencePath: "docs/privacy/report.pdf",
  notes: null,
  createdAtUtc: "2026-08-01T00:00:00Z",
};

async function mockSession(
  page: Page,
  permissions: string[] = [
    "platform.permission.view_privacy_compliance",
    "platform.permission.view_portfolio",
    "platform.permission.manage_organizations",
  ],
) {
  await page.route("**/api/v1/platform/auth/me", async (route) => {
    await route.fulfill({
      json: {
        sessionId: "11111111-1111-1111-1111-111111111111",
        userId: "22222222-2222-2222-2222-222222222222",
        username: "olivia",
        displayName: "Olivia Mendoza",
        email: "olivia@example.test",
        expiresAtUtc: "2026-08-19T12:00:00Z",
        absoluteExpiresAtUtc: "2026-08-20T12:00:00Z",
        selectedOrganizationId: null,
        selectedOrganizationDisplayName: null,
        organizationSelectionState: "None",
        activeOrganizationCount: 0,
        accountClass: "Platform",
      },
    });
  });
  await page.route("**/api/v1/platform/authorization/me**", async (route) => {
    await route.fulfill({
      json: {
        actorIdentifier: "olivia@example.test",
        actorType: "PlatformUser",
        platformUserId: "22222222-2222-2222-2222-222222222222",
        organizationId: null,
        permissions,
      },
    });
  });
  await page.route("**/api/v1/platform/antiforgery/token**", async (route) => {
    await route.fulfill({
      json: { headerName: "X-XSRF-TOKEN", token: "test-antiforgery-token" },
    });
  });
}

async function mockPrivacyApis(
  page: Page,
  options?: { patchFail?: boolean; initialRequirement?: typeof sampleRequirement },
) {
  let requirement = { ...(options?.initialRequirement ?? sampleRequirement) };
  await page.route("**/api/v1/platform/privacy-compliance/**", async (route) => {
    const url = route.request().url();
    const method = route.request().method().toUpperCase();

    if (method === "PATCH" && url.includes(`/requirements/${requirement.id}`)) {
      if (options?.patchFail) {
        await route.fulfill({
          status: 500,
          json: { detail: "upstream failure", title: "Error", status: 500 },
        });
        return;
      }
      const body = route.request().postDataJSON() as { status?: string; notes?: string | null };
      if (url.endsWith("/status") && typeof body.status === "string") {
        requirement = { ...requirement, status: body.status };
      } else if ("notes" in body) {
        requirement = { ...requirement, notes: body.notes ?? null };
      }
      await route.fulfill({ json: requirement });
      return;
    }

    if (url.includes("/overview")) {
      await route.fulfill({ json: sampleOverview });
      return;
    }
    if (url.includes("/systems")) {
      await route.fulfill({ json: [sampleSystem] });
      return;
    }
    if (url.includes("/evidence")) {
      await route.fulfill({ json: [sampleEvidence] });
      return;
    }
    if (url.includes(`/requirements/${requirement.id}`) && !url.includes("export")) {
      await route.fulfill({ json: requirement });
      return;
    }
    if (url.includes("/requirements")) {
      await route.fulfill({ json: [requirement] });
      return;
    }
    await route.fulfill({ status: 404, json: { detail: "not found" } });
  });
}

test("privacy compliance overview, documents, systems, evidence, and PIA routes", async ({
  page,
}) => {
  await mockSession(page);
  await mockPrivacyApis(page);
  await page.setViewportSize({ width: 1440, height: 900 });

  await page.goto("/admin/privacy-compliance");
  await expect(page.getByTestId("privacy-overview-page")).toBeVisible();
  await expect(page.getByTestId("privacy-disclaimer")).toBeVisible();
  await expect(page.getByText("Readiness tooling only")).toBeVisible();
  await expect(page.getByTestId("privacy-overview-tablist")).toBeVisible();
  await expect(page.getByTestId("privacy-overview-tab-category")).toHaveAttribute(
    "aria-selected",
    "true",
  );

  await page.getByTestId("privacy-overview-tab-pia").click();
  await expect(page.getByText("Core processing PIA")).toBeVisible();

  await page.getByTestId("privacy-overview-tab-gaps").click();
  await expect(page.getByTestId("privacy-overview-tab-gaps")).toHaveAttribute(
    "aria-selected",
    "true",
  );
  await expect(page.getByTestId("privacy-overview-panel-gaps")).toBeVisible();
  await expect(page.getByTestId("privacy-overview-panel-gaps").getByText("Important gaps")).toBeVisible();

  await page.getByRole("link", { name: "Documents" }).first().click();
  await expect(page.getByTestId("privacy-documents-page")).toBeVisible();
  await expect(page.getByText("DOC-1")).toBeVisible();

  await page.goto("/admin/privacy-compliance/systems");
  await expect(page.getByTestId("privacy-systems-page")).toBeVisible();
  await expect(page.getByText("POS Core")).toBeVisible();

  await page.goto("/admin/privacy-compliance/evidence");
  await expect(page.getByTestId("privacy-evidence-page")).toBeVisible();
  await expect(page.getByText("Readiness report")).toBeVisible();

  await page.goto("/admin/privacy-compliance/pias");
  await expect(page.getByTestId("privacy-category-pias-page")).toBeVisible();

  await page.goto("/admin/privacy-compliance/dpo-npc");
  await expect(page.getByTestId("privacy-category-dpo-npc-page")).toBeVisible();
});

test("privacy compliance fails closed without permission", async ({ page }) => {
  await mockSession(page, ["platform.permission.view_portfolio"]);

  await page.goto("/admin/privacy-compliance");
  await expect(page.getByRole("heading", { name: /not found/i })).toBeVisible();
  await expect(page.getByTestId("privacy-overview-page")).toHaveCount(0);
});

test("manage user can edit status and notes", async ({ page }) => {
  await mockSession(page, [
    "platform.permission.view_privacy_compliance",
    "platform.permission.manage_privacy_compliance",
  ]);
  await mockPrivacyApis(page);
  await page.setViewportSize({ width: 1440, height: 900 });
  await page.goto("/admin/privacy-compliance/documents");
  await page.getByRole("button", { name: /view details/i }).click();
  await expect(page.getByTestId("privacy-requirement-manage")).toBeVisible();
  await page.getByTestId("privacy-requirement-status").selectOption("Approved");
  await page.getByTestId("privacy-requirement-notes").fill("Reviewed by counsel");
  await page.getByTestId("privacy-requirement-save").click();
  await expect(page.getByTestId("privacy-requirement-save-success")).toBeVisible();
  await expect(page.getByTestId("privacy-requirement-pdf")).toBeVisible();
});

test("view-only user cannot edit requirement", async ({ page }) => {
  await mockSession(page, ["platform.permission.view_privacy_compliance"]);
  await mockPrivacyApis(page, {
    initialRequirement: { ...sampleRequirement, notes: "Locked note" },
  });
  await page.setViewportSize({ width: 1440, height: 900 });
  await page.goto("/admin/privacy-compliance/documents");
  await page.getByRole("button", { name: /view details/i }).click();
  await expect(page.getByTestId("privacy-requirement-drawer")).toBeVisible();
  await expect(page.getByText("Locked note")).toBeVisible();
  await expect(page.getByTestId("privacy-requirement-pdf")).toBeVisible();
  await expect(page.getByTestId("privacy-requirement-manage")).toHaveCount(0);
  await expect(page.getByTestId("privacy-requirement-save")).toHaveCount(0);
});

test("failed save remains truthful without success feedback", async ({ page }) => {
  await mockSession(page, [
    "platform.permission.view_privacy_compliance",
    "platform.permission.manage_privacy_compliance",
  ]);
  await mockPrivacyApis(page, { patchFail: true });
  await page.setViewportSize({ width: 1440, height: 900 });
  await page.goto("/admin/privacy-compliance/documents");
  await page.getByRole("button", { name: /view details/i }).click();
  await page.getByTestId("privacy-requirement-status").selectOption("InProgress");
  await page.getByTestId("privacy-requirement-save").click();
  await expect(page.getByTestId("privacy-requirement-save-error")).toBeVisible();
  await expect(page.getByText("upstream failure")).toBeVisible();
  await expect(page.getByTestId("privacy-requirement-save-success")).toHaveCount(0);
});
