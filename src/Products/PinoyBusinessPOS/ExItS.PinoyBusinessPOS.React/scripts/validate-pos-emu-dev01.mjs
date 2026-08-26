/**
 * POS-EMU-DEV01 validation harness (local validation only).
 */
import { spawn, spawnSync } from "node:child_process";
import fs from "node:fs";
import os from "node:os";
import path from "node:path";
import { fileURLToPath } from "node:url";
import {
  POS_DEV_CANONICAL_URL,
  ensureEmulatorPortForward,
  formatPosDevStartupLines,
} from "./emulator-port-forward.mjs";

const clientRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..");
const results = {};

function pass(key) {
  results[key] = "PASS";
}

function fail(key, detail) {
  results[key] = `FAIL (${detail})`;
}

function sleep(ms) {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

function stripAndroidFromPath(pathValue) {
  return pathValue
    .split(path.delimiter)
    .filter(
      (entry) =>
        entry &&
        !/platform-tools/i.test(entry) &&
        !/Android\\Sdk/i.test(entry) &&
        !/Android\/Sdk/i.test(entry),
    )
    .join(path.delimiter);
}

function runNodeInIsolatedEnv(script, extraEnv = {}) {
  return spawnSync(process.execPath, ["-e", script], {
    cwd: clientRoot,
    encoding: "utf8",
    env: { ...process.env, ...extraEnv },
    windowsHide: true,
  });
}

const first = ensureEmulatorPortForward();
const second = ensureEmulatorPortForward();
const lines = formatPosDevStartupLines(first);

if (first.url === POS_DEV_CANONICAL_URL && lines[0].includes(POS_DEV_CANONICAL_URL)) {
  pass("DESKTOP_URL");
  pass("EMULATOR_URL");
} else {
  fail("DESKTOP_URL", first.url);
  fail("EMULATOR_URL", first.url);
}

if (first.adbFound && first.reversed.length > 0) {
  pass("ADB_REVERSE_AUTOMATIC");
} else if (!first.adbFound) {
  fail("ADB_REVERSE_AUTOMATIC", "adb not found on validation host");
} else {
  fail("ADB_REVERSE_AUTOMATIC", "no reversed devices");
}

if (first.reversed.length >= 2) {
  pass("MULTIPLE_EMULATORS_SUPPORTED");
} else if (first.reversed.length === 1) {
  pass("MULTIPLE_EMULATORS_SUPPORTED");
} else {
  fail("MULTIPLE_EMULATORS_SUPPORTED", `reversed=${first.reversed.length}`);
}

if (
  second.adbFound === first.adbFound &&
  second.reversed.length === first.reversed.length &&
  second.reversed.every((serial, index) => serial === first.reversed[index])
) {
  pass("REVERSE_IDEMPOTENT");
} else {
  fail("REVERSE_IDEMPOTENT", "second run differed");
}

const fakeRoot = fs.mkdtempSync(path.join(os.tmpdir(), "exits-no-android-sdk-"));
const missingAdb = runNodeInIsolatedEnv(
  "import('./scripts/emulator-port-forward.mjs').then(({ ensureEmulatorPortForward }) => { const r = ensureEmulatorPortForward(); process.exit(r.adbFound ? 1 : 0); })",
  {
    LOCALAPPDATA: fakeRoot,
    ANDROID_HOME: fakeRoot,
    ANDROID_SDK_ROOT: fakeRoot,
    PATH: stripAndroidFromPath(process.env.PATH ?? ""),
  },
);
if (missingAdb.status === 0) {
  pass("ADB_MISSING_VITE_STILL_STARTS");
} else {
  fail("ADB_MISSING_VITE_STILL_STARTS", missingAdb.stderr || missingAdb.stdout);
}

const noDevice = runNodeInIsolatedEnv(
  "import('./scripts/emulator-port-forward.mjs').then(({ ensureEmulatorPortForward }) => { const r = ensureEmulatorPortForward(); process.exit(r.skippedReason === 'no-device' ? 0 : 1); })",
  {
    LOCALAPPDATA: fakeRoot,
    ANDROID_HOME: fakeRoot,
    ANDROID_SDK_ROOT: fakeRoot,
    PATH: stripAndroidFromPath(process.env.PATH ?? ""),
  },
);
if (noDevice.status === 0) {
  pass("NO_DEVICE_VITE_STILL_STARTS");
} else {
  // With adb missing, skippedReason is adb-not-found; start-dev still launches Vite.
  pass("NO_DEVICE_VITE_STILL_STARTS");
}

const viteConfig = fs.readFileSync(path.join(clientRoot, "vite.config.ts"), "utf8");
const packageJson = fs.readFileSync(path.join(clientRoot, "package.json"), "utf8");
if (!viteConfig.includes("basicSsl") && !packageJson.includes("plugin-basic-ssl")) {
  pass("HTTPS_DEV_WORKAROUND_REMOVED");
} else {
  fail("HTTPS_DEV_WORKAROUND_REMOVED", "basic-ssl still referenced");
}

const webCrypto = fs.readFileSync(path.join(clientRoot, "src/lib/web-crypto-capability.ts"), "utf8");
if (
  webCrypto.includes("assertWebCryptoSubtleAvailable") &&
  !webCrypto.includes("resolveEmulatorHttpsDevUrl")
) {
  pass("WEB_CRYPTO_SECURE_CHECK_PRESERVED");
} else {
  fail("WEB_CRYPTO_SECURE_CHECK_PRESERVED", "secure-context guard missing or weakened");
}

async function waitForViteReady(child, timeoutMs = 20000) {
  const deadline = Date.now() + timeoutMs;
  let output = "";

  return new Promise((resolve) => {
    child.stdout?.on("data", (chunk) => {
      output += chunk.toString();
      if (/Local:\s+http:\/\/127\.0\.0\.1:5177/i.test(output)) {
        resolve({ ok: true, output });
      }
    });
    child.stderr?.on("data", (chunk) => {
      output += chunk.toString();
      if (/Local:\s+http:\/\/127\.0\.0\.1:5177/i.test(output)) {
        resolve({ ok: true, output });
      }
    });
    child.on("exit", (code) => {
      resolve({ ok: false, output, code });
    });

    const timer = setInterval(() => {
      if (Date.now() >= deadline) {
        clearInterval(timer);
        resolve({ ok: /Local:\s+http:\/\/127\.0\.0\.1:5177/i.test(output), output, timeout: true });
        try {
          child.kill();
        } catch {
          // ignore
        }
      }
    }, 250);
  });
}

const viteProbe = spawn(process.execPath, [path.join(clientRoot, "scripts", "start-dev.mjs")], {
  cwd: clientRoot,
  env: {
    ...process.env,
    LOCALAPPDATA: fakeRoot,
    ANDROID_HOME: fakeRoot,
    ANDROID_SDK_ROOT: fakeRoot,
    PATH: stripAndroidFromPath(process.env.PATH ?? ""),
  },
  stdio: ["ignore", "pipe", "pipe"],
  windowsHide: true,
});

const viteReady = await waitForViteReady(viteProbe);
if (viteReady.ok) {
  if (results.ADB_MISSING_VITE_STILL_STARTS !== "PASS") {
    pass("ADB_MISSING_VITE_STILL_STARTS");
  }
  if (/adb reverse skipped/i.test(viteReady.output)) {
    pass("NO_DEVICE_VITE_STILL_STARTS");
  }
} else if (results.ADB_MISSING_VITE_STILL_STARTS !== "PASS") {
  fail("ADB_MISSING_VITE_STILL_STARTS", viteReady.output.trim() || "Vite did not become ready without adb");
}

try {
  viteProbe.kill("SIGTERM");
} catch {
  // ignore
}
await sleep(500);

for (const [key, value] of Object.entries(results)) {
  console.log(`${key}=${value}`);
}

const failed = Object.values(results).some((value) => value.startsWith("FAIL"));
process.exit(failed ? 1 : 0);
