import { defineConfig, devices } from "@playwright/test";

export default defineConfig({
  testDir: "./e2e",
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  reporter: "list",
  use: {
    baseURL: "http://127.0.0.1:4176",
    trace: "on-first-retry",
  },
  webServer: [
    {
      command: "npm run build && npm run preview",
      url: "http://127.0.0.1:4176",
      reuseExistingServer: !process.env.CI,
      timeout: 120_000,
    },
    {
      command: "npx vite --host 127.0.0.1 --port 5176 --strictPort",
      url: "http://127.0.0.1:5176",
      reuseExistingServer: !process.env.CI,
      timeout: 120_000,
    },
  ],
  projects: [{ name: "chromium", use: { ...devices["Desktop Chrome"] } }],
});
