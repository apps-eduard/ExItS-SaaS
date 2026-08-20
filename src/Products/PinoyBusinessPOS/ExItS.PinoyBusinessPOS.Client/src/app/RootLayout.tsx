import { Outlet } from "react-router-dom";
import { AppShell } from "@/layouts/AppShell";

export function RootLayout() {
  return (
    <AppShell>
      <Outlet />
    </AppShell>
  );
}
