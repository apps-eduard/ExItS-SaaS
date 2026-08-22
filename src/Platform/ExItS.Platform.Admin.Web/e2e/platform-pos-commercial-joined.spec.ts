import { expect, test } from "@playwright/test";
import {
  bootstrapGrowthOrganization,
  checkoutSale,
  createCatalogProduct,
  ensureOpenShift,
  introspectAccessToken,
  issueProductAccessToken,
  readDeviceCapacity,
  readProvenance,
  registerDevice,
  posGet,
  type JoinedBootstrapContext,
  type PosDeviceScope,
} from "./helpers/pa-com-07-joined-api";

const enabled = process.env.PA_COM_07_JOINED === "1";
const adminPassword = process.env.LOCAL_VALIDATION_SHARED_PASSWORD ?? "";

async function loginPlatformAdmin(page: import("@playwright/test").Page) {
  await page.goto("/admin/login");
  await expect(page.getByRole("heading", { name: "Sign In" })).toBeVisible();
  const selector = page.getByLabel("Test User — Local Validation");
  await expect(selector).toBeVisible();
  const option = page.locator("#dev-test-user option").filter({ hasText: "Olivia Mendoza" }).first();
  const value = await option.getAttribute("value");
  expect(value).toBeTruthy();
  await selector.selectOption(value!);
  await page.locator("#sign-in-password").fill(adminPassword);
  await page.getByRole("button", { name: "Sign In" }).click();
  await expect(page).toHaveURL(/\/admin$/);
}

async function openOrganizationSubscriptions(
  page: import("@playwright/test").Page,
  context: JoinedBootstrapContext,
) {
  await page.goto(`/admin/organizations/${context.organizationId}/subscription`);
  await expect(page.getByRole("heading", { name: "Subscription", level: 1 })).toBeVisible();
  await expect(page.getByText("Growth").first()).toBeVisible();
}

test.describe.configure({ mode: "serial" });

test.describe("PA-COM-07 Platform→POS commercial joined integration", () => {
  test.skip(!enabled, "Set PA_COM_07_JOINED=1 after Start-PaCom07MixedValidation.ps1 is healthy.");

  let context: JoinedBootstrapContext;
  let posDeviceScope: PosDeviceScope;
  let shiftId: string;

  test("provenance + strict commercial posture", async ({ request }) => {
    test.skip(!adminPassword, "LOCAL_VALIDATION_SHARED_PASSWORD is required.");
    const provenance = await readProvenance(request);
    expect(provenance.strictCommercialValidation).toBe("ON");
    expect(provenance.developmentGrantMerge).toBe("OFF");
    expect(provenance.posApiRuntimeSha.toLowerCase()).toContain("7e8256b2");
  });

  test("joined commercial spine: Growth devices, Pro upgrade, suspend, reactivate", async ({
    page,
    request,
  }) => {
    test.skip(!adminPassword, "LOCAL_VALIDATION_SHARED_PASSWORD is required.");

    context = await bootstrapGrowthOrganization();
    const intro = await introspectAccessToken(request, context.accessToken);
    expect(intro.active).toBe(true);
    expect(intro.productAccessAllowed).toBe(true);
    expect(intro.subscriptionStatus).toMatch(/Trialing|Active/i);
    expect(intro.enabledFeatureCodes ?? []).toContain("plan-max-active-pos-devices");
    expect(intro.mappedPosRoleCode).toBeTruthy();

    await loginPlatformAdmin(page);
    await openOrganizationSubscriptions(page, context);

    const capacityBefore = await readDeviceCapacity(
      request,
      context.organizationId,
      context.ownerSessionToken,
    );
    expect(capacityBefore.allowed).toBe(3);

    for (let index = 0; index < 3; index += 1) {
      const installationDeviceId = `install-${index}-${crypto.randomUUID()}`;
      const result = await registerDevice(
        request,
        context.organizationId,
        context.branchId,
        context.ownerSessionToken,
        installationDeviceId,
        `Device ${index + 1}`,
      );
      expect(result.ok, result.errorCode).toBe(true);
      if (index === 0) {
        posDeviceScope = {
          branchId: context.branchId,
          installationDeviceId,
        };
      }
    }

    const blocked = await registerDevice(
      request,
      context.organizationId,
      context.branchId,
      context.ownerSessionToken,
      `install-blocked-${crypto.randomUUID()}`,
      "Device blocked",
    );
    expect(blocked.ok).toBe(false);
    expect(blocked.errorCode).toBe("application.pos_device.capacity_exceeded");

    let accessToken = await issueProductAccessToken(
      request,
      context.ownerSessionToken,
      context.organizationId,
    );
    expect((await posGet(request, "/api/v1/pos/catalog/products", accessToken)).status).toBe(200);

    await page.getByRole("button", { name: "Change plan" }).click();
    await page.getByLabel("New plan").selectOption({ label: "Pro" });
    await page.getByRole("button", { name: "Upgrade plan" }).click();
    await expect(page.getByText("Plan upgraded.")).toBeVisible({ timeout: 30_000 });

    const capacityAfterUpgrade = await readDeviceCapacity(
      request,
      context.organizationId,
      context.ownerSessionToken,
    );
    expect(capacityAfterUpgrade.allowed).toBe(10);

    const fourthInstallationId = `install-fourth-${crypto.randomUUID()}`;
    const fourth = await registerDevice(
      request,
      context.organizationId,
      context.branchId,
      context.ownerSessionToken,
      fourthInstallationId,
      "Device 4",
    );
    expect(fourth.ok, fourth.errorCode).toBe(true);
    posDeviceScope = {
      branchId: context.branchId,
      installationDeviceId: fourthInstallationId,
    };

    accessToken = await issueProductAccessToken(
      request,
      context.ownerSessionToken,
      context.organizationId,
    );
    const productId = await createCatalogProduct(request, accessToken);
    shiftId = await ensureOpenShift(request, accessToken);
    expect((await checkoutSale(request, accessToken, productId, posDeviceScope, shiftId)).status).toBe(201);

    await page.getByRole("button", { name: "Suspend subscription" }).click();
    await page.getByRole("button", { name: "Suspend subscription", exact: true }).last().click();
    await expect(page.getByText("Subscription suspended.")).toBeVisible({ timeout: 30_000 });

    accessToken = await issueProductAccessToken(
      request,
      context.ownerSessionToken,
      context.organizationId,
    );
    const suspendedIntro = await introspectAccessToken(request, accessToken);
    expect(suspendedIntro.subscriptionStatus).toBe("Suspended");
    expect((await posGet(request, "/api/v1/pos/catalog/products", accessToken)).status).toBe(403);
    expect((await checkoutSale(request, accessToken, productId, posDeviceScope, shiftId)).status).toBe(403);

    await page.getByRole("button", { name: "Reactivate subscription" }).click();
    await page.getByRole("button", { name: "Reactivate subscription", exact: true }).last().click();
    await expect(page.getByText("Subscription reactivated.")).toBeVisible({ timeout: 30_000 });

    accessToken = await issueProductAccessToken(
      request,
      context.ownerSessionToken,
      context.organizationId,
    );
    const restoredIntro = await introspectAccessToken(request, accessToken);
    expect(restoredIntro.subscriptionStatus).toMatch(/Trialing|Active/i);
    expect((await posGet(request, "/api/v1/pos/catalog/products", accessToken)).status).toBe(200);
    shiftId = await ensureOpenShift(request, accessToken);
    expect((await checkoutSale(request, accessToken, productId, posDeviceScope, shiftId)).status).toBe(201);
  });
});
