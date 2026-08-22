import { useState } from "react";
import { Check, Copy, Share2 } from "lucide-react";
import { useQuery } from "@tanstack/react-query";
import { getMyPublicIdentity } from "@/api/platform/public-identity-client";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingSkeleton } from "@/components/exits/FoundationStates";
import { PageHeader } from "@/components/exits/PageHeader";
import { QrCodeImage } from "@/features/qr/QrCodeImage";
import { useI18n } from "@/i18n/I18nProvider";

export function PersonalMyQrPage() {
  const { t } = useI18n();
  const [copied, setCopied] = useState(false);
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

  async function share() {
    if (!query.data || typeof navigator.share !== "function") return;
    try {
      await navigator.share({
        title: query.data.displayName || t("personal.social.qrTitle"),
        text: `${t("personal.social.qrShareText")} ${query.data.publicUserId}`,
      });
    } catch {
      /* user cancelled */
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

  return (
    <div
      className="mx-auto flex w-full max-w-md min-w-0 flex-col gap-4"
      data-testid="personal-my-qr-page"
    >
      <PageHeader title={t("personal.social.qrTitle")} description={t("personal.social.qrLede")} />
      <Card className="flex flex-col items-center gap-3 p-4 text-center">
        <QrCodeImage
          payload={query.data.qrPayload}
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
        <div className="flex flex-wrap justify-center gap-2">
          <Button
            type="button"
            className="min-h-11"
            data-testid="personal-qr-copy"
            onClick={() => void copyId()}
          >
            <Copy className="size-4" aria-hidden />
            {copied ? t("qr.copied") : t("qr.copyId")}
          </Button>
          {shareAvailable ? (
            <Button
              type="button"
              variant="ghost"
              className="min-h-11"
              data-testid="personal-qr-share"
              onClick={() => void share()}
            >
              <Share2 className="size-4" aria-hidden />
              {t("qr.share")}
            </Button>
          ) : null}
        </div>
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
