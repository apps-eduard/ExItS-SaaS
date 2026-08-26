/** Vitest stub for vite-plugin-pwa virtual module. */
export function registerSW(options?: {
  immediate?: boolean;
  onNeedRefresh?: () => void;
  onRegisterError?: (error?: unknown) => void;
  onOfflineReady?: () => void;
}): (reloadPage?: boolean) => Promise<void> {
  void options;
  return async () => undefined;
}

export function useRegisterSW() {
  return {
    needRefresh: [false, () => undefined] as const,
    offlineReady: [false, () => undefined] as const,
    updateServiceWorker: async () => undefined,
  };
}
