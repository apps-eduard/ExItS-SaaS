import { DashboardWidgetSkeleton } from "@/components/exits/dashboard/DashboardWidgetSkeleton";
import { ShellNotFoundPage } from "@/features/overview/ShellNotFoundPage";

export function PrivacyAuthLoading() {
  return (
    <section aria-busy="true" className="grid gap-4">
      <DashboardWidgetSkeleton />
    </section>
  );
}

export function PrivacyForbidden() {
  return <ShellNotFoundPage />;
}
