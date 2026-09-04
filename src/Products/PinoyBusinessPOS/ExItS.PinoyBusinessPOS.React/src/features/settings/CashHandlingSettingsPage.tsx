import { useMemo, useState } from "react";
import { Plus, RotateCcw, Save, Trash2 } from "lucide-react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { hasOrganizationManagementAuthority } from "@/access/pos-capabilities";
import {
  DEFAULT_PHP_CASH_DENOMINATION_VALUES,
  formatDenominationValue,
  getOperationalSetup,
  listCashDenominations,
  replaceCashDenominations,
  resolveClosingCashCountMode,
  resolveOpeningCashCountMode,
  resolveSetupCompleted,
  updateOperationalSetup,
  type CashDenominationWriteDto,
  type OrganizationCashDenominationDto,
} from "@/api/pos/pos-operational-setup-client";
import { Button } from "@/components/ui/button";
import { Switch } from "@/components/ui/switch";
import { PageHeader } from "@/components/exits/PageHeader";
import { pageBackNav } from "@/navigation/page-back-nav";
import { LoadingSkeleton } from "@/components/exits/FoundationStates";
import { useI18n } from "@/i18n/I18nProvider";
import { formatDenominationCurrency } from "@/lib/format-money";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

function isRequired(mode: string): boolean {
  return mode.localeCompare("Required", undefined, { sensitivity: "accent" }) === 0;
}

function removeDenominationLabel(template: string, amountLabel: string): string {
  return template.includes("{amount}")
    ? template.replace("{amount}", amountLabel)
    : `${template} ${amountLabel}`;
}

export function CashHandlingSettingsPage() {
  const { t } = useI18n();
  const queryClient = useQueryClient();
  const { boundWorkspace, sessionGrant } = useWorkspace();
  const canManage = hasOrganizationManagementAuthority(sessionGrant);

  const workspace = useMemo(() => {
    if (!boundWorkspace?.organizationId) {
      return null;
    }
    return {
      organizationId: boundWorkspace.organizationId,
      branchId: boundWorkspace.branchId,
    };
  }, [boundWorkspace]);

  const setupQuery = useQuery({
    queryKey: ["pos-operational-setup", workspace?.organizationId],
    enabled: Boolean(workspace && canManage),
    queryFn: ({ signal }) => getOperationalSetup(workspace!, signal),
  });

  const denomsQuery = useQuery({
    queryKey: ["pos-cash-denominations", workspace?.organizationId],
    enabled: Boolean(workspace && canManage),
    queryFn: ({ signal }) => listCashDenominations(workspace!, signal),
    staleTime: 0,
    refetchOnMount: "always",
  });

  const [requireOpening, setRequireOpening] = useState<boolean | null>(null);
  const [requireClosing, setRequireClosing] = useState<boolean | null>(null);
  const [newValue, setNewValue] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [okMessage, setOkMessage] = useState<string | null>(null);
  const [savingPolicy, setSavingPolicy] = useState(false);
  const [savingDenoms, setSavingDenoms] = useState(false);

  const setup = setupQuery.data;
  const openingMode = resolveOpeningCashCountMode(setup);
  const closingMode = resolveClosingCashCountMode(setup);
  const openingRequired = requireOpening ?? isRequired(openingMode);
  const closingRequired = requireClosing ?? isRequired(closingMode);
  const denominations = useMemo(() => {
    const items = denomsQuery.data ?? [];
    return [...items].sort((a, b) => a.sortOrder - b.sortOrder || b.value - a.value);
  }, [denomsQuery.data]);

  if (!canManage) {
    return (
      <div data-testid="cash-handling-denied" className="flex flex-col gap-3">
        <PageHeader
          title={t("cashHandling.title")}
          description={t("cashHandling.denied")}
          backTo={pageBackNav.org.to}
          backLabel={t(pageBackNav.org.labelKey)}
          backTestId="page-header-back-org"
        />
      </div>
    );
  }

  if (!workspace || setupQuery.isLoading || denomsQuery.isLoading) {
    return <LoadingSkeleton label={t("loading.label")} />;
  }

  if (setupQuery.isError || !setup) {
    return (
      <div data-testid="cash-handling-error" className="flex flex-col gap-3">
        <PageHeader
          title={t("cashHandling.title")}
          description={t("cashHandling.loadError")}
          backTo={pageBackNav.org.to}
          backLabel={t(pageBackNav.org.labelKey)}
          backTestId="page-header-back-org"
        />
      </div>
    );
  }

  async function savePolicy() {
    if (!workspace || !setup || savingPolicy) {
      return;
    }
    if (!resolveSetupCompleted(setup)) {
      setError(t("cashHandling.saveError"));
      return;
    }
    setSavingPolicy(true);
    setError(null);
    setOkMessage(null);
    try {
      const updated = await updateOperationalSetup(workspace, {
        storeDisplayName: setup.storeDisplayName,
        currencyCode: setup.currencyCode,
        taxPricingMode: setup.taxPricingMode,
        taxRatePercent: setup.taxRatePercent,
        expectedUpdatedAtUtc: setup.updatedAtUtc,
        receiptHeader: setup.receiptHeader,
        receiptFooter: setup.receiptFooter,
        businessAddress: setup.businessAddress,
        contactPhone: setup.contactPhone,
        openingCashCountMode: openingRequired ? "Required" : "Optional",
        closingCashCountMode: closingRequired ? "Required" : "Optional",
      });
      await queryClient.invalidateQueries({
        queryKey: ["pos-operational-setup", workspace.organizationId],
      });
      setRequireOpening(null);
      setRequireClosing(null);
      setOkMessage(t("cashHandling.saved"));
      void updated;
    } catch {
      setError(t("cashHandling.saveError"));
    } finally {
      setSavingPolicy(false);
    }
  }

  async function saveDenominationItems(items: CashDenominationWriteDto[]) {
    if (!workspace || savingDenoms) {
      return false;
    }
    setSavingDenoms(true);
    setError(null);
    setOkMessage(null);
    try {
      await replaceCashDenominations(workspace, { items });
      await queryClient.invalidateQueries({
        queryKey: ["pos-cash-denominations", workspace.organizationId],
      });
      return true;
    } catch {
      setError(t("cashHandling.saveError"));
      return false;
    } finally {
      setSavingDenoms(false);
    }
  }

  async function persistDenoms(next: OrganizationCashDenominationDto[], extra?: { value: number }) {
    const items: CashDenominationWriteDto[] = next.map((d, index) => ({
      value: d.value,
      isEnabled: d.isEnabled,
      sortOrder: d.sortOrder ?? index,
      displayLabel: d.displayLabel,
      denominationId: d.denominationId,
    }));
    if (extra) {
      items.push({
        value: extra.value,
        isEnabled: true,
        sortOrder: items.length,
      });
    }
    await saveDenominationItems(items);
  }

  async function removeDenomination(denom: OrganizationCashDenominationDto) {
    await persistDenoms(denominations.filter((d) => d.denominationId !== denom.denominationId));
  }

  async function addDenomination() {
    const parsed = Number(newValue);
    if (!Number.isFinite(parsed) || parsed <= 0) {
      setError(t("cashHandling.invalidDenomination"));
      return;
    }
    const exists = denominations.some((d) => Math.abs(d.value - parsed) < 0.00001);
    if (exists) {
      setError(t("cashHandling.invalidDenomination"));
      return;
    }
    setNewValue("");
    await persistDenoms(denominations, { value: parsed });
  }

  async function resetDenominationsToDefault() {
    const items = DEFAULT_PHP_CASH_DENOMINATION_VALUES.map((value, index) => ({
      value,
      isEnabled: true,
      sortOrder: index,
    }));
    const saved = await saveDenominationItems(items);
    if (saved) {
      setOkMessage(t("cashHandling.resetSaved"));
    }
  }

  return (
    <div
      data-testid="cash-handling-page"
      className="cash-handling-page exits-page flex min-w-0 flex-col gap-4"
    >
      <PageHeader
        title={t("cashHandling.title")}
        description={t("cashHandling.lede")}
        backTo={pageBackNav.org.to}
        backLabel={t(pageBackNav.org.labelKey)}
        backTestId="page-header-back-org"
      />

      {error ? (
        <p
          className="m-0 text-[length:var(--exits-text-sm)] font-normal text-[var(--exits-danger)]"
          role="alert"
        >
          {error}
        </p>
      ) : null}
      {okMessage ? (
        <p className="m-0 text-[length:var(--exits-text-sm)] font-normal text-[var(--exits-success)]">
          {okMessage}
        </p>
      ) : null}

      <section
        className="catalog-form-section cash-handling-section exits-animate-panel gap-0"
        data-testid="cash-handling-policy"
      >
        <h2 className="catalog-form-section__title exits-type-section-title">
          {t("cashHandling.policyTitle")}
        </h2>

        <div className="cash-handling-policy-rows">
          <div className="cash-handling-policy-row">
            <div className="cash-handling-policy-row__text min-w-0">
              <p className="cash-handling-policy-row__eyebrow exits-type-label m-0">
                {t("cashHandling.openingTitle")}
              </p>
              <p className="cash-handling-policy-row__title m-0" id="cash-handling-opening-label">
                {t("cashHandling.requireOpening")}
              </p>
              <p className="cash-handling-policy-row__help m-0">
                {t("cashHandling.requireOpeningHelp")}
              </p>
            </div>
            <Switch
              checked={openingRequired}
              onCheckedChange={setRequireOpening}
              aria-labelledby="cash-handling-opening-label"
              data-testid="cash-handling-require-opening"
            />
          </div>

          <div className="cash-handling-policy-row">
            <div className="cash-handling-policy-row__text min-w-0">
              <p className="cash-handling-policy-row__eyebrow exits-type-label m-0">
                {t("cashHandling.closingTitle")}
              </p>
              <p className="cash-handling-policy-row__title m-0" id="cash-handling-closing-label">
                {t("cashHandling.requireClosing")}
              </p>
              <p className="cash-handling-policy-row__help m-0">
                {t("cashHandling.requireClosingHelp")}
              </p>
            </div>
            <Switch
              checked={closingRequired}
              onCheckedChange={setRequireClosing}
              aria-labelledby="cash-handling-closing-label"
              data-testid="cash-handling-require-closing"
            />
          </div>
        </div>

        <p className="cash-handling-policy-note m-0">{t("cashHandling.snapshotHint")}</p>

        <div className="cash-handling-policy-actions">
          <Button
            type="button"
            disabled={savingPolicy}
            data-testid="cash-handling-save-policy"
            onClick={() => void savePolicy()}
          >
            <Save className="size-4 shrink-0" aria-hidden />
            {t("cashHandling.save")}
          </Button>
        </div>
      </section>

      <section
        className="catalog-form-section cash-handling-section exits-animate-panel gap-3"
        data-testid="cash-handling-denominations"
      >
        <div>
          <h2 className="catalog-form-section__title exits-type-section-title">
            {t("cashHandling.denominationsTitle")}
          </h2>
          <p className="cash-handling-section-help m-0 mt-1">
            {t("cashHandling.denominationsHelp")}
          </p>
        </div>

        {denominations.length === 0 ? (
          <div data-testid="cash-handling-denoms-empty">
            <p className="m-0 font-medium">{t("cashHandling.emptyDenoms")}</p>
            <p className="cash-handling-section-help mb-0 mt-1">
              {t("cashHandling.emptyDenomsDetail")}
            </p>
          </div>
        ) : (
          <ul className="cash-handling-denom-grid" data-testid="cash-handling-denoms-list">
            {denominations.map((denom) => {
              const amountLabel = formatDenominationCurrency(denom.value);
              const valueKey = formatDenominationValue(denom.value);
              return (
                <li
                  key={denom.denominationId}
                  className="cash-handling-denom-row"
                  data-testid={`cash-handling-denom-${valueKey}`}
                >
                  <span className="cash-handling-denom-row__value tabular-nums">{amountLabel}</span>
                  <Button
                    type="button"
                    variant="ghost"
                    size="icon"
                    className="cash-handling-denom-row__remove"
                    disabled={savingDenoms}
                    aria-label={removeDenominationLabel(t("cashHandling.remove"), amountLabel)}
                    data-testid={`cash-handling-remove-${valueKey}`}
                    onClick={() => void removeDenomination(denom)}
                  >
                    <Trash2 className="size-4 shrink-0" aria-hidden />
                  </Button>
                </li>
              );
            })}
          </ul>
        )}

        <div className="cash-handling-add-row">
          <label className="cash-handling-add-field exits-type-label">
            <span>{t("cashHandling.addDenomination")}</span>
            <input
              data-testid="cash-handling-add-value"
              type="number"
              inputMode="decimal"
              min={0}
              step="any"
              placeholder={t("cashHandling.addDenominationPlaceholder")}
              className="exits-input exits-input--no-spin cash-handling-amount-input tabular-nums"
              value={newValue}
              onChange={(event) => setNewValue(event.target.value)}
            />
          </label>
          <div className="cash-handling-add-actions">
            <Button
              type="button"
              disabled={savingDenoms}
              data-testid="cash-handling-add"
              onClick={() => void addDenomination()}
            >
              <Plus className="size-4 shrink-0" aria-hidden />
              {t("cashHandling.add")}
            </Button>
            <Button
              type="button"
              variant="outline"
              disabled={savingDenoms}
              data-testid="cash-handling-reset-defaults"
              onClick={() => void resetDenominationsToDefault()}
            >
              <RotateCcw className="size-4 shrink-0" aria-hidden />
              {t("cashHandling.resetDefaults")}
            </Button>
          </div>
        </div>
      </section>
    </div>
  );
}
