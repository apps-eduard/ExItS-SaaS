import { forwardRef, type ComponentPropsWithoutRef } from "react";
import { Link, useLocation, type To } from "react-router-dom";
import {
  linkStateWithReturn,
  rememberSmartBackReturnTo,
  type SmartBackLocationState,
} from "@/navigation/smart-back";

export type AppLinkWithReturnProps = Omit<
  ComponentPropsWithoutRef<typeof Link>,
  "state"
> & {
  /** When false, behaves like a normal Link (no return capture). Default true. */
  captureReturn?: boolean;
  /** Merge extra location state alongside returnTo. */
  state?: SmartBackLocationState & Record<string, unknown>;
};

function destinationPathForStorage(to: To, currentPathname: string): string {
  if (typeof to === "string") {
    return to;
  }
  if (typeof to === "number") {
    return currentPathname;
  }
  const pathname = to.pathname ?? currentPathname;
  const search = to.search ?? "";
  const hash = to.hash ?? "";
  return `${pathname}${search}${hash}`;
}

/**
 * Link that preserves the exact current origin (path + search + hash) for Smart Back.
 */
export const AppLinkWithReturn = forwardRef<HTMLAnchorElement, AppLinkWithReturnProps>(
  function AppLinkWithReturn(
    { captureReturn = true, state, to, onClick, ...rest },
    ref,
  ) {
    const location = useLocation();
    const returnState = captureReturn
      ? linkStateWithReturn({
          pathname: location.pathname,
          search: location.search,
          hash: location.hash,
        })
      : undefined;
    const mergedState =
      returnState || state
        ? { ...returnState, ...state }
        : undefined;

    return (
      <Link
        ref={ref}
        to={to}
        state={mergedState}
        onClick={(event) => {
          if (captureReturn && returnState?.returnTo) {
            rememberSmartBackReturnTo(
              destinationPathForStorage(to, location.pathname),
              returnState.returnTo,
            );
          }
          onClick?.(event);
        }}
        {...rest}
      />
    );
  },
);
