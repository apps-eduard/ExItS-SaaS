import { Outlet } from "react-router-dom";
import { PwaUpdateHost } from "@/pwa/PwaUpdateHost";
import { SessionProvider } from "@/session/SessionProvider";

export function RootLayout() {
  return (
    <SessionProvider>
      <Outlet />
      <PwaUpdateHost />
    </SessionProvider>
  );
}
