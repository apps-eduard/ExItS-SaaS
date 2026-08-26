import { useMemo, useState } from "react";
import { useNavigate, useSearchParams } from "react-router-dom";
import { useMutation, useQuery } from "@tanstack/react-query";
import { cn } from "@/lib/cn";
import { listOrganizationMembers } from "@/api/platform/organization-members-client";
import {
  assignProductLocalRole,
  friendlyPosRoleLabel,
  listProductLocalRoles,
  POS_LOCAL_ROLE_CASHIER,
  POS_LOCAL_ROLE_MANAGER,
  POS_LOCAL_ROLE_OWNER,
  type PosLocalRoleCode,
} from "@/api/platform/product-local-roles-client";
import { Button } from "@/components/ui/button";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingSkeleton } from "@/components/exits/FoundationStates";
import { PageHeader } from "@/components/exits/PageHeader";
import { ConfirmationDialog } from "@/components/exits/SheetDialog";
import { useI18n } from "@/i18n/I18nProvider";
import type { MessageKey } from "@/i18n/messages";
import { pageBackNav } from "@/navigation/page-back-nav";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

const ROLE_OPTIONS: ReadonlyArray<{
  code: PosLocalRoleCode;
  labelKey: MessageKey;
  descKey: MessageKey;
}> = [
  {
    code: POS_LOCAL_ROLE_OWNER,
    labelKey: "staffAssign.roleOwner",
    descKey: "staffAssign.roleOwnerDesc",
  },
  {
    code: POS_LOCAL_ROLE_MANAGER,
    labelKey: "staffAssign.roleManager",
    descKey: "staffAssign.roleManagerDesc",
  },
  {
    code: POS_LOCAL_ROLE_CASHIER,
    labelKey: "staffAssign.roleCashier",
    descKey: "staffAssign.roleCashierDesc",
  },
];

export function OrgStaffAssignPage() {
  const { t } = useI18n();
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const { boundWorkspace } = useWorkspace();
  const organizationId = boundWorkspace?.organizationId ?? null;
  const userId = searchParams.get("userId")?.trim() || null;

  const [selectedRole, setSelectedRole] = useState<PosLocalRoleCode | null>(null);
  const [ownerConfirmOpen, setOwnerConfirmOpen] = useState(false);
  const [submitError, setSubmitError] = useState<string | null>(null);

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
    mutationFn: async (roleCode: PosLocalRoleCode) => {
      if (!organizationId || !userId) {
        throw new Error(t("staffAssign.validation"));
      }
      const result = await assignProductLocalRole({
        organizationId,
        userIdentityId: userId,
        roleCode,
        reason: "Assigned from POS client",
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
    },
  });

  const canSubmit = Boolean(organizationId && userId && selectedRole && !assignMutation.isPending);

  const replaceHint = useMemo(() => {
    if (!userQuery.data?.existingRoleLabel) {
      return null;
    }
    return t("staffAssign.replaceHint").replace("{role}", userQuery.data.existingRoleLabel);
  }, [t, userQuery.data?.existingRoleLabel]);

  function requestAssign() {
    if (!selectedRole) {
      return;
    }
    setSubmitError(null);
    if (selectedRole === POS_LOCAL_ROLE_OWNER) {
      setOwnerConfirmOpen(true);
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
        title={t("staffAssign.title")}
        description={t("staffAssign.lede")}
        backTo={pageBackNav.orgStaff.to}
        backLabel={t(pageBackNav.orgStaff.labelKey)}
        backTestId="page-header-back-staff"
      />

      {!userId ? (
        <ErrorState title={t("error.title")} detail={t("staffAssign.validation")} />
      ) : null}

      {userQuery.isLoading ? <LoadingSkeleton count={2} label={t("loading.label")} /> : null}

      {userQuery.isError ? (
        <ErrorState
          title={t("error.title")}
          detail={
            userQuery.error instanceof Error ? userQuery.error.message : t("staffManage.loadError")
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
        </section>
      ) : null}

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
          {ROLE_OPTIONS.map((option) => {
            const selected = selectedRole === option.code;
            return (
              <button
                key={option.code}
                type="button"
                role="radio"
                aria-checked={selected}
                disabled={!userId || assignMutation.isPending}
                data-testid={`org-staff-role-${option.code.toLowerCase()}`}
                className={cn(
                  "staff-assign-role",
                  selected && "staff-assign-role--selected",
                )}
                onClick={() => {
                  setSelectedRole(option.code);
                  setSubmitError(null);
                  setOwnerConfirmOpen(false);
                }}
              >
                <span className="staff-assign-role__check" aria-hidden>
                  {selected ? "✓" : ""}
                </span>
                <span className="staff-assign-role__copy">
                  <span className="staff-assign-role__label">{t(option.labelKey)}</span>
                  <span className="staff-assign-role__desc">{t(option.descKey)}</span>
                </span>
              </button>
            );
          })}
        </div>
      </section>

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
          {assignMutation.isPending ? t("staffAssign.submitting") : t("staffAssign.submit")}
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
    </div>
  );
}
