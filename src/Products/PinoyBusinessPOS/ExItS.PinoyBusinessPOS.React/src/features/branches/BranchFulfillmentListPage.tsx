import { useMemo, useState } from "react";
import { Link, Navigate } from "react-router-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { ChevronRight } from "lucide-react";
import { canManageBranchFulfillment } from "@/access/pos-capabilities";
import {
  listOrganizationBranchesForFulfillment,
  updateBranchFulfillmentSettings,
  type OrganizationBranchDto,
  type UpdateBranchFulfillmentSettingsRequest,
} from "@/api/platform/branch-fulfillment-client";
import { PlatformApiError } from "@/api/platform/platform-http";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingState } from "@/components/exits/LoadingState";
import { PageHeader } from "@/components/exits/PageHeader";
import { StatusChip } from "@/components/exits/StatusChip";
import { BranchFulfillmentSwitch } from "@/features/branches/BranchFulfillmentSwitch";
import { BranchSetupTabLinks } from "@/features/branches/BranchSetupTabLinks";
import { branchFulfillmentEditPath } from "@/features/branches/branch-setup-tabs";
import { resolveFulfillmentToggle } from "@/features/branches/fulfillment-toggle";
import { useI18n } from "@/i18n/I18nProvider";
import { pageBackNav } from "@/navigation/page-back-nav";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

type OptimisticPatch = {
  branchId: string;
  pickupEnabled?: boolean;
  deliveryEnabled?: boolean;
};

export function BranchFulfillmentListPage() {
  const { t } = useI18n();
  const queryClient = useQueryClient();
  const { boundWorkspace, sessionGrant } = useWorkspace();
  const canManage = canManageBranchFulfillment(sessionGrant);
  const organizationId = boundWorkspace?.organizationId;
  const [optimistic, setOptimistic] = useState<OptimisticPatch | null>(null);
  const [toggleError, setToggleError] = useState<string | null>(null);

  const branchesQuery = useQuery({
    queryKey: ["branch-fulfillment-list", organizationId],
    enabled: Boolean(organizationId && canManage),
    queryFn: ({ signal }) => listOrganizationBranchesForFulfillment(organizationId!, signal),
  });

  const toggleMutation = useMutation({
    mutationFn: async (input: {
      branchId: string;
      request: UpdateBranchFulfillmentSettingsRequest;
      previous: OrganizationBranchDto;
    }) => {
      setOptimistic({
        branchId: input.branchId,
        pickupEnabled: input.request.pickupEnabled ?? undefined,
        deliveryEnabled: input.request.deliveryEnabled ?? undefined,
      });
      setToggleError(null);
      try {
        return await updateBranchFulfillmentSettings(
          organizationId!,
          input.branchId,
          input.request,
        );
      } catch (err) {
        setOptimistic(null);
        throw err;
      }
    },
    onSuccess: async () => {
      setOptimistic(null);
      await queryClient.invalidateQueries({
        queryKey: ["branch-fulfillment-list", organizationId],
      });
    },
    onError: (err) => {
      setOptimistic(null);
      setToggleError(
        err instanceof PlatformApiError
          ? (err.problem.detail ?? t("branches.fulfillmentFailed"))
          : t("branches.fulfillmentFailed"),
      );
    },
  });

  const branches = useMemo(() => {
    const items = branchesQuery.data ?? [];
    return [...items]
      .map((branch) => {
        if (!optimistic || optimistic.branchId !== branch.id) {
          return branch;
        }
        return {
          ...branch,
          pickupEnabled: optimistic.pickupEnabled ?? branch.pickupEnabled,
          deliveryEnabled: optimistic.deliveryEnabled ?? branch.deliveryEnabled,
        };
      })
      .sort((a, b) => {
        if (a.isPrimary !== b.isPrimary) {
          return a.isPrimary ? -1 : 1;
        }
        return a.name.localeCompare(b.name);
      });
  }, [branchesQuery.data, optimistic]);

  if (!canManage) {
    return (
      <div
        data-testid="branch-fulfillment-denied"
        className="branch-fulfillment-page exits-page flex min-w-0 flex-col gap-3"
      >
        <PageHeader
          title={t("branches.listTitle")}
          description={t("branches.denied")}
          backTo={pageBackNav.org.to}
          backLabel={t(pageBackNav.org.labelKey)}
          backTestId="page-header-back-org"
        />
      </div>
    );
  }

  if (!organizationId || branchesQuery.isLoading) {
    return <LoadingState label={t("loading.label")} />;
  }

  if (branchesQuery.isError) {
    return (
      <div
        data-testid="branch-fulfillment-list-error"
        className="branch-fulfillment-page exits-page flex min-w-0 flex-col gap-3"
      >
        <PageHeader
          title={t("branches.listTitle")}
          description={t("branches.listLede")}
          backTo={pageBackNav.org.to}
          backLabel={t(pageBackNav.org.labelKey)}
          backTestId="page-header-back-org"
        />
        <ErrorState title={t("branches.loadError")} detail={t("branches.listLede")} />
      </div>
    );
  }

  if (branches.length === 1) {
    return <Navigate to={branchFulfillmentEditPath(branches[0].id)} replace />;
  }

  return (
    <div
      className="branch-fulfillment-page exits-page flex min-w-0 flex-col gap-3"
      data-testid="branch-fulfillment-list"
    >
      <PageHeader
        title={t("branches.listTitle")}
        description={t("branches.listLede")}
        backTo={pageBackNav.org.to}
        backLabel={t(pageBackNav.org.labelKey)}
        backTestId="page-header-back-org"
      />

      {toggleError ? (
        <div className="exits-alert exits-alert--error" role="alert" data-testid="branch-list-toggle-error">
          <p className="m-0 text-[length:var(--exits-text-sm)]">{toggleError}</p>
        </div>
      ) : null}

      {branches.length === 0 ? (
        <EmptyState title={t("branches.emptyTitle")} detail={t("branches.emptyDetail")} />
      ) : (
        <ul className="exits-list m-0 grid list-none gap-2 p-0" data-testid="branch-fulfillment-items">
          {branches.map((branch) => {
            const meta = [branch.code, branch.city].filter(Boolean).join(" · ");
            const pending =
              toggleMutation.isPending &&
              optimistic?.branchId === branch.id;
            const pickup = resolveFulfillmentToggle({
              channel: "pickup",
              enabled: branch.pickupEnabled,
              ready: branch.pickupReady,
              canUseDelivery: branch.canUseDelivery,
              pending,
            });
            const delivery = resolveFulfillmentToggle({
              channel: "delivery",
              enabled: branch.deliveryEnabled,
              ready: branch.deliveryReady,
              canUseDelivery: branch.canUseDelivery,
              pending,
            });

            return (
              <li key={branch.id}>
                <div
                  className="exits-list__card branch-row branch-row--with-toggles"
                  data-testid={`branch-fulfillment-card-${branch.id}`}
                >
                  <Link
                    className="branch-row__info min-w-0 text-foreground no-underline"
                    to={branchFulfillmentEditPath(branch.id)}
                    data-testid={`open-branch-fulfillment-${branch.id}`}
                  >
                    <div className="branch-row__main min-w-0">
                      <strong className="exits-list__name block truncate font-semibold">
                        {branch.name}
                      </strong>
                      {meta ? (
                        <p className="branch-row__meta mb-0 mt-1 truncate text-[length:var(--exits-text-sm)] text-muted">
                          {meta}
                        </p>
                      ) : null}
                    </div>
                    <span className="branch-row__aside">
                      <span className="branch-row__chips shrink-0">
                        <StatusChip tone={branch.status === "Active" ? "success" : "warning"}>
                          {branch.status}
                        </StatusChip>
                      </span>
                      <span className="sr-only">{t("branches.configure")}</span>
                      <ChevronRight
                        className="branch-row__chevron size-4 shrink-0 text-muted"
                        aria-hidden
                      />
                    </span>
                  </Link>

                  <BranchSetupTabLinks branchId={branch.id} summary={branch} t={t} />

                  <div className="branch-row__toggles" data-testid={`branch-toggles-${branch.id}`}>
                    <BranchFulfillmentSwitch
                      checked={pickup.checked}
                      disabled={pickup.disabled}
                      pending={pending}
                      label={t("branches.channel.pickup")}
                      hint={pickup.hintKey ? t(pickup.hintKey) : null}
                      testId={`pickup-switch-${branch.id}`}
                      onCheckedChange={(next) => {
                        if (next && pickup.enableBlocked) {
                          return;
                        }
                        toggleMutation.mutate({
                          branchId: branch.id,
                          previous: branch,
                          request: { pickupEnabled: next },
                        });
                      }}
                    />
                    <BranchFulfillmentSwitch
                      checked={delivery.checked}
                      disabled={delivery.disabled}
                      pending={pending}
                      label={t("branches.channel.delivery")}
                      hint={delivery.hintKey ? t(delivery.hintKey) : null}
                      testId={`delivery-switch-${branch.id}`}
                      onCheckedChange={(next) => {
                        if (next && delivery.enableBlocked) {
                          return;
                        }
                        toggleMutation.mutate({
                          branchId: branch.id,
                          previous: branch,
                          request: { deliveryEnabled: next },
                        });
                      }}
                    />
                  </div>
                </div>
              </li>
            );
          })}
        </ul>
      )}
    </div>
  );
}
