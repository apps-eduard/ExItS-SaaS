/// <reference types="vite/client" />

interface ImportMetaEnv {
  readonly VITE_PLATFORM_API_BASE_URL: string;
}

interface ImportMeta {
  readonly env: ImportMetaEnv;
}

interface ExitsPlatformAdminWebRuntimeConfig {
  platformApiBaseUrl?: string;
}

interface Window {
  __EXITS_PLATFORM_ADMIN_WEB__?: ExitsPlatformAdminWebRuntimeConfig;
}
