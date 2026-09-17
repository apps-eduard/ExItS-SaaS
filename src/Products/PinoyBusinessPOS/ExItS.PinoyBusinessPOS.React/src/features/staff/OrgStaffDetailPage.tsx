import { useEffect, useMemo, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Link, useParams } from "react-router-dom";
import { Pencil } from "lucide-react";
import {
  getMembershipBusinessProfile,
  updateMembershipBusinessProfile,
  type MembershipBusinessProfile,
} from "@/api/platform/membership-business-profile-client";
import { listOrganizationMembers } from "@/api/platform/organization-members-client";
import { PlatformApiError } from "@/api/platform/platform-http";
import {
  hasOrganizationManagementAuthority,
} from "@/access/pos-capabilities";
import { CreatableCombobox } from "@/components/exits/CreatableCombobox";
import { FormDrawer } from "@/components/exits/FormDrawer";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingState } from "@/components/exits/LoadingState";
import { PageHeader } from "@/components/exits/PageHeader";
import { useToast } from "@/components/exits/ToastProvider";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Switch } from "@/components/ui/switch";
import {
  DEFAULT_DEPARTMENTS,
  DEFAULT_JOB_TITLES,
  mergeOrgScopedOptions,
} from "@/features/staff/member-business-profile-catalogs";
import { useI18n } from "@/i18n/I18nProvider";
import { pageBackNav } from "@/navigation/page-back-nav";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

function norm(value: string | null | undefined): string {
  return value?.trim() ?? "";
}

function MemberBusinessProfileEditDrawer({
  open,
  onClose,
  organizationId,
  membershipId,
  profile,
}: {
  open: boolean;
  onClose: () => void;
  organizationId: string;
  membershipId: string;
  profile: MembershipBusinessProfile;
}) {
  const { t } = useI18n();
  const { showToast } = useToast();
  const queryClient = useQueryClient();
  const [department, setDepartment] = useState(profile.department ?? "");
  const [jobTitle, setJobTitle] = useState(profile.jobTitle ?? "");
  const [workPhone, setWorkPhone] = useState(profile.workPhone ?? "");
  const [workEmail, setWorkEmail] = useState(profile.workEmail ?? "");
  const [isBusinessContact, setIsBusinessContact] = useState(profile.isBusinessContact);
  const [formError, setFormError] = useState<string | null>(null);

  useEffect(() => {
    if (!open) return;
    setDepartment(profile.department ?? "");
    setJobTitle(profile.jobTitle ?? "");
    setWorkPhone(profile.workPhone ?? "");
    setWorkEmail(profile.workEmail ?? "");
    setIsBusinessContact(profile.isBusinessContact);
    setFormError(null);
  }, [open, profile]);

  const catalogQuery = useQuery({
    queryKey: ["organization-members", organizationId, "business-profile-catalog"],
    enabled: open && Boolean(organizationId),
    queryFn: async () => listOrganizationMembers(organizationId, undefined),
  });

  const departmentOptions = useMemo(() => {
    const members = catalogQuery.data?.ok ? catalogQuery.data.members : [];
    return mergeOrgScopedOptions(
      DEFAULT_DEPARTMENTS,
      members.map((m) => m.department).concat(department),
    );
  }, [catalogQuery.data, department]);

  const jobTitleOptions = useMemo(() => {
    const members = catalogQuery.data?.ok ? catalogQuery.data.members : [];
    return mergeOrgScopedOptions(
      DEFAULT_JOB_TITLES,
      members.map((m) => m.jobTitle).concat(jobTitle),
    );
  }, [catalogQuery.data, jobTitle]);

  const dirty =
    norm(department) !== norm(profile.department) ||
    norm(jobTitle) !== norm(profile.jobTitle) ||
    norm(workPhone) !== norm(profile.workPhone) ||
    norm(workEmail) !== norm(profile.workEmail) ||
    isBusinessContact !== profile.isBusinessContact;

  const saveMutation = useMutation({
    mutationFn: () =>
      updateMembershipBusinessProfile(organizationId, membershipId, {
        department: department.trim() || null,
        jobTitle: jobTitle.trim() || null,
        workPhone: workPhone.trim() || null,
        workEmail: workEmail.trim() || null,
        isBusinessContact,
      }),
    onSuccess: async () => {
      showToast(t("staffBusinessProfile.saved"), "success");
      await queryClient.invalidateQueries({
        queryKey: ["membership-business-profile", organizationId, membershipId],
      });
      await queryClient.invalidateQueries({ queryKey: ["organization-members", organizationId] });
      onClose();
    },
    onError: (error) => {
      const message =
        error instanceof PlatformApiError
          ? error.message
          : error instanceof Error
            ? error.message
            : t("staffBusinessProfile.saveFailed");
      setFormError(message);
      showToast(message, "error");
    },
  });

  return (
    <FormDrawer
      open={open}
      onOpenChange={(next) => {
        if (!next) onClose();
      }}
      title={t("staffBusinessProfile.editTitle")}
      description={profile.displayName?.trim() || undefined}
      testId="member-business-profile-drawer"
      saveTestId="member-bp-save"
      cancelTestId="member-bp-cancel"
      saveLabel={
        saveMutation.isPending ? t("loading.label") : t("staffBusinessProfile.save")
      }
      cancelLabel={t("staffBusinessProfile.cancel")}
      saving={saveMutation.isPending}
      saveDisabled={!dirty || saveMutation.isPending}
      dirty={dirty}
      confirmUnsavedOnClose
      unsavedTitle={t("staffBusinessProfile.unsavedTitle")}
      unsavedDetail={t("staffBusinessProfile.unsavedDetail")}
      unsavedConfirmLabel={t("staffBusinessProfile.unsavedConfirm")}
      unsavedCancelLabel={t("staffBusinessProfile.unsavedCancel")}
      onSave={() => {
        if (!dirty || saveMutation.isPending) return;
        setFormError(null);
        saveMutation.mutate();
      }}
    >
      {formError ? (
        <p className="m-0 text-sm text-destructive" role="alert" data-testid="member-bp-error">
          {formError}
        </p>
      ) : null}
      <label className="flex flex-col gap-1 text-sm">
        <span>{t("staffBusinessProfile.department")}</span>
        <CreatableCombobox
          value={department}
          onChange={setDepartment}
          options={departmentOptions}
          placeholder={t("staffBusinessProfile.departmentPlaceholder")}
          searchPlaceholder={t("staffBusinessProfile.catalogSearch")}
          createLabel={(q) =>
            t("staffBusinessProfile.createDepartment").replace("{value}", q)
          }
          emptyLabel={t("staffBusinessProfile.catalogEmpty")}
          testId="member-bp-department"
        />
      </label>
      <label className="flex flex-col gap-1 text-sm">
        <span>{t("staffBusinessProfile.jobTitle")}</span>
        <CreatableCombobox
          value={jobTitle}
          onChange={setJobTitle}
          options={jobTitleOptions}
          placeholder={t("staffBusinessProfile.jobTitlePlaceholder")}
          searchPlaceholder={t("staffBusinessProfile.catalogSearch")}
          createLabel={(q) =>
            t("staffBusinessProfile.createJobTitle").replace("{value}", q)
          }
          emptyLabel={t("staffBusinessProfile.catalogEmpty")}
          testId="member-bp-job-title"
        />
      </label>
      <label className="flex flex-col gap-1 text-sm">
        <span>{t("staffBusinessProfile.workPhone")}</span>
        <Input
          value={workPhone}
          onChange={(e) => setWorkPhone(e.target.value)}
          inputMode="tel"
          data-testid="member-bp-work-phone"
        />
      </label>
      <label className="flex flex-col gap-1 text-sm">
        <span>{t("staffBusinessProfile.workEmail")}</span>
        <Input
          value={workEmail}
          onChange={(e) => setWorkEmail(e.target.value)}
          type="email"
          data-testid="member-bp-work-email"
        />
      </label>
      <div className="flex items-center justify-between gap-3 py-1">
        <span className="text-sm">{t("staffBusinessProfile.availableAsContact")}</span>
        <Switch
          checked={isBusinessContact}
          onCheckedChange={setIsBusinessContact}
          data-testid="member-bp-is-business-contact"
        />
      </div>
    </FormDrawer>
  );
}

export function OrgStaffDetailPage() {
  const { t } = useI18n();
  const { membershipId } = useParams<{ membershipId: string }>();
  const { sessionGrant } = useWorkspace();
  const organizationId = sessionGrant?.organizationId ?? null;
  const canEdit = hasOrganizationManagementAuthority(sessionGrant);
  const [editOpen, setEditOpen] = useState(false);

  const query = useQuery({
    queryKey: ["membership-business-profile", organizationId, membershipId],
    enabled: Boolean(organizationId) && Boolean(membershipId),
    queryFn: ({ signal }) => getMembershipBusinessProfile(organizationId!, membershipId!, signal),
  });

  if (!organizationId || !membershipId) {
    return (
      <ErrorState title={t("staffBusinessProfile.missing")} detail={t("staffBusinessProfile.missingDetail")} />
    );
  }
  if (query.isLoading) {
    return <LoadingState label={t("loading.label")} />;
  }
  if (query.isError || !query.data) {
    return (
      <ErrorState
        title={t("staffBusinessProfile.loadFailed")}
        detail={t("staffBusinessProfile.loadFailedDetail")}
        error={query.error}
        operation="getMembershipBusinessProfile"
      />
    );
  }

  const profile = query.data;

  return (
    <div className="flex flex-col gap-4 p-4" data-testid="org-staff-detail-page">
      <PageHeader
        title={profile.displayName || t("staffBusinessProfile.title")}
        subtitle={profile.roleDisplay ?? profile.role}
        backTo={pageBackNav.orgStaff.to}
        backLabel={t(pageBackNav.orgStaff.labelKey)}
        actions={
          canEdit ? (
            <Button type="button" variant="outline" data-testid="member-bp-edit" onClick={() => setEditOpen(true)}>
              <Pencil className="size-4" aria-hidden />
              {t("staffBusinessProfile.edit")}
            </Button>
          ) : null
        }
      />

      <section className="flex flex-col gap-2" aria-labelledby="staff-bp-heading">
        <h2 id="staff-bp-heading" className="m-0 text-base font-semibold">
          {t("staffBusinessProfile.section")}
        </h2>
        <dl className="m-0 grid gap-2 text-sm">
          <div>
            <dt className="text-muted-foreground">{t("staffBusinessProfile.department")}</dt>
            <dd className="m-0">{profile.department?.trim() || t("common.notAvailable")}</dd>
          </div>
          <div>
            <dt className="text-muted-foreground">{t("staffBusinessProfile.jobTitle")}</dt>
            <dd className="m-0">{profile.jobTitle?.trim() || t("common.notAvailable")}</dd>
          </div>
          <div>
            <dt className="text-muted-foreground">{t("staffBusinessProfile.workPhone")}</dt>
            <dd className="m-0">{profile.workPhone?.trim() || t("common.notAvailable")}</dd>
          </div>
          <div>
            <dt className="text-muted-foreground">{t("staffBusinessProfile.workEmail")}</dt>
            <dd className="m-0">{profile.workEmail?.trim() || t("common.notAvailable")}</dd>
          </div>
          <div>
            <dt className="text-muted-foreground">{t("staffBusinessProfile.availableAsContact")}</dt>
            <dd className="m-0" data-testid="member-bp-contact-flag">
              {profile.isBusinessContact ? t("staffBusinessProfile.yes") : t("staffBusinessProfile.no")}
            </dd>
          </div>
        </dl>
        <p className="m-0 text-sm">
          <Link to={`/org/staff/assign?userId=${encodeURIComponent(profile.userId)}`} className="underline">
            {t("staffBusinessProfile.manageAccess")}
          </Link>
        </p>
      </section>

      {canEdit ? (
        <MemberBusinessProfileEditDrawer
          open={editOpen}
          onClose={() => setEditOpen(false)}
          organizationId={organizationId}
          membershipId={membershipId}
          profile={profile}
        />
      ) : null}
    </div>
  );
}
