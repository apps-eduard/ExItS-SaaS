import path from "node:path";
import { fileURLToPath } from "node:url";
import tailwindcss from "@tailwindcss/vite";
import react from "@vitejs/plugin-react";
import { VitePWA } from "vite-plugin-pwa";
import { defineConfig } from "vitest/config";
import { createPlatformApiProxy } from "./vite.platform-api-proxy";
import { createPosApiProxy } from "./vite.pos-api-proxy";
import { blockServiceWorkerScriptsInDev } from "./vite.block-sw-in-dev";
import { createPwaManifest } from "./src/pwa/pwa-manifest";

const rootDir = path.dirname(fileURLToPath(import.meta.url));

export default defineConfig({
  plugins: [
    react(),
    tailwindcss(),
    blockServiceWorkerScriptsInDev(),
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
        maximumFileSizeToCacheInBytes: 3 * 1024 * 1024,
        globPatterns: ["**/*.{js,css,html,ico,png,svg,webmanifest,woff,woff2}"],
        navigateFallback: "index.html",
        navigateFallbackDenylist: [/^\/api\//, /\/platform-api\//, /\/pos-api\//],
        runtimeCaching: [
          {
            urlPattern: ({ url }) =>
              url.pathname.startsWith("/api/") ||
              url.pathname.includes("/platform-api/") ||
              url.pathname.includes("/pos-api/") ||
              /\/(auth|session)\//i.test(url.pathname),
            handler: "NetworkOnly",
          },
          {
            urlPattern: /^https:\/\/fonts\.(?:googleapis|gstatic)\.com\/.*/i,
            handler: "CacheFirst",
            options: {
              cacheName: "pos-static-fonts",
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
    host: "127.0.0.1",
    port: 5177,
    strictPort: true,
    // Android emulator reaches the host loopback as 10.0.2.2; allow that Host header.
    // POS_DEV_PUBLIC_HOST adds Tailscale/LAN PublicHost for Local Validation.
    allowedHosts: [
      "127.0.0.1",
      "localhost",
      "10.0.2.2",
      ...(process.env.POS_DEV_PUBLIC_HOST
        ? [process.env.POS_DEV_PUBLIC_HOST.trim()].filter(Boolean)
        : []),
    ],
    proxy: {
      ...createPlatformApiProxy(),
      ...createPosApiProxy(),
    },
  },
  preview: {
    host: "127.0.0.1",
    port: 4177,
    strictPort: true,
    allowedHosts: [
      "127.0.0.1",
      "localhost",
      "10.0.2.2",
      ...(process.env.POS_DEV_PUBLIC_HOST
        ? [process.env.POS_DEV_PUBLIC_HOST.trim()].filter(Boolean)
        : []),
    ],
    proxy: {
      ...createPlatformApiProxy(),
      ...createPosApiProxy(),
    },
  },
  test: {
    environment: "jsdom",
    setupFiles: ["./src/test/setup.ts"],
    fileParallelism: false,
    include: [
      "src/**/*.test.ts",
      "src/**/*.test.tsx",
      "vite.platform-api-proxy.test.ts",
      "vite.pos-api-proxy.test.ts",
      "vite.proxy-cookie.test.ts",
      "vite.block-sw-in-dev.test.ts",
      "scripts/emulator-port-forward.test.mjs",
    ],
  },
});
