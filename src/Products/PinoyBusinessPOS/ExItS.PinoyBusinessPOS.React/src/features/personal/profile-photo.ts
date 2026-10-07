import { platformApiUrl } from "@/api/platform/browser-session";

export function profilePhotoSrc(url: string | null | undefined): string | null {
  const value = url?.trim() ?? "";
  if (!value) {
    return null;
  }
  if (value.startsWith("/api/")) {
    return platformApiUrl(value);
  }
  if (/^https?:\/\//i.test(value)) {
    return value;
  }
  return null;
}
