import { useMemo, useState, type FormEvent } from "react";
import { Link, useNavigate, useSearchParams } from "react-router-dom";
import {
  acceptInvitationAnonymous,
  acceptInvitationAsPersonal,
  INVITATION_PERSONAL_EMAIL_UNVERIFIED,
  INVITATION_REQUIRES_AUTHENTICATED_PERSONAL,
  type AcceptInvitationResultWire,
} from "@/api/platform/staff-invitation-client";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { ErrorState } from "@/components/exits/ErrorState";
import { PageHeader } from "@/components/exits/PageHeader";
import { StatusChip } from "@/components/exits/StatusChip";
import { useI18n } from "@/i18n/I18nProvider";
import { sessionAccountClass } from "@/session/account-class";
import { useSession } from "@/session/SessionProvider";

export function StaffInvitationAcceptPage() {
  const { t } = useI18n();
  const navigate = useNavigate();
  const [params] = useSearchParams();
  const { status: sessionStatus, session, signOut } = useSession();
  const tokenFromQuery = useMemo(() => (params.get("token") ?? "").trim(), [params]);

  const [token, setToken] = useState(tokenFromQuery);
  const [password, setPassword] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [requiresPersonal, setRequiresPersonal] = useState(false);
  const [success, setSuccess] = useState<AcceptInvitationResultWire | null>(null);

  const accountClass = sessionAccountClass(session);
  const isPersonal = sessionStatus === "authenticated" && accountClass === "Personal";

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    setSubmitting(true);
    setError(null);
    setRequiresPersonal(false);

    const body = { token: token.trim(), password };
    const result = isPersonal
      ? await acceptInvitationAsPersonal(body)
      : await acceptInvitationAnonymous(body);

    setSubmitting(false);
    if (!result.ok) {
      const code = result.body?.errorCode;
      if (code === INVITATION_REQUIRES_AUTHENTICATED_PERSONAL) {
        setRequiresPersonal(true);
        setError(result.body?.detail ?? t("staffAccept.requiresPersonal"));
        return;
      }
      if (code === INVITATION_PERSONAL_EMAIL_UNVERIFIED) {
        setError(result.body?.detail ?? t("staffAccept.unverifiedEmail"));
        return;
      }
      setError(result.body?.detail ?? t("staffAccept.error"));
      return;
    }

    setSuccess(result.result);
    setPassword("");
  }

  if (success) {
    return (
      <div className="flex min-w-0 flex-col gap-4" data-testid="staff-accept-success">
        <PageHeader
          title={t("staffAccept.successTitle")}
          description={t("staffAccept.successLede")}
        />
        <StatusChip tone="success">{t("staffAccept.successBadge")}</StatusChip>
        <Card className="flex flex-col gap-3">
          <p className="m-0">
            <span className="font-semibold">{t("staffAccept.orgLabel")}: </span>
            {success.organizationDisplayName}
          </p>
          <p className="m-0">
            <span className="font-semibold">{t("staffAccept.contactLabel")}: </span>
            {success.contactEmail}
          </p>
          <p className="m-0 break-all" data-testid="staff-login-value">
            <span className="font-semibold">{t("staffAccept.staffLoginLabel")}: </span>
            {success.staffLogin}
          </p>
          <p className="m-0 text-[length:var(--exits-text-sm)] leading-relaxed text-muted">
            {t("staffAccept.separateCredentials")}
          </p>
          <Button
            className="min-h-11 w-full sm:w-auto"
            onClick={() => {
              void (async () => {
                if (sessionStatus === "authenticated") {
                  await signOut();
                }
                navigate("/sign-in", {
                  replace: true,
                  state: { staffLoginHint: success.staffLogin },
                });
              })();
            }}
          >
            {t("staffAccept.signInStaff")}
          </Button>
        </Card>
      </div>
    );
  }

  return (
    <div className="flex min-w-0 flex-col gap-4" data-testid="staff-accept-page">
      <PageHeader title={t("staffAccept.title")} description={t("staffAccept.lede")} />
      <StatusChip tone="info">
        {isPersonal ? t("staffAccept.modePersonal") : t("staffAccept.modeAnonymous")}
      </StatusChip>
      {error ? <ErrorState title={t("error.title")} detail={error} /> : null}
      {requiresPersonal ? (
        <div data-testid="staff-accept-requires-personal">
          <Card className="flex flex-col gap-3">
            <p className="m-0 text-[length:var(--exits-text-sm)] leading-relaxed">
              {t("staffAccept.requiresPersonalDetail")}
            </p>
            <Button asChild className="min-h-11 w-full sm:w-auto">
              <Link
                to="/sign-in"
                state={{ from: `/personal/invitations/accept?token=${encodeURIComponent(token)}` }}
              >
                {t("staffAccept.signInPersonal")}
              </Link>
            </Button>
          </Card>
        </div>
      ) : null}
      <Card>
        <form className="flex flex-col gap-4" onSubmit={(event) => void handleSubmit(event)}>
          <Input
            label={t("staffAccept.tokenLabel")}
            name="token"
            value={token}
            onChange={(event) => setToken(event.target.value)}
            required
          />
          <Input
            label={t("staffAccept.passwordLabel")}
            name="password"
            type="password"
            autoComplete="new-password"
            value={password}
            onChange={(event) => setPassword(event.target.value)}
            required
          />
          <p className="m-0 text-[length:var(--exits-text-sm)] leading-relaxed text-muted">
            {t("staffAccept.passwordHint")}
          </p>
          <Button type="submit" className="min-h-11 w-full sm:w-auto" disabled={submitting}>
            {submitting ? t("staffAccept.submitting") : t("staffAccept.submit")}
          </Button>
        </form>
      </Card>
    </div>
  );
}
