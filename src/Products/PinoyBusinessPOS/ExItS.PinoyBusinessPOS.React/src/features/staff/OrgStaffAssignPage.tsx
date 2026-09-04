import { useEffect, useMemo, useState } from "react";
import { useNavigate, useSearchParams } from "react-router-dom";
import { useMutation, useQuery } from "@tanstack/react-query";
import { cn } from "@/lib/cn";
import {
  listMembershipBranchAssignments,
  setMembershipBranchAssignments,
  type BranchAccessScopeDto,
} from "@/api/platform/membership-branch-assignments-client";
import { listOrganizationMembers } from "@/api/platform/organization-members-client";
import {
  listOrganizationBranches,
  resolvePlatformBranchId,
} from "@/api/platform/platform-auth-client";
import {
  assignProductLocalRole,
  changeProductLocalRole,
  friendlyPosRoleLabel,
  listProductLocalRoles,
  POS_LOCAL_ROLE_OWNER,
} from "@/api/platform/product-local-roles-client";
import { Button } from "@/components/ui/button";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingSkeleton } from "@/components/exits/FoundationStates";
import { PageHeader } from "@/components/exits/PageHeader";
import { ConfirmationDialog } from "@/components/exits/SheetDialog";
import { useProductLocalRoleCatalog } from "@/features/staff/useProductLocalRoleCatalog";
import { listOrganizationAreas } from "@/api/platform/organization-areas-client";
import {
  activeBranchIds,
  assignmentAreaIds,
  assignmentBranchIds,
  branchIdsEqual,
  isImplicitAllBranchesMembershipRole,
  listActiveBranches,
  modeToScope,
  resolvePrimaryOrOnlyBranch,
  scopeToMode,
  shouldOfferAreaScope,
  type BranchScopeMode,
} from "@/features/staff/staff-branch-access";
import { useI18n } from "@/i18n/I18nProvider";
import { pageBackNav } from "@/navigation/page-back-nav";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

export function OrgStaffAssignPage() {
  const { t } = useI18n();
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const { boundWorkspace } = useWorkspace();
  const organizationId = boundWorkspace?.organizationId ?? null;
  const userId = searchParams.get("userId")?.trim() || null;

  const [selectedRole, setSelectedRole] = useState<string | null>(null);
  const [branchScope, setBranchScope] = useState<BranchScopeMode>("specific");
  const [selectedBranchIds, setSelectedBranchIds] = useState<string[]>([]);
  const [selectedAreaIds, setSelectedAreaIds] = useState<string[]>([]);
  const [branchStateReady, setBranchStateReady] = useState(false);
  const [ownerConfirmOpen, setOwnerConfirmOpen] = useState(false);
  const [changeConfirmOpen, setChangeConfirmOpen] = useState(false);
  const [submitError, setSubmitError] = useState<string | null>(null);

  const catalogQuery = useProductLocalRoleCatalog(organizationId);

  const userQuery = useQuery({
    queryKey: ["org-staff-assign-user", organizationId, userId],
    enabled: Boolean(organizationId && userId),
    queryFn: async () => {
      if (!organizationId || !userId) {
        throw new Error(t("staffAssign.validation"));
      }
      const [membersResult, grantsResult] = await Promise.all([
        listOrganizationMembers(organizationId, undefined),
        listProductLocalRoles(organizationId, "Active"),
      ]);
      if (!membersResult.ok) {
        throw new Error(membersResult.body?.detail ?? t("staffManage.loadError"));
      }
      if (!grantsResult.ok) {
        throw new Error(grantsResult.body?.detail ?? t("staffManage.loadError"));
      }
      const member = membersResult.members.find((item) => item.userId === userId) ?? null;
      if (!member) {
        throw new Error(t("staffAssign.validation"));
      }
      const existingGrant =
        grantsResult.grants.find((grant) => grant.userIdentityId === userId) ?? null;
      const displayName =
        member.displayName?.trim() ||
        member.username?.trim() ||
        existingGrant?.userDisplayName?.trim() ||
        member.email?.trim() ||
        t("staffAssign.unknownName");

      const implicitAll = isImplicitAllBranchesMembershipRole(member.role);
      let assignedIds: string[] = [];
      let assignedAreaIds: string[] = [];
      let branchAccessScope: BranchAccessScopeDto = "Explicit";
      if (!implicitAll) {
        const assignmentsResult = await listMembershipBranchAssignments(
          organizationId,
          member.id,
        );
        if (!assignmentsResult.ok) {
          throw new Error(assignmentsResult.body?.detail ?? t("staffManage.loadError"));
        }
        branchAccessScope = assignmentsResult.value.scope;
        assignedIds = assignmentBranchIds(assignmentsResult.value.branches);
        assignedAreaIds = assignmentAreaIds(assignmentsResult.value.areas);
      }

      return {
        membershipId: member.id,
        membershipRole: member.role,
        displayName,
        email: member.email?.trim() || null,
        membershipStatus: member.status,
        existingRoleCode: existingGrant?.roleCode ?? null,
        existingRoleLabel: existingGrant
          ? friendlyPosRoleLabel(
              existingGrant.mappedPosRoleCode,
              existingGrant.roleCode,
              existingGrant.roleDisplay,
            )
          : null,
        implicitAllBranches: implicitAll,
        branchAccessScope,
        assignedBranchIds: assignedIds,
        assignedAreaIds,
      };
    },
  });

  const areasQuery = useQuery({
    queryKey: ["organization-areas", organizationId],
    enabled: Boolean(organizationId),
    queryFn: async ({ signal }) => {
      if (!organizationId) {
        throw new Error(t("staffInvite.noWorkspace"));
      }
      const result = await listOrganizationAreas(organizationId, signal);
      if (!result.ok) {
        throw new Error(result.body?.detail ?? t("areas.loadError"));
      }
      return result.value.areas.filter((area) => area.status === "Active");
    },
  });

  const branchesQuery = useQuery({
    queryKey: ["org-staff-assign-branches", organizationId],
    enabled: Boolean(organizationId),
    queryFn: async () => {
      if (!organizationId) {
        throw new Error(t("staffInvite.noWorkspace"));
      }
      const result = await listOrganizationBranches(organizationId);
      if (!result.ok) {
        throw new Error(result.body?.detail ?? t("staffManage.loadError"));
      }
      return listActiveBranches(result.branches);
    },
  });

  const activeBranches = branchesQuery.data ?? [];
  const allActiveIds = useMemo(() => activeBranchIds(activeBranches), [activeBranches]);
  const singleBranch = activeBranches.length === 1 ? resolvePrimaryOrOnlyBranch(activeBranches) : null;
  const singleBranchId = singleBranch ? resolvePlatformBranchId(singleBranch) : null;
  const activeAreas = areasQuery.data ?? [];
  const areasAvailable = shouldOfferAreaScope({
    activeBranchCount: activeBranches.length,
    activeAreaCount: activeAreas.length,
  });

  useEffect(() => {
    setBranchStateReady(false);
  }, [organizationId, userId]);

  useEffect(() => {
    if (!userQuery.isSuccess || !branchesQuery.isSuccess || branchStateReady) {
      return;
    }
    if (userQuery.data.implicitAllBranches) {
      setBranchScope("all");
      setSelectedBranchIds([]);
      setBranchStateReady(true);
      return;
    }
    if (activeBranches.length <= 1) {
      setBranchScope(scopeToMode(userQuery.data.branchAccessScope));
      setSelectedBranchIds(
        userQuery.data.branchAccessScope === "AllActive"
          ? singleBranchId
            ? [singleBranchId]
            : []
          : userQuery.data.assignedBranchIds.length > 0
            ? [...userQuery.data.assignedBranchIds]
            : singleBranchId
              ? [singleBranchId]
              : [],
      );
      setBranchStateReady(true);
      return;
    }
    const stored = scopeToMode(userQuery.data.branchAccessScope);
    const mode = stored === "areas" && !areasAvailable ? "specific" : stored;
    setBranchScope(mode);
    setSelectedAreaIds([...userQuery.data.assignedAreaIds]);
    setSelectedBranchIds(
      mode === "all"
        ? [...allActiveIds]
        : userQuery.data.assignedBranchIds.length > 0
          ? [...userQuery.data.assignedBranchIds]
          : singleBranchId
            ? [singleBranchId]
            : allActiveIds.slice(0, 1),
    );
    setBranchStateReady(true);
  }, [
    userQuery.isSuccess,
    userQuery.data,
    branchesQuery.isSuccess,
    activeBranches.length,
    allActiveIds,
    singleBranchId,
    areasAvailable,
    branchStateReady,
  ]);

  const resolvedScope: BranchAccessScopeDto | null = useMemo(() => {
    if (!userQuery.data || userQuery.data.implicitAllBranches) {
      return null;
    }
    if (activeBranches.length <= 1) {
      return userQuery.data.branchAccessScope;
    }
    return modeToScope(branchScope);
  }, [userQuery.data, activeBranches.length, branchScope]);

  const resolvedBranchIds = useMemo(() => {
    if (
      !userQuery.data ||
      userQuery.data.implicitAllBranches ||
      resolvedScope === "AllActive" ||
      resolvedScope === "Areas"
    ) {
      return [] as string[];
    }
    if (activeBranches.length <= 1) {
      return singleBranchId ? [singleBranchId] : [];
    }
    return selectedBranchIds.filter((id) => allActiveIds.includes(id));
  }, [
    userQuery.data,
    resolvedScope,
    activeBranches.length,
    singleBranchId,
    allActiveIds,
    selectedBranchIds,
  ]);

  const resolvedAreaIds = useMemo(() => {
    if (resolvedScope !== "Areas") {
      return [] as string[];
    }
    const allowed = activeAreas.map((area) => area.id);
    return selectedAreaIds.filter((id) => allowed.includes(id));
  }, [resolvedScope, activeAreas, selectedAreaIds]);

  const branchesDirty = useMemo(() => {
    if (!userQuery.data || userQuery.data.implicitAllBranches || resolvedScope === null) {
      return false;
    }
    if (resolvedScope !== userQuery.data.branchAccessScope) {
      return true;
    }
    if (resolvedScope === "AllActive") {
      return false;
    }
    if (resolvedScope === "Areas") {
      return !branchIdsEqual(resolvedAreaIds, userQuery.data.assignedAreaIds);
    }
    return !branchIdsEqual(resolvedBranchIds, userQuery.data.assignedBranchIds);
  }, [userQuery.data, resolvedScope, resolvedBranchIds, resolvedAreaIds]);

  const assignMutation = useMutation({
    mutationFn: async (roleCode: string | null) => {
      if (!organizationId || !userId || !userQuery.data) {
        throw new Error(t("staffAssign.validation"));
      }

      const needsRoleWrite =
        Boolean(roleCode) && roleCode !== userQuery.data.existingRoleCode;
      if (needsRoleWrite && roleCode) {
        const reason = userQuery.data.existingRoleCode
          ? "Changed from POS client"
          : "Assigned from POS client";
        const result = userQuery.data.existingRoleCode
          ? await changeProductLocalRole({
              organizationId,
              userIdentityId: userId,
              roleCode,
              reason,
            })
          : await assignProductLocalRole({
              organizationId,
              userIdentityId: userId,
              roleCode,
              reason,
            });
        if (!result.ok) {
          throw new Error(result.body?.detail ?? t("staffAssign.error"));
        }
      }

      if (!userQuery.data.implicitAllBranches && branchesDirty && resolvedScope) {
        if (resolvedScope === "Explicit" && resolvedBranchIds.length === 0) {
          throw new Error(t("staffAssign.branchRequired"));
        }
        if (resolvedScope === "Areas" && resolvedAreaIds.length === 0) {
          throw new Error(t("staffAssign.areaRequired"));
        }
        const branchResult = await setMembershipBranchAssignments(
          organizationId,
          userQuery.data.membershipId,
          resolvedScope === "Areas"
            ? { scope: resolvedScope, branchIds: [], areaIds: resolvedAreaIds }
            : {
                scope: resolvedScope,
                branchIds: resolvedScope === "Explicit" ? resolvedBranchIds : [],
              },
        );
        if (!branchResult.ok) {
          throw new Error(branchResult.body?.detail ?? t("staffAssign.branchSaveError"));
        }
      }
    },
    onSuccess: () => {
      navigate("/org/staff", { replace: true });
    },
    onError: (error: Error) => {
      setSubmitError(error.message);
      setOwnerConfirmOpen(false);
      setChangeConfirmOpen(false);
    },
  });

  const isChanging = Boolean(userQuery.data?.existingRoleCode);
  const sameRoleSelected =
    Boolean(selectedRole && userQuery.data?.existingRoleCode) &&
    selectedRole === userQuery.data?.existingRoleCode;
  const roleSelectionMissing = !selectedRole && !userQuery.data?.existingRoleCode;
  const canSubmit =
    Boolean(
      organizationId &&
        userId &&
        !assignMutation.isPending &&
        userQuery.data?.membershipStatus === "Active" &&
        branchStateReady,
    ) &&
    !roleSelectionMissing &&
    (!sameRoleSelected || branchesDirty) &&
    (userQuery.data?.implicitAllBranches ||
      resolvedScope === "AllActive" ||
      (resolvedScope === "Areas" ? resolvedAreaIds.length > 0 : resolvedBranchIds.length > 0));

  const replaceHint = useMemo(() => {
    if (!userQuery.data?.existingRoleLabel) {
      return null;
    }
    return t("staffAssign.replaceHint").replace("{role}", userQuery.data.existingRoleLabel);
  }, [t, userQuery.data?.existingRoleLabel]);

  const selectedRoleLabel = useMemo(() => {
    const code = selectedRole ?? userQuery.data?.existingRoleCode;
    if (!code) {
      return "";
    }
    const fromCatalog = catalogQuery.data?.find((role) => role.code === code);
    return fromCatalog?.displayName ?? friendlyPosRoleLabel(null, code);
  }, [catalogQuery.data, selectedRole, userQuery.data?.existingRoleCode]);

  function requestAssign() {
    if (!canSubmit) {
      return;
    }
    const roleToApply = selectedRole ?? userQuery.data?.existingRoleCode ?? null;
    setSubmitError(null);
    if (roleToApply === POS_LOCAL_ROLE_OWNER && roleToApply !== userQuery.data?.existingRoleCode) {
      setOwnerConfirmOpen(true);
      return;
    }
    if (
      isChanging &&
      roleToApply &&
      roleToApply !== userQuery.data?.existingRoleCode
    ) {
      setChangeConfirmOpen(true);
      return;
    }
    assignMutation.mutate(roleToApply);
  }

  function toggleBranch(branchId: string) {
    setSelectedBranchIds((current) => {
      if (current.includes(branchId)) {
        return current.filter((id) => id !== branchId);
      }
      return [...current, branchId];
    });
    setSubmitError(null);
  }

  function toggleArea(areaId: string) {
    setSelectedAreaIds((current) => {
      if (current.includes(areaId)) {
        return current.filter((id) => id !== areaId);
      }
      return [...current, areaId];
    });
    setSubmitError(null);
  }

  const pageError = userQuery.isError || catalogQuery.isError || branchesQuery.isError;
  const pageLoading =
    Boolean(userId) &&
    !pageError &&
    (userQuery.isLoading ||
      catalogQuery.isLoading ||
      branchesQuery.isLoading ||
      (userQuery.isSuccess && branchesQuery.isSuccess && !branchStateReady));

  return (
    <div
      className="staff-assign-page exits-page flex min-w-0 flex-col gap-3"
      data-testid="org-staff-assign-page"
    >
      <PageHeader
        title={isChanging ? t("staffAssign.manageTitle") : t("staffAssign.title")}
        description={isChanging ? t("staffAssign.manageLede") : t("staffAssign.lede")}
        backTo={pageBackNav.orgStaff.to}
        backLabel={t(pageBackNav.orgStaff.labelKey)}
        backTestId="page-header-back-staff"
      />

      {!userId ? (
        <ErrorState title={t("error.title")} detail={t("staffAssign.validation")} />
      ) : null}

      {pageLoading && userId ? <LoadingSkeleton count={3} label={t("loading.label")} /> : null}

      {pageError ? (
        <ErrorState
          title={t("error.title")}
          detail={
            userQuery.error instanceof Error
              ? userQuery.error.message
              : catalogQuery.error instanceof Error
                ? catalogQuery.error.message
                : branchesQuery.error instanceof Error
                  ? branchesQuery.error.message
                  : t("staffManage.loadError")
          }
        />
      ) : null}

      {userQuery.isSuccess && !pageLoading ? (
        <section
          className="catalog-form-section exits-animate-panel gap-3"
          data-testid="org-staff-assign-user"
        >
          <h2 className="catalog-form-section__title">{t("staffAssign.userSection")}</h2>
          <div className="staff-assign-user">
            <span className="staff-row__avatar" aria-hidden>
              {userQuery.data.displayName.trim().slice(0, 1).toUpperCase() || "?"}
            </span>
            <div className="min-w-0 flex flex-col gap-0.5">
              <span className="font-semibold">{userQuery.data.displayName}</span>
              {userQuery.data.email ? (
                <span className="text-[length:var(--exits-text-sm)] text-muted">
                  {userQuery.data.email}
                </span>
              ) : null}
            </div>
          </div>
          {replaceHint ? (
            <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">{replaceHint}</p>
          ) : null}
          {userQuery.data.membershipStatus !== "Active" ? (
            <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
              {t("staffManage.suspendedRoleHint")}
            </p>
          ) : null}
        </section>
      ) : null}

      {catalogQuery.isSuccess && !pageLoading ? (
        <section
          className="catalog-form-section exits-animate-panel gap-3"
          aria-labelledby="assign-role-heading"
        >
          <h2 id="assign-role-heading" className="catalog-form-section__title">
            {t("staffAssign.roleSection")}
          </h2>
          <div
            className="staff-assign-roles"
            role="radiogroup"
            aria-labelledby="assign-role-heading"
          >
            {catalogQuery.data.map((option) => {
              const selected = (selectedRole ?? userQuery.data?.existingRoleCode) === option.code;
              const isCurrent = userQuery.data?.existingRoleCode === option.code;
              return (
                <button
                  key={option.code}
                  type="button"
                  role="radio"
                  aria-checked={selected}
                  disabled={
                    !userId ||
                    assignMutation.isPending ||
                    userQuery.data?.membershipStatus !== "Active"
                  }
                  data-testid={`org-staff-role-${option.code.toLowerCase()}`}
                  className={cn(
                    "staff-assign-role",
                    selected && "staff-assign-role--selected",
                    isCurrent && "staff-assign-role--current",
                  )}
                  onClick={() => {
                    setSelectedRole(option.code);
                    setSubmitError(null);
                    setOwnerConfirmOpen(false);
                    setChangeConfirmOpen(false);
                  }}
                >
                  <span className="staff-assign-role__check" aria-hidden>
                    {selected ? "✓" : ""}
                  </span>
                  <span className="staff-assign-role__copy">
                    <span className="staff-assign-role__label">
                      {option.displayName}
                      {isCurrent ? ` (${t("staffAssign.currentRole")})` : ""}
                    </span>
                    <span className="staff-assign-role__desc">{option.description}</span>
                  </span>
                </button>
              );
            })}
          </div>
        </section>
      ) : null}

      {userQuery.isSuccess && branchesQuery.isSuccess && !pageLoading ? (
        <section
          className="catalog-form-section exits-animate-panel gap-3"
          aria-labelledby="assign-branch-heading"
          data-testid="org-staff-assign-branches"
        >
          <h2 id="assign-branch-heading" className="catalog-form-section__title">
            {t("staffAssign.branchSection")}
          </h2>

          {userQuery.data.implicitAllBranches ? (
            <p
              className="m-0 text-[length:var(--exits-text-sm)] text-muted"
              data-testid="org-staff-assign-branch-automatic"
            >
              {t("staffAssign.branchAutomaticAll")}
            </p>
          ) : activeBranches.length === 0 ? (
            <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
              {t("staffAssign.branchNoneActive")}
            </p>
          ) : activeBranches.length === 1 ? (
            <p
              className="staff-assign-branch-note m-0"
              data-testid="org-staff-assign-branch-single"
            >
              {t("staffAssign.singleBranchAutomatic").replace(
                "{branch}",
                singleBranch?.name?.trim() || singleBranch?.code || t("staffAssign.mainBranch"),
              )}
            </p>
          ) : (
            <>
              <div
                className="staff-assign-branch-scopes"
                role="radiogroup"
                aria-labelledby="assign-branch-heading"
              >
                <button
                  type="button"
                  role="radio"
                  aria-checked={branchScope === "all"}
                  disabled={assignMutation.isPending || userQuery.data.membershipStatus !== "Active"}
                  className={cn(
                    "staff-assign-role",
                    branchScope === "all" && "staff-assign-role--selected",
                  )}
                  data-testid="org-staff-branch-scope-all"
                  onClick={() => {
                    setBranchScope("all");
                    setSelectedBranchIds([...allActiveIds]);
                    setSubmitError(null);
                  }}
                >
                  <span className="staff-assign-role__check" aria-hidden>
                    {branchScope === "all" ? "✓" : ""}
                  </span>
                  <span className="staff-assign-role__copy">
                    <span className="staff-assign-role__label">{t("staffAssign.allBranches")}</span>
                    <span className="staff-assign-role__desc">
                      {t("staffAssign.allBranchesHint")}
                    </span>
                  </span>
                </button>
                {areasAvailable ? (
                  <button
                    type="button"
                    role="radio"
                    aria-checked={branchScope === "areas"}
                    disabled={
                      assignMutation.isPending || userQuery.data.membershipStatus !== "Active"
                    }
                    className={cn(
                      "staff-assign-role",
                      branchScope === "areas" && "staff-assign-role--selected",
                    )}
                    data-testid="org-staff-branch-scope-areas"
                    onClick={() => {
                      setBranchScope("areas");
                      if (selectedAreaIds.length === 0 && userQuery.data.assignedAreaIds.length > 0) {
                        setSelectedAreaIds([...userQuery.data.assignedAreaIds]);
                      }
                      setSubmitError(null);
                    }}
                  >
                    <span className="staff-assign-role__check" aria-hidden>
                      {branchScope === "areas" ? "✓" : ""}
                    </span>
                    <span className="staff-assign-role__copy">
                      <span className="staff-assign-role__label">{t("staffAssign.areaScope")}</span>
                      <span className="staff-assign-role__desc">
                        {t("staffAssign.areaScopeHint")}
                      </span>
                    </span>
                  </button>
                ) : null}
                <button
                  type="button"
                  role="radio"
                  aria-checked={branchScope === "specific"}
                  disabled={assignMutation.isPending || userQuery.data.membershipStatus !== "Active"}
                  className={cn(
                    "staff-assign-role",
                    branchScope === "specific" && "staff-assign-role--selected",
                  )}
                  data-testid="org-staff-branch-scope-specific"
                  onClick={() => {
                    setBranchScope("specific");
                    if (selectedBranchIds.length === 0) {
                      setSelectedBranchIds(
                        userQuery.data.assignedBranchIds.length > 0
                          ? [...userQuery.data.assignedBranchIds]
                          : allActiveIds.slice(0, 1),
                      );
                    }
                    setSubmitError(null);
                  }}
                >
                  <span className="staff-assign-role__check" aria-hidden>
                    {branchScope === "specific" ? "✓" : ""}
                  </span>
                  <span className="staff-assign-role__copy">
                    <span className="staff-assign-role__label">
                      {t("staffAssign.specificBranches")}
                    </span>
                    <span className="staff-assign-role__desc">
                      {t("staffAssign.specificBranchesHint")}
                    </span>
                  </span>
                </button>
              </div>

              {branchScope === "areas" ? (
                <ul
                  className="staff-assign-branch-list m-0 grid list-none gap-2 p-0"
                  data-testid="org-staff-area-checklist"
                >
                  {activeAreas.map((area) => {
                    const checked = selectedAreaIds.includes(area.id);
                    return (
                      <li key={area.id}>
                        <label
                          className={cn(
                            "staff-assign-branch-option",
                            checked && "staff-assign-branch-option--selected",
                          )}
                        >
                          <input
                            type="checkbox"
                            className="staff-assign-branch-option__input"
                            checked={checked}
                            disabled={
                              assignMutation.isPending ||
                              userQuery.data.membershipStatus !== "Active"
                            }
                            data-testid={`org-staff-area-${area.id}`}
                            onChange={() => toggleArea(area.id)}
                          />
                          <span className="staff-assign-branch-option__copy">
                            <span className="staff-assign-branch-option__name">{area.name}</span>
                            <span className="staff-assign-branch-option__code">
                              {t("areas.branchCount").replace("{count}", String(area.branchCount))}
                            </span>
                          </span>
                        </label>
                      </li>
                    );
                  })}
                </ul>
              ) : null}

              {branchScope === "specific" ? (
                <ul
                  className="staff-assign-branch-list m-0 grid list-none gap-2 p-0"
                  data-testid="org-staff-branch-checklist"
                >
                  {activeBranches.map((branch) => {
                    const id = resolvePlatformBranchId(branch);
                    if (!id) {
                      return null;
                    }
                    const checked = selectedBranchIds.includes(id);
                    return (
                      <li key={id}>
                        <label
                          className={cn(
                            "staff-assign-branch-option",
                            checked && "staff-assign-branch-option--selected",
                          )}
                        >
                          <input
                            type="checkbox"
                            className="staff-assign-branch-option__input"
                            checked={checked}
                            disabled={
                              assignMutation.isPending ||
                              userQuery.data.membershipStatus !== "Active"
                            }
                            data-testid={`org-staff-branch-${id}`}
                            onChange={() => toggleBranch(id)}
                          />
                          <span className="staff-assign-branch-option__copy">
                            <span className="staff-assign-branch-option__name">
                              {branch.name}
                              {branch.isPrimary ? ` (${t("staffAssign.mainBranch")})` : ""}
                            </span>
                            {branch.code ? (
                              <span className="staff-assign-branch-option__code">{branch.code}</span>
                            ) : null}
                          </span>
                        </label>
                      </li>
                    );
                  })}
                </ul>
              ) : null}
            </>
          )}
        </section>
      ) : null}

      {submitError ? (
        <div className="exits-alert exits-alert--error" role="alert">
          <p className="m-0 text-[length:var(--exits-text-sm)]">{submitError}</p>
        </div>
      ) : null}

      <div className="exits-animate-toolbar">
        <Button
          type="button"
          className="w-full sm:w-auto"
          disabled={!canSubmit}
          data-testid="org-staff-assign-submit"
          onClick={requestAssign}
        >
          {assignMutation.isPending
            ? t("staffAssign.submitting")
            : isChanging
              ? t("staffAssign.manageConfirmAction")
              : t("staffAssign.submit")}
        </Button>
      </div>

      <ConfirmationDialog
        open={ownerConfirmOpen}
        title={t("staffAssign.ownerConfirmTitle")}
        detail={t("staffAssign.ownerConfirmMessage")}
        confirmLabel={t("staffAssign.ownerConfirmAction")}
        cancelLabel={t("staffManage.cancel")}
        onCancel={() => setOwnerConfirmOpen(false)}
        onConfirm={() => {
          assignMutation.mutate(POS_LOCAL_ROLE_OWNER);
        }}
        testId="org-staff-owner-confirm"
      />

      <ConfirmationDialog
        open={changeConfirmOpen}
        title={t("staffAssign.changeConfirmTitle")}
        detail={t("staffAssign.changeConfirmDetail")
          .replace("{name}", userQuery.data?.displayName ?? "")
          .replace("{from}", userQuery.data?.existingRoleLabel ?? "")
          .replace("{to}", selectedRoleLabel)}
        confirmLabel={t("staffAssign.changeConfirmAction")}
        cancelLabel={t("staffManage.cancel")}
        onCancel={() => setChangeConfirmOpen(false)}
        onConfirm={() => {
          const roleToApply = selectedRole ?? userQuery.data?.existingRoleCode ?? null;
          if (roleToApply) {
            assignMutation.mutate(roleToApply);
          }
        }}
        testId="org-staff-change-confirm"
      />
    </div>
  );
}
