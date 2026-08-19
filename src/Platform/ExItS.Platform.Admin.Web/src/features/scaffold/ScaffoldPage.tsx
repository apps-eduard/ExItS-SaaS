import { Alert } from "@/components/ui/alert";
import { Avatar } from "@/components/ui/avatar";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { DropdownMenu, DropdownMenuItem } from "@/components/ui/dropdown-menu";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Separator } from "@/components/ui/separator";
import { Sheet } from "@/components/ui/sheet";
import { Skeleton } from "@/components/ui/skeleton";
import { Tooltip } from "@/components/ui/tooltip";
import { usePreferences } from "@/hooks/use-preferences";
import { formatCurrency, formatDate, formatNumber } from "@/lib/i18n/format";
import type { Density, Language, ThemeMode } from "@/lib/preferences/ui-preferences";

export function ScaffoldPage() {
  const { t, theme, language, density, setTheme, setLanguage, setDensity } = usePreferences();
  const sampleDate = new Date("2026-08-19T10:00:00Z");

  return (
    <main className="mx-auto max-w-4xl space-y-6 p-[var(--exits-page-padding)]">
      <header>
        <p className="text-[length:var(--exits-text-xs)] font-semibold tracking-wide text-muted uppercase">
          ExItS
        </p>
        <h1 className="text-[length:var(--exits-text-xl)] font-bold">{t("app.title")}</h1>
        <p className="mt-2 text-muted">{t("scaffold.subtitle")}</p>
      </header>

      <section className="grid gap-3 sm:grid-cols-3">
        {(["system", "light", "dark"] as const).map((mode) => (
          <Button
            key={mode}
            type="button"
            variant={theme === mode ? "default" : "outline"}
            aria-pressed={theme === mode}
            onClick={() => setTheme(mode satisfies ThemeMode)}
          >
            {t(`preferences.theme.${mode}`)}
          </Button>
        ))}
      </section>

      <section className="flex flex-wrap gap-3">
        <Button
          type="button"
          variant={language === "en" ? "default" : "outline"}
          aria-pressed={language === "en"}
          onClick={() => setLanguage("en" satisfies Language)}
        >
          {t("preferences.language.en")}
        </Button>
        <Button
          type="button"
          variant={language === "fil-PH" ? "default" : "outline"}
          aria-pressed={language === "fil-PH"}
          onClick={() => setLanguage("fil-PH")}
        >
          {t("preferences.language.fil")}
        </Button>
      </section>

      <section className="flex flex-wrap gap-3">
        {(["comfortable", "balanced", "compact"] as const).map((value) => (
          <Button
            key={value}
            type="button"
            variant={density === value ? "default" : "outline"}
            aria-pressed={density === value}
            onClick={() => setDensity(value satisfies Density)}
          >
            {t(`preferences.density.${value}`)}
          </Button>
        ))}
      </section>

      <section aria-label={t("scaffold.surfaces")} className="grid gap-3 sm:grid-cols-3">
        <div className="rounded-[var(--exits-density-radius)] border border-border bg-background p-4 font-[var(--exits-font-tabular)] text-sm">
          --exits-bg
        </div>
        <div className="rounded-[var(--exits-density-radius)] border border-border bg-surface p-4 font-[var(--exits-font-tabular)] text-sm shadow-sm">
          --exits-surface
        </div>
        <div className="rounded-[var(--exits-density-radius)] border border-border bg-surface-elevated p-4 font-[var(--exits-font-tabular)] text-sm shadow-md">
          --exits-surface-elevated
        </div>
      </section>

      <Card>
        <h2 className="text-[length:var(--exits-text-lg)] font-bold">{t("scaffold.controls")}</h2>
        <div className="mt-4 grid gap-3">
          <Label htmlFor="sample-name">{t("scaffold.sampleLabel")}</Label>
          <Input id="sample-name" placeholder={t("scaffold.samplePlaceholder")} />
          <p className="text-[length:var(--exits-text-xs)] text-muted">
            {t("scaffold.sampleHelp")}
          </p>
          <div className="flex flex-wrap gap-2">
            <Button type="button">{t("scaffold.primaryAction")}</Button>
            <Button type="button" variant="outline">
              {t("scaffold.secondaryAction")}
            </Button>
            <Button type="button" variant="destructive">
              {t("scaffold.destructiveAction")}
            </Button>
          </div>
        </div>
      </Card>

      <Card>
        <h2 className="text-[length:var(--exits-text-lg)] font-bold">{t("scaffold.status")}</h2>
        <div className="mt-3 flex flex-wrap items-center gap-2">
          <Badge tone="success">{t("badge.success")}</Badge>
          <Badge tone="warning">{t("badge.warning")}</Badge>
          <Badge tone="danger">{t("badge.danger")}</Badge>
          <Badge tone="info">{t("badge.info")}</Badge>
          <Avatar initials="EA" />
        </div>
        <Separator className="my-4" />
        <Alert title={t("alert.title")}>{t("alert.body")}</Alert>
        <Skeleton className="mt-4 h-8 w-full" />
        <p className="mt-3 font-[var(--exits-font-tabular)] text-sm">
          {formatDate(sampleDate, language)} · {formatNumber(12890, language)} ·{" "}
          {formatCurrency(2500, language)}
        </p>
        <div className="mt-4 flex flex-wrap gap-2">
          <Tooltip content={t("ui.menu.open")}>
            <span>
              <DropdownMenu
                label={t("ui.menu")}
                trigger={
                  <Button type="button" variant="outline">
                    {t("ui.menu.open")}
                  </Button>
                }
              >
                <DropdownMenuItem>{t("scaffold.secondaryAction")}</DropdownMenuItem>
              </DropdownMenu>
            </span>
          </Tooltip>
          <Sheet
            title={t("ui.sheet.title")}
            trigger={
              <Button type="button" variant="secondary">
                {t("ui.sheet.open")}
              </Button>
            }
          >
            {t("ui.sheet.body")}
          </Sheet>
        </div>
      </Card>
    </main>
  );
}
