import { spawnSync } from "node:child_process";
import fs from "node:fs";
import path from "node:path";

export const POS_DEV_PORT = 5177;
export const POS_DEV_CANONICAL_URL = `http://127.0.0.1:${POS_DEV_PORT}`;

function adbBinaryName() {
  return process.platform === "win32" ? "adb.exe" : "adb";
}

function canRunAdb(adb) {
  const result = spawnSync(adb, ["version"], { encoding: "utf8", windowsHide: true });
  return !result.error && result.status === 0;
}

export function resolveAdbExecutable() {
  const binary = adbBinaryName();
  const candidates = [];

  for (const root of [process.env.ANDROID_HOME, process.env.ANDROID_SDK_ROOT]) {
    if (root) {
      candidates.push(path.join(root, "platform-tools", binary));
    }
  }

  const localAppData = process.env.LOCALAPPDATA;
  if (localAppData) {
    candidates.push(path.join(localAppData, "Android", "Sdk", "platform-tools", binary));
  }

  for (const candidate of candidates) {
    if (fs.existsSync(candidate) && canRunAdb(candidate)) {
      return candidate;
    }
  }

  if (canRunAdb("adb")) {
    return "adb";
  }

  return null;
}

export function parseAdbDevices(output) {
  return output
    .split(/\r?\n/)
    .slice(1)
    .map((line) => line.trim())
    .filter(Boolean)
    .map((line) => {
      const match = line.match(/^(\S+)\s+(\S+)/);
      if (!match) {
        return null;
      }
      return { serial: match[1], state: match[2] };
    })
    .filter(Boolean);
}

function runAdb(adb, args) {
  return spawnSync(adb, args, { encoding: "utf8", windowsHide: true });
}

export function ensureEmulatorPortForward(options = {}) {
  const port = options.port ?? POS_DEV_PORT;
  const url = `http://127.0.0.1:${port}`;
  const adb = resolveAdbExecutable();

  if (!adb) {
    return {
      adbFound: false,
      port,
      url,
      reversed: [],
      skippedReason: "adb-not-found",
    };
  }

  const devicesResult = runAdb(adb, ["devices"]);
  if (devicesResult.error || devicesResult.status !== 0) {
    return {
      adbFound: true,
      port,
      url,
      reversed: [],
      skippedReason: "adb-devices-failed",
    };
  }

  const devices = parseAdbDevices(devicesResult.stdout ?? "");
  const ready = devices.filter((device) => device.state === "device");
  const reversed = [];

  for (const { serial } of ready) {
    const reverseResult = runAdb(adb, [
      "-s",
      serial,
      "reverse",
      `tcp:${port}`,
      `tcp:${port}`,
    ]);
    if (reverseResult.status === 0) {
      reversed.push(serial);
    }
  }

  return {
    adbFound: true,
    port,
    url,
    reversed,
    skippedReason: ready.length === 0 ? "no-device" : undefined,
  };
}

export function formatPosDevStartupLines(result) {
  const lines = [`[pos-dev] Desktop:  ${result.url}`];

  if (result.reversed.length > 0) {
    lines.push(
      `[pos-dev] Emulator: ${result.url}  (adb reverse active on ${result.reversed.join(", ")})`,
    );
  } else if (!result.adbFound) {
    lines.push(
      `[pos-dev] Emulator: ${result.url}  (adb reverse skipped — adb not installed)`,
    );
  } else {
    lines.push(
      `[pos-dev] Emulator: ${result.url}  (adb reverse skipped — no emulator connected)`,
    );
  }

  return lines;
}
