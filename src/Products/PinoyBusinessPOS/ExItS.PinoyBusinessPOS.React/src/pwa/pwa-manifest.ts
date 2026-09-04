export const PWA_APP_NAME = "Pinoy Business POS";
export const PWA_SHORT_NAME = "ExItS POS";
export const PWA_START_URL = "/";
export const PWA_DISPLAY = "standalone" as const;
export const PWA_THEME_COLOR = "#166534";
export const PWA_BACKGROUND_COLOR = "#f3f4f6";
export const PWA_DESCRIPTION = "Pinoy Business POS client. Online-first static application shell.";

export const PWA_ICON_FILES = [
  "icon-192.png",
  "icon-512.png",
  "icon-192-maskable.png",
  "icon-512-maskable.png",
] as const;

/** Same-origin API prefixes kept NetworkOnly in the service worker. */
export const PWA_API_PATH_PATTERN = /\/api\//;
export const PWA_PLATFORM_API_PATH_PATTERN = /\/platform-api\//;

/** Generic auth/session path protection. */
export const PWA_AUTH_PATH_PATTERN = /\/(auth|session)\//i;

export function createPwaManifest() {
  return {
    name: PWA_APP_NAME,
    short_name: PWA_SHORT_NAME,
    description: PWA_DESCRIPTION,
    start_url: PWA_START_URL,
    display: PWA_DISPLAY,
    background_color: PWA_BACKGROUND_COLOR,
    theme_color: PWA_THEME_COLOR,
    lang: "en",
    icons: [
      { src: "icon-192.png", sizes: "192x192", type: "image/png", purpose: "any" as const },
      { src: "icon-512.png", sizes: "512x512", type: "image/png", purpose: "any" as const },
      {
        src: "icon-192-maskable.png",
        sizes: "192x192",
        type: "image/png",
        purpose: "maskable" as const,
      },
      {
        src: "icon-512-maskable.png",
        sizes: "512x512",
        type: "image/png",
        purpose: "maskable" as const,
      },
    ],
  };
}
