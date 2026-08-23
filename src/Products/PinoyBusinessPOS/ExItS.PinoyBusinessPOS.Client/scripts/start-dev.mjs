import { spawn } from "node:child_process";
import path from "node:path";
import { fileURLToPath } from "node:url";
import {
  POS_DEV_PORT,
  ensureEmulatorPortForward,
  formatPosDevStartupLines,
} from "./emulator-port-forward.mjs";

const clientRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..");
const forwardResult = ensureEmulatorPortForward({ port: POS_DEV_PORT });

for (const line of formatPosDevStartupLines(forwardResult)) {
  console.log(line);
}

// Default loopback; Local Validation Tailscale/LAN sets POS_DEV_HOST=0.0.0.0.
const host = (process.env.POS_DEV_HOST ?? "127.0.0.1").trim() || "127.0.0.1";
const publicHost = (process.env.POS_DEV_PUBLIC_HOST ?? "").trim();
const lanBound = host === "0.0.0.0" || Boolean(publicHost);

// DEV-only Offline PIN fallback for Tailscale/LAN HTTP (crypto.subtle unavailable).
// Production builds never run this script. Gate still requires import.meta.env.DEV
// plus insecure context / missing subtle — localhost keeps the secure Web Crypto path.
const childEnv = { ...process.env };
if (
  lanBound &&
  (childEnv.VITE_ALLOW_INSECURE_OFFLINE_PIN === undefined ||
    childEnv.VITE_ALLOW_INSECURE_OFFLINE_PIN === "")
) {
  childEnv.VITE_ALLOW_INSECURE_OFFLINE_PIN = "true";
}
if (childEnv.VITE_ALLOW_INSECURE_OFFLINE_PIN === "true") {
  console.log(
    "[pos-dev] VITE_ALLOW_INSECURE_OFFLINE_PIN=true (DEV insecure Offline PIN on non-secure HTTP)",
  );
}

const viteBin = path.join(clientRoot, "node_modules", "vite", "bin", "vite.js");
const vite = spawn(
  process.execPath,
  [viteBin, "--host", host, "--port", String(POS_DEV_PORT), "--strictPort"],
  {
    cwd: clientRoot,
    stdio: "inherit",
    env: childEnv,
  },
);

vite.on("exit", (code, signal) => {
  if (signal) {
    process.kill(process.pid, signal);
    return;
  }
  process.exit(code ?? 0);
});

vite.on("error", (error) => {
  console.error("[pos-dev] Failed to start Vite:", error.message);
  process.exit(1);
});
