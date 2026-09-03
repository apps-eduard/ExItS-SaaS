import { test, expect } from "@playwright/test";

const publicPaths = [
  "/",
  "/products",
  "/pos",
  "/service-pro",
  "/pricing",
  "/about",
  "/contact",
  "/privacy",
  "/terms",
];

test("sitemap.xml includes all public routes and excludes TBD product routes", async ({
  request,
}) => {
  const response = await request.get("/sitemap.xml");
  expect(response.ok()).toBeTruthy();
  const xml = await response.text();

  for (const path of publicPaths) {
    const url =
      path === "/" ? "https://exits.ph" : `https://exits.ph${path}`;
    expect(xml).toContain(`<loc>${url}</loc>`);
  }

  expect(xml).not.toContain("/loan-manager");
  expect(xml).not.toContain("/bnpl");
  expect(xml).not.toContain("/pawn-manager");
  expect(xml).toMatch(/<priority>0\.2<\/priority>/);
});

test("robots.txt allows indexing and points to sitemap", async ({ request }) => {
  const response = await request.get("/robots.txt");
  expect(response.ok()).toBeTruthy();
  const body = await response.text();
  expect(body).toContain("User-Agent: *");
  expect(body).toContain("Allow: /");
  expect(body).toContain("Sitemap: https://exits.ph/sitemap.xml");
});

test("OG image routes render PNG for each public page", async ({ request }) => {
  const ogNames = [
    "exits-og-home.png",
    "exits-og-pos.png",
    "products-og.png",
    "service-pro-og.png",
    "pricing-og.png",
    "about-og.png",
    "contact-og.png",
    "privacy-og.png",
    "terms-og.png",
    "default-og.png",
  ];

  for (const name of ogNames) {
    const response = await request.get(`/og/${name}`);
    expect(response.ok(), name).toBeTruthy();
    expect(response.headers()["content-type"] ?? "").toContain("image/");
  }
});

test("primary pages expose canonical and FAQ/organization structured data where expected", async ({
  page,
}) => {
  await page.goto("/");
  await expect(page.locator('link[rel="canonical"]')).toHaveAttribute(
    "href",
    "https://exits.ph",
  );
  const homeLd = await page.locator('script[type="application/ld+json"]').allTextContents();
  expect(homeLd.some((block) => block.includes('"Organization"'))).toBe(true);
  expect(homeLd.some((block) => block.includes('"FAQPage"'))).toBe(true);

  await page.goto("/pos");
  const posLd = await page.locator('script[type="application/ld+json"]').allTextContents();
  expect(posLd.some((block) => block.includes('"SoftwareApplication"'))).toBe(true);
  expect(posLd.some((block) => block.includes('"FAQPage"'))).toBe(true);
  expect(posLd.join("\n")).not.toContain('"price"');

  await page.goto("/pricing");
  const pricingLd = await page.locator('script[type="application/ld+json"]').allTextContents();
  expect(pricingLd.some((block) => block.includes('"FAQPage"'))).toBe(true);
  expect(pricingLd.join("\n")).not.toContain('"Offer"');

  await page.goto("/service-pro");
  const serviceLd = await page.locator('script[type="application/ld+json"]').allTextContents();
  expect(serviceLd.some((block) => block.includes('"SoftwareApplication"'))).toBe(false);
});
