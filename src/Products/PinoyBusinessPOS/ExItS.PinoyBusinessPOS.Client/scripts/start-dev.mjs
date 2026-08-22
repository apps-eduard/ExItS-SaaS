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

const viteBin = path.join(clientRoot, "node_modules", "vite", "bin", "vite.js");
const vite = spawn(
  process.execPath,
  [viteBin, "--host", "127.0.0.1", "--port", String(POS_DEV_PORT), "--strictPort"],
  {
    cwd: clientRoot,
    stdio: "inherit",
    env: process.env,
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
