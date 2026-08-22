import { useState } from "react";
import { Pause, Play, XCircle } from "lucide-react";
import { PLATFORM_PERMISSIONS } from "@/api/authorization/authorization-types";
import type { OrganizationDetail } from "@/api/organizations/organization-types";
import { Alert } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { ConfirmActionDialog } from "@/components/exits/ConfirmActionDialog";
import { organizationMutationFailureCopy } from "@/features/organizations/organization-mutation-feedback";
import {
  useCloseOrganizationMutation,
  useReactivateOrganizationMutation,
  useSuspendOrganizationMutation,
} from "@/features/organizations/use-organization-mutations";
import { useAuthorization } from "@/hooks/use-authorization";
import { usePreferences } from "@/hooks/use-preferences";
import type { MessageKey } from "@/lib/i18n/messages";

type LifecycleKind = "suspend" | "reactivate" | "close";

const COPY: Record<
  LifecycleKind,
  { title: MessageKey; description: MessageKey; confirm: MessageKey; destructive: boolean }
> = {
  suspend: {
    title: "organization.lifecycle.suspend.title",
    description: "organization.lifecycle.suspend.description",
    confirm: "organization.lifecycle.suspend.confirm",
    destructive: true,
  },
  reactivate: {
    title: "organization.lifecycle.reactivate.title",
    description: "organization.lifecycle.reactivate.description",
    confirm: "organization.lifecycle.reactivate.confirm",
    destructive: false,
  },
  close: {
    title: "organization.lifecycle.close.title",
    description: "organization.lifecycle.close.description",
    confirm: "organization.lifecycle.close.confirm",
    destructive: true,
  },
};

export function OrganizationLifecycleOperator({ organization }: { organization: OrganizationDetail }) {
  const { t } = usePreferences();
  const authorization = useAuthorization();
  const canManage = authorization.hasPermission(PLATFORM_PERMISSIONS.manageOrganizations);
  const suspendMutation = useSuspendOrganizationMutation();
  const reactivateMutation = useReactivateOrganizationMutation();
  const closeMutation = useCloseOrganizationMutation();
  const [confirm, setConfirm] = useState<LifecycleKind | null>(null);
  const [feedback, setFeedback] = useState<{ tone: "danger" | "success"; title: string; detail: string } | null>(
    null,
  );

  const pending =
    suspendMutation.isPending || reactivateMutation.isPending || closeMutation.isPending;

  if (!canManage || organization.status === "Closed") {
    return null;
  }

  async function runConfirm() {
    if (!confirm || pending) {
      return;
    }
    setFeedback(null);
    try {
      if (confirm === "suspend") {
        await suspendMutation.mutateAsync(organization.id);
        setFeedback({
          tone: "success",
          title: t("organization.lifecycle.suspend.success"),
          detail: "",
        });
      } else if (confirm === "reactivate") {
        await reactivateMutation.mutateAsync(organization.id);
        setFeedback({
          tone: "success",
          title: t("organization.lifecycle.reactivate.success"),
          detail: "",
        });
      } else {
        await closeMutation.mutateAsync(organization.id);
        setFeedback({
          tone: "success",
          title: t("organization.lifecycle.close.success"),
          detail: "",
        });
      }
      setConfirm(null);
    } catch (error) {
      const copy = organizationMutationFailureCopy(error, t);
      setFeedback({ tone: "danger", title: copy.title, detail: copy.detail });
    }
  }

  return (
    <div className="grid gap-2">
      {feedback ? (
        <Alert title={feedback.title} tone={feedback.tone === "danger" ? "danger" : "success"}>
          {feedback.detail}
        </Alert>
      ) : null}
      <div className="flex flex-wrap gap-2">
        {organization.status === "Active" ? (
          <Button type="button" size="sm" variant="outline" disabled={pending} onClick={() => setConfirm("suspend")}>
            <Pause aria-hidden className="mr-2 size-4" />
            {t("organization.lifecycle.suspend.action")}
          </Button>
        ) : null}
        {organization.status === "Suspended" ? (
          <Button type="button" size="sm" disabled={pending} onClick={() => setConfirm("reactivate")}>
            <Play aria-hidden className="mr-2 size-4" />
            {t("organization.lifecycle.reactivate.action")}
          </Button>
        ) : null}
        {organization.status !== "Closed" ? (
          <Button
            type="button"
            size="sm"
            variant="destructive"
            disabled={pending}
            onClick={() => setConfirm("close")}
          >
            <XCircle aria-hidden className="mr-2 size-4" />
            {t("organization.lifecycle.close.action")}
          </Button>
        ) : null}
      </div>
      {confirm ? (
        <ConfirmActionDialog
          open
          title={t(COPY[confirm].title)}
          description={t(COPY[confirm].description)}
          confirmLabel={t(COPY[confirm].confirm)}
          cancelLabel={t("organization.admin.dialog.dismiss")}
          pendingLabel={t("organization.admin.submitting")}
          destructive={COPY[confirm].destructive}
          pending={pending}
          onCancel={() => setConfirm(null)}
          onConfirm={() => void runConfirm()}
        />
      ) : null}
    </div>
  );
}
