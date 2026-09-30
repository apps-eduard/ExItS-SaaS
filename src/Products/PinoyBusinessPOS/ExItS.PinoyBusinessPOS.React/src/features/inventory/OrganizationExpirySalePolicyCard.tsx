import { useEffect, useMemo, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  deleteOrganizationCategoryExpirySalePolicy,
  getOrganizationExpirySalePolicy,
  listOrganizationCategoryExpirySalePolicies,
  putOrganizationCategoryExpirySalePolicy,
  putOrganizationExpirySalePolicy,
} from "@/api/pos/pos-expiry-sale-policy-client";
import { listCatalogCategories } from "@/api/pos/pos-catalog-client";
import { PosApiError, type PosWorkspaceScope } from "@/api/pos/pos-http";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { ErrorState } from "@/components/exits/ErrorState";
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

async function invalidateExpirySalePolicyQueries(queryClient: ReturnType<typeof useQueryClient>) {
  await queryClient.invalidateQueries({ queryKey: ["inventory"] });
  await queryClient.invalidateQueries({ queryKey: ["expiry-sale-policy"] });
}

export function OrganizationExpirySalePolicyCard({ workspace, canEdit }: Props) {
  const { t } = useI18n();
  const { showToast } = useToast();
  const queryClient = useQueryClient();
  const [orgDays, setOrgDays] = useState(0);
  const [rules, setRules] = useState<CategoryExpirySaleOverrideRule[]>([]);
  const [dirty, setDirty] = useState(false);

  const orgQuery = useQuery({
    queryKey: ["expiry-sale-policy", "organization", workspace.organizationId],
    queryFn: ({ signal }) => getOrganizationExpirySalePolicy(workspace, signal),
  });

  const categoriesOverridesQuery = useQuery({
    queryKey: ["expiry-sale-policy", "organization-categories", workspace.organizationId],
    queryFn: ({ signal }) => listOrganizationCategoryExpirySalePolicies(workspace, signal),
  });

  const categoriesQuery = useQuery({
    queryKey: ["catalog", "categories", workspace.organizationId],
    queryFn: ({ signal }) => listCatalogCategories(workspace, { page: 1, pageSize: 200 }, signal),
  });

  useEffect(() => {
    if (!orgQuery.data || dirty) return;
    setOrgDays(orgQuery.data.stopSellingDaysBeforeExpiry);
  }, [orgQuery.data, dirty]);

  useEffect(() => {
    if (!categoriesOverridesQuery.data || dirty) return;
    setRules(
      categoriesOverridesQuery.data.map((r) => ({
        categoryId: r.categoryId,
        stopSellingDaysBeforeExpiry: r.stopSellingDaysBeforeExpiry,
      })),
    );
  }, [categoriesOverridesQuery.data, dirty]);

  const baselineRules = useMemo(
    () =>
      (categoriesOverridesQuery.data ?? []).map((r) => ({
        categoryId: r.categoryId,
        stopSellingDaysBeforeExpiry: r.stopSellingDaysBeforeExpiry,
      })),
    [categoriesOverridesQuery.data],
  );

  const saveMutation = useMutation({
    mutationFn: async () => {
      const days = Number.isFinite(orgDays) ? Math.max(0, Math.min(365, Math.trunc(orgDays))) : 0;
      await putOrganizationExpirySalePolicy(workspace, { stopSellingDaysBeforeExpiry: days });

      const baselineById = new Map(baselineRules.map((r) => [r.categoryId, r]));
      const nextById = new Map(rules.map((r) => [r.categoryId, r]));

      for (const [categoryId] of baselineById) {
        if (!nextById.has(categoryId)) {
          await deleteOrganizationCategoryExpirySalePolicy(workspace, categoryId);
        }
      }

      for (const rule of rules) {
        const baseline = baselineById.get(rule.categoryId);
        if (
          !baseline ||
          baseline.stopSellingDaysBeforeExpiry !== rule.stopSellingDaysBeforeExpiry
        ) {
          await putOrganizationCategoryExpirySalePolicy(workspace, rule.categoryId, {
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

  const loading = orgQuery.isLoading || categoriesOverridesQuery.isLoading;
  const error =
    orgQuery.error instanceof Error
      ? orgQuery.error.message
      : categoriesOverridesQuery.error instanceof Error
        ? categoriesOverridesQuery.error.message
        : null;

  return (
    <Card
      className="flex flex-col gap-3 p-3"
      treatment="bordered"
      data-testid="expiry-sale-policy-org-card"
    >
      <div className="flex flex-col gap-1">
        <h2 className="m-0 text-[length:var(--exits-text-sm)] font-semibold">
          {t("inventory.expirySalePolicy.title")}
        </h2>
        <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">
          {t("inventory.expirySalePolicy.lede")}
        </p>
      </div>

      {loading ? <LoadingState label={t("loading.label")} /> : null}
      {error ? <ErrorState title={t("error.title")} detail={error} /> : null}

      {!loading && !error ? (
        <>
          <div className="w-[20%] min-w-[4.5rem] max-w-full">
            <Input
              label={t("inventory.expirySalePolicy.orgDefaultDays")}
              type="number"
              min={0}
              max={365}
              step={1}
              value={orgDays}
              disabled={!canEdit}
              onChange={(e) => {
                setDirty(true);
                setOrgDays(Number(e.target.value || 0));
              }}
              data-testid="expiry-sale-policy-org-days"
            />
          </div>
          <Notice tone="info">{t("inventory.expirySalePolicy.orgHelp")}</Notice>
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
              testIdPrefix="org-expiry-sale-overrides"
            />
          </div>
          {canEdit ? (
            <Button
              type="button"
              className="w-auto self-end"
              disabled={saveMutation.isPending || !dirty}
              onClick={() => saveMutation.mutate()}
              data-testid="expiry-sale-policy-org-save"
            >
              {t("inventory.expirySalePolicy.save")}
            </Button>
          ) : null}
        </>
      ) : null}
    </Card>
  );
}
