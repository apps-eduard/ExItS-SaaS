/**
 * Toast actions may render outside RouterProvider (ToastProvider wraps the router
 * in AppProviders). Register a navigate function from inside the router tree so
 * toast CTAs can client-navigate without react-router Link / Router context.
 */

export type ToastNavigateFn = (to: string) => void | Promise<void>;

let toastNavigate: ToastNavigateFn | null = null;

export function setToastNavigate(fn: ToastNavigateFn | null): void {
  toastNavigate = fn;
}

export function getToastNavigate(): ToastNavigateFn | null {
  return toastNavigate;
}
