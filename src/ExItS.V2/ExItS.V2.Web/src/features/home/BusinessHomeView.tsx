import { AlertTriangle, Building2 } from "lucide-react";
import { useTranslation } from "react-i18next";
import type { BusinessHomeData } from "@/api/home-business-data";
import { Avatar, AvatarFallback } from "@/components/ui/avatar";
import { Badge } from "@/components/ui/badge";
import { Card, CardContent } from "@/components/ui/card";
import { EmptyState } from "@/components/ui/empty-state";
import { formatCurrency, formatNumber } from "@/lib/format";
import { cn } from "@/lib/utils";
import { MotionList, MotionListItem, MotionListPresence } from "@/motion/MotionList";
import { MotionReveal } from "@/motion/MotionReveal";
import { SectionLabel, WidgetMessage, isFailedWidget } from "./home-ui";
import { UnavailableButton } from "./UnavailableAction";

function greetingPeriod():
  | "periodMorning"
  | "periodAfternoon"
  | "periodEvening" {
  const hour = new Date().getHours();
  if (hour < 12) return "periodMorning";
  if (hour < 18) return "periodAfternoon";
  return "periodEvening";
}

function Chip({
  label,
  value,
  warn,
}: {
  label: string;
  value: string;
  warn?: boolean;
}) {
  return (
    <div
      className={cn(
        "rounded-xl border px-3 py-2",
        warn ? "border-warning/40 bg-warning/10" : "border-border bg-card",
      )}
    >
      <p className="text-[10px] font-medium text-muted-foreground">{label}</p>
      <p className="mt-0.5 font-mono text-sm font-semibold tabular-nums">{value}</p>
    </div>
  );
}

function BusinessHomeView({
  data,
  loading,
  reveal = true,
  onRetry,
}: {
  data: BusinessHomeData | null;
  loading: boolean;
  reveal?: boolean;
  onRetry: () => void;
}) {
  const { t } = useTranslation();
  const firstName = (data?.profile.displayName ?? "").split(/\s+/)[0] || "there";

  return (
    <div className="space-y-5 md:space-y-6">
      <MotionReveal index={0} animate={reveal}>
      <section className="flex items-start gap-3">
        <Avatar className="size-11 border border-border shadow-sm sm:size-12">
          <AvatarFallback className="bg-primary/10 text-sm font-semibold text-primary">
            {data?.profile.initials ?? "?"}
          </AvatarFallback>
        </Avatar>
        <div className="min-w-0 flex-1 space-y-1">
          <div className="flex flex-wrap items-center gap-2">
            <Badge variant="default" className="px-1.5 py-0 text-[10px]">
              {t("nav.business")}
            </Badge>
            {data?.workspaceLabel ? (
              <span className="text-[11px] text-muted-foreground">
                {data.workspaceLabel}
              </span>
            ) : null}
          </div>
          <h1 className="text-[1.35rem] font-semibold leading-tight tracking-tight sm:text-2xl">
            {loading
              ? t("auth.loadingSession")
              : t("home.greeting", {
                  period: t(`home.${greetingPeriod()}`),
                  name: firstName,
                })}
          </h1>
          <p className="text-sm text-muted-foreground">{t("home.businessSubtitle")}</p>
        </div>
      </section>
      </MotionReveal>

      <MotionReveal index={1} animate={reveal} className="space-y-2.5">
      <section className="space-y-2.5">
        <SectionLabel>{t("home.todayTitle")}</SectionLabel>
        <Card className="overflow-hidden border-primary/20 shadow-none">
          <CardContent className="space-y-3 p-4">
            {loading || !data ? (
              <WidgetMessage status="loading" />
            ) : isFailedWidget(data.today) ? (
              <WidgetMessage
                status={data.today.status}
                message={data.today.message}
                onRetry={onRetry}
              />
            ) : (
              <>
                <div>
                  <p className="text-[11px] font-medium text-muted-foreground">
                    {t("home.metricSales")}
                  </p>
                  <p className="mt-1 font-mono text-2xl font-semibold tracking-tight tabular-nums sm:text-3xl">
                    {formatCurrency(data.today.data!.salesTotal)}
                  </p>
                  <p className="mt-1 text-xs text-muted-foreground">
                    {data.today.data!.saleCount === 0
                      ? t("homeData.zeroSales")
                      : t("home.saleCount", {
                          count: data.today.data!.saleCount,
                        })}
                  </p>
                </div>
                <div className="grid grid-cols-3 gap-2">
                  <Chip
                    label={t("home.metricPosUtang")}
                    value={formatCurrency(data.today.data!.openUtang)}
                    warn={data.today.data!.openUtang > 0}
                  />
                  <Chip
                    label={t("home.metricLowStock")}
                    value={formatNumber(data.today.data!.lowStock)}
                    warn={data.today.data!.lowStock > 0}
                  />
                  <Chip
                    label={t("home.metricOpenShifts")}
                    value={formatNumber(data.today.data!.openShifts)}
                  />
                </div>
              </>
            )}
          </CardContent>
        </Card>
      </section>
      </MotionReveal>

      <MotionReveal index={2} animate={reveal} className="space-y-2.5">
      <section className="space-y-2.5">
        <SectionLabel>
          <span className="inline-flex items-center gap-1.5">
            <AlertTriangle className="size-3.5" aria-hidden />
            {t("home.needsAttentionTitle")}
          </span>
        </SectionLabel>
        <Card className="shadow-none">
          <CardContent className="p-0">
            {loading || !data ? (
              <div className="p-4">
                <WidgetMessage status="loading" />
              </div>
            ) : isFailedWidget(data.attention) ? (
              <div className="p-4">
                <WidgetMessage
                  status={data.attention.status}
                  message={data.attention.message}
                  onRetry={onRetry}
                />
              </div>
            ) : data.attention.status === "empty" ? (
              <div className="p-4">
                <EmptyState
                  title={t("homeData.zeroAttention")}
                  className="border-0 bg-transparent py-6"
                />
              </div>
            ) : (
              <MotionListPresence>
                <MotionList className="divide-y divide-border">
                  {data.attention.data!.items.map((item, index) => (
                    <MotionListItem key={item.id} id={item.id} index={index}>
                      <UnavailableButton className="flex w-full items-center justify-between gap-3 px-4 py-3 text-left transition-colors duration-150 hover:bg-muted/40">
                        <div className="min-w-0 space-y-0.5">
                          <Badge
                            variant={item.kind === "expired" ? "destructive" : "outline"}
                            className="px-1.5 py-0 text-[10px]"
                          >
                            {t(`home.bizAttention.${item.kind}`)}
                          </Badge>
                          <p className="truncate text-sm font-medium">{item.title}</p>
                        </div>
                        <span className="shrink-0 text-xs text-muted-foreground">
                          {item.kind === "utang"
                            ? formatCurrency(Number(item.detail) || 0)
                            : item.detail}
                        </span>
                      </UnavailableButton>
                    </MotionListItem>
                  ))}
                </MotionList>
              </MotionListPresence>
            )}
          </CardContent>
        </Card>
      </section>
      </MotionReveal>

      <MotionReveal index={3} animate={reveal} className="space-y-2.5">
      <section className="space-y-2.5">
        <SectionLabel>{t("home.businessShortcuts")}</SectionLabel>
        <div className="grid grid-cols-2 gap-2 sm:grid-cols-3 md:grid-cols-5">
          {[
            t("home.shortcutBranches"),
            t("home.shortcutStaff"),
            t("home.shortcutAreas"),
            t("home.shortcutDashboard"),
            t("home.shortcutReports"),
          ].map((label) => (
            <UnavailableButton
              key={label}
              className="flex min-h-14 items-center gap-2 rounded-xl border border-border bg-card px-3 py-2.5 text-left"
            >
              <span className="inline-flex size-8 items-center justify-center rounded-lg bg-primary/10 text-primary">
                <Building2 className="size-4" aria-hidden />
              </span>
              <span className="text-xs font-semibold leading-snug">{label}</span>
            </UnavailableButton>
          ))}
        </div>
        <p className="text-[11px] text-muted-foreground">
          {t("home.planCapacityGap")}
        </p>
      </section>
      </MotionReveal>
    </div>
  );
}

export { BusinessHomeView };
