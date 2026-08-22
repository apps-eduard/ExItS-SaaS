import { useMemo, useState } from "react";
import { RotateCcw, Trash2 } from "lucide-react";
import { Link } from "react-router-dom";
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
import { Card } from "@/components/ui/card";
import { PageHeader } from "@/components/exits/PageHeader";
import { LoadingSkeleton } from "@/components/exits/FoundationStates";
import { useI18n } from "@/i18n/I18nProvider";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

function isRequired(mode: string): boolean {
  return mode.localeCompare("Required", undefined, { sensitivity: "accent" }) === 0;
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
        <PageHeader title={t("cashHandling.title")} description={t("cashHandling.denied")} />
        <Button asChild variant="ghost" className="min-h-11 w-fit">
          <Link to="/org">{t("notFound.home")}</Link>
        </Button>
      </div>
    );
  }

  if (!workspace || setupQuery.isLoading || denomsQuery.isLoading) {
    return <LoadingSkeleton label={t("loading.label")} />;
  }

  if (setupQuery.isError || !setup) {
    return (
      <div data-testid="cash-handling-error" className="flex flex-col gap-3">
        <PageHeader title={t("cashHandling.title")} description={t("cashHandling.loadError")} />
        <Button asChild variant="ghost" className="min-h-11 w-fit">
          <Link to="/org">{t("notFound.home")}</Link>
        </Button>
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
      // keep local setup via invalidate
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
    <div data-testid="cash-handling-page" className="flex min-w-0 flex-col gap-4">
      <PageHeader title={t("cashHandling.title")} description={t("cashHandling.lede")} />

      {error ? (
        <p className="m-0 text-[length:var(--exits-text-sm)] text-[var(--exits-danger)]">{error}</p>
      ) : null}
      {okMessage ? (
        <p className="m-0 text-[length:var(--exits-text-sm)] text-[var(--exits-success)]">
          {okMessage}
        </p>
      ) : null}

      <Card className="flex flex-col gap-3">
        <label className="flex min-h-11 cursor-pointer items-start gap-3">
          <input
            data-testid="cash-handling-require-opening"
            type="checkbox"
            className="mt-1 size-5"
            checked={openingRequired}
            onChange={(event) => setRequireOpening(event.target.checked)}
          />
          <span>
            <span className="block font-medium">{t("cashHandling.requireOpening")}</span>
            <span className="mt-1 block text-[length:var(--exits-text-sm)] text-muted">
              {t("cashHandling.requireOpeningHelp")}
            </span>
          </span>
        </label>
        <label className="flex min-h-11 cursor-pointer items-start gap-3">
          <input
            data-testid="cash-handling-require-closing"
            type="checkbox"
            className="mt-1 size-5"
            checked={closingRequired}
            onChange={(event) => setRequireClosing(event.target.checked)}
          />
          <span>
            <span className="block font-medium">{t("cashHandling.requireClosing")}</span>
            <span className="mt-1 block text-[length:var(--exits-text-sm)] text-muted">
              {t("cashHandling.requireClosingHelp")}
            </span>
          </span>
        </label>
        <p className="mb-0 text-[length:var(--exits-text-sm)] text-muted">
          {t("cashHandling.snapshotHint")}
        </p>
        <Button
          type="button"
          className="min-h-11 w-fit"
          disabled={savingPolicy}
          data-testid="cash-handling-save-policy"
          onClick={() => void savePolicy()}
        >
          {t("cashHandling.save")}
        </Button>
      </Card>

      <Card className="flex flex-col gap-3">
        <div>
          <h2 className="m-0 text-[length:var(--exits-text-md)] font-semibold">
            {t("cashHandling.denominationsTitle")}
          </h2>
          <p className="mb-0 mt-1 text-[length:var(--exits-text-sm)] text-muted">
            {t("cashHandling.denominationsHelp")}
          </p>
        </div>

        {denominations.length === 0 ? (
          <div data-testid="cash-handling-denoms-empty">
            <p className="m-0 font-medium">{t("cashHandling.emptyDenoms")}</p>
            <p className="mb-0 mt-1 text-[length:var(--exits-text-sm)] text-muted">
              {t("cashHandling.emptyDenomsDetail")}
            </p>
          </div>
        ) : (
          <ul
            className="m-0 grid list-none grid-cols-2 gap-2 p-0"
            data-testid="cash-handling-denoms-list"
          >
            {denominations.map((denom) => (
              <li
                key={denom.denominationId}
                className="flex min-h-11 min-w-0 items-center justify-between gap-2 rounded-[var(--exits-radius-md)] border border-border bg-surface px-3"
                data-testid={`cash-handling-denom-${formatDenominationValue(denom.value)}`}
              >
                <span className="min-w-0 truncate tabular-nums font-medium">
                  {formatDenominationValue(denom.value)}
                </span>
                <Button
                  type="button"
                  variant="ghost"
                  size="icon"
                  className="size-9 shrink-0 text-destructive hover:bg-destructive/10 hover:text-destructive"
                  disabled={savingDenoms}
                  aria-label={t("cashHandling.remove")}
                  data-testid={`cash-handling-remove-${formatDenominationValue(denom.value)}`}
                  onClick={() => void removeDenomination(denom)}
                >
                  <Trash2 className="size-[15px]" aria-hidden />
                </Button>
              </li>
            ))}
          </ul>
        )}

        <div className="flex flex-wrap items-end gap-2">
          <label className="flex min-w-[10rem] flex-1 flex-col gap-1 text-[length:var(--exits-text-sm)]">
            <span>{t("cashHandling.addDenomination")}</span>
            <input
              data-testid="cash-handling-add-value"
              type="number"
              inputMode="decimal"
              min={0}
              step="0.01"
              placeholder={t("cashHandling.addDenominationPlaceholder")}
              className="min-h-11 rounded-[var(--exits-radius-md)] border border-border bg-surface px-3 tabular-nums"
              value={newValue}
              onChange={(event) => setNewValue(event.target.value)}
            />
          </label>
          <Button
            type="button"
            className="min-h-11"
            disabled={savingDenoms}
            data-testid="cash-handling-add"
            onClick={() => void addDenomination()}
          >
            {t("cashHandling.add")}
          </Button>
          <Button
            type="button"
            variant="ghost"
            className="min-h-11 inline-flex items-center gap-2 border border-border"
            disabled={savingDenoms}
            data-testid="cash-handling-reset-defaults"
            onClick={() => void resetDenominationsToDefault()}
          >
            <RotateCcw className="size-4 shrink-0" aria-hidden />
            {t("cashHandling.resetDefaults")}
          </Button>
        </div>
      </Card>

      <Button asChild variant="ghost" className="min-h-11 w-fit">
        <Link to="/org">{t("notFound.home")}</Link>
      </Button>
    </div>
  );
}
