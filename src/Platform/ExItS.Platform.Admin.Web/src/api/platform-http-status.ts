import { PlatformApiError } from "@/api/platform-http";

export function isPlatformForbidden(error: unknown): boolean {
  return error instanceof PlatformApiError && (error.status === 401 || error.status === 403);
}

export function isPlatformNotFound(error: unknown): boolean {
  return error instanceof PlatformApiError && error.status === 404;
}
