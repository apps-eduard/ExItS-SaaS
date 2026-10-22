import { useState } from "react";
import { Check, Copy, Download, Image, Link2 } from "lucide-react";
import { useQuery } from "@tanstack/react-query";
import { getMyPublicIdentity } from "@/api/platform/public-identity-client";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingSkeleton } from "@/components/exits/FoundationStates";
import { PageHeader } from "@/components/exits/PageHeader";
import { buildQrPngFile, downloadQrPng } from "@/features/qr/download-qr-png";
import { QrCodeImage } from "@/features/qr/QrCodeImage";
import { useI18n } from "@/i18n/I18nProvider";
import { buildCanonicalPersonalConnectUrl } from "@/lib/personal-connect-url";
import { personalPageBackNav } from "@/navigation/page-back-nav";

export function PersonalMyQrPage() {
  const { t } = useI18n();
  const [copied, setCopied] = useState(false);
  const [downloadError, setDownloadError] = useState(false);
  const [shareError, setShareError] = useState(false);
  const query = useQuery({
    queryKey: ["personal", "public-identity"],
    queryFn: ({ signal }) => getMyPublicIdentity(signal),
  });

  async function copyId() {
    if (!query.data) return;
    try {
      await navigator.clipboard.writeText(query.data.publicUserId);
      setCopied(true);
      window.setTimeout(() => setCopied(false), 2000);
    } catch {
      /* clipboard may be unavailable */
    }
  }

  async function downloadQr() {
    if (!query.data) return;
    setDownloadError(false);
    try {
      await downloadQrPng({
        payload: buildCanonicalPersonalConnectUrl(query.data.publicUserId),
        filename: `ExItS-${query.data.publicUserId}`,
      });
    } catch {
      setDownloadError(true);
    }
  }

  async function shareLink() {
    if (!query.data || typeof navigator.share !== "function") return;
    setShareError(false);
    const url = buildCanonicalPersonalConnectUrl(query.data.publicUserId);
    try {
      await navigator.share({
        title: t("personal.social.qrShareText"),
        text: t("personal.social.qrShareText"),
        url,
      });
    } catch (err) {
      if (err instanceof DOMException && err.name === "AbortError") {
        return;
      }
      setShareError(true);
    }
  }

  async function shareQrPicture() {
    if (!query.data || typeof navigator.share !== "function") return;
    setShareError(false);
    setDownloadError(false);
    const payload = buildCanonicalPersonalConnectUrl(query.data.publicUserId);
    const filename = `ExItS-${query.data.publicUserId}`;
    try {
      const file = await buildQrPngFile({ payload, filename });
      const shareData: ShareData = {
        title: t("personal.social.qrShareText"),
        text: t("personal.social.qrShareText"),
        files: [file],
      };
      if (typeof navigator.canShare === "function" && !navigator.canShare(shareData)) {
        await downloadQrPng({ payload, filename });
        return;
      }
      await navigator.share(shareData);
    } catch (err) {
      if (err instanceof DOMException && err.name === "AbortError") {
        return;
      }
      try {
        await downloadQrPng({ payload, filename });
      } catch {
        setShareError(true);
        setDownloadError(true);
      }
    }
  }

  if (query.isPending) return <LoadingSkeleton />;
  if (query.isError || !query.data) {
    return (
      <ErrorState
        title={t("personal.social.loadErrorTitle")}
        detail={t("personal.social.loadErrorDetail")}
      />
    );
  }

  const shareAvailable = typeof navigator !== "undefined" && typeof navigator.share === "function";
  const connectUrl = buildCanonicalPersonalConnectUrl(query.data.publicUserId);

  return (
    <div
      className="personal-page personal-my-qr-page exits-page flex w-full min-w-0 flex-col gap-3"
      data-testid="personal-my-qr-page"
    >
      <PageHeader
        title={t("personal.social.qrTitle")}
        description={t("personal.social.qrLede")}
        backTo={personalPageBackNav.more.to}
        backLabel={t(personalPageBackNav.more.labelKey)}
        backTestId="page-header-back-my-qr"
      />
      <Card
        className="catalog-form-section exits-animate-panel personal-section personal-my-qr-glass flex flex-col items-center gap-3 p-4 text-center"
        data-shell="soft-glass"
        data-testid="personal-my-qr-card"
      >
        <QrCodeImage
          payload={connectUrl}
          label={t("personal.social.qrImageAlt")}
          testId="personal-my-qr-image"
        />
        <p
          className="m-0 text-[length:var(--exits-text-sm)] font-semibold"
          data-testid="personal-qr-display-name"
        >
          {query.data.displayName}
        </p>
        <p className="m-0 break-all font-semibold tracking-wide" data-testid="personal-public-id">
          {query.data.publicUserId}
        </p>
        <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
          {t("personal.social.qrConnectHint")}
        </p>
        <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">
          {t("personal.social.qrSafety")}
        </p>
        <div className="personal-my-qr-actions" data-testid="personal-qr-actions">
          <Button
            type="button"
            intent="primary"
            appearance="glossy"
            className="personal-my-qr-actions__btn"
            data-testid="personal-qr-copy"
            onClick={() => void copyId()}
          >
            <Copy className="size-4 shrink-0" aria-hidden />
            {copied ? t("qr.copied") : t("qr.copyId")}
          </Button>
          <Button
            type="button"
            intent="primary"
            appearance="glossy"
            className="personal-my-qr-actions__btn"
            data-testid="personal-qr-download"
            onClick={() => void downloadQr()}
          >
            <Download className="size-4 shrink-0" aria-hidden />
            {t("qr.download")}
          </Button>
          {shareAvailable ? (
            <Button
              type="button"
              intent="primary"
              appearance="glossy"
              className="personal-my-qr-actions__btn"
              data-testid="personal-qr-share-link"
              onClick={() => void shareLink()}
            >
              <Link2 className="size-4 shrink-0" aria-hidden />
              {t("qr.shareLink")}
            </Button>
          ) : null}
          {shareAvailable ? (
            <Button
              type="button"
              intent="primary"
              appearance="glossy"
              className="personal-my-qr-actions__btn"
              data-testid="personal-qr-share-picture"
              onClick={() => void shareQrPicture()}
            >
              <Image className="size-4 shrink-0" aria-hidden />
              {t("qr.sharePicture")}
            </Button>
          ) : null}
        </div>
        {downloadError || shareError ? (
          <p className="m-0 text-[length:var(--exits-text-sm)] text-danger" role="alert">
            {t("branches.storefrontQr.downloadFailed")}
          </p>
        ) : null}
        {copied ? (
          <p className="m-0 inline-flex items-center gap-1 text-[length:var(--exits-text-xs)] text-muted">
            <Check className="size-3.5" aria-hidden />
            {t("qr.copied")}
          </p>
        ) : null}
      </Card>
    </div>
  );
}
