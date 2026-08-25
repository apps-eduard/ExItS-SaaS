import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

export const LIVE_PLATFORM_HEALTH_URL = "http://127.0.0.1:8091/health";
export const LIVE_ANTIFORGERY_COOKIE = ".ExItS.Platform.Antiforgery";
export const LIVE_OWNER_EMAIL = "kizy@gmail.com";

const repoRoot = join(dirname(fileURLToPath(import.meta.url)), "../../../../../");

export function loadLocalValidationSharedPassword(): string | null {
  if (process.env.LOCAL_VALIDATION_SHARED_PASSWORD?.trim()) {
    return process.env.LOCAL_VALIDATION_SHARED_PASSWORD.trim();
  }
  try {
    const envPath = join(repoRoot, "deploy/docker/.env.local-validation");
    const text = readFileSync(envPath, "utf8");
    const match = text.match(/^LOCAL_VALIDATION_SHARED_PASSWORD=(.+)$/m);
    return match?.[1]?.trim() ?? null;
  } catch {
    return null;
  }
}

export async function isLivePlatformApiListening(): Promise<boolean> {
  try {
    const response = await fetch(LIVE_PLATFORM_HEALTH_URL, { signal: AbortSignal.timeout(3000) });
    return response.ok;
  } catch {
    return false;
  }
}

export async function skipUnlessLivePlatformApi(test: { skip: () => void }): Promise<boolean> {
  const listening = await isLivePlatformApiListening();
  if (!listening) {
    test.skip();
    return false;
  }
  const password = loadLocalValidationSharedPassword();
  if (!password) {
    test.skip();
    return false;
  }
  return true;
}
