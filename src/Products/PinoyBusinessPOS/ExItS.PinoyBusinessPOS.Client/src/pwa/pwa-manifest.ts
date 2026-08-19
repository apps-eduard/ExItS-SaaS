export const PWA_APP_NAME = "ExItS Mobile";
export const PWA_SHORT_NAME = "ExItS Mobile";
export const PWA_START_URL = "/";
export const PWA_SCOPE = "/";
export const PWA_DISPLAY = "standalone";
export const PWA_THEME_COLOR = "#166534";
export const PWA_BACKGROUND_COLOR = "#eef3f0";
export const PWA_LANG = "en";
export const PWA_DEFAULT_APP_VERSION = "0.0.1-impl-03a";
export const PWA_PLATFORM_API_PREFIX_PATTERN = /\/platform-api\//i;

export const PWA_ICON_FILES = [
  "icon-192.png",
  "icon-512.png",
  "icon-192-maskable.png",
  "icon-512-maskable.png",
  "apple-touch-icon.png",
] as const;

export const PWA_MANIFEST_ICONS = [
  {
    src: "icons/icon-192.png",
    sizes: "192x192",
    type: "image/png",
    purpose: "any",
  },
  {
    src: "icons/icon-512.png",
    sizes: "512x512",
    type: "image/png",
    purpose: "any",
  },
  {
    src: "icons/icon-192-maskable.png",
    sizes: "192x192",
    type: "image/png",
    purpose: "maskable",
  },
  {
    src: "icons/icon-512-maskable.png",
    sizes: "512x512",
    type: "image/png",
    purpose: "maskable",
  },
] as const;

export const PWA_API_PATH_PATTERN = /\/api\//i;
export const PWA_API_PORT_PATTERN = /:8091|:8092/;
export const PWA_NETWORK_ONLY_METHODS = ["GET", "POST", "PUT", "PATCH", "DELETE", "HEAD"] as const;

export function createPwaManifest() {
  return {
    id: PWA_START_URL,
    name: PWA_APP_NAME,
    short_name: PWA_SHORT_NAME,
    description: "Your business and personal ExItS experience.",
    start_url: PWA_START_URL,
    scope: PWA_SCOPE,
    display: PWA_DISPLAY,
    background_color: PWA_BACKGROUND_COLOR,
    theme_color: PWA_THEME_COLOR,
    lang: PWA_LANG,
    icons: PWA_MANIFEST_ICONS.map((icon) => ({ ...icon })),
  } as const;
}
