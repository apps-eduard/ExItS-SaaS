/// <reference types="vite/client" />

interface ImportMetaEnv {
  readonly VITE_PLATFORM_API_BASE_URL: string;
  readonly VITE_BUILD_SHA: string;
  /** When "true", enable Local Validation UI tools (weak password, test-user helpers). */
  readonly VITE_LOCAL_VALIDATION_TOOLS: string;
}

interface ImportMeta {
  readonly env: ImportMetaEnv;
}

interface ExitsPlatformAdminWebRuntimeConfig {
  app?: string;
  platformApiBaseUrl?: string;
  platformApiSameOrigin?: boolean;
  localValidationToolsEnabled?: boolean;
  buildSha?: string;
}

interface Window {
  __EXITS_PLATFORM_ADMIN_WEB__?: ExitsPlatformAdminWebRuntimeConfig;
}

declare module "*?raw" {
  const content: string;
  export default content;
}
