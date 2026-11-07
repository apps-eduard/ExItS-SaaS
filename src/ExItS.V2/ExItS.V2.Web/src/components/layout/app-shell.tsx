import type { ReactNode } from "react";
import { MobileNav } from "./mobile-nav";
import { Sidebar } from "./sidebar";
import { TopNav } from "./top-nav";

function AppShell({ children }: { children: ReactNode }) {
  return (
    <div className="flex min-h-dvh bg-background">
      <Sidebar />
      <div className="flex min-w-0 flex-1 flex-col">
        <TopNav />
        <main className="flex-1 px-4 pb-[calc(4.75rem+env(safe-area-inset-bottom))] pt-3.5 md:px-6 md:pt-5 lg:pb-8">
          <div className="mx-auto w-full max-w-5xl lg:max-w-6xl">{children}</div>
        </main>
        <MobileNav />
      </div>
    </div>
  );
}

export { AppShell };
