import path from "node:path";
import { fileURLToPath } from "node:url";
import { defineConfig, type ProxyOptions } from "vite";
import react from "@vitejs/plugin-react";
import tailwindcss from "@tailwindcss/vite";
import { VitePWA } from "vite-plugin-pwa";
import {
  createPwaManifest,
  PWA_API_PATH_PATTERN,
  PWA_API_PORT_PATTERN,
  PWA_ICON_FILES,
  PWA_NETWORK_ONLY_METHODS,
  PWA_PLATFORM_API_PREFIX_PATTERN,
} from "./src/pwa/pwa-manifest.ts";
import {
  PLATFORM_API_PROXY_PREFIX,
  resolvePlatformProxyTarget,
  rewritePlatformProxyPath,
  stripSetCookieDomain,
} from "./src/pwa/platform-api-proxy.ts";

const rootDir = path.dirname(fileURLToPath(import.meta.url));

const apiNetworkOnly = PWA_NETWORK_ONLY_METHODS.flatMap((method) =>
  [PWA_API_PATH_PATTERN, PWA_API_PORT_PATTERN, PWA_PLATFORM_API_PREFIX_PATTERN].map(
    (urlPattern) => ({
      urlPattern,
      handler: "NetworkOnly" as const,
      method,
    }),
  ),
);

function createPlatformApiProxy(): Record<string, ProxyOptions> {
  const target = resolvePlatformProxyTarget();
  return {
    [PLATFORM_API_PROXY_PREFIX]: {
      target,
      changeOrigin: true,
      secure: false,
      ws: false,
      xfwd: false,
      rewrite: rewritePlatformProxyPath,
      configure(proxy) {
        proxy.on("proxyRes", (proxyRes) => {
          const cookies = proxyRes.headers["set-cookie"];
          if (!cookies) {
            return;
          }
          proxyRes.headers["set-cookie"] = cookies.map(stripSetCookieDomain);
        });
      },
    },
  };
}

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
        navigateFallbackDenylist: [/^\/api\//, /^\/platform-api\//],
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
    host: true,
    port: 5175,
    strictPort: true,
    proxy: createPlatformApiProxy(),
  },
  preview: {
    host: true,
    port: 4175,
    strictPort: true,
    proxy: createPlatformApiProxy(),
  },
});
