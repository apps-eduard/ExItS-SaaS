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
if (manifest.name !== "Pinoy Business POS") {
  fail("Manifest name must be Pinoy Business POS.");
}
if (manifest.short_name !== "ExItS POS") {
  fail("Manifest short_name must be ExItS POS.");
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
const apiHandler = sw.match(
  /startsWith\("\/api\/"\)[\s\S]{0,180}?new e\.(NetworkOnly|CacheFirst|StaleWhileRevalidate)/,
);
if (!apiHandler || apiHandler[1] !== "NetworkOnly") {
  fail("Service worker must route /api/ with NetworkOnly.");
}
if (/BackgroundSyncPlugin|workbox-background-sync/.test(sw)) {
  fail("Background Sync is prohibited.");
}
if (/IndexedDB|idbKeyval|LocalStore|OPFS|sqlite/i.test(sw)) {
  fail("Financial browser stores are prohibited in the service worker.");
}
if (!/\/\(auth\|session\)\//.test(sw) && !/auth[\s\S]{0,160}NetworkOnly/.test(sw)) {
  fail("Service worker must keep auth/session traffic NetworkOnly.");
}
if (
  /registerRoute[\s\S]{0,240}(?:product-access|organizations|sales|payments)[\s\S]{0,80}(?:CacheFirst|StaleWhileRevalidate)/.test(
    sw,
  )
) {
  fail("Business API responses must not use a runtime cache.");
}
if (!/platform-api/.test(sw)) {
  fail("Service worker must mention /platform-api NetworkOnly routing.");
}
if (
  !/includes\("\/platform-api\/"\)[\s\S]{0,200}NetworkOnly/.test(sw) &&
  !/platform-api[\s\S]{0,200}NetworkOnly/.test(sw)
) {
  fail("Service worker must route /platform-api/ with NetworkOnly.");
}
if (!/pos-api/.test(sw)) {
  fail("Service worker must mention /pos-api NetworkOnly routing.");
}
if (
  !/includes\("\/pos-api\/"\)[\s\S]{0,200}NetworkOnly/.test(sw) &&
  !/pos-api[\s\S]{0,200}NetworkOnly/.test(sw)
) {
  fail("Service worker must route /pos-api/ with NetworkOnly.");
}

process.stdout.write("PWA validation passed.\n");
