import { defineConfig, devices } from "@playwright/test";

const clientRoot = import.meta.dirname;

export default defineConfig({
  testDir: "./e2e",
  testMatch: [
    "**/workspace-live-runtime.spec.ts",
    "**/pos-branch-fulfillment-ui-closure-01.spec.ts",
  ],
  fullyParallel: false,
  forbidOnly: !!process.env.CI,
  retries: 0,
  reporter: "list",
  timeout: 120_000,
  use: {
    baseURL: "http://127.0.0.1:5177",
    trace: "on-first-retry",
  },
  webServer: {
    cwd: clientRoot,
    command: "npm run dev",
    url: "http://127.0.0.1:5177",
    reuseExistingServer: true,
    timeout: 120_000,
  },
  projects: [{ name: "chromium", use: { ...devices["Desktop Chrome"] } }],
});
