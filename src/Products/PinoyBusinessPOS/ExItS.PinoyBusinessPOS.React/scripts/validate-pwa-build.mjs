import { existsSync, readdirSync, readFileSync } from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const clientRoot = path.dirname(path.dirname(fileURLToPath(import.meta.url)));
const distDir = path.join(clientRoot, "dist");

function fail(message) {
  process.stderr.write(`${message}\n`);
  process.exit(1);
}

function read(relativePath) {
  const absolute = path.join(distDir, relativePath);
  if (!existsSync(absolute)) {
    fail(`Missing ${relativePath} in dist.`);
  }
  return readFileSync(absolute);
}

if (!existsSync(distDir)) {
  fail("dist/ is missing. Run npm run build before npm run test:pwa.");
}

const manifest = JSON.parse(read("manifest.webmanifest").toString("utf8"));
if (manifest.name !== "Pinoy Business POS" || manifest.short_name !== "ExItS POS") {
  fail("POS manifest app identity is incorrect.");
}
if (manifest.display !== "standalone") {
  fail("Manifest display must be standalone.");
}
if (manifest.start_url !== "/") {
  fail("POS manifest start_url must be /.");
}
const personalManifest = JSON.parse(read("manifest-personal.webmanifest").toString("utf8"));
if (personalManifest.name !== "ExItS" || personalManifest.short_name !== "ExItS") {
  fail("Personal manifest app identity is incorrect.");
}
if (personalManifest.start_url !== "/personal" || personalManifest.scope !== "/" || personalManifest.display !== "standalone") {
  fail("Personal manifest start identity is incorrect.");
}

const requiredIcons = [
  "icons/icon-192.png",
  "icons/icon-512.png",
  "icons/icon-192-maskable.png",
  "icons/icon-512-maskable.png",
];
for (const icon of requiredIcons) {
  if (!existsSync(path.join(distDir, icon))) {
    fail(`Missing icon ${icon}.`);
  }
}

const serviceWorker = [
  read("sw.js").toString("utf8"),
  ...readdirSync(distDir)
    .filter((name) => name.startsWith("workbox-") && name.endsWith(".js"))
    .map((name) => read(name).toString("utf8")),
].join("\n");
if (
  !serviceWorker.includes("precacheAndRoute") &&
  !serviceWorker.includes("createHandlerBoundToURL")
) {
  fail("Production service worker does not precache a static app shell.");
}
if (!serviceWorker.includes("NetworkOnly")) {
  fail("Production service worker must keep API traffic NetworkOnly.");
}
if (!/\\\/api\\\//.test(serviceWorker) && !serviceWorker.includes("/api/")) {
  fail("Production service worker must exclude /api/ from runtime cache.");
}
if (!serviceWorker.includes("platform-api")) {
  fail("Production service worker must keep /platform-api traffic NetworkOnly.");
}
if (!serviceWorker.includes("/pos-api/") || !serviceWorker.includes("(auth|session)")) {
  fail("Production service worker must keep /pos-api and auth/session traffic NetworkOnly.");
}
if (/BackgroundSyncPlugin|workbox-background-sync/.test(serviceWorker)) {
  fail("Service worker must not register a Background Sync financial queue.");
}
if (/CacheFirst[\s\S]{0,180}\/api\/|\/api\/[\s\S]{0,180}CacheFirst/.test(serviceWorker)) {
  fail("Service worker must not use a cache-first strategy for /api/.");
}

const hashedAsset = /assets\/index-[A-Za-z0-9_-]+\.(js|css)/.test(serviceWorker);
if (!hashedAsset) {
  fail("Service worker precache must include hashed JS/CSS assets.");
}

process.stdout.write("PWA build validation passed.\n");
