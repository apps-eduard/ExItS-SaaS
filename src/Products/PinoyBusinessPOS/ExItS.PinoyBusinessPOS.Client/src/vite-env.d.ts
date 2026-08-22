/// <reference types="vite/client" />
/// <reference types="vite-plugin-pwa/client" />

interface ImportMetaEnv {
  readonly VITE_OFFLINE_OPERATING_GRANT_PUBLIC_KEY_PEM?: string;
}

interface ImportMeta {
  readonly env: ImportMetaEnv;
}
