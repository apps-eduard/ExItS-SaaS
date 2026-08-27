import { useState, type ReactNode } from "react";
import { OnlineRequiredPageState } from "@/components/exits/OnlineRequiredBoot";
import { LoadingState } from "@/components/exits/LoadingState";
import { useAppOnline, useOptionalConnectivity } from "@/connectivity/ConnectivityProvider";
import { useI18n } from "@/i18n/I18nProvider";

/**
 * Organization data load gate: prefer OnlineRequired over endless skeleton when offline
 * and no data has been painted yet. Preserves already-loaded UI when connection drops.
 */
export function OrganizationQueryGate({
  title,
  isLoading,
  isError,
  hasData,
  onRetry,
  children,
}: {
  title: string;
  isLoading: boolean;
  isError: boolean;
  hasData: boolean;
  onRetry?: () => void;
  children: ReactNode;
}) {
  const { t } = useI18n();
  const online = useAppOnline();
  const connectivity = useOptionalConnectivity();
  const [retrying, setRetrying] = useState(false);

  if (!hasData && !online && (isLoading || isError)) {
    return (
      <OnlineRequiredPageState
        title={title}
        detail={t("connectivity.pageNeedsInternet")}
        retrying={retrying}
        onRetry={
          onRetry || connectivity
            ? async () => {
                setRetrying(true);
                try {
                  if (connectivity) {
                    await connectivity.retry();
                  }
                  onRetry?.();
                } finally {
                  setRetrying(false);
                }
              }
            : undefined
        }
      />
    );
  }

  if (!hasData && isLoading) {
    return <LoadingState label={t("loading.label")} />;
  }

  return children;
}
