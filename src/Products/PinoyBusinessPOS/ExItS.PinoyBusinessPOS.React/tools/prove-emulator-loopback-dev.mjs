/**
 * POS-EMU-DEV01 emulator loopback + offline PIN Web Crypto proof.
 * Requires: npm run dev on :5177, Platform API local validation, Android emulator with Chrome.
 */
/* eslint-disable no-undef */
import { chromium } from "playwright";
import { spawnSync } from "node:child_process";
import path from "node:path";
import os from "node:os";
import { ensureEmulatorPortForward } from "../scripts/emulator-port-forward.mjs";

const adb = path.join(
  os.homedir(),
  "AppData",
  "Local",
  "Android",
  "Sdk",
  "platform-tools",
  "adb.exe",
);
const username = "kizy@gmail.com";
const password = "1";
const signInUrl = "http://127.0.0.1:5177/sign-in";
const pinSetupUrl = "http://127.0.0.1:5177/offline-pin-setup";
const pin = String(600000 + Math.floor(Math.random() * 300000));

function runAdb(args) {
  const result = spawnSync(adb, args, { encoding: "utf8" });
  if (result.error) {
    throw result.error;
  }
  if (result.status !== 0) {
    throw new Error(result.stderr || result.stdout || `adb ${args.join(" ")} failed`);
  }
  return result.stdout.trim();
}

const forward = ensureEmulatorPortForward();
if (forward.reversed.length === 0) {
  throw new Error("No emulator received adb reverse.");
}

const devices = runAdb(["devices"])
  .split(/\r?\n/)
  .slice(1)
  .map((line) => line.split("\t")[0])
  .filter((id) => id.startsWith("emulator-"));
if (devices.length === 0) {
  throw new Error("No Android emulator device found.");
}
const device = devices[0];

runAdb(["-s", device, "forward", "tcp:9222", "localabstract:chrome_devtools_remote"]);
runAdb(["-s", device, "shell", "am", "start", "-a", "android.intent.action.VIEW", "-d", signInUrl]);

const browser = await chromium.connectOverCDP("http://127.0.0.1:9222");
const context = browser.contexts()[0];
let page =
  context.pages().find((candidate) => candidate.url().includes("127.0.0.1:5177/sign-in")) ??
  context.pages().find((candidate) => candidate.url().includes("127.0.0.1:5177")) ??
  context.pages()[0];
if (!page) {
  page = await context.newPage();
}

await page.goto(signInUrl, { waitUntil: "domcontentloaded", timeout: 30000 }).catch(() => undefined);

const onSignIn = await page
  .locator('[data-testid="sign-in-page"]')
  .isVisible({ timeout: 5000 })
  .catch(() => false);

if (onSignIn) {
  await page.getByLabel("Email or staff login").fill(username);
  await page.getByLabel("Password").fill(password);
  await page.getByTestId("sign-in-submit").click();
  await page.waitForURL(/\/offline-pin-setup|\/workspace/, { timeout: 60000 });
} else if (!page.url().includes("127.0.0.1:5177")) {
  await page.goto(signInUrl, { waitUntil: "networkidle", timeout: 60000 });
}

const loopback = await page.evaluate(() => ({
  href: window.location.href,
  isSecureContext: window.isSecureContext,
  hasSubtle: typeof crypto !== "undefined" && crypto.subtle !== undefined,
  origin: window.location.origin,
}));

if (!loopback.origin.includes("127.0.0.1:5177")) {
  throw new Error(`Unexpected emulator origin: ${loopback.origin}`);
}
if (!loopback.isSecureContext || !loopback.hasSubtle) {
  throw new Error(
    `Loopback secure context missing: secure=${loopback.isSecureContext} subtle=${loopback.hasSubtle}`,
  );
}

console.log("ACTUAL_EMULATOR_LOOPBACK=PASS");
console.log("ACTUAL_EMULATOR_ORIGIN=" + loopback.origin);
console.log("ACTUAL_EMULATOR_SECURE_CONTEXT=YES");
console.log("ACTUAL_EMULATOR_CRYPTO_SUBTLE=YES");

if (!page.url().includes("/offline-pin-setup")) {
  await page.goto(pinSetupUrl, { waitUntil: "networkidle", timeout: 60000 });
}

await page.waitForSelector('[data-testid="offline-pin-setup-page"]', { timeout: 60000 });

const pinPage = await page.evaluate(() => ({
  isSecureContext: window.isSecureContext,
  hasSubtle: typeof crypto !== "undefined" && crypto.subtle !== undefined,
}));

if (!pinPage.isSecureContext || !pinPage.hasSubtle) {
  throw new Error("Offline PIN page lost secure Web Crypto context.");
}

console.log("OFFLINE_PIN_WEBCRYPTO=PASS");

const submit = page.getByTestId("offline-pin-enroll-submit");
await page.getByTestId("offline-pin-enroll-input").fill(pin);
await page.getByTestId("offline-pin-enroll-confirm").fill(pin);

if (!(await submit.isEnabled())) {
  throw new Error("Save offline PIN remained disabled after valid PIN input.");
}

await submit.click();
await page.waitForURL((url) => !url.pathname.includes("/offline-pin-setup"), { timeout: 60000 });

console.log("ACTUAL_EMULATOR_PIN_ENROLL=PASS");

await browser.close();
