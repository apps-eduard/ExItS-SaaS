import { useEffect, useMemo, useState } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { Building2, Loader2, WifiOff } from "lucide-react";
import { listLinkedMerchants } from "@/api/platform/linked-merchants-client";
import { resolvePublicOrganizationId } from "@/api/platform/public-identity-client";
import { lookupPublicStoreLanding } from "@/api/platform/public-store-client";
import { PlatformApiError } from "@/api/platform/platform-http";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingSkeleton } from "@/components/exits/FoundationStates";
import { useBrowserOnline } from "@/connectivity/browser-online";
import { normalizePublicOrganizationId } from "@/features/store/business-qr-url";
import { InstallExitsOffer } from "@/features/store/InstallExitsOffer";
import {
  buildSignInHrefForStore,
  buildSignUpHrefForStore,
  rememberStoreAcquisitionIntent,
} from "@/features/store/store-acquisition";
import { useI18n } from "@/i18n/I18nProvider";
import { sessionAccountClass } from "@/session/account-class";
import { useSession } from "@/session/SessionProvider";

export function PublicStoreLandingPage() {
  const { t } = useI18n();
  const navigate = useNavigate();
  const online = useBrowserOnline();
  const { status, session } = useSession();
  const { publicOrganizationId: rawParam = "" } = useParams();
  const publicOrganizationId = normalizePublicOrganizationId(rawParam);
  const accountClass = sessionAccountClass(session);
  const isAuthenticated = status === "authenticated";
  const isPersonal = isAuthenticated && accountClass === "Personal";
  const isOrgStaff =
    isAuthenticated && (accountClass === "Organization" || accountClass === "Platform");

  useEffect(() => {
    if (publicOrganizationId) {
      rememberStoreAcquisitionIntent(publicOrganizationId);
    }
  }, [publicOrganizationId]);

  const landingQuery = useQuery({
    queryKey: ["public", "store", publicOrganizationId],
    enabled: Boolean(publicOrganizationId) && online,
    queryFn: ({ signal }) => lookupPublicStoreLanding(publicOrganizationId!, signal),
    retry: false,
    meta: { suppressGlobalError: true, operation: "public store landing" },
  });

  const continueQuery = useQuery({
    queryKey: ["public", "store", "continue", publicOrganizationId, session?.userId],
    enabled: Boolean(publicOrganizationId) && isPersonal && online && landingQuery.isSuccess,
    queryFn: async ({ signal }) => {
      const resolved = await resolvePublicOrganizationId(
        publicOrganizationId!,
        "public-store-continue",
        signal,
      );
      const linked = await listLinkedMerchants(1, 50, signal);
      const match = linked.items.find((m) => m.organizationId === resolved.organizationId);
      return {
        organizationId: resolved.organizationId,
        linked: Boolean(match),
        displayName: resolved.displayName,
      };
    },
    retry: false,
    meta: { suppressGlobalError: true, operation: "public store continue" },
  });

  const [continuing, setContinuing] = useState(false);

  const unavailableDetail = useMemo(() => {
    if (!publicOrganizationId) {
      return t("store.landing.invalidId");
    }
    if (!online) {
      return t("store.landing.offlineDetail");
    }
    if (landingQuery.isError) {
      const err = landingQuery.error;
      if (err instanceof PlatformApiError && err.status === 404) {
        return t("store.landing.unavailableDetail");
      }
      return t("store.landing.loadErrorDetail");
    }
    return null;
  }, [publicOrganizationId, online, landingQuery.isError, landingQuery.error, t]);

  async function handleContinueToStore() {
    if (!continueQuery.data || continuing) {
      return;
    }
    setContinuing(true);
    try {
      if (continueQuery.data.linked) {
        await navigate(`/personal/linked-merchants/${continueQuery.data.organizationId}/shop`, {
          replace: true,
        });
      } else {
        await navigate("/personal/linked-merchants", { replace: true });
      }
    } finally {
      setContinuing(false);
    }
  }

  if (!publicOrganizationId) {
    return (
      <PublicShell>
        <ErrorState
          title={t("store.landing.unavailableTitle")}
          detail={t("store.landing.invalidId")}
        />
      </PublicShell>
    );
  }

  if (!online) {
    return (
      <PublicShell>
        <div
          className="flex flex-col items-center gap-3 text-center"
          data-testid="public-store-offline"
        >
          <WifiOff className="size-8 text-muted" aria-hidden />
          <h1 className="m-0 text-[length:var(--exits-text-lg)] font-bold">
            {t("store.landing.offlineTitle")}
          </h1>
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
            {t("store.landing.offlineDetail")}
          </p>
        </div>
      </PublicShell>
    );
  }

  if (landingQuery.isPending) {
    return (
      <PublicShell>
        <LoadingSkeleton />
      </PublicShell>
    );
  }

  if (landingQuery.isError || !landingQuery.data) {
    return (
      <PublicShell>
        <ErrorState
          title={t("store.landing.unavailableTitle")}
          detail={unavailableDetail ?? t("store.landing.unavailableDetail")}
        />
      </PublicShell>
    );
  }

  const store = landingQuery.data;

  return (
    <PublicShell>
      <div
        className="mx-auto flex w-full max-w-md min-w-0 flex-col gap-4"
        data-testid="public-store-landing-page"
      >
        <Card className="flex flex-col items-center gap-3 p-5 text-center">
          <Building2 className="size-10 text-primary" aria-hidden />
          <h1
            className="m-0 text-[length:var(--exits-text-xl)] font-bold tracking-tight"
            data-testid="public-store-name"
          >
            {store.displayName}
          </h1>
          <p
            className="m-0 break-all font-semibold tracking-wide text-muted"
            data-testid="public-store-org-id"
          >
            {store.publicOrganizationId}
          </p>
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
            {t("store.landing.lede")}
          </p>
          {!store.orderingAvailable ? (
            <p className="m-0 text-[length:var(--exits-text-sm)] text-muted" role="status">
              {t("store.landing.orderingUnavailable")}
            </p>
          ) : null}
        </Card>

        {isOrgStaff ? (
          <Card className="flex flex-col gap-3 p-4" data-testid="public-store-staff-blocked">
            <p className="m-0 text-[length:var(--exits-text-sm)] font-semibold">
              {t("store.landing.staffTitle")}
            </p>
            <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
              {t("store.landing.staffDetail")}
            </p>
            <Button asChild className="min-h-11" data-testid="public-store-sign-in-personal">
              <Link to={buildSignInHrefForStore(store.publicOrganizationId)}>
                {t("store.landing.signInPersonal")}
              </Link>
            </Button>
          </Card>
        ) : null}

        {isPersonal ? (
          <div className="flex flex-col gap-2">
            <Button
              type="button"
              className="min-h-11"
              data-testid="public-store-continue"
              disabled={continuing || continueQuery.isPending || continueQuery.isError}
              onClick={() => void handleContinueToStore()}
            >
              {continuing || continueQuery.isPending ? (
                <Loader2 className="size-4 animate-spin" aria-hidden />
              ) : null}
              {t("store.landing.continueStore")}
            </Button>
            {continueQuery.isError ? (
              <p className="m-0 text-[length:var(--exits-text-sm)] text-destructive" role="alert">
                {t("store.landing.continueFailed")}
              </p>
            ) : null}
            <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">
              {t("store.landing.linkConsentHint")}
            </p>
            <InstallExitsOffer />
          </div>
        ) : null}

        {!isAuthenticated ? (
          <div className="flex flex-col gap-2">
            <Button asChild className="min-h-11" data-testid="public-store-sign-in">
              <Link to={buildSignInHrefForStore(store.publicOrganizationId)}>
                {t("store.landing.signIn")}
              </Link>
            </Button>
            <Button
              asChild
              variant="ghost"
              className="min-h-11"
              data-testid="public-store-create-account"
            >
              <Link to={buildSignUpHrefForStore(store.publicOrganizationId)}>
                {t("store.landing.createAccount")}
              </Link>
            </Button>
            <InstallExitsOffer />
          </div>
        ) : null}
      </div>
    </PublicShell>
  );
}

function PublicShell({ children }: { children: React.ReactNode }) {
  return (
    <div className="min-h-dvh bg-background px-4 py-8 text-foreground">
      <div className="mx-auto w-full max-w-lg">{children}</div>
    </div>
  );
}
