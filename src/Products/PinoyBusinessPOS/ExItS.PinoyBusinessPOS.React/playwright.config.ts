import { defineConfig, devices } from "@playwright/test";
import { execSync } from "node:child_process";

const clientRoot = import.meta.dirname;
const prepareProductionEnv =
  process.platform === "win32"
    ? "copy /Y .env.production.example .env.production.local >nul"
    : "cp .env.production.example .env.production.local";

function resolveE2eBuildSha(): string {
  const fromEnv = process.env.VITE_POS_BUILD_SHA?.trim();
  if (fromEnv) {
    return fromEnv;
  }
  try {
    return execSync("git rev-parse HEAD", {
      cwd: clientRoot,
      encoding: "utf8",
      stdio: ["ignore", "pipe", "ignore"],
    }).trim();
  } catch {
    return "unknown";
  }
}

export default defineConfig({
  testDir: "./e2e",
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  reporter: "list",
  use: {
    baseURL: "http://127.0.0.1:4177",
    trace: "on-first-retry",
  },
  webServer: {
    cwd: clientRoot,
    // Skip `tsc -b` (unrelated WIP). Tolerate vite-plugin-pwa precache warnings that exit non-zero when dist is still produced.
    command:
      process.platform === "win32"
        ? `${prepareProductionEnv} && npx vite build & if not exist dist\\index.html exit /b 1 & npm run preview`
        : `${prepareProductionEnv} && (npx vite build || true) && test -f dist/index.html && npm run preview`,
    url: "http://127.0.0.1:4177",
    reuseExistingServer: !process.env.CI,
    timeout: 180_000,
    // Explicit SHA so production preview builds stay verifiable without baking git into default prod assets.
    env: {
      ...process.env,
      VITE_POS_BUILD_SHA: resolveE2eBuildSha(),
    },
  },
  projects: [{ name: "chromium", use: { ...devices["Desktop Chrome"] } }],
});
