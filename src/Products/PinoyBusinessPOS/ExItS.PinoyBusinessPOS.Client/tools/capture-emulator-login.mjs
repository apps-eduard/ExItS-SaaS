/**
 * Captures actual Android emulator Chrome login API response via DevTools (not Host-header simulation).
 * Never logs password or cookie/token values.
 */
/* eslint-disable no-undef */
import { chromium } from "playwright";
import { spawnSync } from "node:child_process";
import path from "node:path";
import os from "node:os";

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
const loginPath = "/platform-api/api/v1/platform/auth/login";

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
const context = browser.contexts()[0] ?? (await browser.newContext());
let page =
  context.pages().find((candidate) => candidate.url().includes("127.0.0.1:5177")) ??
  context.pages()[0];
if (!page) {
  page = await context.newPage();
  await page.goto(signInUrl, { waitUntil: "domcontentloaded", timeout: 30000 });
}

await page.goto(signInUrl, { waitUntil: "domcontentloaded", timeout: 30000 }).catch(() => undefined);
await page.waitForSelector('[data-testid="sign-in-page"]', { timeout: 30000 });

const evaluated = await page.evaluate(
  async ({ usernameOrEmail, passwordValue, apiPath }) => {
    const response = await fetch(apiPath, {
      method: "POST",
      credentials: "include",
      headers: {
        Accept: "application/json",
        "Content-Type": "application/json",
        "X-Correlation-Id": "emulator-login-capture-01",
      },
      body: JSON.stringify({ usernameOrEmail, password: passwordValue }),
    });
    const text = await response.text();
    let problem = {};
    try {
      problem = JSON.parse(text);
    } catch {
      problem = { detail: text.slice(0, 240) };
    }
    return {
      status: response.status,
      errorCode: problem.errorCode ?? problem.ErrorCode ?? null,
      traceId: problem.traceId ?? problem.TraceId ?? null,
      detail: problem.detail ?? problem.title ?? null,
      origin: window.location.origin,
    };
  },
  {
    usernameOrEmail: username,
    passwordValue: password,
    apiPath: loginPath,
  },
);

const capture = {
  status: evaluated.status,
  errorCode: evaluated.errorCode,
  traceId: evaluated.traceId,
  detail: evaluated.detail,
  correlationId: "emulator-login-capture-01",
};

await browser.close();

if (capture.status === null) {
  throw new Error("Timed out waiting for emulator login response.");
}

console.log("ACTUAL_EMULATOR_LOGIN_STATUS=" + capture.status);
console.log("ACTUAL_EMULATOR_ERROR_CODE=" + (capture.errorCode ?? "(none)"));
console.log("ACTUAL_EMULATOR_TRACE_ID_PRESENT=" + (capture.traceId ? "YES" : "NO"));
console.log(
  "ACTUAL_EMULATOR_CORRELATION_ID_PRESENT=" + (capture.correlationId ? "YES" : "NO"),
);
console.log("ACTUAL_EMULATOR_ERROR_DETAIL=" + (capture.detail ?? "(none)"));
