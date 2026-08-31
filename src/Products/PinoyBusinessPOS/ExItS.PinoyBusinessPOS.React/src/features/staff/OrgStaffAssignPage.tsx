import { useMemo, useState } from "react";
import { useNavigate, useSearchParams } from "react-router-dom";
import { useMutation, useQuery } from "@tanstack/react-query";
import { cn } from "@/lib/cn";
import { listOrganizationMembers } from "@/api/platform/organization-members-client";
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
      const existingGrant =
        grantsResult.grants.find((grant) => grant.userIdentityId === userId) ?? null;
      const displayName =
        member?.displayName?.trim() ||
        member?.username?.trim() ||
        existingGrant?.userDisplayName?.trim() ||
        member?.email?.trim() ||
        t("staffAssign.unknownName");
      return {
        displayName,
        email: member?.email?.trim() || null,
        membershipStatus: member?.status ?? "Unknown",
        existingRoleCode: existingGrant?.roleCode ?? null,
        existingRoleLabel: existingGrant
          ? friendlyPosRoleLabel(
              existingGrant.mappedPosRoleCode,
              existingGrant.roleCode,
              existingGrant.roleDisplay,
            )
          : null,
      };
    },
  });

  const assignMutation = useMutation({
    mutationFn: async (roleCode: string) => {
      if (!organizationId || !userId) {
        throw new Error(t("staffAssign.validation"));
      }
      const reason = userQuery.data?.existingRoleCode
        ? "Changed from POS client"
        : "Assigned from POS client";
      const result = userQuery.data?.existingRoleCode
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
      return result.grant;
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
  const canSubmit =
    Boolean(organizationId && userId && selectedRole && !assignMutation.isPending && !sameRoleSelected) &&
    userQuery.data?.membershipStatus === "Active";

  const replaceHint = useMemo(() => {
    if (!userQuery.data?.existingRoleLabel) {
      return null;
    }
    return t("staffAssign.replaceHint").replace("{role}", userQuery.data.existingRoleLabel);
  }, [t, userQuery.data?.existingRoleLabel]);

  const selectedRoleLabel = useMemo(() => {
    if (!selectedRole) {
      return "";
    }
    const fromCatalog = catalogQuery.data?.find((role) => role.code === selectedRole);
    return fromCatalog?.displayName ?? friendlyPosRoleLabel(null, selectedRole);
  }, [catalogQuery.data, selectedRole]);

  function requestAssign() {
    if (!selectedRole || sameRoleSelected) {
      return;
    }
    setSubmitError(null);
    if (selectedRole === POS_LOCAL_ROLE_OWNER) {
      setOwnerConfirmOpen(true);
      return;
    }
    if (isChanging) {
      setChangeConfirmOpen(true);
      return;
    }
    assignMutation.mutate(selectedRole);
  }

  return (
    <div
      className="staff-assign-page exits-page flex min-w-0 flex-col gap-3"
      data-testid="org-staff-assign-page"
    >
      <PageHeader
        title={isChanging ? t("staffAssign.changeTitle") : t("staffAssign.title")}
        description={isChanging ? t("staffAssign.changeLede") : t("staffAssign.lede")}
        backTo={pageBackNav.orgStaff.to}
        backLabel={t(pageBackNav.orgStaff.labelKey)}
        backTestId="page-header-back-staff"
      />

      {!userId ? (
        <ErrorState title={t("error.title")} detail={t("staffAssign.validation")} />
      ) : null}

      {userQuery.isLoading || catalogQuery.isLoading ? (
        <LoadingSkeleton count={2} label={t("loading.label")} />
      ) : null}

      {userQuery.isError || catalogQuery.isError ? (
        <ErrorState
          title={t("error.title")}
          detail={
            userQuery.error instanceof Error
              ? userQuery.error.message
              : catalogQuery.error instanceof Error
                ? catalogQuery.error.message
                : t("staffManage.loadError")
          }
        />
      ) : null}

      {userQuery.isSuccess ? (
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

      {catalogQuery.isSuccess ? (
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
              const selected = selectedRole === option.code;
              const isCurrent = userQuery.data?.existingRoleCode === option.code;
              return (
                <button
                  key={option.code}
                  type="button"
                  role="radio"
                  aria-checked={selected}
                  disabled={!userId || assignMutation.isPending || userQuery.data?.membershipStatus !== "Active"}
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

      {submitError ? (
        <div className="exits-alert exits-alert--error" role="alert">
          <p className="m-0 text-[length:var(--exits-text-sm)]">{submitError}</p>
        </div>
      ) : null}

      <div className="exits-animate-toolbar">
        <Button
          type="button"
          className="min-h-11 w-full sm:w-auto"
          disabled={!canSubmit}
          data-testid="org-staff-assign-submit"
          onClick={requestAssign}
        >
          {assignMutation.isPending
            ? t("staffAssign.submitting")
            : isChanging
              ? t("staffAssign.changeConfirmAction")
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
          if (selectedRole === POS_LOCAL_ROLE_OWNER) {
            assignMutation.mutate(POS_LOCAL_ROLE_OWNER);
          }
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
          if (selectedRole) {
            assignMutation.mutate(selectedRole);
          }
        }}
        testId="org-staff-change-confirm"
      />
    </div>
  );
}
