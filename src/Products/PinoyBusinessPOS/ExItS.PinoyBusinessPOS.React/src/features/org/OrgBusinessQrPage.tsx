import { useState } from "react";
import { Copy, Link2, Share2 } from "lucide-react";
import { useQuery } from "@tanstack/react-query";
import { getOrganizationPublicIdentity } from "@/api/platform/public-identity-client";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingSkeleton } from "@/components/exits/FoundationStates";
import { PageHeader } from "@/components/exits/PageHeader";
import { pageBackNav } from "@/navigation/page-back-nav";
import { QrCodeImage } from "@/features/qr/QrCodeImage";
import {
  buildBusinessQrAcquisitionPayload,
  buildPublicStoreAbsoluteUrl,
} from "@/features/store/business-qr-url";
import { useI18n } from "@/i18n/I18nProvider";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

export function OrgBusinessQrPage() {
  const { t } = useI18n();
  const { boundWorkspace } = useWorkspace();
  const organizationId = boundWorkspace?.organizationId ?? null;
  const [copiedLink, setCopiedLink] = useState(false);
  const [copiedId, setCopiedId] = useState(false);

  const query = useQuery({
    queryKey: ["org", "public-identity", organizationId],
    enabled: organizationId !== null,
    queryFn: ({ signal }) => getOrganizationPublicIdentity(organizationId!, signal),
  });

  const storeUrl = query.data
    ? buildPublicStoreAbsoluteUrl(query.data.publicOrganizationId)
    : null;
  const qrPayload = query.data
    ? buildBusinessQrAcquisitionPayload(query.data.publicOrganizationId)
    : null;

  async function copyStoreLink() {
    if (!storeUrl) return;
    try {
      await navigator.clipboard.writeText(storeUrl);
      setCopiedLink(true);
      window.setTimeout(() => setCopiedLink(false), 2000);
    } catch {
      /* ignore */
    }
  }

  async function copyBusinessId() {
    if (!query.data) return;
    try {
      await navigator.clipboard.writeText(query.data.publicOrganizationId);
      setCopiedId(true);
      window.setTimeout(() => setCopiedId(false), 2000);
    } catch {
      /* ignore */
    }
  }

  async function share() {
    if (!query.data || !storeUrl || typeof navigator.share !== "function") return;
    try {
      await navigator.share({
        title: query.data.displayName,
        text: t("org.businessQr.shareText"),
        url: storeUrl,
      });
    } catch {
      /* cancelled */
    }
  }

  if (!organizationId) {
    return (
      <ErrorState title={t("org.businessQr.loadErrorTitle")} detail={t("org.businessQr.noOrg")} />
    );
  }
  if (query.isPending) return <LoadingSkeleton />;
  if (query.isError || !query.data || !qrPayload || !storeUrl) {
    return (
      <ErrorState
        title={t("org.businessQr.loadErrorTitle")}
        detail={t("org.businessQr.loadErrorDetail")}
      />
    );
  }

  const shareAvailable = typeof navigator !== "undefined" && typeof navigator.share === "function";

  return (
    <div
      className="mx-auto flex w-full max-w-md min-w-0 flex-col gap-4"
      data-testid="org-business-qr-page"
    >
      <PageHeader
        title={t("org.businessQr.title")}
        description={t("org.businessQr.lede")}
        backTo={pageBackNav.org.to}
        backLabel={t(pageBackNav.org.labelKey)}
        backTestId="page-header-back-org"
      />
      <Card className="flex flex-col items-center gap-3 p-4 text-center">
        <QrCodeImage
          payload={qrPayload}
          label={t("org.businessQr.imageAlt")}
          testId="org-business-qr-image"
        />
        <p
          className="m-0 text-[length:var(--exits-text-sm)] font-semibold"
          data-testid="org-business-qr-name"
        >
          {query.data.displayName}
        </p>
        <p className="m-0 break-all font-semibold tracking-wide" data-testid="org-public-id">
          {query.data.publicOrganizationId}
        </p>
        <p
          className="m-0 break-all text-[length:var(--exits-text-xs)] text-muted"
          data-testid="org-business-store-url"
        >
          {storeUrl}
        </p>
        <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
          {t("org.businessQr.connectHint")}
        </p>
        <div className="flex flex-wrap justify-center gap-2">
          <Button
            type="button"
            data-testid="org-business-qr-copy-link"
            onClick={() => void copyStoreLink()}
          >
            <Link2 className="size-4" aria-hidden />
            {copiedLink ? t("qr.copied") : t("org.businessQr.copyStoreLink")}
          </Button>
          <Button
            type="button"
            variant="ghost"
            data-testid="org-business-qr-copy"
            onClick={() => void copyBusinessId()}
          >
            <Copy className="size-4" aria-hidden />
            {copiedId ? t("qr.copied") : t("org.businessQr.copyBusinessId")}
          </Button>
          {shareAvailable ? (
            <Button
              type="button"
              variant="ghost"
              data-testid="org-business-qr-share"
              onClick={() => void share()}
            >
              <Share2 className="size-4" aria-hidden />
              {t("qr.share")}
            </Button>
          ) : null}
        </div>
      </Card>
    </div>
  );
}
