import { PLATFORM_PERMISSIONS } from "@/api/authorization/authorization-types";
import { PageHeader } from "@/components/exits/PageHeader";
import { Skeleton } from "@/components/ui/skeleton";
import { PaymentsList } from "@/features/payments/PaymentsList";
import { ShellNotFoundPage } from "@/features/overview/ShellNotFoundPage";
import { useAuthorization } from "@/hooks/use-authorization";
import { usePreferences } from "@/hooks/use-preferences";

export function PaymentsPage() {
  const { t } = usePreferences();
  const authorization = useAuthorization();
  const canList =
    authorization.status === "loaded" &&
    authorization.hasPermission(PLATFORM_PERMISSIONS.manageManualPayments);

  if (authorization.status === "loading") {
    return (
      <section aria-busy="true">
        <Skeleton className="h-8 w-48" />
        <Skeleton className="mt-3 h-16 w-full max-w-xl" />
      </section>
    );
  }

  if (!canList) {
    return <ShellNotFoundPage />;
  }

  return (
    <section className="grid gap-4">
      <PageHeader
        title={t("payments.portfolio.title")}
        description={t("payments.portfolio.description")}
      />
      <PaymentsList enabled={canList} />
    </section>
  );
}
