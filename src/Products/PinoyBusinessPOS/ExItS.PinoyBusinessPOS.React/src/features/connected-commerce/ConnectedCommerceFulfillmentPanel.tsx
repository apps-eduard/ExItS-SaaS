import { useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Package, Store, Truck } from "lucide-react";
import {
  canManageBranchFulfillment,
  canManageSuppliers,
  hasOrganizationManagementAuthority,
} from "@/access/pos-capabilities";
import { PosApiError, type PosWorkspaceScope } from "@/api/pos/pos-http";
import {
  getOrganizationOfferDelivery,
  updateBranchFulfillmentSettingsViaPos,
  updateOrganizationBranchFulfillmentDefaults,
  updateOrganizationOfferDelivery,
} from "@/api/pos/pos-connected-commerce-client";
import {
  listOrganizationBranchesForFulfillment,
  normalizeFulfillmentReadiness,
  setBranchOnlineOrdersPaused,
  type OrganizationBranchDto,
  type UpdateBranchFulfillmentSettingsRequest,
} from "@/api/platform/branch-fulfillment-client";
import { PlatformApiError } from "@/api/platform/platform-http";
import { Button } from "@/components/ui/button";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingState } from "@/components/exits/LoadingState";
import { Notice } from "@/components/exits/Notice";
import { StatusChip } from "@/components/exits/StatusChip";
import { useToast } from "@/components/exits/ToastProvider";
import {
  BranchDeliveryOrgOfferGuardDialog,
  isOrganizationDeliveryNotOfferedError,
} from "@/features/branches/BranchDeliveryOrgOfferGuardDialog";
import { branchFulfillmentEditPath } from "@/features/branches/branch-setup-tabs";
import { isWarehouseBranch } from "@/features/branches/branch-type";
import {
  invalidateOrganizationOfferDeliveryQueries,
  organizationOfferDeliveryQueryKey,
} from "@/features/branches/offer-delivery-queries";
import { resolveConnectedCommerceBranchRowStatus } from "@/features/connected-commerce/connected-commerce-branch-row-status";
import { FulfillmentSwitchCard } from "@/features/connected-commerce/FulfillmentSwitchCard";
import { useI18n } from "@/i18n/I18nProvider";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

type OptimisticPatch = {
  branchId: string;
  pickupEnabled?: boolean;
  deliveryEnabled?: boolean;
  customerOrderingEnabled?: boolean;
  onlineOrdersPaused?: boolean;
};

type DefaultsPatch = {
  defaultPickupEnabled?: boolean;
  defaultDeliveryEnabled?: boolean;
  defaultOnlineOrdersEnabled?: boolean;
};

type ConnectedCommerceFulfillmentPanelProps = {
  workspace: PosWorkspaceScope;
  organizationId: string;
};

function errorMessage(err: unknown, fallback: string): string {
  if (err instanceof PosApiError) {
    return err.problem.detail ?? err.problem.title ?? err.message ?? fallback;
  }
  if (err instanceof PlatformApiError) {
    return err.problem.detail ?? err.problem.title ?? err.message ?? fallback;
  }
  if (err instanceof Error && err.message.trim()) {
    return err.message;
  }
  return fallback;
}

export function ConnectedCommerceFulfillmentPanel({
  workspace,
  organizationId,
}: ConnectedCommerceFulfillmentPanelProps) {
  const { t } = useI18n();
  const { showToast } = useToast();
  const queryClient = useQueryClient();
  const { sessionGrant } = useWorkspace();
  const canEditOrg =
    hasOrganizationManagementAuthority(sessionGrant) || canManageSuppliers(sessionGrant);
  const canEditBranches = canManageBranchFulfillment(sessionGrant);

  const [toggleError, setToggleError] = useState<string | null>(null);
  const [optimistic, setOptimistic] = useState<OptimisticPatch | null>(null);
  const [optimisticDefaults, setOptimisticDefaults] = useState<DefaultsPatch | null>(null);
  const [orgDeliveryGuardOpen, setOrgDeliveryGuardOpen] = useState(false);

  const settingsQuery = useQuery({
    queryKey: ["connected-commerce", "fulfillment", organizationId],
    queryFn: ({ signal }) => getOrganizationOfferDelivery(workspace, signal),
  });

  useQuery({
    queryKey: organizationOfferDeliveryQueryKey(organizationId),
    queryFn: ({ signal }) => getOrganizationOfferDelivery(workspace, signal),
  });

  const branchesQuery = useQuery({
    queryKey: ["branch-fulfillment-list", organizationId, "connected-commerce"],
    enabled: Boolean(organizationId),
    queryFn: ({ signal }) => listOrganizationBranchesForFulfillment(organizationId, signal),
  });

  async function refreshFulfillment() {
    await invalidateOrganizationOfferDeliveryQueries(queryClient, organizationId);
    await queryClient.invalidateQueries({
      queryKey: ["branch-fulfillment-list", organizationId],
    });
  }

  const offerMutation = useMutation({
    mutationFn: async (offerDelivery: boolean) =>
      updateOrganizationOfferDelivery(workspace, offerDelivery),
    onSuccess: async () => {
      setToggleError(null);
      await refreshFulfillment();
      showToast(t("connectedCommerce.fulfillmentSaved"), "success");
    },
    onError: (err) =>
      setToggleError(errorMessage(err, t("connectedCommerce.fulfillmentSaveFailed"))),
  });

  const defaultsMutation = useMutation({
    mutationFn: async (body: {
      defaultPickupEnabled: boolean;
      defaultDeliveryEnabled: boolean;
      defaultOnlineOrdersEnabled: boolean;
    }) =>
      updateOrganizationBranchFulfillmentDefaults(workspace, {
        offerDelivery: settingsQuery.data?.offerDelivery === true,
        ...body,
      }),
    onSuccess: async () => {
      setOptimisticDefaults(null);
      setToggleError(null);
      await refreshFulfillment();
      showToast(t("connectedCommerce.fulfillmentSaved"), "success");
    },
    onError: (err) => {
      setOptimisticDefaults(null);
      setToggleError(errorMessage(err, t("connectedCommerce.fulfillmentSaveFailed")));
    },
  });

  const branchToggleMutation = useMutation({
    mutationFn: async (input: {
      branchId: string;
      request?: UpdateBranchFulfillmentSettingsRequest;
      pause?: boolean;
      optimistic: OptimisticPatch;
    }) => {
      setOptimistic(input.optimistic);
      setToggleError(null);
      try {
        if (input.request) {
          const raw = await updateBranchFulfillmentSettingsViaPos(
            workspace,
            input.branchId,
            input.request,
          );
          normalizeFulfillmentReadiness(raw);
        }
        if (typeof input.pause === "boolean") {
          await setBranchOnlineOrdersPaused(organizationId, input.branchId, {
            paused: input.pause,
            reason: input.pause ? "Paused from Connected Commerce fulfillment" : null,
          });
        }
      } catch (err) {
        setOptimistic(null);
        throw err;
      }
    },
    onSuccess: async () => {
      setOptimistic(null);
      await refreshFulfillment();
    },
    onError: (err) => {
      setOptimistic(null);
      if (isOrganizationDeliveryNotOfferedError(err)) {
        setOrgDeliveryGuardOpen(true);
        setToggleError(null);
        return;
      }
      setToggleError(errorMessage(err, t("branches.fulfillmentFailed")));
    },
  });

  const settings = settingsQuery.data;
  const offerDelivery =
    offerMutation.isPending && offerMutation.variables != null
      ? offerMutation.variables
      : settings?.offerDelivery === true;
  const defaultPickup =
    optimisticDefaults?.defaultPickupEnabled ?? settings?.defaultPickupEnabled === true;
  const defaultDelivery =
    optimisticDefaults?.defaultDeliveryEnabled ?? settings?.defaultDeliveryEnabled === true;
  const defaultOnline =
    optimisticDefaults?.defaultOnlineOrdersEnabled ??
    settings?.defaultOnlineOrdersEnabled === true;

  const branches = useMemo(() => {
    const items = (branchesQuery.data ?? []).filter(
      (b) =>
        String(b.status).toLowerCase() === "active" &&
        !isWarehouseBranch(b.branchType),
    );
    return [...items]
      .map((branch) => {
        if (!optimistic || optimistic.branchId !== branch.id) {
          return branch;
        }
        return {
          ...branch,
          pickupEnabled: optimistic.pickupEnabled ?? branch.pickupEnabled,
          deliveryEnabled: optimistic.deliveryEnabled ?? branch.deliveryEnabled,
          customerOrderingEnabled:
            optimistic.customerOrderingEnabled ?? branch.customerOrderingEnabled,
          onlineOrdersPaused: optimistic.onlineOrdersPaused ?? branch.onlineOrdersPaused,
        };
      })
      .sort((a, b) => {
        if (a.isPrimary !== b.isPrimary) {
          return a.isPrimary ? -1 : 1;
        }
        return a.name.localeCompare(b.name);
      });
  }, [branchesQuery.data, optimistic]);

  if (settingsQuery.isLoading || branchesQuery.isLoading) {
    return <LoadingState label={t("loading.label")} />;
  }

  if (settingsQuery.isError) {
    return (
      <ErrorState
        title={t("connectedCommerce.fulfillmentLoadError")}
        detail={errorMessage(settingsQuery.error, t("connectedCommerce.lede"))}
      />
    );
  }

  function patchDefaults(partial: DefaultsPatch) {
    if (!canEditOrg || defaultsMutation.isPending) {
      return;
    }
    const next = {
      defaultPickupEnabled: partial.defaultPickupEnabled ?? defaultPickup,
      defaultDeliveryEnabled: partial.defaultDeliveryEnabled ?? defaultDelivery,
      defaultOnlineOrdersEnabled: partial.defaultOnlineOrdersEnabled ?? defaultOnline,
    };
    setOptimisticDefaults(next);
    setToggleError(null);
    defaultsMutation.mutate(next);
  }

  function toggleBranchChannel(
    branch: OrganizationBranchDto,
    channel: "pickup" | "delivery" | "online",
    next: boolean,
  ) {
    if (!canEditBranches || branchToggleMutation.isPending) {
      return;
    }

    if (channel === "pickup") {
      branchToggleMutation.mutate({
        branchId: branch.id,
        request: { pickupEnabled: next },
        optimistic: { branchId: branch.id, pickupEnabled: next },
      });
      return;
    }

    if (channel === "delivery") {
      if (next && !branch.deliveryEnabled && !offerDelivery) {
        setOrgDeliveryGuardOpen(true);
        return;
      }
      branchToggleMutation.mutate({
        branchId: branch.id,
        request: { deliveryEnabled: next },
        optimistic: { branchId: branch.id, deliveryEnabled: next },
      });
      return;
    }

    // Online Orders: enable+resume when turning ON; pause when turning OFF (keep enabled).
    if (next) {
      const needsEnable = !branch.customerOrderingEnabled;
      const needsResume = branch.onlineOrdersPaused;
      branchToggleMutation.mutate({
        branchId: branch.id,
        request: needsEnable ? { customerOrderingEnabled: true } : undefined,
        pause: needsResume ? false : undefined,
        optimistic: {
          branchId: branch.id,
          customerOrderingEnabled: true,
          onlineOrdersPaused: false,
        },
      });
      return;
    }

    if (branch.customerOrderingEnabled && !branch.onlineOrdersPaused) {
      branchToggleMutation.mutate({
        branchId: branch.id,
        pause: true,
        optimistic: {
          branchId: branch.id,
          customerOrderingEnabled: true,
          onlineOrdersPaused: true,
        },
      });
      return;
    }

    branchToggleMutation.mutate({
      branchId: branch.id,
      request: { customerOrderingEnabled: false },
      optimistic: {
        branchId: branch.id,
        customerOrderingEnabled: false,
        onlineOrdersPaused: false,
      },
    });
  }

  return (
    <div className="cc-fulfillment" data-testid="connected-commerce-fulfillment">
      {toggleError ? (
        <Notice tone="danger" testId="connected-commerce-fulfillment-error">
          {toggleError}
        </Notice>
      ) : null}

      <section
        className="cc-fulfillment__section"
        data-testid="connected-commerce-org-fulfillment"
      >
        <header className="cc-fulfillment__header">
          <h2 className="cc-fulfillment__title">{t("connectedCommerce.orgFulfillmentTitle")}</h2>
          <p className="cc-fulfillment__lede">{t("connectedCommerce.orgFulfillmentHelp")}</p>
        </header>

        <div className="cc-fulfillment__switch-grid cc-fulfillment__switch-grid--single">
          <FulfillmentSwitchCard
            title={t("connectedCommerce.offerDelivery")}
            hint={t("connectedCommerce.offerDeliveryHelp")}
            Icon={Truck}
            checked={offerDelivery}
            disabled={!canEditOrg || offerMutation.isPending}
            pending={offerMutation.isPending}
            statusLabel={
              offerDelivery
                ? t("connectedCommerce.offerDeliveryOn")
                : t("connectedCommerce.deliveryPausedGlobally")
            }
            statusTone={offerDelivery ? "success" : "warning"}
            statusTestId={
              offerDelivery
                ? "connected-commerce-offer-delivery-status"
                : "connected-commerce-delivery-paused-chip"
            }
            testId="connected-commerce-offer-delivery"
            onCheckedChange={(next) => {
              setToggleError(null);
              offerMutation.mutate(next);
            }}
          />
        </div>

        <p className="cc-fulfillment__footnote" data-testid="connected-commerce-no-org-online-master">
          {t("connectedCommerce.fulfillmentFootnote")}
        </p>
      </section>

      <section
        className="cc-fulfillment__section"
        data-testid="connected-commerce-branch-defaults"
      >
        <header className="cc-fulfillment__header">
          <h2 className="cc-fulfillment__title">{t("connectedCommerce.branchDefaultsTitle")}</h2>
          <p className="cc-fulfillment__lede">{t("connectedCommerce.branchDefaultsHelp")}</p>
        </header>

        <div className="cc-fulfillment__switch-grid">
          <FulfillmentSwitchCard
            title={t("connectedCommerce.defaultPickup")}
            hint={t("connectedCommerce.defaultPickupHint")}
            Icon={Package}
            checked={defaultPickup}
            disabled={!canEditOrg || defaultsMutation.isPending}
            pending={defaultsMutation.isPending}
            statusLabel={
              defaultPickup ? t("connectedCommerce.chip.on") : t("connectedCommerce.chip.off")
            }
            statusTone={defaultPickup ? "success" : "neutral"}
            testId="connected-commerce-default-pickup"
            onCheckedChange={(next) => patchDefaults({ defaultPickupEnabled: next })}
          />
          <FulfillmentSwitchCard
            title={t("connectedCommerce.defaultDelivery")}
            hint={t("connectedCommerce.defaultDeliveryHint")}
            Icon={Truck}
            checked={defaultDelivery}
            disabled={!canEditOrg || defaultsMutation.isPending}
            pending={defaultsMutation.isPending}
            statusLabel={
              defaultDelivery ? t("connectedCommerce.chip.on") : t("connectedCommerce.chip.off")
            }
            statusTone={defaultDelivery ? "success" : "neutral"}
            testId="connected-commerce-default-delivery"
            onCheckedChange={(next) => patchDefaults({ defaultDeliveryEnabled: next })}
          />
          <FulfillmentSwitchCard
            title={t("connectedCommerce.defaultOnlineOrders")}
            hint={t("connectedCommerce.defaultOnlineOrdersHint")}
            Icon={Store}
            checked={defaultOnline}
            disabled={!canEditOrg || defaultsMutation.isPending}
            pending={defaultsMutation.isPending}
            statusLabel={
              defaultOnline ? t("connectedCommerce.chip.on") : t("connectedCommerce.chip.off")
            }
            statusTone={defaultOnline ? "success" : "neutral"}
            testId="connected-commerce-default-online"
            onCheckedChange={(next) => patchDefaults({ defaultOnlineOrdersEnabled: next })}
          />
        </div>
      </section>

      <section
        className="cc-fulfillment__section"
        data-testid="connected-commerce-branch-table"
      >
        <header className="cc-fulfillment__header">
          <h2 className="cc-fulfillment__title">{t("connectedCommerce.branchTableTitle")}</h2>
          <p className="cc-fulfillment__lede">{t("connectedCommerce.branchTableHelp")}</p>
        </header>

        {branchesQuery.isError ? (
          <ErrorState
            title={t("branches.loadError")}
            detail={errorMessage(branchesQuery.error, t("connectedCommerce.branchTableHelp"))}
          />
        ) : branches.length === 0 ? (
          <EmptyState
            align="center"
            icon={<Store className="size-5" strokeWidth={1.75} />}
            title={t("connectedCommerce.noBranches")}
            detail={t("connectedCommerce.branchTableHelp")}
          />
        ) : (
          <ul className="cc-fulfillment__rows" data-testid="connected-commerce-branch-rows">
            {branches.map((branch) => {
              const pending =
                branchToggleMutation.isPending && optimistic?.branchId === branch.id;
              const row = resolveConnectedCommerceBranchRowStatus({
                pickupEnabled: branch.pickupEnabled,
                pickupReady: branch.pickupReady,
                pickupSectionsComplete: branch.pickupSectionsComplete,
                pickupSectionsTotal: branch.pickupSectionsTotal,
                deliveryEnabled: branch.deliveryEnabled,
                deliveryReady: branch.deliveryReady,
                deliverySectionsComplete: branch.deliverySectionsComplete,
                deliverySectionsTotal: branch.deliverySectionsTotal,
                customerOrderingEnabled: branch.customerOrderingEnabled,
                customerOrderingReady: branch.customerOrderingReady,
                onlineOrdersPaused: branch.onlineOrdersPaused,
                orgOfferDelivery: offerDelivery,
              });
              const onlineOn =
                branch.customerOrderingEnabled && !branch.onlineOrdersPaused;
              const actionLabel =
                row.actionKind === "completeSetup"
                  ? t("connectedCommerce.completeSetup")
                  : t("connectedCommerce.configure");

              return (
                <li
                  key={branch.id}
                  className="cc-fulfillment__row"
                  data-testid={`connected-commerce-branch-row-${branch.id}`}
                >
                  <div className="cc-fulfillment__row-heading">
                    <div className="cc-fulfillment__row-identity">
                      <p className="cc-fulfillment__branch-name">{branch.name}</p>
                      <StatusChip
                        tone={row.overall.tone}
                        appearance="outline"
                        shape="soft"
                        data-testid={`cc-branch-status-${branch.id}`}
                      >
                        {t(row.overall.statusKey)}
                      </StatusChip>
                    </div>
                    <Button asChild appearance="outline" size="default">
                      <Link
                        to={branchFulfillmentEditPath(branch.id)}
                        data-testid={`cc-configure-${branch.id}`}
                      >
                        {actionLabel}
                      </Link>
                    </Button>
                  </div>

                  <div className="cc-fulfillment__switch-grid">
                    <FulfillmentSwitchCard
                      title={t("connectedCommerce.pickup")}
                      Icon={Package}
                      checked={branch.pickupEnabled}
                      disabled={!canEditBranches || pending}
                      pending={pending}
                      statusLabel={t(row.pickup.statusKey)}
                      statusTone={row.pickup.tone}
                      testId={`cc-pickup-switch-${branch.id}`}
                      onCheckedChange={(next) =>
                        toggleBranchChannel(branch, "pickup", next)
                      }
                    />
                    <FulfillmentSwitchCard
                      title={t("connectedCommerce.delivery")}
                      hint={
                        !offerDelivery && !branch.deliveryEnabled
                          ? t("connectedCommerce.enableOfferDeliveryFirst")
                          : null
                      }
                      Icon={Truck}
                      checked={branch.deliveryEnabled}
                      disabled={!canEditBranches || pending}
                      pending={pending}
                      statusLabel={t(row.delivery.statusKey)}
                      statusTone={row.delivery.tone}
                      statusTestId={`cc-branch-delivery-${branch.id}`}
                      testId={`cc-delivery-switch-${branch.id}`}
                      onCheckedChange={(next) =>
                        toggleBranchChannel(branch, "delivery", next)
                      }
                    />
                    <FulfillmentSwitchCard
                      title={t("connectedCommerce.onlineOrders")}
                      hint={
                        branch.customerOrderingEnabled && branch.onlineOrdersPaused
                          ? t("connectedCommerce.branch.onlinePausedHint")
                          : null
                      }
                      Icon={Store}
                      checked={onlineOn}
                      disabled={!canEditBranches || pending}
                      pending={pending}
                      statusLabel={t(row.onlineOrders.statusKey)}
                      statusTone={row.onlineOrders.tone}
                      testId={`cc-online-switch-${branch.id}`}
                      onCheckedChange={(next) =>
                        toggleBranchChannel(branch, "online", next)
                      }
                    />
                  </div>
                </li>
              );
            })}
          </ul>
        )}
      </section>

      <BranchDeliveryOrgOfferGuardDialog
        open={orgDeliveryGuardOpen}
        onCancel={() => setOrgDeliveryGuardOpen(false)}
        t={t}
      />
    </div>
  );
}
