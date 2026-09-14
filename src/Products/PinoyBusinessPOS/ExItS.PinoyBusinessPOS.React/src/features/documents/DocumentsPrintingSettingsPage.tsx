import { useEffect, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { hasOrganizationManagementAuthority } from "@/access/pos-capabilities";
import {
  getOrganizationComplianceProfileSummary,
  getOrganizationComplianceReadinessSummary,
  getOrganizationSalesDocumentCapability,
  isBirComplianceModuleUnlocked,
} from "@/api/platform/organization-sales-document-capability-client";
import { ErrorState } from "@/components/exits/ErrorState";
import { PageHeader } from "@/components/exits/PageHeader";
import { useToast } from "@/components/exits/ToastProvider";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Switch } from "@/components/ui/switch";
import {
  DEFAULT_DOCUMENT_SETTINGS,
  resetSalesDisclaimerDefaults,
  type OrganizationDocumentSettings,
} from "@/features/documents/document-settings";
import { useOrganizationDocumentSettings } from "@/features/documents/use-organization-document-settings";
import { useI18n } from "@/i18n/I18nProvider";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

type DocsMode = "standard" | "bir";

function ToggleRow({
  label,
  checked,
  onCheckedChange,
  disabled,
  testId,
}: {
  label: string;
  checked: boolean;
  onCheckedChange: (next: boolean) => void;
  disabled?: boolean;
  testId: string;
}) {
  return (
    <div className="flex items-center justify-between gap-3 py-1.5">
      <span className="text-sm">{label}</span>
      <Switch
        checked={checked}
        onCheckedChange={onCheckedChange}
        disabled={disabled}
        data-testid={testId}
      />
    </div>
  );
}

export function DocumentsPrintingSettingsPage() {
  const { t } = useI18n();
  const { showToast } = useToast();
  const { sessionGrant, boundWorkspace } = useWorkspace();
  const organizationId = boundWorkspace?.organizationId ?? null;
  const canEdit = hasOrganizationManagementAuthority(sessionGrant);
  const { settings, save } = useOrganizationDocumentSettings(organizationId);
  const [draft, setDraft] = useState<OrganizationDocumentSettings>(settings);
  const [mode, setMode] = useState<DocsMode>("standard");

  useEffect(() => {
    setDraft(settings);
  }, [settings]);

  const capabilityQuery = useQuery({
    queryKey: ["sales-document-capability", organizationId],
    enabled: Boolean(organizationId),
    queryFn: ({ signal }) => getOrganizationSalesDocumentCapability(organizationId!, signal),
    staleTime: 60_000,
  });

  const birUnlocked = isBirComplianceModuleUnlocked(capabilityQuery.data);

  const profileQuery = useQuery({
    queryKey: ["compliance-profile-summary", organizationId],
    enabled: Boolean(organizationId) && birUnlocked && mode === "bir",
    queryFn: ({ signal }) => getOrganizationComplianceProfileSummary(organizationId!, signal),
    staleTime: 60_000,
  });

  const readinessQuery = useQuery({
    queryKey: ["compliance-readiness-summary", organizationId],
    enabled: Boolean(organizationId) && birUnlocked && mode === "bir",
    queryFn: ({ signal }) => getOrganizationComplianceReadinessSummary(organizationId!, signal),
    staleTime: 60_000,
  });

  function update(next: OrganizationDocumentSettings) {
    const withName = { ...next, header: { ...next.header, showBusinessName: true } };
    withName.footer = {
      ...withName.footer,
      showDocumentDisclaimer: withName.sales.showSalesDisclaimer,
    };
    setDraft(withName);
  }

  function selectBirMode() {
    setMode("bir");
    if (!birUnlocked) {
      // Selecting the tab shows the locked panel; also toast on intent to open unavailable config.
      showToast(t("documentsPrinting.birLockedToast"), "error");
    }
  }

  if (!organizationId) {
    return (
      <ErrorState
        title={t("documentsPrinting.orgRequired")}
        detail={t("documentsPrinting.orgRequiredDetail")}
      />
    );
  }

  return (
    <div className="flex flex-col gap-4 p-4" data-testid="documents-printing-settings-page">
      <PageHeader
        title={t("documentsPrinting.title")}
        description={t("documentsPrinting.lede")}
        backTo="/org"
        backLabel={t("admin.nav.overview")}
      />

      {!canEdit ? (
        <p className="m-0 text-sm text-muted" data-testid="documents-printing-readonly">
          {t("documentsPrinting.readOnly")}
        </p>
      ) : null}

      <div
        className="flex flex-wrap gap-2"
        role="tablist"
        aria-label={t("documentsPrinting.modesLabel")}
        data-testid="documents-printing-modes"
      >
        <Button
          type="button"
          variant={mode === "standard" ? "default" : "outline"}
          role="tab"
          aria-selected={mode === "standard"}
          data-testid="documents-mode-standard"
          onClick={() => setMode("standard")}
        >
          {t("documentsPrinting.modeStandard")}
        </Button>
        <Button
          type="button"
          variant={mode === "bir" ? "default" : "outline"}
          role="tab"
          aria-selected={mode === "bir"}
          data-testid="documents-mode-bir"
          onClick={selectBirMode}
        >
          {t("documentsPrinting.modeBir")}
        </Button>
      </div>

      {mode === "bir" ? (
        <section
          className="flex flex-col gap-3 rounded-md border border-border p-3"
          data-testid="documents-bir-panel"
        >
          {!birUnlocked ? (
            <>
              <p className="m-0 text-sm" data-testid="documents-bir-locked-message">
                {t("documentsPrinting.birNotEnabled")}
              </p>
              <Button
                type="button"
                variant="outline"
                className="w-fit"
                data-testid="documents-bir-contact-support"
                onClick={() => showToast(t("documentsPrinting.birLockedToast"), "error")}
              >
                {t("documentsPrinting.birContactSupport")}
              </Button>
            </>
          ) : (
            <>
              <p className="m-0 text-sm text-muted" data-testid="documents-bir-semantics">
                {t("documentsPrinting.birSemantics")}
              </p>
              <dl className="m-0 grid gap-2 text-sm" data-testid="documents-bir-capability">
                <div className="flex justify-between gap-2">
                  <dt>{t("documentsPrinting.birEligibility")}</dt>
                  <dd className="m-0 font-medium">
                    {capabilityQuery.data?.complianceEligibilityStatus ?? "—"}
                  </dd>
                </div>
                <div className="flex justify-between gap-2">
                  <dt>{t("documentsPrinting.birTaxDocumentIssuance")}</dt>
                  <dd className="m-0 font-medium">
                    {capabilityQuery.data?.taxDocumentIssuanceEnabled
                      ? t("documentsPrinting.birFlagOn")
                      : t("documentsPrinting.birFlagOff")}
                  </dd>
                </div>
                <div className="flex justify-between gap-2">
                  <dt>{t("documentsPrinting.birRuntime")}</dt>
                  <dd className="m-0 font-medium">
                    {capabilityQuery.data?.taxDocumentImplementationAvailable
                      ? t("documentsPrinting.birFlagOn")
                      : t("documentsPrinting.birRuntimeHardOff")}
                  </dd>
                </div>
              </dl>
              <dl className="m-0 grid gap-2 text-sm" data-testid="documents-bir-profile">
                <div className="flex justify-between gap-2">
                  <dt>{t("documentsPrinting.birRegisteredName")}</dt>
                  <dd className="m-0 font-medium">
                    {profileQuery.data?.registeredTaxpayerName?.trim() || "—"}
                  </dd>
                </div>
                <div className="flex justify-between gap-2">
                  <dt>{t("documentsPrinting.birMaskedTin")}</dt>
                  <dd className="m-0 font-medium">{profileQuery.data?.maskedTin || "—"}</dd>
                </div>
                <div className="flex justify-between gap-2">
                  <dt>{t("documentsPrinting.birSetupStatus")}</dt>
                  <dd className="m-0 font-medium">{profileQuery.data?.setupStatus || "—"}</dd>
                </div>
              </dl>
              <p className="m-0 text-sm" data-testid="documents-bir-readiness">
                {t("documentsPrinting.birReadinessSummary", {
                  blockers: String(readinessQuery.data?.blockingCount ?? "—"),
                  warnings: String(readinessQuery.data?.warningCount ?? "—"),
                })}
              </p>
            </>
          )}
        </section>
      ) : (
        <>
          <section className="flex flex-col gap-2" aria-labelledby="doc-header-settings">
            <h2 id="doc-header-settings" className="m-0 text-base font-semibold">
              {t("documentsPrinting.headerSection")}
            </h2>
            <ToggleRow
              label={t("documentsPrinting.showLogo")}
              checked={draft.header.showLogo}
              disabled={!canEdit}
              testId="doc-setting-show-logo"
              onCheckedChange={(v) => update({ ...draft, header: { ...draft.header, showLogo: v } })}
            />
            <ToggleRow
              label={t("documentsPrinting.showBusinessName")}
              checked
              disabled
              testId="doc-setting-show-business-name"
              onCheckedChange={() => undefined}
            />
            <ToggleRow
              label={t("documentsPrinting.showBusinessAddress")}
              checked={draft.header.showBusinessAddress}
              disabled={!canEdit}
              testId="doc-setting-show-address"
              onCheckedChange={(v) =>
                update({ ...draft, header: { ...draft.header, showBusinessAddress: v } })
              }
            />
            <ToggleRow
              label={t("documentsPrinting.showBusinessPhone")}
              checked={draft.header.showBusinessPhone}
              disabled={!canEdit}
              testId="doc-setting-show-phone"
              onCheckedChange={(v) =>
                update({ ...draft, header: { ...draft.header, showBusinessPhone: v } })
              }
            />
            <ToggleRow
              label={t("documentsPrinting.showBusinessEmail")}
              checked={draft.header.showBusinessEmail}
              disabled={!canEdit}
              testId="doc-setting-show-email"
              onCheckedChange={(v) =>
                update({ ...draft, header: { ...draft.header, showBusinessEmail: v } })
              }
            />
            <ToggleRow
              label={t("documentsPrinting.showWebsite")}
              checked={draft.header.showWebsite}
              disabled={!canEdit}
              testId="doc-setting-show-website"
              onCheckedChange={(v) =>
                update({ ...draft, header: { ...draft.header, showWebsite: v } })
              }
            />
            <ToggleRow
              label={t("documentsPrinting.showBranchName")}
              checked={draft.header.showBranchName}
              disabled={!canEdit}
              testId="doc-setting-show-branch-name"
              onCheckedChange={(v) =>
                update({ ...draft, header: { ...draft.header, showBranchName: v } })
              }
            />
            <ToggleRow
              label={t("documentsPrinting.showBranchAddress")}
              checked={draft.header.showBranchAddress}
              disabled={!canEdit}
              testId="doc-setting-show-branch-address"
              onCheckedChange={(v) =>
                update({ ...draft, header: { ...draft.header, showBranchAddress: v } })
              }
            />
          </section>

          <section className="flex flex-col gap-2" aria-labelledby="doc-footer-settings">
            <h2 id="doc-footer-settings" className="m-0 text-base font-semibold">
              {t("documentsPrinting.footerSection")}
            </h2>
            <ToggleRow
              label={t("documentsPrinting.showCustomFooter")}
              checked={draft.footer.showCustomFooter}
              disabled={!canEdit}
              testId="doc-setting-show-custom-footer"
              onCheckedChange={(v) =>
                update({ ...draft, footer: { ...draft.footer, showCustomFooter: v } })
              }
            />
            <label className="flex flex-col gap-1 text-sm">
              <span>{t("documentsPrinting.customFooterText")}</span>
              <Input
                value={draft.footer.customFooterText}
                disabled={!canEdit}
                data-testid="doc-setting-custom-footer-text"
                onChange={(e) =>
                  update({
                    ...draft,
                    footer: { ...draft.footer, customFooterText: e.target.value },
                  })
                }
              />
            </label>
            <ToggleRow
              label={t("documentsPrinting.showBusinessContact")}
              checked={draft.footer.showBusinessContact}
              disabled={!canEdit}
              testId="doc-setting-show-footer-contact"
              onCheckedChange={(v) =>
                update({ ...draft, footer: { ...draft.footer, showBusinessContact: v } })
              }
            />
            <ToggleRow
              label={t("documentsPrinting.showPageNumber")}
              checked={draft.footer.showPageNumber}
              disabled={!canEdit}
              testId="doc-setting-show-page-number"
              onCheckedChange={(v) =>
                update({ ...draft, footer: { ...draft.footer, showPageNumber: v } })
              }
            />
          </section>

          <section className="flex flex-col gap-2" aria-labelledby="doc-sales-settings">
            <h2 id="doc-sales-settings" className="m-0 text-base font-semibold">
              {t("documentsPrinting.salesSection")}
            </h2>
            <label className="flex flex-col gap-1 text-sm">
              <span>{t("documentsPrinting.documentTitle")}</span>
              <Input
                value={draft.sales.title}
                disabled={!canEdit}
                data-testid="doc-setting-sales-title"
                onChange={(e) =>
                  update({ ...draft, sales: { ...draft.sales, title: e.target.value } })
                }
              />
            </label>
            <ToggleRow
              label={t("documentsPrinting.showCustomerName")}
              checked={draft.sales.showCustomerName}
              disabled={!canEdit}
              testId="doc-setting-sales-customer-name"
              onCheckedChange={(v) =>
                update({ ...draft, sales: { ...draft.sales, showCustomerName: v } })
              }
            />
            <ToggleRow
              label={t("documentsPrinting.showCustomerAddress")}
              checked={draft.sales.showCustomerAddress}
              disabled={!canEdit}
              testId="doc-setting-sales-customer-address"
              onCheckedChange={(v) =>
                update({ ...draft, sales: { ...draft.sales, showCustomerAddress: v } })
              }
            />
            <ToggleRow
              label={t("documentsPrinting.showCashier")}
              checked={draft.sales.showCashier}
              disabled={!canEdit}
              testId="doc-setting-sales-cashier"
              onCheckedChange={(v) =>
                update({ ...draft, sales: { ...draft.sales, showCashier: v } })
              }
            />
            <ToggleRow
              label={t("documentsPrinting.showPaymentMethod")}
              checked={draft.sales.showPaymentMethod}
              disabled={!canEdit}
              testId="doc-setting-sales-payment"
              onCheckedChange={(v) =>
                update({ ...draft, sales: { ...draft.sales, showPaymentMethod: v } })
              }
            />
            <ToggleRow
              label={t("documentsPrinting.showSku")}
              checked={draft.sales.showSku}
              disabled={!canEdit}
              testId="doc-setting-sales-sku"
              onCheckedChange={(v) => update({ ...draft, sales: { ...draft.sales, showSku: v } })}
            />
            <ToggleRow
              label={t("documentsPrinting.showDiscount")}
              checked={draft.sales.showDiscount}
              disabled={!canEdit}
              testId="doc-setting-sales-discount"
              onCheckedChange={(v) =>
                update({ ...draft, sales: { ...draft.sales, showDiscount: v } })
              }
            />

            <ToggleRow
              label={t("documentsPrinting.showSalesDisclaimer")}
              checked={draft.sales.showSalesDisclaimer}
              disabled={!canEdit}
              testId="doc-setting-show-sales-disclaimer"
              onCheckedChange={(v) =>
                update({
                  ...draft,
                  sales: { ...draft.sales, showSalesDisclaimer: v },
                })
              }
            />
            <label className="flex flex-col gap-1 text-sm">
              <span>{t("documentsPrinting.disclaimerTitle")}</span>
              <Input
                value={draft.sales.salesDisclaimerTitle}
                disabled={!canEdit}
                data-testid="doc-setting-disclaimer-title"
                onChange={(e) =>
                  update({
                    ...draft,
                    sales: { ...draft.sales, salesDisclaimerTitle: e.target.value },
                  })
                }
              />
            </label>
            <label className="flex flex-col gap-1 text-sm">
              <span>{t("documentsPrinting.disclaimerBody")}</span>
              <textarea
                className="min-h-24 rounded-md border border-border bg-background px-3 py-2 text-sm"
                value={draft.sales.salesDisclaimerBody}
                disabled={!canEdit}
                data-testid="doc-setting-disclaimer-body"
                onChange={(e) =>
                  update({
                    ...draft,
                    sales: { ...draft.sales, salesDisclaimerBody: e.target.value },
                  })
                }
              />
            </label>
            {canEdit ? (
              <Button
                type="button"
                variant="outline"
                className="w-fit"
                data-testid="doc-setting-disclaimer-reset"
                onClick={() => update(resetSalesDisclaimerDefaults(draft))}
              >
                {t("documentsPrinting.resetDisclaimer")}
              </Button>
            ) : null}
          </section>

          <section className="flex flex-col gap-2" aria-labelledby="doc-quotation-settings">
            <h2 id="doc-quotation-settings" className="m-0 text-base font-semibold">
              {t("documentsPrinting.quotationSection")}
            </h2>
            <label className="flex flex-col gap-1 text-sm">
              <span>{t("documentsPrinting.documentTitle")}</span>
              <Input
                value={draft.quotation.title}
                disabled={!canEdit}
                data-testid="doc-setting-quotation-title"
                onChange={(e) =>
                  update({ ...draft, quotation: { ...draft.quotation, title: e.target.value } })
                }
              />
            </label>
            <ToggleRow
              label={t("documentsPrinting.showQuotationStatement")}
              checked={draft.quotation.showQuotationStatement}
              disabled={!canEdit}
              testId="doc-setting-quotation-statement-toggle"
              onCheckedChange={(v) =>
                update({
                  ...draft,
                  quotation: { ...draft.quotation, showQuotationStatement: v },
                })
              }
            />
            <label className="flex flex-col gap-1 text-sm">
              <span>{t("documentsPrinting.quotationStatement")}</span>
              <textarea
                className="min-h-20 rounded-md border border-border bg-background px-3 py-2 text-sm"
                value={draft.quotation.quotationStatement}
                disabled={!canEdit}
                data-testid="doc-setting-quotation-statement"
                onChange={(e) =>
                  update({
                    ...draft,
                    quotation: { ...draft.quotation, quotationStatement: e.target.value },
                  })
                }
              />
            </label>
          </section>

          <section className="flex flex-col gap-2" aria-labelledby="doc-po-settings">
            <h2 id="doc-po-settings" className="m-0 text-base font-semibold">
              {t("documentsPrinting.poSection")}
            </h2>
            <label className="flex flex-col gap-1 text-sm">
              <span>{t("documentsPrinting.documentTitle")}</span>
              <Input
                value={draft.purchaseOrder.title}
                disabled={!canEdit}
                data-testid="doc-setting-po-title"
                onChange={(e) =>
                  update({
                    ...draft,
                    purchaseOrder: { ...draft.purchaseOrder, title: e.target.value },
                  })
                }
              />
            </label>
          </section>

          <section className="flex flex-col gap-2" aria-labelledby="doc-grn-settings">
            <h2 id="doc-grn-settings" className="m-0 text-base font-semibold">
              {t("documentsPrinting.grnSection")}
            </h2>
            <label className="flex flex-col gap-1 text-sm">
              <span>{t("documentsPrinting.documentTitle")}</span>
              <Input
                value={draft.goodsReceipt.title}
                disabled={!canEdit}
                data-testid="doc-setting-grn-title"
                onChange={(e) =>
                  update({
                    ...draft,
                    goodsReceipt: { ...draft.goodsReceipt, title: e.target.value },
                  })
                }
              />
            </label>
          </section>

          {canEdit ? (
            <div className="flex flex-wrap gap-2">
              <Button
                type="button"
                data-testid="documents-printing-save"
                onClick={() => {
                  save(draft);
                  showToast(t("documentsPrinting.saved"), "success");
                }}
              >
                {t("documentsPrinting.save")}
              </Button>
              <Button
                type="button"
                variant="outline"
                data-testid="documents-printing-reset"
                onClick={() => {
                  const next = structuredClone(DEFAULT_DOCUMENT_SETTINGS);
                  setDraft(next);
                  save(next);
                  showToast(t("documentsPrinting.resetDone"), "success");
                }}
              >
                {t("documentsPrinting.reset")}
              </Button>
            </div>
          ) : null}
        </>
      )}
    </div>
  );
}
