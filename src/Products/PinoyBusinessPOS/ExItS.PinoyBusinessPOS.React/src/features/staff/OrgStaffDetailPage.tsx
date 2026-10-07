import { useEffect, useMemo, useState, type ReactNode } from "react";
import { IdCard, Pencil, UserRound } from "lucide-react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Link, useParams } from "react-router-dom";
import {
  getMembershipBusinessProfile,
  updateMembershipBusinessProfile,
  type MembershipBusinessProfile,
} from "@/api/platform/membership-business-profile-client";
import { listOrganizationMembers } from "@/api/platform/organization-members-client";
import { PlatformApiError } from "@/api/platform/platform-http";
import { hasOrganizationManagementAuthority } from "@/access/pos-capabilities";
import { CreatableCombobox } from "@/components/exits/CreatableCombobox";
import { ExitsSelect } from "@/components/exits/ExitsSelect";
import { ExitsTabs } from "@/components/exits/ExitsTabs";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingState } from "@/components/exits/LoadingState";
import { PageHeader } from "@/components/exits/PageHeader";
import { useToast } from "@/components/exits/ToastProvider";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Switch } from "@/components/ui/switch";
import { initialsFor } from "@/features/personal/people-status";
import { profilePhotoSrc } from "@/features/personal/profile-photo";
import {
  DEFAULT_DEPARTMENTS,
  DEFAULT_JOB_TITLES,
  mergeOrgScopedOptions,
} from "@/features/staff/member-business-profile-catalogs";
import { useI18n } from "@/i18n/I18nProvider";
import type { MessageKey } from "@/i18n/messages";
import { cn } from "@/lib/cn";
import { pageBackNav } from "@/navigation/page-back-nav";
import { AddressForm } from "@/platform/geography/AddressForm";
import { emptySharedAddress, type SharedAddressValue } from "@/platform/geography/types";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

function norm(value: string | null | undefined): string {
  return value?.trim() ?? "";
}

function staffFacingName(profile: MembershipBusinessProfile): string {
  return profile.staffDisplayName?.trim() || profile.displayName.trim();
}

function picked(
  captured: boolean,
  stored: string | null | undefined,
  fallback: string | null | undefined,
): string {
  if (captured) return stored?.trim() ?? "";
  return stored?.trim() || fallback?.trim() || "";
}

function staffAddressValue(profile: MembershipBusinessProfile): SharedAddressValue {
  const captured = profile.profileDetailsCaptured;
  const personal = !captured && !profile.addressLine1 ? profile.personal : null;
  const countryName = (profile.country || personal?.country || "").trim();
  const blank = emptySharedAddress(false);
  return {
    ...blank,
    addressType: "Work",
    countryCode: countryName ? "" : blank.countryCode,
    countryName: countryName || blank.countryName,
    administrativeAreaName: profile.provinceState || personal?.provinceState || "",
    cityName: profile.cityMunicipality || personal?.cityMunicipality || "",
    barangay: profile.barangay || personal?.barangay || "",
    addressLine1: profile.addressLine1 || personal?.addressLine1 || "",
    addressLine2: profile.addressLine2 || personal?.addressLine2 || "",
    postalCode: profile.postalCode || personal?.postalCode || "",
  };
}

function MemberBusinessProfileEditForm({
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
  const personal = profile.personal;
  const captured = profile.profileDetailsCaptured;
  const [firstName, setFirstName] = useState(() => picked(captured, profile.firstName, personal?.firstName));
  const [middleName, setMiddleName] = useState(() => picked(captured, profile.middleName, personal?.middleName));
  const [lastName, setLastName] = useState(() => picked(captured, profile.lastName, personal?.lastName));
  const [dateOfBirth, setDateOfBirth] = useState(() => picked(captured, profile.dateOfBirth, personal?.dateOfBirth));
  const [gender, setGender] = useState(() => picked(captured, profile.gender, personal?.gender));
  const [nationality, setNationality] = useState(() => picked(captured, profile.nationality, personal?.nationality));
  const [photoUrl, setPhotoUrl] = useState(() => picked(captured, profile.profilePhotoUrl, personal?.profilePhotoUrl));
  const [mobile, setMobile] = useState(() => picked(captured, profile.mobileNumber, personal?.mobileNumber));
  const [email, setEmail] = useState(() => picked(captured, profile.email, personal?.email));
  const [displayName, setDisplayName] = useState(() => staffFacingName(profile));
  const [department, setDepartment] = useState(profile.department ?? "");
  const [jobTitle, setJobTitle] = useState(profile.jobTitle ?? "");
  const [workPhone, setWorkPhone] = useState(profile.workPhone ?? "");
  const [workEmail, setWorkEmail] = useState(profile.workEmail ?? "");
  const [isBusinessContact, setIsBusinessContact] = useState(profile.isBusinessContact);
  const [staffId, setStaffId] = useState(profile.staffId ?? "");
  const [address, setAddress] = useState(() => staffAddressValue(profile));
  const [formError, setFormError] = useState<string | null>(null);

  useEffect(() => {
    if (!open) return;
    setFirstName(picked(captured, profile.firstName, personal?.firstName));
    setMiddleName(picked(captured, profile.middleName, personal?.middleName));
    setLastName(picked(captured, profile.lastName, personal?.lastName));
    setDateOfBirth(picked(captured, profile.dateOfBirth, personal?.dateOfBirth));
    setGender(picked(captured, profile.gender, personal?.gender));
    setNationality(picked(captured, profile.nationality, personal?.nationality));
    setPhotoUrl(picked(captured, profile.profilePhotoUrl, personal?.profilePhotoUrl));
    setMobile(picked(captured, profile.mobileNumber, personal?.mobileNumber));
    setEmail(picked(captured, profile.email, personal?.email));
    setDisplayName(staffFacingName(profile));
    setDepartment(profile.department ?? "");
    setJobTitle(profile.jobTitle ?? "");
    setWorkPhone(profile.workPhone ?? "");
    setWorkEmail(profile.workEmail ?? "");
    setIsBusinessContact(profile.isBusinessContact);
    setStaffId(profile.staffId ?? "");
    setAddress(staffAddressValue(profile));
    setFormError(null);
  }, [captured, open, personal, profile]);

  const catalogQuery = useQuery({
    queryKey: ["organization-members", organizationId, "business-profile-catalog"],
    enabled: open && Boolean(organizationId),
    queryFn: async () => listOrganizationMembers(organizationId, undefined),
  });

  const departmentOptions = useMemo(() => {
    const members = catalogQuery.data?.ok ? catalogQuery.data.members : [];
    return mergeOrgScopedOptions(DEFAULT_DEPARTMENTS, members.map((m) => m.department).concat(department));
  }, [catalogQuery.data, department]);

  const jobTitleOptions = useMemo(() => {
    const members = catalogQuery.data?.ok ? catalogQuery.data.members : [];
    return mergeOrgScopedOptions(DEFAULT_JOB_TITLES, members.map((m) => m.jobTitle).concat(jobTitle));
  }, [catalogQuery.data, jobTitle]);

  const baseline = staffAddressValue(profile);
  const dirty =
    norm(firstName) !== norm(picked(captured, profile.firstName, personal?.firstName)) ||
    norm(middleName) !== norm(picked(captured, profile.middleName, personal?.middleName)) ||
    norm(lastName) !== norm(picked(captured, profile.lastName, personal?.lastName)) ||
    norm(dateOfBirth) !== norm(picked(captured, profile.dateOfBirth, personal?.dateOfBirth)) ||
    norm(gender) !== norm(picked(captured, profile.gender, personal?.gender)) ||
    norm(nationality) !== norm(picked(captured, profile.nationality, personal?.nationality)) ||
    norm(photoUrl) !== norm(picked(captured, profile.profilePhotoUrl, personal?.profilePhotoUrl)) ||
    norm(mobile) !== norm(picked(captured, profile.mobileNumber, personal?.mobileNumber)) ||
    norm(email) !== norm(picked(captured, profile.email, personal?.email)) ||
    norm(displayName) !== norm(staffFacingName(profile)) ||
    norm(department) !== norm(profile.department) ||
    norm(jobTitle) !== norm(profile.jobTitle) ||
    norm(workPhone) !== norm(profile.workPhone) ||
    norm(workEmail) !== norm(profile.workEmail) ||
    isBusinessContact !== profile.isBusinessContact ||
    norm(staffId) !== norm(profile.staffId) ||
    norm(address.countryName) !== norm(baseline.countryName) ||
    norm(address.addressLine1) !== norm(baseline.addressLine1) ||
    norm(address.addressLine2) !== norm(baseline.addressLine2) ||
    norm(address.barangay) !== norm(baseline.barangay) ||
    norm(address.cityName) !== norm(baseline.cityName) ||
    norm(address.administrativeAreaName) !== norm(baseline.administrativeAreaName) ||
    norm(address.postalCode) !== norm(baseline.postalCode);

  const saveMutation = useMutation({
    mutationFn: () =>
      updateMembershipBusinessProfile(organizationId, membershipId, {
        department: department.trim() || null,
        jobTitle: jobTitle.trim() || null,
        workPhone: workPhone.trim() || null,
        workEmail: workEmail.trim() || null,
        isBusinessContact,
        staffId: staffId.trim() || null,
        country: address.countryName.trim() || null,
        addressLine1: address.addressLine1.trim() || null,
        addressLine2: address.addressLine2.trim() || null,
        barangay: address.barangay.trim() || null,
        cityMunicipality: address.cityName.trim() || null,
        provinceState: address.administrativeAreaName.trim() || null,
        postalCode: address.postalCode.trim() || null,
        firstName: firstName.trim() || null,
        middleName: middleName.trim() || null,
        lastName: lastName.trim() || null,
        dateOfBirth: dateOfBirth.trim() || null,
        gender: gender.trim() || null,
        nationality: nationality.trim() || null,
        profilePhotoUrl: photoUrl.trim() || null,
        mobileNumber: mobile.trim() || null,
        email: email.trim() || null,
        staffDisplayName: displayName.trim() || null,
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

  if (!open) return null;

  return (
    <form
      className="flex flex-col gap-4"
      data-testid="member-business-profile-drawer"
      onSubmit={(event) => {
        event.preventDefault();
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
      <div className="personal-profile-layout">
        <ProfileSection title={t("staffBusinessProfile.sectionIdentity")} tone="details" wide>
          <Input label={t("personal.profile.firstName")} value={firstName} onChange={(event) => setFirstName(event.target.value)} data-testid="member-bp-first-name" />
          <Input label={t("personal.profile.middleName")} value={middleName} onChange={(event) => setMiddleName(event.target.value)} />
          <Input label={t("personal.profile.lastName")} value={lastName} onChange={(event) => setLastName(event.target.value)} data-testid="member-bp-last-name" />
          <Input label={t("personal.profile.birthDate")} type="date" value={dateOfBirth} onChange={(event) => setDateOfBirth(event.target.value)} />
          <label className="flex min-w-0 flex-col gap-1 text-sm">
            <span className="font-semibold">{t("personal.profile.gender")}</span>
            <ExitsSelect
              value={gender}
              options={genderOptions(gender, t)}
              placeholder={t("personal.profile.selectPlaceholder")}
              onChange={setGender}
              testId="member-bp-gender"
            />
          </label>
          <Input label={t("personal.profile.nationality")} value={nationality} onChange={(event) => setNationality(event.target.value)} />
          <Input
            label={t("personal.profile.displayName")}
            value={displayName}
            onChange={(event) => setDisplayName(event.target.value)}
            data-testid="member-bp-display-name"
          />
          <Input label={t("personal.profile.mobile")} value={mobile} onChange={(event) => setMobile(event.target.value)} inputMode="tel" />
          <Input label={t("personal.profile.email")} value={email} onChange={(event) => setEmail(event.target.value)} type="email" />
        </ProfileSection>
        <ProfileSection title={t("staffBusinessProfile.sectionWorkplace")} tone="workplace" wide>
          <Input label={t("staffBusinessProfile.staffId")} value={staffId} onChange={(event) => setStaffId(event.target.value)} data-testid="member-bp-staff-id" />
          <label className="flex flex-col gap-1 text-sm">
            <span className="font-semibold">{t("staffBusinessProfile.department")}</span>
            <CreatableCombobox
              value={department}
              onChange={setDepartment}
              options={departmentOptions}
              placeholder={t("staffBusinessProfile.departmentPlaceholder")}
              searchPlaceholder={t("staffBusinessProfile.catalogSearch")}
              createLabel={(query) => t("staffBusinessProfile.createDepartment").replace("{value}", query)}
              emptyLabel={t("staffBusinessProfile.catalogEmpty")}
              testId="member-bp-department"
            />
          </label>
          <label className="flex flex-col gap-1 text-sm">
            <span className="font-semibold">{t("staffBusinessProfile.jobTitle")}</span>
            <CreatableCombobox
              value={jobTitle}
              onChange={setJobTitle}
              options={jobTitleOptions}
              placeholder={t("staffBusinessProfile.jobTitlePlaceholder")}
              searchPlaceholder={t("staffBusinessProfile.catalogSearch")}
              createLabel={(query) => t("staffBusinessProfile.createJobTitle").replace("{value}", query)}
              emptyLabel={t("staffBusinessProfile.catalogEmpty")}
              testId="member-bp-job-title"
            />
          </label>
          <Input label={t("staffBusinessProfile.workPhone")} value={workPhone} onChange={(event) => setWorkPhone(event.target.value)} inputMode="tel" data-testid="member-bp-work-phone" />
          <Input label={t("staffBusinessProfile.workEmail")} value={workEmail} onChange={(event) => setWorkEmail(event.target.value)} type="email" data-testid="member-bp-work-email" />
          <div className="flex items-center justify-between gap-3 py-1">
            <span className="text-sm">{t("staffBusinessProfile.availableAsContact")}</span>
            <Switch
              checked={isBusinessContact}
              onCheckedChange={setIsBusinessContact}
              data-testid="member-bp-is-business-contact"
            />
          </div>
        </ProfileSection>
        <ProfileSection title={t("staffBusinessProfile.sectionAddress")} tone="address" wide>
          <div className="personal-profile-field--wide">
            <AddressForm
              value={address}
              onChange={setAddress}
              showAddressType={false}
              showPrimary={false}
              testIdPrefix="member-bp-address"
            />
          </div>
        </ProfileSection>
      </div>
      <div className="flex gap-2">
        <Button type="button" variant="outline" data-testid="member-bp-cancel" onClick={onClose}>
          {t("staffBusinessProfile.cancel")}
        </Button>
        <Button type="submit" data-testid="member-bp-save" disabled={!dirty || saveMutation.isPending}>
          {saveMutation.isPending ? t("loading.label") : t("staffBusinessProfile.save")}
        </Button>
      </div>
    </form>
  );
}

function StaffRecordView({
  profile,
  source,
  testId,
}: {
  profile: MembershipBusinessProfile;
  source: "staff" | "personal";
  testId?: string;
}) {
  const { t } = useI18n();
  const personal = profile.personal;
  const captured = profile.profileDetailsCaptured;
  const blank = t("common.notAvailable");
  const show = (value: string) => value.trim() || blank;
  if (source === "personal" && !personal) {
    return <p className="m-0 text-sm text-muted-foreground">{t("staffBusinessProfile.personalEmpty")}</p>;
  }
  const firstName = source === "personal" ? personal?.firstName ?? "" : picked(captured, profile.firstName, personal?.firstName);
  const middleName = source === "personal" ? personal?.middleName ?? "" : picked(captured, profile.middleName, personal?.middleName);
  const lastName = source === "personal" ? personal?.lastName ?? "" : picked(captured, profile.lastName, personal?.lastName);
  const dateOfBirth = source === "personal" ? personal?.dateOfBirth ?? "" : picked(captured, profile.dateOfBirth, personal?.dateOfBirth);
  const gender = source === "personal" ? personal?.gender ?? "" : picked(captured, profile.gender, personal?.gender);
  const nationality = source === "personal" ? personal?.nationality ?? "" : picked(captured, profile.nationality, personal?.nationality);
  const mobile = source === "personal" ? personal?.mobileNumber ?? "" : picked(captured, profile.mobileNumber, personal?.mobileNumber);
  const email = source === "personal" ? personal?.email ?? "" : picked(captured, profile.email, personal?.email);
  const staffAddress = source === "staff" ? staffAddressValue(profile) : null;

  return (
    <div className="flex flex-col gap-4" data-testid={testId}>
      <div className="personal-profile-layout">
        <ProfileSection title={t("staffBusinessProfile.sectionIdentity")} tone="details" wide>
          <ReadOnlyField label={t("personal.profile.firstName")} value={show(firstName)} />
          <ReadOnlyField label={t("personal.profile.middleName")} value={show(middleName ?? "")} />
          <ReadOnlyField label={t("personal.profile.lastName")} value={show(lastName)} />
          <ReadOnlyField label={t("personal.profile.birthDate")} value={show(dateOfBirth)} />
          <ReadOnlyField label={t("personal.profile.gender")} value={show(genderLabel(gender, t))} />
          <ReadOnlyField label={t("personal.profile.nationality")} value={show(nationality)} />
          {source === "staff" ? (
            <ReadOnlyField label={t("personal.profile.displayName")} value={show(staffFacingName(profile))} />
          ) : null}
          <ReadOnlyField label={t("personal.profile.mobile")} value={show(mobile)} />
          <ReadOnlyField label={t("personal.profile.email")} value={show(email)} />
        </ProfileSection>
        {source === "staff" ? (
          <ProfileSection title={t("staffBusinessProfile.sectionWorkplace")} tone="workplace" wide>
            <ReadOnlyField label={t("staffBusinessProfile.staffId")} value={profile.staffId?.trim() || blank} testId="member-bp-staff-id-value" />
            <ReadOnlyField label={t("staffBusinessProfile.department")} value={profile.department?.trim() || blank} />
            <ReadOnlyField label={t("staffBusinessProfile.jobTitle")} value={profile.jobTitle?.trim() || blank} />
            <ReadOnlyField label={t("staffBusinessProfile.workPhone")} value={profile.workPhone?.trim() || blank} />
            <ReadOnlyField label={t("staffBusinessProfile.workEmail")} value={profile.workEmail?.trim() || blank} />
            <ReadOnlyField
              label={t("staffBusinessProfile.availableAsContact")}
              value={profile.isBusinessContact ? t("staffBusinessProfile.yes") : t("staffBusinessProfile.no")}
              testId="member-bp-contact-flag"
            />
          </ProfileSection>
        ) : null}
        <ProfileSection title={t("staffBusinessProfile.sectionAddress")} tone="address" wide>
          <ReadOnlyField label={t("personal.profile.country")} value={show(source === "personal" ? personal?.country ?? "" : staffAddress?.countryName ?? "")} />
          <ReadOnlyField label={t("personal.profile.province")} value={show(source === "personal" ? personal?.provinceState ?? "" : staffAddress?.administrativeAreaName ?? "")} />
          <ReadOnlyField label={t("personal.profile.city")} value={show(source === "personal" ? personal?.cityMunicipality ?? "" : staffAddress?.cityName ?? "")} />
          <ReadOnlyField label={t("personal.profile.barangay")} value={show(source === "personal" ? personal?.barangay ?? "" : staffAddress?.barangay ?? "")} />
          <ReadOnlyField label={t("personal.profile.address1")} value={show(source === "personal" ? personal?.addressLine1 ?? "" : staffAddress?.addressLine1 ?? "")} />
          <ReadOnlyField label={t("personal.profile.address2")} value={show(source === "personal" ? personal?.addressLine2 ?? "" : staffAddress?.addressLine2 ?? "")} />
          <ReadOnlyField label={t("personal.profile.postal")} value={show(source === "personal" ? personal?.postalCode ?? "" : staffAddress?.postalCode ?? "")} />
        </ProfileSection>
      </div>
    </div>
  );
}

export function OrgStaffDetailPage() {
  const { t } = useI18n();
  const { membershipId } = useParams<{ membershipId: string }>();
  const [tab, setTab] = useState("profile");
  const { sessionGrant, boundWorkspace } = useWorkspace();
  const organizationId = boundWorkspace?.organizationId ?? null;
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
  const shownName = staffFacingName(profile);
  const staffLabel = t("staffInvite.orgRoleStaff");
  const headerRole = profile.jobTitle?.trim() || profile.roleDisplay?.trim() || profile.role;
  const shownRole = headerRole.localeCompare(staffLabel, undefined, { sensitivity: "accent" }) === 0 ? "" : headerRole;
  const personalPhoto = profile.personal?.profilePhotoUrl?.trim() ?? "";
  const staffPhoto = profile.profilePhotoUrl?.trim() ?? "";
  const photo = tab === "personal" ? personalPhoto : staffPhoto || personalPhoto;

  return (
    <div className="personal-profile-page flex w-full min-w-0 flex-col gap-4 p-4" data-testid="org-staff-detail-page">
      <div className="personal-profile-header">
        <ProfilePhotoPreview url={photo} fallbackUrl={personalPhoto} name={shownName} />
        <PageHeader
          title={shownName || t("staffBusinessProfile.title")}
          subtitleClassName="flex min-w-0 flex-wrap items-baseline gap-x-2 gap-y-0.5"
          subtitle={
            <>
              <span className="font-bold text-foreground">{staffLabel}</span>
              {shownRole ? <span className="font-bold text-primary">{shownRole}</span> : null}
              <Link
                to={`/org/staff/assign?userId=${encodeURIComponent(profile.userId)}`}
                className="font-bold text-primary underline"
              >
                {t("staffBusinessProfile.manageAccess")}
              </Link>
            </>
          }
          backTo={pageBackNav.orgStaff.to}
          backLabel={t(pageBackNav.orgStaff.labelKey)}
        />
      </div>

      {!profile.profileDetailsCaptured ? (
        <p className="m-0 text-sm text-muted-foreground">{t("staffBusinessProfile.addressFromPersonal")}</p>
      ) : null}

      <ExitsTabs
        variant="underline"
        scrollable
        ariaLabel={t("staffBusinessProfile.tabProfile")}
        testId="org-staff-profile-tabs"
        value={tab}
        onValueChange={(value) => {
          setTab(value);
          if (value === "edit") setEditOpen(true);
        }}
        items={[
          { key: "profile", label: t("staffBusinessProfile.tabProfile"), icon: IdCard, testId: "org-staff-tab-profile" },
          ...(canEdit ? [{ key: "edit", label: t("staffBusinessProfile.tabEdit"), icon: Pencil, testId: "org-staff-tab-edit" }] : []),
          { key: "personal", label: t("staffBusinessProfile.tabPersonal"), icon: UserRound, testId: "org-staff-tab-personal" },
        ]}
        panels={{
          profile: <StaffRecordView profile={profile} source="staff" />,
          edit: canEdit ? (
            <MemberBusinessProfileEditForm
              open={editOpen || tab === "edit"}
              onClose={() => { setEditOpen(false); setTab("profile"); }}
              organizationId={organizationId}
              membershipId={membershipId}
              profile={profile}
            />
          ) : null,
          personal: <StaffRecordView profile={profile} source="personal" testId="org-staff-personal-panel" />,
        }}
      />
    </div>
  );
}

function genderOptions(current: string, t: (key: MessageKey) => string) {
  const options = [
    { value: "Male", label: t("personal.profile.genderMale") },
    { value: "Female", label: t("personal.profile.genderFemale") },
    { value: "Other", label: t("personal.profile.genderOther") },
  ];
  if (current && !options.some((option) => option.value === current)) {
    options.unshift({ value: current, label: current });
  }
  return options;
}

function genderLabel(value: string, t: (key: MessageKey) => string) {
  return genderOptions(value, t).find((option) => option.value === value)?.label ?? value;
}

function ReadOnlyField({ label, value, testId }: { label: string; value: string; testId?: string }) {
  return (
    <div className="flex min-w-0 flex-col gap-1 text-sm">
      <span className="font-semibold">{label}</span>
      <span data-testid={testId}>{value}</span>
    </div>
  );
}

function ProfilePhotoPreview({
  url,
  fallbackUrl = "",
  name,
  compact = false,
}: {
  url: string;
  fallbackUrl?: string;
  name: string;
  compact?: boolean;
}) {
  const [useFallback, setUseFallback] = useState(false);
  const [failed, setFailed] = useState(false);
  useEffect(() => {
    setUseFallback(false);
    setFailed(false);
  }, [url, fallbackUrl]);
  const activeUrl = useFallback ? fallbackUrl : url;
  const src = failed ? null : profilePhotoSrc(activeUrl);
  const initials = initialsFor(name);
  return (
    <span
      className={compact ? "personal-profile-header__photo personal-profile-header__photo--compact" : "personal-profile-header__photo"}
      data-testid={compact ? "org-staff-photo" : "org-staff-header-photo"}
    >
      {src ? (
        <img
          src={src}
          alt=""
          onError={() => {
            if (!useFallback && fallbackUrl && fallbackUrl !== url) {
              setUseFallback(true);
              return;
            }
            setFailed(true);
          }}
        />
      ) : initials === "?" ? "—" : initials}
    </span>
  );
}

function ProfileSection({
  title,
  children,
  tone,
  wide = false,
}: {
  title: string;
  children: ReactNode;
  tone?: "personal" | "address" | "workplace" | "details";
  wide?: boolean;
}) {
  return (
    <section
      className={cn(
        "catalog-form-section personal-profile-section flex flex-col gap-3",
        tone === "personal" && "personal-profile-section--personal",
        tone === "address" && "personal-profile-section--address",
        tone === "workplace" && "personal-profile-section--workplace",
        tone === "details" && "personal-profile-section--details",
        wide && "personal-profile-section--wide",
      )}
    >
      <h2 className="catalog-form-section__title">{title}</h2>
      <div className="personal-profile-fields">{children}</div>
    </section>
  );
}
