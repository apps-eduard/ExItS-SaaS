import { existsSync, readFileSync } from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

function candidateEnvFiles(): string[] {
  const files: string[] = [];
  let dir = path.dirname(fileURLToPath(import.meta.url));
  for (let index = 0; index < 12; index += 1) {
    if (existsSync(path.join(dir, "ExItS.slnx"))) {
      files.push(path.join(dir, "deploy", "docker", ".env.local-validation"));
      const sibling = path.join(
        path.dirname(dir),
        "ExItS-SaaS",
        "deploy",
        "docker",
        ".env.local-validation",
      );
      files.push(sibling);
      break;
    }
    dir = path.dirname(dir);
  }
  return files;
}

function readPasswordFromEnvFile(envPath: string): string | undefined {
  if (!existsSync(envPath)) {
    return undefined;
  }
  const text = readFileSync(envPath, "utf8");
  const match = text.match(/^LOCAL_VALIDATION_SHARED_PASSWORD=(.*)$/m);
  const value = match?.[1]?.trim().replace(/^["']|["']$/g, "");
  if (!value || value.startsWith("REPLACE_")) {
    return undefined;
  }
  return value;
}

export function readLocalValidationSharedPassword(): string {
  const fromEnv = process.env.LOCAL_VALIDATION_SHARED_PASSWORD?.trim();
  if (fromEnv && !fromEnv.startsWith("REPLACE_")) {
    return fromEnv;
  }

  for (const envPath of candidateEnvFiles()) {
    const value = readPasswordFromEnvFile(envPath);
    if (value) {
      return value;
    }
  }

  throw new Error(
    "LOCAL_VALIDATION_SHARED_PASSWORD is required (env or deploy/docker/.env.local-validation).",
  );
}
