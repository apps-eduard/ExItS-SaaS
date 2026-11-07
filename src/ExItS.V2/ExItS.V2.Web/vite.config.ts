import path from "node:path";
import { fileURLToPath } from "node:url";
import tailwindcss from "@tailwindcss/vite";
import react from "@vitejs/plugin-react";
import type { ProxyOptions } from "vite";
import { defineConfig } from "vite";
import { rewriteProxiedSetCookieHeaders } from "./vite.proxy-cookie.ts";

const rootDir = path.dirname(fileURLToPath(import.meta.url));

/** Independent from POS (:5177) and POS preview (:4177). */
const V2_DEV_PORT = 5180;
const V2_PREVIEW_PORT = 4180;

const LOOPBACK = new Set(["127.0.0.1", "localhost", "::1", "[::1]"]);

function resolveLoopbackTarget(envName: string, fallback: string): string {
  const raw = process.env[envName]?.trim() || fallback;
  const url = new URL(raw);
  if (!LOOPBACK.has(url.hostname.toLowerCase())) {
    throw new Error(`${envName} must target loopback. Received '${url.hostname}'.`);
  }
  return url.origin;
}

function createApiProxies(): Record<string, ProxyOptions> {
  const platformTarget = resolveLoopbackTarget(
    "EXITS_PLATFORM_API_PROXY_TARGET",
    "http://127.0.0.1:5288",
  );
  const posTarget = resolveLoopbackTarget(
    "EXITS_POS_API_PROXY_TARGET",
    "http://127.0.0.1:5290",
  );

  const rewritePrefix = (prefix: string) => (pathname: string) => {
    if (pathname === prefix) return "/";
    if (pathname.startsWith(`${prefix}/`)) {
      const stripped = pathname.slice(prefix.length);
      return stripped.length > 0 ? stripped : "/";
    }
    return pathname;
  };

  return {
    "/platform-api": {
      target: platformTarget,
      changeOrigin: true,
      secure: false,
      cookieDomainRewrite: "",
      configure: (proxy) => {
        proxy.on("proxyRes", rewriteProxiedSetCookieHeaders);
      },
      rewrite: rewritePrefix("/platform-api"),
    },
    "/pos-api": {
      target: posTarget,
      changeOrigin: true,
      secure: false,
      cookieDomainRewrite: "",
      configure: (proxy) => {
        proxy.on("proxyRes", rewriteProxiedSetCookieHeaders);
      },
      rewrite: rewritePrefix("/pos-api"),
    },
  };
}

export default defineConfig({
  plugins: [react(), tailwindcss()],
  resolve: {
    alias: {
      "@": path.resolve(rootDir, "src"),
    },
  },
  server: {
    host: "127.0.0.1",
    port: V2_DEV_PORT,
    strictPort: true,
    proxy: createApiProxies(),
  },
  preview: {
    host: "127.0.0.1",
    port: V2_PREVIEW_PORT,
    strictPort: true,
    proxy: createApiProxies(),
  },
});
