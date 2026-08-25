/// <reference types="vite/client" />
/// <reference types="vite-plugin-pwa/client" />

interface ImportMetaEnv {
  readonly VITE_OFFLINE_OPERATING_GRANT_PUBLIC_KEY_PEM?: string;
  /** DEV-only. Must be the string "true" to allow insecure Offline PIN on HTTP Tailscale. */
  readonly VITE_ALLOW_INSECURE_OFFLINE_PIN?: string;
}

interface ImportMeta {
  readonly env: ImportMetaEnv;
}
