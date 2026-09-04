import { Link } from "react-router-dom";
import { MapPin } from "lucide-react";
import { Button } from "@/components/ui/button";
import { EmptyState } from "@/components/exits/EmptyState";
import { PageHeader } from "@/components/exits/PageHeader";
import { useI18n } from "@/i18n/I18nProvider";

/**
 * Shown when the user is in an org-only bind (Manage Business) and opens a
 * branch-scoped surface. Must never look like an infinite session check.
 */
export function BranchRequiredPanel({
  title,
  testId = "branch-required-panel",
}: {
  title?: string;
  testId?: string;
}) {
  const { t } = useI18n();

  return (
    <div className="flex min-w-0 flex-col gap-4" data-testid={testId}>
      <PageHeader title={title ?? t("workspace.branchRequiredTitle")} />
      <EmptyState
        title={t("workspace.branchRequiredTitle")}
        detail={t("workspace.branchRequiredDetail")}
      />
      <Button asChild className="w-full gap-2 sm:w-auto">
        <Link to="/workspace" data-testid="branch-required-choose-workspace">
          <MapPin className="size-4 shrink-0" aria-hidden />
          {t("workspace.branchRequiredCta")}
        </Link>
      </Button>
    </div>
  );
}
