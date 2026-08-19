import { useMemo, useState } from "react";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { PageHeader } from "@/components/exits/PageHeader";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { StatusChip } from "@/components/ui/badge";
import { LoadingState } from "@/components/ui/skeleton";
import { useI18n } from "@/i18n/I18nProvider";
import { usePreferences } from "@/hooks/usePreferences";
import { normalizeDiagnosticError } from "@/lib/diagnostics/normalize-diagnostic-error";

export function FoundationHomePage() {
  const { t } = useI18n();
  const { preferences } = usePreferences();
  const [boom, setBoom] = useState(false);

  const sampleRecord = useMemo(
    () =>
      normalizeDiagnosticError(new Error("Unable to complete this operation."), {
        locale: preferences.locale,
        theme: preferences.theme,
        pathname: "/",
        createReference: () => "ERR-FOUND",
        now: () => "2026-08-19T00:00:00.000Z",
        browserPlatform: "foundation-preview",
      }),
    [preferences.locale, preferences.theme],
  );

  if (boom) {
    throw new Error("Simulated foundation runtime error");
  }

  return (
    <div className="flex flex-col gap-5">
      <PageHeader title={t("foundation.title")} subtitle={t("foundation.subtitle")} />
      <p className="m-0 max-w-prose">{t("foundation.intro")}</p>
      <p className="m-0 rounded-[var(--exits-radius-md)] border border-border bg-surface-muted px-3 py-2 text-[length:var(--exits-text-sm)] text-muted">
        {t("foundation.notLiveNotice")}
      </p>

      <Card data-density="compact" className="flex flex-col gap-3">
        <div className="flex flex-wrap items-center gap-2">
          <h2 className="m-0 text-[length:var(--exits-text-lg)] font-semibold">
            {t("foundation.opsPreviewTitle")}
          </h2>
          <StatusChip tone="info">{t("status.preview")}</StatusChip>
        </div>
        <p className="m-0 text-muted">{t("foundation.opsPreviewHint")}</p>
        <div>
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
            {t("foundation.sampleAmountLabel")}
          </p>
          <p className="tabular-nums m-0 text-[length:var(--exits-text-2xl)] font-bold">1,250.00</p>
        </div>
        <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
          {t("foundation.densityNote")}
        </p>
      </Card>

      <section className="flex flex-col gap-4">
        <h2 className="m-0 text-[length:var(--exits-text-lg)] font-semibold">
          {t("foundation.statesTitle")}
        </h2>
        <Card>
          <EmptyState title={t("empty.title")} body={t("empty.body")} />
        </Card>
        <Card>
          <LoadingState label={t("loading.label")} />
        </Card>
        <Card>
          <ErrorState title={t("error.title")} body={t("error.body")} record={sampleRecord} />
        </Card>
        <Button type="button" variant="outline" onClick={() => setBoom(true)}>
          {t("error.simulate")}
        </Button>
      </section>
    </div>
  );
}
