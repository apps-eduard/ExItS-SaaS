import path from "node:path";
import { fileURLToPath } from "node:url";
import tailwindcss from "@tailwindcss/vite";
import react from "@vitejs/plugin-react";
import { VitePWA } from "vite-plugin-pwa";
import { defineConfig } from "vitest/config";
import { createPlatformApiProxy } from "./vite.platform-api-proxy";
import { createPwaManifest } from "./src/pwa/pwa-manifest";

const rootDir = path.dirname(fileURLToPath(import.meta.url));

export default defineConfig({
  plugins: [
    react(),
    tailwindcss(),
    VitePWA({
      registerType: "prompt",
      injectRegister: false,
      filename: "sw.js",
      includeAssets: [
        "icon-192.png",
        "icon-512.png",
        "icon-192-maskable.png",
        "icon-512-maskable.png",
      ],
      manifest: createPwaManifest(),
      devOptions: {
        enabled: false,
      },
      workbox: {
        globPatterns: ["**/*.{js,css,html,ico,png,svg,webmanifest,woff,woff2}"],
        navigateFallback: "index.html",
        navigateFallbackDenylist: [
          /^\/api\//,
          /\/platform-api\//,
          /^\/activate-account/,
          /^\/reset-password/,
        ],
        runtimeCaching: [
          {
            urlPattern: ({ url }) =>
              url.pathname.startsWith("/api/") ||
              url.pathname.includes("/platform-api/") ||
              url.pathname === "/activate-account" ||
              url.pathname === "/reset-password" ||
              /\/(auth|session)\//i.test(url.pathname),
            handler: "NetworkOnly",
          },
          {
            urlPattern: /^https:\/\/fonts\.(?:googleapis|gstatic)\.com\/.*/i,
            handler: "CacheFirst",
            options: {
              cacheName: "plm-static-fonts",
              expiration: {
                maxEntries: 16,
                maxAgeSeconds: 60 * 60 * 24 * 365,
              },
            },
          },
        ],
      },
    }),
  ],
  resolve: {
    alias: {
      "@": path.resolve(rootDir, "src"),
    },
  },
  server: {
    port: 5176,
    strictPort: true,
    proxy: createPlatformApiProxy(),
  },
  preview: {
    port: 4176,
    strictPort: true,
    host: "127.0.0.1",
    proxy: createPlatformApiProxy(),
  },
  test: {
    environment: "jsdom",
    setupFiles: ["./src/test/setup.ts"],
    include: ["src/**/*.test.ts", "src/**/*.test.tsx", "vite.platform-api-proxy.test.ts"],
  },
});
