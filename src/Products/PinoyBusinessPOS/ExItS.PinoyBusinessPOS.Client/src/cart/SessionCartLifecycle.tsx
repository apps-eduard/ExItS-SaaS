import { useEffect } from "react";
import { useSessionCartOptional } from "@/cart/SessionCartProvider";
import { useSession } from "@/session/SessionProvider";

/** Clears the in-memory session cart when the browser session ends. */
export function SessionCartLifecycle() {
  const { status } = useSession();
  const cart = useSessionCartOptional();

  useEffect(() => {
    if (status === "unauthenticated" || status === "expired") {
      cart?.clear();
    }
  }, [cart, status]);

  return null;
}
