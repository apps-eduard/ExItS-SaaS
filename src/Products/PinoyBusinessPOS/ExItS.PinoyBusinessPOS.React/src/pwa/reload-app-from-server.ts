/**
 * Drop the installed app copy and load the current page from the server.
 * Used by Refresh from server so a new version applies even when no update message is showing.
 * Offline sales data stays in the local database; only the saved app files are cleared.
 */
export async function reloadAppFromServer(target: Window = window): Promise<void> {
  if ("serviceWorker" in target.navigator) {
    const registrations = await target.navigator.serviceWorker.getRegistrations();
    await Promise.all(registrations.map((registration) => registration.unregister()));
  }

  if ("caches" in target) {
    const keys = await target.caches.keys();
    await Promise.all(keys.map((key) => target.caches.delete(key)));
  }

  target.location.reload();
}
