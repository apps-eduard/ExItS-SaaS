import { useEffect } from "react";
import { useSession } from "@/session/SessionProvider";
import { useSellingMode } from "@/selling/SellingModeProvider";

/** Clears selling mode when the browser session ends. */
export function SellingModeLifecycle() {
  const { status } = useSession();
  const { clear } = useSellingMode();

  useEffect(() => {
    if (status === "unauthenticated" || status === "expired") {
      clear();
    }
  }, [clear, status]);

  return null;
}
