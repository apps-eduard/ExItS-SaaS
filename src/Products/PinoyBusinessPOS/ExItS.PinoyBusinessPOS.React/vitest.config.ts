import path from "node:path";
import { fileURLToPath } from "node:url";
import { defineConfig } from "vitest/config";
import react from "@vitejs/plugin-react";

const rootDir = path.dirname(fileURLToPath(import.meta.url));

export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: {
      "@": path.resolve(rootDir, "./src"),
      "virtual:pwa-register": path.resolve(rootDir, "./src/pwa/register-sw.mock.ts"),
      "virtual:pwa-register/react": path.resolve(rootDir, "./src/pwa/register-sw.mock.ts"),
    },
  },
  test: {
    environment: "jsdom",
    globals: false,
    setupFiles: ["./src/test/setup.ts"],
    fileParallelism: false,
    include: [
      "src/**/*.test.ts",
      "src/**/*.test.tsx",
      "vite.platform-api-proxy.test.ts",
      "vite.pos-api-proxy.test.ts",
      "vite.proxy-cookie.test.ts",
      "vite.block-sw-in-dev.test.ts",
      "vite.dev-port-health.test.ts",
      "scripts/emulator-port-forward.test.mjs",
    ],
  },
});
