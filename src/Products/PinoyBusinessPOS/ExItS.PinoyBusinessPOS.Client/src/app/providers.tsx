import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { useState, type ReactNode } from "react";
import { SessionProvider } from "@/auth/SessionProvider";
import { AppErrorBoundary } from "@/components/exits/AppErrorBoundary";
import { I18nProvider } from "@/i18n/I18nProvider";
import { PreferencesProvider, usePreferences } from "@/hooks/usePreferences";

function LocaleBridge({ children }: { children: ReactNode }) {
  const { preferences } = usePreferences();
  return <I18nProvider locale={preferences.locale}>{children}</I18nProvider>;
}

export function AppProviders({ children }: { children: ReactNode }) {
  const [queryClient] = useState(
    () =>
      new QueryClient({
        defaultOptions: {
          queries: { retry: false, refetchOnWindowFocus: false },
        },
      }),
  );

  return (
    <QueryClientProvider client={queryClient}>
      <PreferencesProvider>
        <LocaleBridge>
          <SessionProvider>
            <AppErrorBoundary>{children}</AppErrorBoundary>
          </SessionProvider>
        </LocaleBridge>
      </PreferencesProvider>
    </QueryClientProvider>
  );
}
