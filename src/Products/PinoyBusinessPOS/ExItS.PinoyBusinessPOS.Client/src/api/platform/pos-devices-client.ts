import {
  platformRequest,
  PlatformApiError,
  type PlatformProblemDetails,
} from "@/api/platform/platform-http";

export type PosDeviceStatus = "Active" | "Revoked" | string;

export type PosDeviceDto = {
  id: string;
  organizationId: string;
  branchId: string;
  installationDeviceId: string;
  friendlyName: string;
  platform?: string | null;
  model?: string | null;
  appVersion?: string | null;
  status: PosDeviceStatus;
  registeredAtUtc: string;
  lastSeenAtUtc: string;
  revokedAtUtc?: string | null;
};

export type PosDeviceCapacityDto = {
  used: number;
  allowed: number;
};

export type PosDeviceAuthorizationDto = {
  posDeviceId: string;
  branchId: string;
  installationDeviceId: string;
};

export type PosDeviceRegistrationTokenDto = {
  id: string;
  organizationId: string;
  token: string;
  qrPayload: string;
  createdAtUtc: string;
  expiresAtUtc: string;
  status: string;
  expiresInMinutes: number;
};

export type RegisterPosDeviceRequest = {
  branchId: string;
  installationDeviceId: string;
  friendlyName: string;
  platform?: string | null;
  model?: string | null;
  appVersion?: string | null;
};

export type RedeemPosDeviceRegistrationTokenRequest = {
  token: string;
  branchId: string;
  installationDeviceId: string;
  friendlyName: string;
  platform?: string | null;
  model?: string | null;
  appVersion?: string | null;
};

export type AuthorizePosDeviceRequest = {
  installationDeviceId: string;
  branchId?: string | null;
};

export type RevokePosDeviceRequest = {
  reason: string;
  stepUpToken?: string | null;
};

function devicesBase(organizationId: string): string {
  return `/api/v1/platform/organizations/${organizationId}/pos-devices`;
}

function normalizeDevice(raw: Record<string, unknown>): PosDeviceDto {
  return {
    id: String(raw.id ?? raw.Id ?? ""),
    organizationId: String(raw.organizationId ?? raw.OrganizationId ?? ""),
    branchId: String(raw.branchId ?? raw.BranchId ?? ""),
    installationDeviceId: String(raw.installationDeviceId ?? raw.InstallationDeviceId ?? ""),
    friendlyName: String(raw.friendlyName ?? raw.FriendlyName ?? ""),
    platform: (raw.platform ?? raw.Platform ?? null) as string | null,
    model: (raw.model ?? raw.Model ?? null) as string | null,
    appVersion: (raw.appVersion ?? raw.AppVersion ?? null) as string | null,
    status: String(raw.status ?? raw.Status ?? ""),
    registeredAtUtc: String(raw.registeredAtUtc ?? raw.RegisteredAtUtc ?? ""),
    lastSeenAtUtc: String(raw.lastSeenAtUtc ?? raw.LastSeenAtUtc ?? ""),
    revokedAtUtc: (raw.revokedAtUtc ?? raw.RevokedAtUtc ?? null) as string | null,
  };
}

function normalizeCapacity(raw: Record<string, unknown>): PosDeviceCapacityDto {
  return {
    used: Number(raw.used ?? raw.Used ?? 0),
    allowed: Number(raw.allowed ?? raw.Allowed ?? 0),
  };
}

function normalizeAuthorization(raw: Record<string, unknown>): PosDeviceAuthorizationDto {
  return {
    posDeviceId: String(raw.posDeviceId ?? raw.PosDeviceId ?? ""),
    branchId: String(raw.branchId ?? raw.BranchId ?? ""),
    installationDeviceId: String(raw.installationDeviceId ?? raw.InstallationDeviceId ?? ""),
  };
}

function normalizeToken(raw: Record<string, unknown>): PosDeviceRegistrationTokenDto {
  return {
    id: String(raw.id ?? raw.Id ?? ""),
    organizationId: String(raw.organizationId ?? raw.OrganizationId ?? ""),
    token: String(raw.token ?? raw.Token ?? ""),
    qrPayload: String(raw.qrPayload ?? raw.QrPayload ?? ""),
    createdAtUtc: String(raw.createdAtUtc ?? raw.CreatedAtUtc ?? ""),
    expiresAtUtc: String(raw.expiresAtUtc ?? raw.ExpiresAtUtc ?? ""),
    status: String(raw.status ?? raw.Status ?? ""),
    expiresInMinutes: Number(raw.expiresInMinutes ?? raw.ExpiresInMinutes ?? 0),
  };
}

export type PosDevicesClientResult<T> =
  | { ok: true; value: T }
  | { ok: false; status: number; body: PlatformProblemDetails | null; errorCode?: string };

async function wrap<T>(fn: () => Promise<T>): Promise<PosDevicesClientResult<T>> {
  try {
    return { ok: true, value: await fn() };
  } catch (error) {
    if (error instanceof PlatformApiError) {
      return {
        ok: false,
        status: error.status,
        body: error.problem,
        errorCode: error.errorCode,
      };
    }
    throw error;
  }
}

export async function listPosDevices(
  organizationId: string,
  signal?: AbortSignal,
): Promise<PosDevicesClientResult<PosDeviceDto[]>> {
  return wrap(async () => {
    const payload = await platformRequest<unknown>({
      method: "GET",
      path: devicesBase(organizationId),
      signal,
    });
    const list = Array.isArray(payload) ? payload : [];
    return list.map((item) => normalizeDevice(item as Record<string, unknown>));
  });
}

export async function getPosDeviceCapacity(
  organizationId: string,
  signal?: AbortSignal,
): Promise<PosDevicesClientResult<PosDeviceCapacityDto>> {
  return wrap(async () => {
    const payload = await platformRequest<Record<string, unknown>>({
      method: "GET",
      path: `${devicesBase(organizationId)}/capacity`,
      signal,
    });
    return normalizeCapacity(payload);
  });
}

export async function registerPosDevice(
  organizationId: string,
  body: RegisterPosDeviceRequest,
  signal?: AbortSignal,
): Promise<PosDevicesClientResult<PosDeviceDto>> {
  return wrap(async () => {
    const payload = await platformRequest<Record<string, unknown>>({
      method: "POST",
      path: `${devicesBase(organizationId)}/register`,
      body: {
        branchId: body.branchId,
        installationDeviceId: body.installationDeviceId,
        friendlyName: body.friendlyName,
        platform: body.platform ?? "Browser",
        model: body.model ?? null,
        appVersion: body.appVersion ?? null,
      },
      signal,
    });
    return normalizeDevice(payload);
  });
}

export async function createPosDeviceRegistrationToken(
  organizationId: string,
  signal?: AbortSignal,
): Promise<PosDevicesClientResult<PosDeviceRegistrationTokenDto>> {
  return wrap(async () => {
    const payload = await platformRequest<Record<string, unknown>>({
      method: "POST",
      path: `${devicesBase(organizationId)}/registration-tokens`,
      body: {},
      signal,
    });
    return normalizeToken(payload);
  });
}

export async function redeemPosDeviceRegistrationToken(
  organizationId: string,
  body: RedeemPosDeviceRegistrationTokenRequest,
  signal?: AbortSignal,
): Promise<PosDevicesClientResult<PosDeviceDto>> {
  return wrap(async () => {
    const payload = await platformRequest<Record<string, unknown>>({
      method: "POST",
      path: `${devicesBase(organizationId)}/registration-tokens/redeem`,
      body: {
        token: body.token,
        branchId: body.branchId,
        installationDeviceId: body.installationDeviceId,
        friendlyName: body.friendlyName,
        platform: body.platform ?? "Browser",
        model: body.model ?? null,
        appVersion: body.appVersion ?? null,
      },
      signal,
    });
    return normalizeDevice(payload);
  });
}

export async function authorizePosDevice(
  organizationId: string,
  body: AuthorizePosDeviceRequest,
  signal?: AbortSignal,
): Promise<PosDevicesClientResult<PosDeviceAuthorizationDto>> {
  return wrap(async () => {
    const payload = await platformRequest<Record<string, unknown>>({
      method: "POST",
      path: `${devicesBase(organizationId)}/authorize`,
      body: {
        installationDeviceId: body.installationDeviceId,
        branchId: body.branchId ?? null,
      },
      signal,
    });
    return normalizeAuthorization(payload);
  });
}

export async function revokePosDevice(
  organizationId: string,
  deviceId: string,
  body: RevokePosDeviceRequest,
  signal?: AbortSignal,
): Promise<PosDevicesClientResult<PosDeviceDto>> {
  return wrap(async () => {
    const payload = await platformRequest<Record<string, unknown>>({
      method: "POST",
      path: `${devicesBase(organizationId)}/${deviceId}/revoke`,
      body: {
        reason: body.reason,
        stepUpToken: body.stepUpToken ?? null,
      },
      signal,
    });
    return normalizeDevice(payload);
  });
}
