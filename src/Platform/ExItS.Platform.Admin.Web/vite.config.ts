import path from "node:path";
import { fileURLToPath } from "node:url";
import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import tailwindcss from "@tailwindcss/vite";

const rootDir = path.dirname(fileURLToPath(import.meta.url));

function resolveDevApiProxyTarget(): string {
  const configured = process.env.VITE_PLATFORM_API_PROXY_TARGET?.trim();
  return configured && configured.length > 0 ? configured : "http://127.0.0.1:8091";
}

export default defineConfig({
  plugins: [react(), tailwindcss()],
  resolve: {
    alias: {
      "@": path.resolve(rootDir, "./src"),
    },
  },
  define: {
    "import.meta.env.VITE_BUILD_SHA": JSON.stringify(
      process.env.VITE_BUILD_SHA ?? process.env.EXITS_GIT_SHA ?? "",
    ),
  },
  server: {
    host: true,
    port: 8095,
    strictPort: true,
    proxy: {
      "/api": {
        target: resolveDevApiProxyTarget(),
        changeOrigin: true,
      },
    },
  },
  preview: {
    host: true,
    port: 4173,
    strictPort: true,
  },
});
