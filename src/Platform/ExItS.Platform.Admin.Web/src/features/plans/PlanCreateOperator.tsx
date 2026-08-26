import { useState } from "react";
import { Plus } from "lucide-react";
import { useNavigate } from "react-router-dom";
import { planDetailHref } from "@/api/catalog/plan-list-query";
import type { CreatePlanBody } from "@/api/catalog/plan-mutations-client";
import { classifyCommercialMutationFailure } from "@/api/commercial/commercial-errors";
import { Alert } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { ConfirmActionDialog } from "@/components/exits/ConfirmActionDialog";
import { Input } from "@/components/ui/input";
import { useCreatePlanMutation } from "@/features/commercial/use-commercial-mutations";
import { useAuthorizedCatalogProductsQuery } from "@/features/navigation/use-catalog-products-query";
import { planMutationFailureCopy } from "@/features/plans/plan-mutation-feedback";
import { usePreferences } from "@/hooks/use-preferences";

const controlClass =
  "h-[var(--exits-control-height)] min-h-[var(--exits-touch-target-min)] rounded-[var(--exits-density-radius)] border border-input bg-surface px-3 text-[length:var(--exits-text-sm)] text-foreground";

export function PlanCreateOperator() {
  const { t } = usePreferences();
  const navigate = useNavigate();
  const productsQuery = useAuthorizedCatalogProductsQuery();
  const createPlan = useCreatePlanMutation();
  const [open, setOpen] = useState(false);
  const [productCode, setProductCode] = useState("");
  const [code, setCode] = useState("");
  const [displayName, setDisplayName] = useState("");
  const [description, setDescription] = useState("");
  const [monthlyPrice, setMonthlyPrice] = useState("");
  const [annualPrice, setAnnualPrice] = useState("");
  const [currencyCode, setCurrencyCode] = useState("PHP");
  const [errorCopy, setErrorCopy] = useState<{ title: string; detail: string } | null>(null);

  const products = productsQuery.isSuccess ? (productsQuery.data?.items ?? []) : [];
  const defaultProductCode = products[0]?.code ?? "";
  const catalogBlocked =
    productsQuery.isError || (productsQuery.isSuccess && products.length === 0);
  const catalogBlockedMessage = productsQuery.isError
    ? t("plans.create.catalogUnavailable")
    : productsQuery.isSuccess && products.length === 0
      ? t("plans.create.catalogEmpty")
      : null;

  function openDialog() {
    setProductCode((current) => current || defaultProductCode);
    setOpen(true);
  }

  function resetForm() {
    setCode("");
    setDisplayName("");
    setDescription("");
    setMonthlyPrice("");
    setAnnualPrice("");
    setCurrencyCode("PHP");
    setErrorCopy(null);
    createPlan.reset();
  }

  function closeDialog() {
    resetForm();
    setOpen(false);
  }

  function buildBody(): CreatePlanBody | null {
    if (!code.trim() || !displayName.trim()) {
      return null;
    }
    const body: CreatePlanBody = {
      code: code.trim(),
      displayName: displayName.trim(),
    };
    if (description.trim()) {
      body.description = description.trim();
    }
    if (monthlyPrice.trim()) {
      const value = Number(monthlyPrice);
      if (Number.isFinite(value)) body.monthlyPrice = value;
    }
    if (annualPrice.trim()) {
      const value = Number(annualPrice);
      if (Number.isFinite(value)) body.annualPrice = value;
    }
    if (currencyCode.trim()) {
      body.currencyCode = currencyCode.trim();
    }
    return body;
  }

  async function submit() {
    const body = buildBody();
    if (!productCode || !body || createPlan.isPending || catalogBlocked) {
      return;
    }
    setErrorCopy(null);
    try {
      const plan = await createPlan.mutateAsync({ productCode, body });
      closeDialog();
      navigate(planDetailHref(plan.id));
    } catch (error) {
      const copy = planMutationFailureCopy(error, t);
      setErrorCopy(copy);
      if (classifyCommercialMutationFailure(error).kind === "conflict") {
        // keep dialog open for correction
      }
    }
  }

  const ready = Boolean(productCode && code.trim() && displayName.trim() && !catalogBlocked);

  return (
    <>
      <Button
        type="button"
        size="sm"
        disabled={productsQuery.isPending}
        aria-busy={productsQuery.isPending}
        onClick={openDialog}
      >
        <Plus aria-hidden className="mr-2 size-4" />
        {t("plans.create.action")}
      </Button>
      {open ? (
        <ConfirmActionDialog
          open
          title={t("plans.create.title")}
          description={t("plans.create.description")}
          confirmLabel={t("plans.create.confirm")}
          cancelLabel={t("plans.create.cancel")}
          pendingLabel={t("plans.create.pending")}
          pending={createPlan.isPending || productsQuery.isPending}
          confirmDisabled={!ready}
          error={
            errorCopy ? (
              <Alert title={errorCopy.title} tone="danger">
                {errorCopy.detail}
              </Alert>
            ) : catalogBlockedMessage ? (
              <Alert title={catalogBlockedMessage} tone="danger" />
            ) : null
          }
          onCancel={closeDialog}
          onConfirm={() => void submit()}
        >
          {productsQuery.isPending ? (
            <p className="text-[length:var(--exits-text-sm)] text-muted" role="status" aria-busy="true">
              {t("plans.create.catalogLoading")}
            </p>
          ) : null}
          {!productsQuery.isPending && !catalogBlocked ? (
            <label className="grid gap-1 text-[length:var(--exits-text-xs)] font-medium" htmlFor="plan-create-product">
              {t("plans.create.product")}
              <select
                id="plan-create-product"
                className={controlClass}
                value={productCode}
                onChange={(event) => setProductCode(event.target.value)}
              >
                {products.map((product) => (
                  <option key={product.code} value={product.code}>
                    {product.displayName}
                  </option>
                ))}
              </select>
            </label>
          ) : null}
          <label className="grid gap-1 text-[length:var(--exits-text-xs)] font-medium" htmlFor="plan-create-code">
            {t("plans.create.code")}
            <Input
              id="plan-create-code"
              value={code}
              onChange={(event) => setCode(event.target.value)}
              autoComplete="off"
            />
          </label>
          <label className="grid gap-1 text-[length:var(--exits-text-xs)] font-medium" htmlFor="plan-create-name">
            {t("plans.create.displayName")}
            <Input
              id="plan-create-name"
              value={displayName}
              onChange={(event) => setDisplayName(event.target.value)}
              autoComplete="off"
            />
          </label>
          <label className="grid gap-1 text-[length:var(--exits-text-xs)] font-medium" htmlFor="plan-create-description">
            {t("plans.create.descriptionField")}
            <Input
              id="plan-create-description"
              value={description}
              onChange={(event) => setDescription(event.target.value)}
              autoComplete="off"
            />
          </label>
          <div className="grid gap-2 sm:grid-cols-3">
            <label className="grid gap-1 text-[length:var(--exits-text-xs)] font-medium" htmlFor="plan-create-monthly">
              {t("plans.create.monthlyPrice")}
              <Input
                id="plan-create-monthly"
                inputMode="decimal"
                value={monthlyPrice}
                onChange={(event) => setMonthlyPrice(event.target.value)}
              />
            </label>
            <label className="grid gap-1 text-[length:var(--exits-text-xs)] font-medium" htmlFor="plan-create-annual">
              {t("plans.create.annualPrice")}
              <Input
                id="plan-create-annual"
                inputMode="decimal"
                value={annualPrice}
                onChange={(event) => setAnnualPrice(event.target.value)}
              />
            </label>
            <label className="grid gap-1 text-[length:var(--exits-text-xs)] font-medium" htmlFor="plan-create-currency">
              {t("plans.create.currency")}
              <Input
                id="plan-create-currency"
                value={currencyCode}
                onChange={(event) => setCurrencyCode(event.target.value)}
              />
            </label>
          </div>
        </ConfirmActionDialog>
      ) : null}
    </>
  );
}
