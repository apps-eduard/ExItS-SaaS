import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { useState, type ReactNode } from "react";
import { ConnectivityProvider } from "@/connectivity/ConnectivityProvider";
import { attachGlobalQueryErrorHandlers } from "@/diagnostics/attach-global-query-error-handlers";
import { PreferencesProvider } from "@/hooks/usePreferences";
import { I18nProvider } from "@/i18n/I18nProvider";
import { OfflineSyncProvider } from "@/offline/OfflineSyncProvider";
import { OutboxSyncHost } from "@/offline/OutboxSyncHost";

export function AppProviders({ children }: { children: ReactNode }) {
  const [queryClient] = useState(() => {
    const client = new QueryClient({
      defaultOptions: {
        queries: {
          retry: false,
          refetchOnWindowFocus: false,
        },
        mutations: {
          /**
           * React Query's default is to pause a mutation while the browser reports offline and
           * fire it on reconnect. This app must not do that: the offline-capable writes decide
           * for themselves whether to reach the network or queue an encrypted operation, and a
           * paused mutation never runs that decision — it leaves the person watching a spinner
           * while nothing is saved, then posts later outside the outbox that guards replay.
           * Running always means an offline write reaches its own offline branch, and a write
           * with no offline branch fails visibly instead of silently deferring.
           */
          networkMode: "always",
        },
      },
    });
    attachGlobalQueryErrorHandlers(client);
    return client;
  });

  return (
    <QueryClientProvider client={queryClient}>
      <PreferencesProvider>
        <I18nProvider>
          <ConnectivityProvider>
            <OfflineSyncProvider>
              <OutboxSyncHost />
              {children}
            </OfflineSyncProvider>
          </ConnectivityProvider>
        </I18nProvider>
      </PreferencesProvider>
    </QueryClientProvider>
  );
}
