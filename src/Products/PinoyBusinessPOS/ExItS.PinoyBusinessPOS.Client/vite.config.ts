import path from "node:path";
import { fileURLToPath } from "node:url";
import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import tailwindcss from "@tailwindcss/vite";
import { VitePWA } from "vite-plugin-pwa";
import {
  createPwaManifest,
  PWA_API_PATH_PATTERN,
  PWA_API_PORT_PATTERN,
  PWA_ICON_FILES,
  PWA_NETWORK_ONLY_METHODS,
} from "./src/pwa/pwa-manifest.ts";

const rootDir = path.dirname(fileURLToPath(import.meta.url));

const apiNetworkOnly = PWA_NETWORK_ONLY_METHODS.flatMap((method) =>
  [PWA_API_PATH_PATTERN, PWA_API_PORT_PATTERN].map((urlPattern) => ({
    urlPattern,
    handler: "NetworkOnly" as const,
    method,
  })),
);

export default defineConfig({
  plugins: [
    react(),
    tailwindcss(),
    VitePWA({
      registerType: "prompt",
      injectRegister: false,
      includeAssets: ["favicon.svg", ...PWA_ICON_FILES.map((file) => `icons/${file}`)],
      manifest: createPwaManifest(),
      workbox: {
        globPatterns: ["**/*.{js,css,html,svg,png,ico,webp,woff,woff2,webmanifest}"],
        navigateFallback: "index.html",
        navigateFallbackDenylist: [/^\/api\//],
        cleanupOutdatedCaches: true,
        skipWaiting: false,
        clientsClaim: false,
        navigationPreload: false,
        runtimeCaching: apiNetworkOnly,
      },
      devOptions: {
        enabled: false,
      },
    }),
  ],
  resolve: {
    alias: {
      "@": path.resolve(rootDir, "./src"),
    },
  },
  server: {
    port: 5175,
    strictPort: true,
  },
  preview: {
    port: 4175,
    strictPort: true,
  },
});
