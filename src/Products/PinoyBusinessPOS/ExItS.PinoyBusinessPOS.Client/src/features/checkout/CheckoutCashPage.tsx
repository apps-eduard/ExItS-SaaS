import { useEffect, useMemo, useRef, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { canCreateSale } from "@/access/pos-capabilities";
import { checkoutSale } from "@/api/pos/pos-sales-client";
import { roundMoney } from "@/cart/sell-cart-helpers";
import { lineAmount, useSessionCart } from "@/cart/SessionCartProvider";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { PageHeader } from "@/components/exits/PageHeader";
import { MoneyDisplay } from "@/components/exits/MoneyQuantity";
import { describeCheckoutSaleError } from "@/features/checkout/checkout-sale-errors";
import { mapCartLinesToCheckoutRequest } from "@/features/checkout/map-cart-to-checkout";
import { useShiftContext } from "@/features/shifts/ShiftContextProvider";
import { useI18n } from "@/i18n/I18nProvider";
import { isPosDeviceReadyForMoney } from "@/workspace/pos-device-context";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

function newSaleId(): string {
  return crypto.randomUUID();
}

function parseCashTender(raw: string): number | null {
  const trimmed = raw.trim();
  if (!/^\d+(\.\d{1,2})?$/.test(trimmed)) {
    return null;
  }
  const value = Number(trimmed);
  if (!Number.isFinite(value) || value < 0) {
    return null;
  }
  return value;
}

export function CheckoutCashPage() {
  const { t } = useI18n();
  const navigate = useNavigate();
  const { boundWorkspace, sessionGrant, posDevice } = useWorkspace();
  const cart = useSessionCart();
  const { readiness, currentShift, refresh } = useShiftContext();

  const [cashReceived, setCashReceived] = useState("");
  const [initializedTender, setInitializedTender] = useState(false);
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);
  const attemptSaleIdRef = useRef<string>(newSaleId());
  const submittingRef = useRef(false);
  const completedRef = useRef(false);

  const workspaceScope = useMemo(() => {
    if (!boundWorkspace?.branchId) {
      return null;
    }
    return {
      organizationId: boundWorkspace.organizationId,
      branchId: boundWorkspace.branchId,
    };
  }, [boundWorkspace]);

  const allowSale = canCreateSale(sessionGrant);
  const moneyReady = readiness.moneyPostReady === true;
  const deviceReady = isPosDeviceReadyForMoney(posDevice);
  const previewTotal = cart.subtotal;
  const parsedTender = parseCashTender(cashReceived);
  const tenderOk = parsedTender !== null && parsedTender + 1e-9 >= previewTotal;
  const changeAdvisory =
    tenderOk && parsedTender !== null ? roundMoney(Math.max(0, parsedTender - previewTotal)) : null;

  useEffect(() => {
    // Fail-closed gate pages must stay visible even with an empty cart.
    if (!moneyReady || !deviceReady || !readiness.shiftGateReady) {
      return;
    }
    if (completedRef.current) {
      return;
    }
    if (cart.lineCount === 0 && !saving) {
      navigate("/sell", { replace: true });
    }
  }, [cart.lineCount, deviceReady, moneyReady, navigate, readiness.shiftGateReady, saving]);

  useEffect(() => {
    if (!initializedTender && previewTotal > 0) {
      setCashReceived(previewTotal.toFixed(2));
      setInitializedTender(true);
    }
  }, [initializedTender, previewTotal]);

  if (!allowSale) {
    return (
      <div data-testid="checkout-denied" className="flex flex-col gap-3">
        <PageHeader title={t("checkout.title")} description={t("checkout.deniedDetail")} />
        <Button asChild variant="ghost" className="min-h-11 w-fit">
          <Link to="/">{t("notFound.home")}</Link>
        </Button>
      </div>
    );
  }

  if (!moneyReady || !deviceReady || !readiness.shiftGateReady || !readiness.shiftId) {
    const deviceBlocked = !deviceReady;
    return (
      <div data-testid="checkout-blocked" className="flex min-w-0 flex-col gap-4">
        <PageHeader title={t("checkout.title")} description={t("checkout.blockedLede")} />
        <Card data-testid="checkout-gate-message">
          <p className="m-0 text-[length:var(--exits-text-sm)]">
            {deviceBlocked
              ? t("checkout.blockedDevice")
              : readiness.status === "blocked_no_shift" || readiness.status === "blocked_closed"
                ? t("checkout.blockedShift")
                : t("checkout.blockedGeneric")}
          </p>
          <div className="mt-3 flex flex-wrap gap-2">
            {deviceBlocked ? (
              <Button asChild className="min-h-11" data-testid="checkout-register-device">
                <Link to="/devices/register">{t("checkout.registerDevice")}</Link>
              </Button>
            ) : (
              <Button asChild className="min-h-11" data-testid="checkout-open-shift">
                <Link to="/shifts/open">{t("shift.openTitle")}</Link>
              </Button>
            )}
            <Button asChild variant="ghost" className="min-h-11">
              <Link to="/sell">{t("checkout.backToCart")}</Link>
            </Button>
          </div>
        </Card>
      </div>
    );
  }

  if (!workspaceScope || cart.lineCount === 0) {
    return null;
  }

  async function onConfirm() {
    if (submittingRef.current || saving || !workspaceScope || !readiness.shiftId) {
      return;
    }
    if (parsedTender === null || !tenderOk) {
      setSubmitError(t("checkout.tenderInvalid"));
      return;
    }

    submittingRef.current = true;
    setSaving(true);
    setSubmitError(null);

    const saleId = attemptSaleIdRef.current;
    const lines = mapCartLinesToCheckoutRequest(cart.lines);

    try {
      await refresh();
      const sale = await checkoutSale(workspaceScope, {
        lines,
        paymentMethod: "Cash",
        amountTendered: Number(parsedTender.toFixed(2)),
        saleId,
        shiftId: readiness.shiftId,
      });
      completedRef.current = true;
      cart.clear();
      attemptSaleIdRef.current = newSaleId();
      navigate(`/sell/sales/${sale.saleId}/summary`, { replace: true });
    } catch (error) {
      setSubmitError(describeCheckoutSaleError(error, t));
    } finally {
      submittingRef.current = false;
      setSaving(false);
    }
  }

  return (
    <div data-testid="checkout-cash-page" className="flex min-w-0 flex-col gap-4">
      <PageHeader title={t("checkout.title")} description={t("checkout.cashLede")} />

      {submitError ? (
        <Card data-testid="checkout-error">
          <p className="m-0 text-[length:var(--exits-text-sm)] text-[var(--exits-danger)]">
            {submitError}
          </p>
        </Card>
      ) : null}

      <Card>
        <h2 className="m-0 text-[length:var(--exits-text-md)] font-semibold">
          {t("checkout.orderPreview")}
        </h2>
        <ul className="mb-0 mt-2 list-none space-y-2 p-0">
          {cart.lines.map((line) => (
            <li
              key={line.lineKey}
              className="flex items-start justify-between gap-2 text-[length:var(--exits-text-sm)]"
            >
              <span className="min-w-0 truncate">
                {line.name} × {line.quantity} {line.unitLabel}
              </span>
              <MoneyDisplay amount={lineAmount(line)} />
            </li>
          ))}
        </ul>
        <p
          data-testid="checkout-total"
          className="mb-0 mt-3 text-[length:var(--exits-text-md)] font-semibold"
        >
          {t("checkout.totalAmount")}: <MoneyDisplay amount={previewTotal} />
        </p>
        {currentShift ? (
          <p className="mb-0 mt-1 text-[length:var(--exits-text-xs)] text-muted">
            {t("checkout.shiftHint")
              .replace("{shift}", currentShift.shiftNumber)
              .replace("{register}", currentShift.registerCode ?? "—")}
          </p>
        ) : null}
      </Card>

      <Card>
        <label
          className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]"
          htmlFor="checkout-cash-received"
        >
          {t("checkout.cashReceived")}
          <input
            id="checkout-cash-received"
            data-testid="checkout-cash-received"
            type="number"
            inputMode="decimal"
            min={0}
            step="0.01"
            value={cashReceived}
            disabled={saving}
            className="min-h-11 rounded-[var(--exits-radius-md)] border border-border bg-surface px-3 tabular-nums"
            onChange={(event) => setCashReceived(event.target.value)}
          />
        </label>
        <p data-testid="checkout-change" className="mb-0 mt-3 text-[length:var(--exits-text-sm)]">
          {t("checkout.change")}:{" "}
          {changeAdvisory === null ? (
            <span className="text-muted">—</span>
          ) : (
            <MoneyDisplay amount={changeAdvisory} />
          )}
        </p>
        <p className="mb-0 mt-1 text-[length:var(--exits-text-xs)] text-muted">
          {t("checkout.changeAdvisory")}
        </p>
      </Card>

      <div className="flex flex-wrap gap-2">
        <Button
          type="button"
          data-testid="checkout-confirm"
          className="min-h-11"
          disabled={saving || !tenderOk || cart.lineCount === 0}
          onClick={() => void onConfirm()}
        >
          {saving ? t("checkout.confirming") : t("checkout.confirmCash")}
        </Button>
        <Button asChild type="button" variant="ghost" className="min-h-11" disabled={saving}>
          <Link to="/sell">{t("checkout.backToCart")}</Link>
        </Button>
      </div>
    </div>
  );
}
