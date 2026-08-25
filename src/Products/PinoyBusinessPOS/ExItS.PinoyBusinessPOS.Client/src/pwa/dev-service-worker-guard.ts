const DEV_SW_CLEARED_SESSION_KEY = "exits.pos-client.dev-sw-cleared.v1";

export type DevServiceWorkerRecovery = {
  recovered: boolean;
  willReload: boolean;
  unregistered: number;
  cachesCleared: number;
};

/**
 * Vite development must not remain controlled by a previously registered
 * production/preview service worker for the same origin (common on Android
 * emulator Chrome after visiting preview/build). A stale worker can serve an
 * older sign-in shell while desktop (no SW) shows the current source.
 */
export async function recoverDevelopmentOriginFromStaleServiceWorker(): Promise<DevServiceWorkerRecovery> {
  if (!import.meta.env.DEV) {
    return { recovered: false, willReload: false, unregistered: 0, cachesCleared: 0 };
  }

  if (typeof navigator === "undefined" || !("serviceWorker" in navigator)) {
    return { recovered: false, willReload: false, unregistered: 0, cachesCleared: 0 };
  }

  const registrations = await navigator.serviceWorker.getRegistrations();
  let unregistered = 0;
  for (const registration of registrations) {
    if (await registration.unregister()) {
      unregistered += 1;
    }
  }

  let cachesCleared = 0;
  if ("caches" in window) {
    const keys = await caches.keys();
    await Promise.all(
      keys.map(async (key) => {
        if (await caches.delete(key)) {
          cachesCleared += 1;
        }
      }),
    );
  }

  const recovered = unregistered > 0 || cachesCleared > 0;
  if (!recovered) {
    return { recovered: false, willReload: false, unregistered: 0, cachesCleared: 0 };
  }

  // One reload per tab session so the next navigation is network/Vite-controlled.
  try {
    if (window.sessionStorage.getItem(DEV_SW_CLEARED_SESSION_KEY) === "1") {
      return { recovered: true, willReload: false, unregistered, cachesCleared };
    }
    window.sessionStorage.setItem(DEV_SW_CLEARED_SESSION_KEY, "1");
  } catch {
    return { recovered: true, willReload: false, unregistered, cachesCleared };
  }

  window.location.reload();
  return { recovered: true, willReload: true, unregistered, cachesCleared };
}

export { DEV_SW_CLEARED_SESSION_KEY };
