import { useEffect, useMemo, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  deleteBranchCategoryExpirySalePolicy,
  deleteBranchExpirySalePolicy,
  getBranchExpirySalePolicy,
  getEffectiveExpirySalePolicy,
  listBranchCategoryExpirySalePolicies,
  putBranchCategoryExpirySalePolicy,
  putBranchExpirySalePolicy,
} from "@/api/pos/pos-expiry-sale-policy-client";
import { listCatalogCategories } from "@/api/pos/pos-catalog-client";
import { PosApiError, type PosWorkspaceScope } from "@/api/pos/pos-http";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { ErrorState } from "@/components/exits/ErrorState";
import { ExitsPillSelect } from "@/components/exits/ExitsPillSelect";
import { LoadingState } from "@/components/exits/LoadingState";
import { Notice } from "@/components/exits/Notice";
import { useToast } from "@/components/exits/ToastProvider";
import {
  CategoryExpirySaleOverridesPanel,
  type CategoryExpirySaleOverrideRule,
} from "@/features/inventory/CategoryExpirySaleOverridesPanel";
import { useI18n } from "@/i18n/I18nProvider";

type Props = {
  workspace: PosWorkspaceScope;
  canEdit: boolean;
};

type BranchMode = "UseOrganization" | "Custom";

async function invalidateExpirySalePolicyQueries(queryClient: ReturnType<typeof useQueryClient>) {
  await queryClient.invalidateQueries({ queryKey: ["inventory"] });
  await queryClient.invalidateQueries({ queryKey: ["expiry-sale-policy"] });
}

function sourceLabel(
  source: string,
  t: ReturnType<typeof useI18n>["t"],
): string {
  switch (source) {
    case "OrganizationDefault":
      return t("inventory.expirySalePolicy.sourceOrganizationDefault");
    case "OrganizationCategory":
      return t("inventory.expirySalePolicy.sourceOrganizationCategory");
    case "Branch":
      return t("inventory.expirySalePolicy.sourceBranch");
    case "BranchCategory":
      return t("inventory.expirySalePolicy.sourceBranchCategory");
    default:
      return source;
  }
}

export function BranchExpirySalePolicyCard({ workspace, canEdit }: Props) {
  const { t } = useI18n();
  const { showToast } = useToast();
  const queryClient = useQueryClient();
  const [mode, setMode] = useState<BranchMode>("UseOrganization");
  const [branchDays, setBranchDays] = useState(0);
  const [rules, setRules] = useState<CategoryExpirySaleOverrideRule[]>([]);
  const [dirty, setDirty] = useState(false);

  const branchQuery = useQuery({
    queryKey: [
      "expiry-sale-policy",
      "branch",
      workspace.organizationId,
      workspace.branchId,
    ],
    enabled: Boolean(workspace.branchId),
    queryFn: ({ signal }) => getBranchExpirySalePolicy(workspace, signal),
  });

  const effectiveQuery = useQuery({
    queryKey: [
      "expiry-sale-policy",
      "effective",
      workspace.organizationId,
      workspace.branchId,
    ],
    enabled: Boolean(workspace.branchId),
    queryFn: ({ signal }) => getEffectiveExpirySalePolicy(workspace, null, signal),
  });

  const categoryOverridesQuery = useQuery({
    queryKey: [
      "expiry-sale-policy",
      "branch-categories",
      workspace.organizationId,
      workspace.branchId,
    ],
    enabled: Boolean(workspace.branchId),
    queryFn: ({ signal }) => listBranchCategoryExpirySalePolicies(workspace, signal),
  });

  const categoriesQuery = useQuery({
    queryKey: ["catalog", "categories", workspace.organizationId],
    queryFn: ({ signal }) => listCatalogCategories(workspace, { page: 1, pageSize: 200 }, signal),
  });

  useEffect(() => {
    if (!branchQuery.data || dirty) return;
    setMode(branchQuery.data.isExplicit ? "Custom" : "UseOrganization");
    setBranchDays(branchQuery.data.stopSellingDaysBeforeExpiry);
  }, [branchQuery.data, dirty]);

  useEffect(() => {
    if (!categoryOverridesQuery.data || dirty) return;
    setRules(
      categoryOverridesQuery.data.map((r) => ({
        categoryId: r.categoryId,
        stopSellingDaysBeforeExpiry: r.stopSellingDaysBeforeExpiry,
      })),
    );
  }, [categoryOverridesQuery.data, dirty]);

  const baselineRules = useMemo(
    () =>
      (categoryOverridesQuery.data ?? []).map((r) => ({
        categoryId: r.categoryId,
        stopSellingDaysBeforeExpiry: r.stopSellingDaysBeforeExpiry,
      })),
    [categoryOverridesQuery.data],
  );

  const saveMutation = useMutation({
    mutationFn: async () => {
      if (mode === "UseOrganization") {
        if (branchQuery.data?.isExplicit) {
          await deleteBranchExpirySalePolicy(workspace);
        }
      } else {
        const days = Number.isFinite(branchDays)
          ? Math.max(0, Math.min(365, Math.trunc(branchDays)))
          : 0;
        await putBranchExpirySalePolicy(workspace, { stopSellingDaysBeforeExpiry: days });
      }

      const baselineById = new Map(baselineRules.map((r) => [r.categoryId, r]));
      const nextById = new Map(rules.map((r) => [r.categoryId, r]));

      for (const [categoryId] of baselineById) {
        if (!nextById.has(categoryId)) {
          await deleteBranchCategoryExpirySalePolicy(workspace, categoryId);
        }
      }

      for (const rule of rules) {
        const baseline = baselineById.get(rule.categoryId);
        if (
          !baseline ||
          baseline.stopSellingDaysBeforeExpiry !== rule.stopSellingDaysBeforeExpiry
        ) {
          await putBranchCategoryExpirySalePolicy(workspace, rule.categoryId, {
            stopSellingDaysBeforeExpiry: rule.stopSellingDaysBeforeExpiry,
          });
        }
      }
    },
    onSuccess: async () => {
      setDirty(false);
      showToast({ tone: "success", title: t("inventory.expirySalePolicy.saved") });
      await invalidateExpirySalePolicyQueries(queryClient);
    },
    onError: (err) => {
      showToast({
        tone: "error",
        title: t("error.title"),
        description:
          err instanceof PosApiError ? (err.problem.detail ?? err.message) : (err as Error).message,
      });
    },
  });

  const loading =
    branchQuery.isLoading || effectiveQuery.isLoading || categoryOverridesQuery.isLoading;
  const error =
    branchQuery.error instanceof Error
      ? branchQuery.error.message
      : effectiveQuery.error instanceof Error
        ? effectiveQuery.error.message
        : categoryOverridesQuery.error instanceof Error
          ? categoryOverridesQuery.error.message
          : null;

  const effective = effectiveQuery.data;

  return (
    <Card
      className="flex flex-col gap-3 p-3"
      treatment="bordered"
      data-testid="expiry-sale-policy-branch-card"
    >
      <div className="flex flex-col gap-1">
        <h2 className="m-0 text-[length:var(--exits-text-sm)] font-semibold">
          {t("inventory.expirySalePolicy.branchTitle")}
        </h2>
        <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">
          {t("inventory.expirySalePolicy.branchLede")}
        </p>
      </div>

      {loading ? <LoadingState label={t("loading.label")} /> : null}
      {error ? <ErrorState title={t("error.title")} detail={error} /> : null}

      {!loading && !error && effective ? (
        <>
          <dl
            className="m-0 grid grid-cols-[auto_1fr] gap-x-4 gap-y-1 text-[length:var(--exits-text-sm)]"
            data-testid="expiry-sale-policy-effective"
          >
            <dt className="text-muted">{t("inventory.expirySalePolicy.effectiveDays")}</dt>
            <dd className="m-0 justify-self-end tabular-nums font-medium">
              {t("inventory.expirySaleOverride.days").replace(
                "{n}",
                String(effective.stopSellingDaysBeforeExpiry),
              )}
            </dd>
            <dt className="text-muted">{t("inventory.expirySalePolicy.effectiveSource")}</dt>
            <dd className="m-0 justify-self-end font-medium">
              {sourceLabel(effective.source, t)}
            </dd>
            <dt className="text-muted">{t("inventory.expirySalePolicy.orgDefaultDays")}</dt>
            <dd className="m-0 justify-self-end tabular-nums">
              {effective.organizationDefaultDays}
            </dd>
            {effective.organizationCategoryDays != null ? (
              <>
                <dt className="text-muted">
                  {t("inventory.expirySalePolicy.orgCategoryDays")}
                </dt>
                <dd className="m-0 justify-self-end tabular-nums">
                  {effective.organizationCategoryDays}
                </dd>
              </>
            ) : null}
            {effective.branchDefaultDays != null ? (
              <>
                <dt className="text-muted">{t("inventory.expirySalePolicy.branchDefaultDays")}</dt>
                <dd className="m-0 justify-self-end tabular-nums">{effective.branchDefaultDays}</dd>
              </>
            ) : null}
          </dl>

          <div className="flex flex-col gap-1.5">
            <span className="exits-type-label">{t("inventory.expirySalePolicy.branchOverride")}</span>
            <ExitsPillSelect
              mode="single"
              aria-label={t("inventory.expirySalePolicy.branchOverride")}
              value={mode}
              onChange={(next) => {
                setDirty(true);
                setMode(next);
              }}
              options={[
                {
                  value: "UseOrganization",
                  label: t("inventory.expirySalePolicy.modeUseOrganization"),
                },
                {
                  value: "Custom",
                  label: t("inventory.expirySalePolicy.modeCustomBranch"),
                },
              ]}
              disabled={!canEdit}
              testId="expiry-sale-policy-branch-mode"
            />
          </div>

          {mode === "Custom" ? (
            <div className="w-[20%] min-w-[4.5rem] max-w-full">
              <Input
                label={t("inventory.expirySalePolicy.branchDays")}
                type="number"
                min={0}
                max={365}
                step={1}
                value={branchDays}
                disabled={!canEdit}
                onChange={(e) => {
                  setDirty(true);
                  setBranchDays(Number(e.target.value || 0));
                }}
                data-testid="expiry-sale-policy-branch-days"
              />
            </div>
          ) : null}

          <Notice tone="info">{t("inventory.expirySalePolicy.branchHelp")}</Notice>

          <div className="border-t border-border pt-3">
            <CategoryExpirySaleOverridesPanel
              rules={rules}
              categories={(categoriesQuery.data?.items ?? []).map((c) => ({
                categoryId: c.categoryId,
                name: c.name,
              }))}
              canEdit={canEdit}
              onChange={(next) => {
                setDirty(true);
                setRules(next);
              }}
              testIdPrefix="branch-expiry-sale-overrides"
            />
          </div>

          {canEdit ? (
            <Button
              type="button"
              className="w-auto self-end"
              disabled={saveMutation.isPending || !dirty}
              onClick={() => saveMutation.mutate()}
              data-testid="expiry-sale-policy-branch-save"
            >
              {t("inventory.expirySalePolicy.save")}
            </Button>
          ) : null}
        </>
      ) : null}
    </Card>
  );
}
