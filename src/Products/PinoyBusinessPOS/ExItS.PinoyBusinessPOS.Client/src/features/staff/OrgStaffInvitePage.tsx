import { useState, type FormEvent } from "react";
import { Link, useNavigate } from "react-router-dom";
import { createStaffInvitation } from "@/api/platform/staff-invitation-client";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { ErrorState } from "@/components/exits/ErrorState";
import { PageHeader } from "@/components/exits/PageHeader";
import { pageBackNav } from "@/navigation/page-back-nav";
import { StatusChip } from "@/components/exits/StatusChip";
import { useI18n } from "@/i18n/I18nProvider";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

export function OrgStaffInvitePage() {
  const { t } = useI18n();
  const navigate = useNavigate();
  const { boundWorkspace } = useWorkspace();
  const [contactEmail, setContactEmail] = useState("");
  const [displayName, setDisplayName] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [createdToken, setCreatedToken] = useState<string | null>(null);

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    if (!boundWorkspace) {
      setError(t("staffInvite.noWorkspace"));
      return;
    }
    setSubmitting(true);
    setError(null);
    const result = await createStaffInvitation({
      organizationId: boundWorkspace.organizationId,
      contactEmail,
      displayName: displayName.trim() || undefined,
    });
    setSubmitting(false);
    if (!result.ok) {
      setError(result.body?.detail ?? t("staffInvite.error"));
      return;
    }
    if (result.invitation.acceptToken) {
      setCreatedToken(result.invitation.acceptToken);
    } else {
      navigate("/org", { replace: true });
    }
  }

  if (createdToken) {
    const acceptPath = `/personal/invitations/accept?token=${encodeURIComponent(createdToken)}`;
    return (
      <div className="flex min-w-0 flex-col gap-4" data-testid="staff-invite-created">
        <PageHeader
          title={t("staffInvite.createdTitle")}
          description={t("staffInvite.createdLede")}
          backTo={pageBackNav.org.to}
          backLabel={t(pageBackNav.org.labelKey)}
          backTestId="page-header-back-org"
        />
        <StatusChip tone="success">{t("staffInvite.createdBadge")}</StatusChip>
        <Card className="flex flex-col gap-3">
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
            {t("staffInvite.contactIsNotLogin")}
          </p>
          <p className="m-0 break-all text-[length:var(--exits-text-sm)]">
            <span className="font-semibold">{t("staffInvite.acceptLinkLabel")}: </span>
            {acceptPath}
          </p>
          <Button asChild className="min-h-11 w-full sm:w-auto">
            <Link to={acceptPath}>{t("staffInvite.openAccept")}</Link>
          </Button>
        </Card>
      </div>
    );
  }

  return (
    <div className="flex min-w-0 flex-col gap-4" data-testid="staff-invite-page">
      <PageHeader
        title={t("staffInvite.title")}
        description={t("staffInvite.lede")}
        backTo={pageBackNav.org.to}
        backLabel={t(pageBackNav.org.labelKey)}
        backTestId="page-header-back-org"
      />
      <StatusChip tone="info">{t("staffInvite.badge")}</StatusChip>
      {error ? <ErrorState title={t("error.title")} detail={error} /> : null}
      <Card>
        <form className="flex flex-col gap-4" onSubmit={(event) => void handleSubmit(event)}>
          <Input
            label={t("staffInvite.contactEmailLabel")}
            name="contactEmail"
            type="email"
            autoComplete="email"
            value={contactEmail}
            onChange={(event) => setContactEmail(event.target.value)}
            required
          />
          <p className="m-0 text-[length:var(--exits-text-sm)] leading-relaxed text-muted">
            {t("staffInvite.contactEmailHint")}
          </p>
          <Input
            label={t("staffInvite.displayNameLabel")}
            name="displayName"
            value={displayName}
            onChange={(event) => setDisplayName(event.target.value)}
          />
          <p className="m-0 text-[length:var(--exits-text-sm)] leading-relaxed text-muted">
            {t("staffInvite.roleHint")}
          </p>
          <Button type="submit" className="min-h-11 w-full sm:w-auto" disabled={submitting}>
            {submitting ? t("staffInvite.submitting") : t("staffInvite.submit")}
          </Button>
        </form>
      </Card>
    </div>
  );
}
