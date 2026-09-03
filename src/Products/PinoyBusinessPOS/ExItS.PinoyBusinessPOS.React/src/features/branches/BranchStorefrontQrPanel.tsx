import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { Copy, Download } from "lucide-react";
import { getBranchFulfillmentReadiness } from "@/api/platform/branch-fulfillment-client";
import { getOrganizationPublicIdentity } from "@/api/platform/public-identity-client";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { QrCodeImage } from "@/features/qr/QrCodeImage";
import { downloadQrPng } from "@/features/qr/download-qr-png";
import {
  buildBranchQrDownloadFilename,
  buildBranchStoreQrAcquisitionPayload,
  buildPublicBranchStoreAbsoluteUrl,
} from "@/features/store/business-qr-url";
import { branchFulfillmentEditPath } from "@/features/branches/branch-setup-tabs";
import { useI18n } from "@/i18n/I18nProvider";

type BranchStorefrontQrPanelProps = {
  organizationId: string;
  organizationDisplayName: string;
  branchId: string;
  branchName: string;
  branchStatus: string;
};

export function BranchStorefrontQrPanel({
  organizationId,
  organizationDisplayName,
  branchId,
  branchName,
  branchStatus,
}: BranchStorefrontQrPanelProps) {
  const { t } = useI18n();
  const [copied, setCopied] = useState(false);
  const [downloadError, setDownloadError] = useState<string | null>(null);

  const identityQuery = useQuery({
    queryKey: ["org", "public-identity", organizationId],
    queryFn: ({ signal }) => getOrganizationPublicIdentity(organizationId, signal),
  });

  const readinessQuery = useQuery({
    queryKey: ["branch-fulfillment-readiness", organizationId, branchId],
    queryFn: ({ signal }) =>
      getBranchFulfillmentReadiness(organizationId, branchId, signal),
  });

  const publicOrgId = identityQuery.data?.publicOrganizationId ?? null;
  const branchIsActive = branchStatus.toLowerCase() === "active";
  const storefrontOperational =
    readinessQuery.data?.customerOrderingReady === true &&
    readinessQuery.data?.customerOrderingEnabled === true &&
    !readinessQuery.data?.onlineOrdersPaused;

  // Stable public URL exists once we have ORG###### + branch GUID. Do not hide the QR
  // solely because fulfillment setup is incomplete — warn instead.
  const canShowQr = Boolean(publicOrgId && branchIsActive);
  const storeUrl = canShowQr
    ? buildPublicBranchStoreAbsoluteUrl(publicOrgId!, branchId)
    : null;
  const qrPayload = canShowQr
    ? buildBranchStoreQrAcquisitionPayload(publicOrgId!, branchId)
    : null;

  useEffect(() => {
    if (!copied) return;
    const handle = window.setTimeout(() => setCopied(false), 2000);
    return () => window.clearTimeout(handle);
  }, [copied]);

  async function copyLink() {
    if (!storeUrl) return;
    try {
      await navigator.clipboard.writeText(storeUrl);
      setCopied(true);
    } catch {
      /* ignore */
    }
  }

  async function downloadQr() {
    if (!qrPayload) return;
    setDownloadError(null);
    try {
      await downloadQrPng({
        payload: qrPayload,
        filename: buildBranchQrDownloadFilename(organizationDisplayName, branchName),
      });
    } catch {
      setDownloadError(t("branches.storefrontQr.downloadFailed"));
    }
  }

  const loading = identityQuery.isPending;
  const loadFailed = identityQuery.isError;
  const showNotReadyNotice =
    !loading &&
    !loadFailed &&
    canShowQr &&
    readinessQuery.isSuccess &&
    !storefrontOperational;
  const showBlocked =
    !loading && !loadFailed && !canShowQr;

  return (
    <Card
      className="flex flex-col gap-3 p-3"
      data-testid="branch-storefront-qr-panel"
      id="branch-storefront-qr"
    >
      <h2 className="m-0 text-[length:var(--exits-text-md)] font-semibold">
        {t("branches.storefrontQr.title")}
      </h2>

      {loading ? (
        <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
          {t("branches.storefrontQr.loading")}
        </p>
      ) : null}

      {loadFailed ? (
        <p className="m-0 text-[length:var(--exits-text-sm)] text-danger" role="alert">
          {t("branches.storefrontQr.loadFailed")}
        </p>
      ) : null}

      {showBlocked ? (
        <div className="flex flex-col gap-2" data-testid="branch-storefront-qr-not-ready">
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
            {t("branches.storefrontQr.notReady")}
          </p>
          <Button asChild type="button" variant="outline" className="min-h-11 w-full sm:w-auto">
            <Link
              to={branchFulfillmentEditPath(branchId)}
              data-testid="branch-storefront-qr-setup"
            >
              {t("branches.storefrontQr.completeSetup")}
            </Link>
          </Button>
        </div>
      ) : null}

      {qrPayload && storeUrl ? (
        <div className="flex flex-col items-center gap-2 text-center">
          {showNotReadyNotice ? (
            <div
              className="flex w-full flex-col gap-2 text-left"
              data-testid="branch-storefront-qr-setup-hint"
            >
              <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                {t("branches.storefrontQr.setupHint")}
              </p>
              <Button asChild type="button" variant="outline" className="min-h-11 w-full sm:w-auto">
                <Link
                  to={branchFulfillmentEditPath(branchId)}
                  data-testid="branch-storefront-qr-setup"
                >
                  {t("branches.storefrontQr.completeSetup")}
                </Link>
              </Button>
            </div>
          ) : null}
          <QrCodeImage
            payload={qrPayload}
            label={t("branches.storefrontQr.imageAlt").replace("{branch}", branchName)}
            testId="branch-storefront-qr-image"
            maxPx={200}
          />
          <p
            className="m-0 text-[length:var(--exits-text-sm)] text-muted"
            data-testid="branch-storefront-qr-scan-hint"
          >
            {t("branches.storefrontQr.scanHint").replace("{branch}", branchName)}
          </p>
          <p
            className="m-0 w-full break-all text-[length:var(--exits-text-xs)] text-muted"
            data-testid="branch-storefront-qr-url"
          >
            {storeUrl}
          </p>
          <div className="grid w-full grid-cols-2 gap-2">
            <Button
              type="button"
              variant="outline"
              className="min-h-11 w-full"
              onClick={() => void copyLink()}
              data-testid="branch-storefront-qr-copy"
            >
              <Copy className="size-4" aria-hidden />
              {copied ? t("branches.storefrontQr.copied") : t("branches.storefrontQr.copyLink")}
            </Button>
            <Button
              type="button"
              variant="outline"
              className="min-h-11 w-full"
              onClick={() => void downloadQr()}
              data-testid="branch-storefront-qr-download"
            >
              <Download className="size-4" aria-hidden />
              {t("branches.storefrontQr.download")}
            </Button>
          </div>
          {downloadError ? (
            <p className="m-0 text-[length:var(--exits-text-sm)] text-danger" role="alert">
              {downloadError}
            </p>
          ) : null}
        </div>
      ) : null}
    </Card>
  );
}
