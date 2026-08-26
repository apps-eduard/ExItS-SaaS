import { defineConfig, devices } from "@playwright/test";

export default defineConfig({
  testDir: "./e2e",
  testMatch: "platform-pos-commercial-joined.spec.ts",
  fullyParallel: false,
  workers: 1,
  timeout: 180_000,
  expect: { timeout: 30_000 },
  retries: 0,
  reporter: "list",
  use: {
    baseURL: process.env.PA_COM_07_ADMIN_BASE_URL ?? "http://127.0.0.1:8095",
    trace: "on-first-retry",
  },
  projects: [
    {
      name: "chromium",
      use: { ...devices["Desktop Chrome"] },
    },
  ],
});
