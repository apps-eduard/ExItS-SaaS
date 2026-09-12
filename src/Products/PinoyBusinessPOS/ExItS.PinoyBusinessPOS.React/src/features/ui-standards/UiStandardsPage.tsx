import { useState } from "react";
import { Navigate } from "react-router-dom";
import { isFrontendLocalValidationMode } from "@/api/platform/local-validation-gate";
import { PageHeader } from "@/components/exits/PageHeader";
import { UnderlineTabBar } from "@/components/exits/UnderlineTabBar";
import { useI18n } from "@/i18n/I18nProvider";
import { UiStandardsButtonsPanel } from "@/features/ui-standards/UiStandardsButtonsPanel";
import { UiStandardsChipsPanel } from "@/features/ui-standards/UiStandardsChipsPanel";
import { UiStandardsTabsPanel } from "@/features/ui-standards/UiStandardsTabsPanel";
import { UiStandardsModuleSubnavPanel } from "@/features/ui-standards/UiStandardsModuleSubnavPanel";
import { UiStandardsActionChipsPanel } from "@/features/ui-standards/UiStandardsActionChipsPanel";
import { UiStandardsCardsPanel } from "@/features/ui-standards/UiStandardsCardsPanel";
import { UiStandardsTablesPanel } from "@/features/ui-standards/UiStandardsTablesPanel";
import { UiStandardsDisclosureToolbar } from "@/features/ui-standards/UiStandardsDisclosureToolbar";
import { useUiStandardsDisclosure } from "@/features/ui-standards/useUiStandardsDisclosure";
import type { UiStandardsTab } from "@/features/ui-standards/ui-standards-disclosure";

type StandardsTab = UiStandardsTab;

export function UiStandardsPage() {
  const { t } = useI18n();
  const [tab, setTab] = useState<StandardsTab>("tables");
  const { isOpen, setOpen, expandAll, collapseAll, resetLayout } = useUiStandardsDisclosure();

  if (!isFrontendLocalValidationMode()) {
    return <Navigate to="/" replace />;
  }

  return (
    <div className="exits-page flex min-w-0 flex-col gap-3" data-testid="ui-standards-page">
      <PageHeader title={t("uiStandards.title")} description={t("uiStandards.description")} />

      <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">{t("uiStandards.prefsHint")}</p>
      <p className="m-0 text-[length:var(--exits-text-sm)] text-muted" data-testid="ui-standards-copy-hint">
        {t("uiStandards.copyCommandsHint")}
      </p>

      <div
        className="sticky top-0 z-20 -mx-1 flex flex-col gap-2 border-b border-border bg-[color-mix(in_srgb,var(--exits-bg)_92%,transparent)] px-1 py-2 backdrop-blur-md"
        data-testid="ui-standards-sticky-nav"
      >
        <UnderlineTabBar
          ariaLabel={t("uiStandards.sections")}
          testId="ui-standards-tabs"
          activeKey={tab}
          onChange={(key) => setTab(key as StandardsTab)}
          items={[
            { key: "tables", label: t("uiStandards.tabTables"), testId: "ui-standards-tab-tables" },
            { key: "buttons", label: t("uiStandards.tabButtons"), testId: "ui-standards-tab-buttons" },
            { key: "chips", label: t("uiStandards.tabChips"), testId: "ui-standards-tab-chips" },
            { key: "tabs", label: t("uiStandards.tabTabs"), testId: "ui-standards-tab-tabs" },
            {
              key: "module-subnav",
              label: t("uiStandards.tabModuleSubnav"),
              testId: "ui-standards-tab-module-subnav",
            },
            {
              key: "action-chips",
              label: t("uiStandards.tabActionChips"),
              testId: "ui-standards-tab-action-chips",
            },
            { key: "cards", label: t("uiStandards.tabCards"), testId: "ui-standards-tab-cards" },
          ]}
        />
        <UiStandardsDisclosureToolbar
          onExpandAll={() => expandAll(tab)}
          onCollapseAll={() => collapseAll(tab)}
          onReset={resetLayout}
          expandLabel={t("uiStandards.expandAll")}
          collapseLabel={t("uiStandards.collapseAll")}
          resetLabel={t("uiStandards.resetLayout")}
        />
      </div>

      {tab === "tables" ? <UiStandardsTablesPanel isOpen={isOpen} setOpen={setOpen} /> : null}
      {tab === "buttons" ? <UiStandardsButtonsPanel isOpen={isOpen} setOpen={setOpen} /> : null}
      {tab === "chips" ? <UiStandardsChipsPanel isOpen={isOpen} setOpen={setOpen} /> : null}
      {tab === "tabs" ? <UiStandardsTabsPanel isOpen={isOpen} setOpen={setOpen} /> : null}
      {tab === "module-subnav" ? (
        <UiStandardsModuleSubnavPanel isOpen={isOpen} setOpen={setOpen} />
      ) : null}
      {tab === "action-chips" ? (
        <UiStandardsActionChipsPanel isOpen={isOpen} setOpen={setOpen} />
      ) : null}
      {tab === "cards" ? <UiStandardsCardsPanel isOpen={isOpen} setOpen={setOpen} /> : null}
    </div>
  );
}
