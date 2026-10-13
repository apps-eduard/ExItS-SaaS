import { clearInstalledApp } from "@/pwa/reload-app-from-server";

/**
 * Switch to the waiting app copy before the page reloads.
 * A reload that happens first leaves the old copy in control, so the refresh
 * prompt is still there the next time the site opens.
 */
export async function activatePwaUpdate(target: Window = window): Promise<void> {
  const sw = "serviceWorker" in target.navigator ? target.navigator.serviceWorker : undefined;
  if (!sw) {
    return;
  }

  const tookControl = waitForControllerChange(target, sw, 4000);
  const registration = await sw.getRegistration();
  const waiting = registration?.waiting ?? registration?.installing ?? null;
  waiting?.postMessage({ type: "SKIP_WAITING" });
  if (await tookControl) {
    return;
  }

  await clearInstalledApp(target);
}

function waitForControllerChange(
  target: Window,
  sw: ServiceWorkerContainer,
  timeoutMs: number,
): Promise<boolean> {
  return new Promise((resolve) => {
    let settled = false;
    const finish = (ready: boolean) => {
      if (settled) {
        return;
      }
      settled = true;
      resolve(ready);
    };
    const timer = target.setTimeout(() => finish(false), timeoutMs);
    sw.addEventListener(
      "controllerchange",
      () => {
        target.clearTimeout(timer);
        finish(true);
      },
      { once: true },
    );
  });
}
