import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { useState, type ReactNode } from "react";
import { PreferencesProvider } from "@/hooks/usePreferences";
import { I18nProvider } from "@/i18n/I18nProvider";
import { OfflineSyncProvider } from "@/offline/OfflineSyncProvider";

export function AppProviders({ children }: { children: ReactNode }) {
  const [queryClient] = useState(
    () =>
      new QueryClient({
        defaultOptions: {
          queries: {
            retry: false,
            refetchOnWindowFocus: false,
          },
        },
      }),
  );

  return (
    <QueryClientProvider client={queryClient}>
      <PreferencesProvider>
        <I18nProvider>
          <OfflineSyncProvider>{children}</OfflineSyncProvider>
        </I18nProvider>
      </PreferencesProvider>
    </QueryClientProvider>
  );
}
