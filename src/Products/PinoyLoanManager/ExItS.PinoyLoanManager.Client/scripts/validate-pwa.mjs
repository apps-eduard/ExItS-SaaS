import { existsSync, readFileSync } from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const rootDir = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..");
const distDir = path.join(rootDir, "dist");

function fail(message) {
  process.stderr.write(`${message}\n`);
  process.exit(1);
}

function read(fileName) {
  try {
    return readFileSync(path.join(distDir, fileName), "utf8");
  } catch {
    fail(`Missing ${fileName}. Run npm run build before PWA validation.`);
  }
}

const manifest = JSON.parse(read("manifest.webmanifest"));
if (manifest.name !== "Pinoy Loan Manager") {
  fail("Manifest name must be Pinoy Loan Manager.");
}
if (manifest.short_name !== "PinoyLoan") {
  fail("Manifest short_name must be PinoyLoan.");
}
if (manifest.start_url !== "/") {
  fail("Manifest start_url must be /.");
}
if (manifest.display !== "standalone") {
  fail("Manifest display must be standalone.");
}
if (manifest.theme_color !== "#166534") {
  fail("Manifest theme_color must be #166534.");
}

for (const icon of manifest.icons ?? []) {
  const iconPath = path.join(distDir, String(icon.src).replace(/^\//, ""));
  if (!existsSync(iconPath)) {
    fail(`Missing built icon ${icon.src}.`);
  }
}

const sw = read("sw.js");
if (!sw.includes("NetworkOnly")) {
  fail("Service worker must use NetworkOnly for API traffic.");
}
if (!sw.includes("/api/")) {
  fail("Service worker must mention /api/ NetworkOnly routing.");
}
if (/BackgroundSyncPlugin|workbox-background-sync/.test(sw)) {
  fail("Background Sync is prohibited.");
}
if (!/platform-api/.test(sw)) {
  fail("Service worker must mention /platform-api NetworkOnly routing.");
}
if (!/startsWith\("\/api\/"\)[\s\S]{0,200}NetworkOnly/.test(sw)) {
  fail("Service worker must route /api/ with NetworkOnly.");
}
if (
  !/includes\("\/platform-api\/"\)[\s\S]{0,200}NetworkOnly/.test(sw) &&
  !/platform-api[\s\S]{0,200}NetworkOnly/.test(sw)
) {
  fail("Service worker must route /platform-api/ with NetworkOnly.");
}
if (/startsWith\("\/api\/"\)[\s\S]{0,160}(?:CacheFirst|StaleWhileRevalidate)/.test(sw)) {
  fail("API CacheFirst/StaleWhileRevalidate is prohibited.");
}
if (/IndexedDB|idbKeyval|LocalStore/.test(sw)) {
  fail("Financial/auth IndexedDB stores are prohibited in the service worker.");
}
if (!/\/\(auth\|session\)\//.test(sw) && !/auth[\s\S]{0,160}NetworkOnly/.test(sw)) {
  fail("Service worker must keep auth/session traffic NetworkOnly.");
}
if (!/activate-account/.test(sw) || !/reset-password/.test(sw)) {
  fail("Service worker must exclude activation/reset navigation from stale fallback.");
}
if (
  /registerRoute[\s\S]{0,240}(?:product-access|organizations)[\s\S]{0,80}(?:CacheFirst|StaleWhileRevalidate)/.test(
    sw,
  )
) {
  fail("Organization and product-access responses must not use a runtime cache.");
}

process.stdout.write("PWA validation passed.\n");
