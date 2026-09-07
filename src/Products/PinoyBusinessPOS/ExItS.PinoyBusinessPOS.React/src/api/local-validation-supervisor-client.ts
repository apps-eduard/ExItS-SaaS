/**
 * Local Validation supervisor client (loopback / Vite same-origin proxy).
 * Destructive controls require browser on localhost/127.0.0.1.
 */

/** Direct loopback origin (tests / non-browser). */
export const LOCAL_VALIDATION_SUPERVISOR_ORIGIN = "http://127.0.0.1:8099";

/** Dev same-origin proxy prefix (avoids CORS; Vite → 127.0.0.1:8099). */
export const LOCAL_VALIDATION_SUPERVISOR_PROXY_PREFIX = "/__dev__/lv-supervisor";

export function resolveSupervisorBaseUrl(): string {
  if (typeof window === "undefined") {
    return LOCAL_VALIDATION_SUPERVISOR_ORIGIN;
  }
  // Prefer Vite proxy when served by React POS so login controls work same-origin.
  return LOCAL_VALIDATION_SUPERVISOR_PROXY_PREFIX;
}

export type LocalValidationServiceStatus =
  | "Up"
  | "Down"
  | "Starting"
  | "Restarting"
  | "Failed";

export type LocalValidationServiceRow = {
  key: string;
  label: string;
  port: number;
  status: LocalValidationServiceStatus | string;
  restartable: boolean;
  kind?: string;
};

export type LocalValidationHealthResponse = {
  checkedAtUtc?: string;
  busy?: boolean;
  operation?: string | null;
  progress?: string | null;
  services: LocalValidationServiceRow[];
};

export function isLocalValidationControlHost(): boolean {
  if (typeof window === "undefined") {
    return false;
  }
  const host = window.location.hostname;
  return host === "127.0.0.1" || host === "localhost";
}

async function readJson<T>(response: Response): Promise<T> {
  return (await response.json()) as T;
}

function supervisorUrl(path: string): string {
  const base = resolveSupervisorBaseUrl().replace(/\/$/, "");
  const suffix = path.startsWith("/") ? path : `/${path}`;
  return `${base}${suffix}`;
}

export async function fetchSupervisorHealth(
  signal?: AbortSignal,
): Promise<LocalValidationHealthResponse | null> {
  try {
    const response = await fetch(supervisorUrl("/health/services"), {
      method: "GET",
      cache: "no-store",
      signal,
    });
    if (!response.ok) {
      return null;
    }
    const data = await readJson<LocalValidationHealthResponse>(response);
    if (!Array.isArray(data?.services)) {
      return null;
    }
    return data;
  } catch {
    return null;
  }
}

export async function fetchSupervisorOperation(signal?: AbortSignal): Promise<{
  busy: boolean;
  operation?: string | null;
  progress?: string | null;
} | null> {
  try {
    const response = await fetch(supervisorUrl("/operation"), {
      method: "GET",
      cache: "no-store",
      signal,
    });
    if (!response.ok) {
      return null;
    }
    return (await response.json()) as {
      busy: boolean;
      operation?: string | null;
      progress?: string | null;
    };
  } catch {
    return null;
  }
}

export async function restartSupervisorService(serviceKey: string): Promise<{
  ok: boolean;
  message: string;
  status: number;
}> {
  const response = await fetch(
    supervisorUrl(`/services/${encodeURIComponent(serviceKey)}/restart`),
    { method: "POST", cache: "no-store" },
  );
  const body = await response.json().catch(() => ({}));
  if (response.status === 409) {
    return {
      ok: false,
      status: 409,
      message: body?.message ?? "Local Validation control is busy.",
    };
  }
  if (!response.ok) {
    return {
      ok: false,
      status: response.status,
      message: body?.error ?? body?.message ?? `Restart failed (${response.status}).`,
    };
  }
  return {
    ok: true,
    status: response.status,
    message: body?.message ?? "Restarted successfully.",
  };
}

export async function restartAllSupervisorApps(): Promise<{
  ok: boolean;
  message: string;
  status: number;
}> {
  const response = await fetch(supervisorUrl("/services/restart-all"), {
    method: "POST",
    cache: "no-store",
  });
  const body = await response.json().catch(() => ({}));
  if (response.status === 409) {
    return {
      ok: false,
      status: 409,
      message: body?.message ?? "Local Validation control is busy.",
    };
  }
  if (!response.ok) {
    return {
      ok: false,
      status: response.status,
      message: body?.error ?? body?.message ?? `Restart apps failed (${response.status}).`,
    };
  }
  return {
    ok: true,
    status: response.status,
    message: body?.message ?? "Local Validation applications restarted.",
  };
}

export async function resetLocalValidationData(): Promise<{
  ok: boolean;
  message: string;
  status: number;
}> {
  const response = await fetch(supervisorUrl("/reset"), {
    method: "POST",
    cache: "no-store",
  });
  const body = await response.json().catch(() => ({}));
  if (response.status === 409) {
    return {
      ok: false,
      status: 409,
      message: body?.message ?? "Local Validation control is busy.",
    };
  }
  if (!response.ok) {
    return {
      ok: false,
      status: response.status,
      message: body?.error ?? body?.message ?? `Reset failed (${response.status}).`,
    };
  }
  return {
    ok: true,
    status: response.status,
    message:
      body?.message ?? "Local Validation reset complete. Olivia and Rafael restored.",
  };
}
