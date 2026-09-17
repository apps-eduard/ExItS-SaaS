import { useMemo, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import {
  createStaffInvitationByExItsId,
  resolveStaffInviteTarget,
  type StaffInviteTargetWire,
} from "@/api/platform/staff-invitation-client";
import { POS_LOCAL_ROLE_CASHIER, POS_LOCAL_ROLE_OWNER } from "@/api/platform/product-local-roles-client";
import { Button } from "@/components/ui/button";
import { ErrorState } from "@/components/exits/ErrorState";
import { PageHeader } from "@/components/exits/PageHeader";
import { ConfirmationDialog } from "@/components/exits/SheetDialog";
import { StatusChip } from "@/components/exits/StatusChip";
import { QrScanOrEnter } from "@/features/qr/QrScanOrEnter";
import { useProductLocalRoleCatalog } from "@/features/staff/useProductLocalRoleCatalog";
import { useI18n } from "@/i18n/I18nProvider";
import { pageBackNav } from "@/navigation/page-back-nav";
import { useBrowserOnline } from "@/connectivity/browser-online";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

type Step = "find" | "confirm" | "access" | "sent";

export function OrgStaffInvitePage() {
  const { t } = useI18n();
  const navigate = useNavigate();
  const online = useBrowserOnline();
  const { boundWorkspace } = useWorkspace();
  const organizationId = boundWorkspace?.organizationId ?? null;
  const catalogQuery = useProductLocalRoleCatalog(organizationId);
  const [step, setStep] = useState<Step>("find");
  const [target, setTarget] = useState<StaffInviteTargetWire | null>(null);
  const [productRole, setProductRole] = useState<string>(POS_LOCAL_ROLE_CASHIER);
  const [ownerConfirmOpen, setOwnerConfirmOpen] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const roleOptions = useMemo(() => catalogQuery.data ?? [], [catalogQuery.data]);

  async function resolveInput(raw: string) {
    if (!boundWorkspace) {
      setError(t("staffInvite.noWorkspace"));
      return;
    }
    if (!online) {
      setError(t("staffInvite.onlineRequired"));
      return;
    }
    setSubmitting(true);
    setError(null);
    const result = await resolveStaffInviteTarget({
      organizationId: boundWorkspace.organizationId,
      input: raw,
    });
    setSubmitting(false);
    if (!result.ok) {
      setError(result.body?.detail ?? t("staffInvite.notFound"));
      setTarget(null);
      setStep("find");
      return;
    }
    setTarget(result.target);
    setStep("confirm");
  }

  async function sendInvite(roleCode: string) {
    if (!boundWorkspace || !target) return;
    if (!online) {
      setError(t("staffInvite.onlineRequired"));
      return;
    }
    setSubmitting(true);
    setError(null);
    const result = await createStaffInvitationByExItsId({
      organizationId: boundWorkspace.organizationId,
      publicUserIdOrQrPayload: target.publicUserId,
      productRole: roleCode,
    });
    setSubmitting(false);
    setOwnerConfirmOpen(false);
    if (!result.ok) {
      setError(result.body?.detail ?? t("staffInvite.error"));
      return;
    }
    setStep("sent");
  }

  function requestSendInvite() {
    if (productRole === POS_LOCAL_ROLE_OWNER) {
      setOwnerConfirmOpen(true);
      return;
    }
    void sendInvite(productRole);
  }

  if (!boundWorkspace) {
    return (
      <div className="staff-invite-page exits-page flex min-w-0 flex-col gap-3" data-testid="staff-invite-page">
        <PageHeader
          title={t("staffInvite.title")}
          description={t("staffInvite.ledeNative")}
          backTo={pageBackNav.orgStaff.to}
          backLabel={t(pageBackNav.orgStaff.labelKey)}
          backTestId="page-header-back-staff"
        />
        <ErrorState title={t("error.title")} detail={t("staffInvite.noWorkspace")} />
      </div>
    );
  }

  if (step === "sent") {
    return (
      <div
        className="staff-invite-page exits-page flex min-w-0 flex-col gap-3"
        data-testid="staff-invite-sent"
      >
        <PageHeader
          title={t("staffInvite.sentTitle")}
          description={t("staffInvite.sentLede")}
          backTo={pageBackNav.orgStaff.to}
          backLabel={t(pageBackNav.orgStaff.labelKey)}
          backTestId="page-header-back-staff"
        />
        <StatusChip tone="success">{t("staffInvite.sentBadge")}</StatusChip>
        <section className="catalog-form-section exits-animate-panel flex flex-col gap-3">
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
            {t("staffInvite.sentDetail")}
          </p>
          <Button asChild className="w-full sm:w-auto">
            <Link to="/org/staff">{t("staffInvite.backToStaff")}</Link>
          </Button>
        </section>
      </div>
    );
  }

  return (
    <div
      className="staff-invite-page exits-page flex min-w-0 flex-col gap-3"
      data-testid="staff-invite-page"
    >
      <PageHeader
        title={t("staffInvite.title")}
        description={t("staffInvite.ledeNative")}
        backTo={pageBackNav.orgStaff.to}
        backLabel={t(pageBackNav.orgStaff.labelKey)}
        backTestId="page-header-back-staff"
      />
      <StatusChip tone="info">{t("staffInvite.badge")}</StatusChip>
      {error ? <ErrorState title={t("error.title")} detail={error} /> : null}

      {step === "find" ? (
        <section className="catalog-form-section exits-animate-panel flex flex-col gap-3">
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
            {t("staffInvite.findHint")}
          </p>
          <QrScanOrEnter
            expectedPurpose="personal"
            disabled={submitting || !online}
            onResolvedPayload={(payload) => void resolveInput(payload)}
          />
        </section>
      ) : null}

      {step === "confirm" && target ? (
        <section
          className="catalog-form-section exits-animate-panel flex flex-col gap-3"
          data-testid="staff-invite-confirm"
        >
          <p className="m-0 font-semibold">{target.displayName}</p>
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">{target.publicUserId}</p>
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
            {t("staffInvite.personalAccount")}
          </p>
          <p className="m-0 text-[length:var(--exits-text-sm)]">
            {t("staffInvite.invitingTo").replace(
              "{org}",
              boundWorkspace.organizationDisplayName ?? t("staffInvite.thisBusiness"),
            )}
          </p>
          <div className="flex flex-wrap gap-2">
            <Button
              type="button"
              variant="outline"
              disabled={submitting}
              onClick={() => {
                setTarget(null);
                setStep("find");
              }}
            >
              {t("staffInvite.tryAnother")}
            </Button>
            <Button type="button" disabled={submitting} onClick={() => setStep("access")}>
              {t("staffInvite.continue")}
            </Button>
          </div>
        </section>
      ) : null}

      {step === "access" && target ? (
        <section
          className="catalog-form-section exits-animate-panel flex flex-col gap-3"
          data-testid="staff-invite-access"
        >
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
            {t("staffInvite.orgRoleFixed")}
          </p>
          <fieldset className="m-0 border-0 p-0">
            <legend className="mb-2 text-[length:var(--exits-text-sm)] font-semibold">
              {t("staffInvite.posRoleLabel")}
            </legend>
            {catalogQuery.isLoading ? (
              <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">{t("loading.label")}</p>
            ) : null}
            {catalogQuery.isError ? (
              <ErrorState title={t("error.title")} detail={t("orgRoles.loadError")} />
            ) : null}
            <div className="flex flex-col gap-2" role="radiogroup" aria-label={t("staffInvite.posRoleLabel")}>
              {roleOptions.map((option) => (
                <label
                  key={option.code}
                  className="flex items-start gap-2 text-[length:var(--exits-text-sm)]"
                  data-testid={`staff-invite-role-${option.code.toLowerCase()}`}
                >
                  <input
                    type="radio"
                    name="pos-role"
                    value={option.code}
                    checked={productRole === option.code}
                    onChange={() => setProductRole(option.code)}
                  />
                  <span className="flex min-w-0 flex-col gap-0.5">
                    <span className="font-semibold">{option.displayName}</span>
                    <span className="text-muted">{option.description}</span>
                  </span>
                </label>
              ))}
            </div>
          </fieldset>
          {productRole === POS_LOCAL_ROLE_OWNER ? (
            <p
              className="m-0 text-[length:var(--exits-text-sm)] text-muted"
              data-testid="staff-invite-pos-owner-warning"
            >
              {t("staffInvite.posOwnerWarning")}
            </p>
          ) : null}
          <div className="flex flex-wrap gap-2">
            <Button type="button" variant="outline" disabled={submitting} onClick={() => setStep("confirm")}>
              {t("staffInvite.back")}
            </Button>
            <Button
              type="button"
              disabled={submitting || !online || catalogQuery.isLoading || roleOptions.length === 0}
              data-testid="staff-invite-send"
              onClick={requestSendInvite}
            >
              {submitting ? t("staffInvite.submitting") : t("staffInvite.send")}
            </Button>
          </div>
        </section>
      ) : null}

      <Button
        type="button"
        variant="ghost"
        className="self-start"
        onClick={() => navigate("/org/staff")}
      >
        {t("staffInvite.cancel")}
      </Button>

      <ConfirmationDialog
        open={ownerConfirmOpen}
        title={t("staffAssign.ownerConfirmTitle")}
        detail={t("staffAssign.ownerConfirmMessage")}
        confirmLabel={t("staffAssign.ownerConfirmAction")}
        cancelLabel={t("staffManage.cancel")}
        onCancel={() => setOwnerConfirmOpen(false)}
        onConfirm={() => void sendInvite(POS_LOCAL_ROLE_OWNER)}
        testId="staff-invite-owner-confirm"
      />
    </div>
  );
}
