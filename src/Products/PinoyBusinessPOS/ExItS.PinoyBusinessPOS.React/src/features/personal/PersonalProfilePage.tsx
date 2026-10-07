import { useEffect, useMemo, useState, type ReactNode } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useNavigate, useSearchParams } from "react-router-dom";
import {
  getPersonalProfile,
  updatePersonalProfile,
  uploadPersonalProfilePhoto,
  type PersonalProfileDto,
  type UpdatePersonalProfileRequest,
} from "@/api/platform/start-business-client";
import { PersonalAddressesSection } from "@/features/personal/PersonalAddressesSection";
import { initialsFor } from "@/features/personal/people-status";
import { PlatformApiError } from "@/api/platform/platform-http";
import { ExitsSelect } from "@/components/exits/ExitsSelect";
import { ExitsUpload } from "@/components/exits/ExitsUpload";
import { ExitsTabs } from "@/components/exits/ExitsTabs";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingSkeleton } from "@/components/exits/FoundationStates";
import { Notice } from "@/components/exits/Notice";
import { PageHeader } from "@/components/exits/PageHeader";
import { useI18n } from "@/i18n/I18nProvider";
import type { MessageKey } from "@/i18n/messages";
import { usePersonalAvatarPhoto, useSetPersonalAvatarPhoto } from "@/features/personal/personal-avatar-context";
import { profilePhotoSrc } from "@/features/personal/profile-photo";
import { personalPageBackNav } from "@/navigation/page-back-nav";
import { cn } from "@/lib/cn";
import { useSession } from "@/session/SessionProvider";

const FIELD_LABEL: Record<string, MessageKey> = {
  FirstName: "personal.profile.firstName",
  LastName: "personal.profile.lastName",
  DisplayName: "personal.profile.displayName",
  MobileNumber: "personal.profile.mobile",
  Email: "personal.profile.email",
  Country: "personal.profile.country",
  AddressLine1: "personal.profile.address1",
  Barangay: "personal.profile.barangay",
  CityMunicipality: "personal.profile.city",
  Province: "personal.profile.province",
  ProvinceState: "personal.profile.province",
};

type Draft = {
  firstName: string;
  middleName: string;
  lastName: string;
  displayName: string;
  profilePhotoUrl: string;
  dateOfBirth: string;
  gender: string;
  nationality: string;
  phone: string;
  alternativeMobile: string;
  showProfilePhoto: string;
  showDisplayName: string;
  showCity: string;
  showMobile: string;
  showEmail: string;
};

function draftFrom(profile: PersonalProfileDto): Draft {
  return {
    firstName: profile.firstName ?? "",
    middleName: profile.middleName ?? "",
    lastName: profile.lastName ?? "",
    displayName: profile.displayName,
    profilePhotoUrl: profile.profilePhotoUrl ?? "",
    dateOfBirth: profile.dateOfBirth ?? "",
    gender: profile.gender ?? "",
    nationality: profile.nationality ?? "",
    phone: profile.phone ?? "",
    alternativeMobile: profile.alternativeMobile ?? "",
    showProfilePhoto: profile.showProfilePhoto,
    showDisplayName: profile.showDisplayName,
    showCity: profile.showCity,
    showMobile: profile.showMobile,
    showEmail: profile.showEmail,
  };
}

export function PersonalProfilePage() {
  const { t } = useI18n();
  const navigate = useNavigate();
  const { refreshSession } = useSession();
  const queryClient = useQueryClient();
  const [searchParams] = useSearchParams();
  const purpose = searchParams.get("complete");
  const returnTo = searchParams.get("return");
  const livePhotoUrl = usePersonalAvatarPhoto();
  const setLivePhotoUrl = useSetPersonalAvatarPhoto();
  const [tab, setTab] = useState("overview");
  const [draft, setDraft] = useState<Draft | null>(null);
  const [formError, setFormError] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);

  const profileQuery = useQuery({
    queryKey: ["personal", "profile"],
    queryFn: ({ signal }) => getPersonalProfile(signal),
  });
  useEffect(() => {
    if (profileQuery.data && !draft) {
      setDraft(draftFrom(profileQuery.data));
    }
  }, [profileQuery.data, draft]);

  const missing = useMemo(() => {
    const profile = profileQuery.data;
    if (!profile) {
      return [];
    }
    if (purpose === "staff") {
      return profile.missingForStaff;
    }
    if (purpose === "customer") {
      return profile.missingForCustomer;
    }
    return profile.missingForBase;
  }, [profileQuery.data, purpose]);

  const uploadMutation = useMutation({
    mutationFn: (file: File) => uploadPersonalProfilePhoto(file),
    onSuccess: (data) => {
      queryClient.setQueryData(["personal", "profile"], data);
      setDraft(draftFrom(data));
      setLivePhotoUrl(data.profilePhotoUrl);
      setFormError(null);
    },
    onError: (error) => {
      setFormError(
        error instanceof PlatformApiError
          ? (error.problem.detail ?? error.message)
          : error instanceof Error
            ? error.message
            : t("personal.profile.saveFailed"),
      );
    },
  });

  const saveMutation = useMutation({
    mutationFn: (request: UpdatePersonalProfileRequest) => updatePersonalProfile(request),
    onSuccess: async (data) => {
      queryClient.setQueryData(["personal", "profile"], data);
      setDraft(draftFrom(data));
      setFormError(null);
      setSuccessMessage(t("personal.profile.updated"));
      await refreshSession();
      if (returnTo) {
        navigate(returnTo);
      }
    },
    onError: (error) => {
      setSuccessMessage(null);
      setFormError(
        error instanceof PlatformApiError
          ? (error.problem.detail ?? error.message)
          : error instanceof Error
            ? error.message
            : t("personal.profile.saveFailed"),
      );
    },
  });

  function set<K extends keyof Draft>(key: K, value: Draft[K]) {
    setDraft((current) => (current ? { ...current, [key]: value } : current));
  }

  function save() {
    if (!draft || saveMutation.isPending) {
      return;
    }
    setFormError(null);
    saveMutation.mutate({
      displayName: draft.displayName,
      firstName: draft.firstName,
      middleName: draft.middleName,
      lastName: draft.lastName,
      phone: draft.phone,
      alternativeMobile: draft.alternativeMobile,
      dateOfBirth: draft.dateOfBirth || null,
      clearDateOfBirth: !draft.dateOfBirth,
      gender: draft.gender,
      nationality: draft.nationality,
      profilePhotoUrl: draft.profilePhotoUrl || livePhotoUrl || null,
      showProfilePhoto: draft.showProfilePhoto,
      showDisplayName: draft.showDisplayName,
      showCity: draft.showCity,
      showMobile: draft.showMobile,
      showEmail: draft.showEmail,
    });
  }

  if (profileQuery.isLoading || !draft) {
    return <LoadingSkeleton label={t("personal.profile.loading")} />;
  }

  if (profileQuery.isError || !profileQuery.data) {
    return (
      <ErrorState
        title={t("personal.profile.loadFailed")}
        detail={t("personal.profile.loadFailedDetail")}
        error={profileQuery.error}
        operation="personal.profile.load"
      />
    );
  }

  const profile = profileQuery.data;
  const photoUrl = livePhotoUrl || draft.profilePhotoUrl;

  function onPhotoSelected(file: File | undefined) {
    if (!file || uploadMutation.isPending) {
      return;
    }
    setFormError(null);
    uploadMutation.mutate(file);
  }

  return (
    <div className="personal-profile-page personal-page exits-page flex w-full min-w-0 flex-col gap-4" data-testid="personal-profile-card">
      <div className="personal-profile-header">
        <ProfilePhotoPreview url={photoUrl} name={draft.displayName} />
        <PageHeader
        title={t("personal.profile.title")}
        description={t("personal.profile.lede")}
        backTo={personalPageBackNav.more.to}
        backLabel={t(personalPageBackNav.more.labelKey)}
        backTestId="page-header-back-profile"
        />
      </div>
      <p className="m-0 text-[length:var(--exits-text-sm)]" data-testid="personal-profile-completeness">
        {t("personal.profile.percent").replace("{percent}", String(profile.completionPercent))}
      </p>
      {purpose && missing.length > 0 ? (
        <Notice tone="warning" testId="personal-profile-missing">
          <p className="m-0 font-semibold">{t("personal.profile.completeBefore")}</p>
          <p className="m-0">
            {t("personal.profile.missing")}:{" "}
            {missing.map((field) => (FIELD_LABEL[field] ? t(FIELD_LABEL[field]) : field)).join(", ")}
          </p>
        </Notice>
      ) : null}
      {successMessage ? <Notice tone="success" testId="personal-profile-success">{successMessage}</Notice> : null}
      {formError ? <Notice tone="danger">{formError}</Notice> : null}

      <ExitsTabs
        variant="underline"
        scrollable
        ariaLabel={t("personal.profile.title")}
        testId="personal-profile-tabs"
        value={tab}
        onValueChange={setTab}
        items={[
          { key: "overview", label: t("personal.profile.tabOverview"), testId: "personal-profile-tab-overview" },
          { key: "personal", label: t("personal.profile.sectionPersonal"), testId: "personal-profile-tab-personal" },
          { key: "addresses", label: t("personal.profile.sectionAddress"), testId: "personal-profile-tab-addresses" },
          { key: "privacy", label: t("personal.profile.sectionPrivacy"), testId: "personal-profile-tab-privacy" },
        ]}
        panels={{
          overview: (
            <div className="personal-profile-layout" data-testid="personal-profile-overview">
              <ProfileSection title={t("personal.profile.sectionPersonal")} tone="personal" wide>
                <ReadOnlyField label={t("personal.profile.firstName")} value={draft.firstName} />
                <ReadOnlyField label={t("personal.profile.middleName")} value={draft.middleName} />
                <ReadOnlyField label={t("personal.profile.lastName")} value={draft.lastName} />
                <ReadOnlyField label={t("personal.profile.displayName")} value={draft.displayName} />
                <ReadOnlyField label={t("personal.profile.birthDate")} value={draft.dateOfBirth} />
                <ReadOnlyField label={t("personal.profile.gender")} value={genderLabel(draft.gender, t)} />
                <ReadOnlyField label={t("personal.profile.nationality")} value={draft.nationality} />
                <div className="flex min-w-0 flex-col gap-1 text-[length:var(--exits-text-sm)]">
                  <span className="font-semibold">{t("personal.profile.photo")}</span>
                  <ProfilePhotoPreview url={photoUrl} name={draft.displayName} compact />
                </div>
                <ReadOnlyField label={t("personal.profile.mobile")} value={draft.phone} />
                <ReadOnlyField label={t("personal.profile.altMobile")} value={draft.alternativeMobile} />
                <ReadOnlyField label={t("personal.profile.email")} value={profile.email} />
              </ProfileSection>
              <ProfileSection title={t("personal.profile.sectionAddress")} tone="address" wide>
                <PersonalAddressesSection profile={profile} readOnly />
              </ProfileSection>
              <ProfileSection title={t("personal.profile.sectionPrivacy")} tone="privacy" wide>
                <ReadOnlyField label={t("personal.profile.showPhoto")} value={visibilityLabel(draft.showProfilePhoto, t)} />
                <ReadOnlyField label={t("personal.profile.showName")} value={visibilityLabel(draft.showDisplayName, t)} />
                <ReadOnlyField label={t("personal.profile.showCity")} value={visibilityLabel(draft.showCity, t)} />
                <ReadOnlyField label={t("personal.profile.showMobile")} value={visibilityLabel(draft.showMobile, t)} />
                <ReadOnlyField label={t("personal.profile.showEmail")} value={visibilityLabel(draft.showEmail, t)} />
              </ProfileSection>
            </div>
          ),
          personal: (
            <div className="flex flex-col gap-4">
              <ProfileSection title={t("personal.profile.sectionPersonal")} tone="personal" wide>
                <Input label={`${t("personal.profile.firstName")} · ${t("personal.profile.required")}`} value={draft.firstName} onChange={(event) => set("firstName", event.target.value)} data-testid="personal-profile-first-name" />
                <Input label={`${t("personal.profile.middleName")} · ${t("personal.profile.optional")}`} value={draft.middleName} onChange={(event) => set("middleName", event.target.value)} />
                <Input label={`${t("personal.profile.lastName")} · ${t("personal.profile.required")}`} value={draft.lastName} onChange={(event) => set("lastName", event.target.value)} data-testid="personal-profile-last-name" />
                <Input label={`${t("personal.profile.displayName")} · ${t("personal.profile.required")}`} value={draft.displayName} onChange={(event) => set("displayName", event.target.value)} data-testid="personal-profile-name-value" />
                <Input label={`${t("personal.profile.birthDate")} · ${t("personal.profile.optional")} · ${t("personal.profile.private")}`} type="date" value={draft.dateOfBirth} onChange={(event) => set("dateOfBirth", event.target.value)} />
                <label className="flex min-w-0 flex-col gap-1 text-[length:var(--exits-text-sm)]">
                  <span className="font-semibold">{t("personal.profile.gender")} · {t("personal.profile.optional")} · {t("personal.profile.private")}</span>
                  <ExitsSelect
                    value={draft.gender}
                    options={genderOptions(draft.gender, t)}
                    placeholder={t("personal.profile.selectPlaceholder")}
                    onChange={(gender) => set("gender", gender)}
                    testId="personal-profile-gender"
                  />
                </label>
                <Input label={`${t("personal.profile.nationality")} · ${t("personal.profile.optional")} · ${t("personal.profile.private")}`} value={draft.nationality} onChange={(event) => set("nationality", event.target.value)} />
                <div className="flex min-w-0 flex-col gap-1 text-[length:var(--exits-text-sm)]">
                  <span className="font-semibold">{`${t("personal.profile.photoUpload")} · ${t("personal.profile.optional")} · ${t("personal.profile.private")}`}</span>
                  <ExitsUpload
                    variant="button"
                    uploadLabel={uploadMutation.isPending ? t("personal.profile.photoUploading") : t("personal.profile.photoUpload")}
                    accept="image/jpeg,image/png,image/webp"
                    disabled={uploadMutation.isPending}
                    testId="personal-profile-photo-upload"
                    onSelectFiles={(files) => onPhotoSelected(files[0])}
                  />
                </div>
                <Input label={`${t("personal.profile.mobile")} · ${t("personal.profile.required")}`} value={draft.phone} onChange={(event) => set("phone", event.target.value)} data-testid="personal-profile-mobile" />
                <Input label={`${t("personal.profile.altMobile")} · ${t("personal.profile.optional")}`} value={draft.alternativeMobile} onChange={(event) => set("alternativeMobile", event.target.value)} />
                <Input label={`${t("personal.profile.email")} · ${t("personal.profile.required")}`} value={profile.email} readOnly />
              </ProfileSection>
              <SaveButton pending={saveMutation.isPending} onClick={save} label={saveMutation.isPending ? t("personal.profile.saving") : t("personal.profile.save")} />
            </div>
          ),
          addresses: (
            <ProfileSection title={t("personal.profile.sectionAddress")} tone="address" wide>
              <PersonalAddressesSection profile={profile} />
            </ProfileSection>
          ),
          privacy: (
            <div className="flex flex-col gap-4">
              <ProfileSection title={t("personal.profile.sectionPrivacy")} tone="privacy" wide>
                <WideField>
                  <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">{t("personal.profile.privacyNote")}</p>
                </WideField>
                <VisibilityField label={t("personal.profile.showPhoto")} value={draft.showProfilePhoto} onChange={(value) => set("showProfilePhoto", value)} t={t} />
                <VisibilityField label={t("personal.profile.showName")} value={draft.showDisplayName} onChange={(value) => set("showDisplayName", value)} t={t} />
                <VisibilityField label={t("personal.profile.showCity")} value={draft.showCity} onChange={(value) => set("showCity", value)} t={t} />
                <VisibilityField label={t("personal.profile.showMobile")} value={draft.showMobile} onChange={(value) => set("showMobile", value)} t={t} />
                <VisibilityField label={t("personal.profile.showEmail")} value={draft.showEmail} onChange={(value) => set("showEmail", value)} t={t} />
              </ProfileSection>
              <SaveButton pending={saveMutation.isPending} onClick={save} label={saveMutation.isPending ? t("personal.profile.saving") : t("personal.profile.save")} />
            </div>
          ),
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

function visibilityLabel(value: string, t: (key: MessageKey) => string) {
  if (value === "Connections") {
    return t("personal.profile.visibilityConnections");
  }
  if (value === "Public") {
    return t("personal.profile.visibilityPublic");
  }
  return t("personal.profile.visibilityPrivate");
}

function ReadOnlyField({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex min-w-0 flex-col gap-1 text-[length:var(--exits-text-sm)]">
      <span className="font-semibold">{label}</span>
      <span>{value.trim() || "—"}</span>
    </div>
  );
}

function ProfilePhotoPreview({
  url,
  name,
  compact = false,
}: {
  url: string;
  name: string;
  compact?: boolean;
}) {
  const src = profilePhotoSrc(url);
  const initials = initialsFor(name);
  return (
    <span
      className={compact ? "personal-profile-header__photo personal-profile-header__photo--compact" : "personal-profile-header__photo"}
      data-testid={compact ? "personal-profile-photo-preview" : "personal-profile-header-photo"}
    >
      {src ? <img src={src} alt="" /> : initials === "?" ? "—" : initials}
    </span>
  );
}

function SaveButton({ pending, onClick, label }: { pending: boolean; onClick: () => void; label: string }) {
  return (
    <div className="personal-profile-actions">
      <Button type="button" onClick={onClick} disabled={pending} data-testid="personal-profile-save">
        {label}
      </Button>
    </div>
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
  tone?: "personal" | "address" | "privacy";
  wide?: boolean;
}) {
  return (
    <section
      className={cn(
        "catalog-form-section personal-profile-section flex flex-col gap-3",
        tone === "personal" && "personal-profile-section--personal",
        tone === "address" && "personal-profile-section--address",
        tone === "privacy" && "personal-profile-section--privacy",
        wide && "personal-profile-section--wide",
      )}
    >
      <h2 className="catalog-form-section__title">{title}</h2>
      <div className="personal-profile-fields">{children}</div>
    </section>
  );
}

function WideField({ children }: { children: ReactNode }) {
  return <div className="personal-profile-field--wide">{children}</div>;
}

function VisibilityField({
  label,
  value,
  onChange,
  t,
}: {
  label: string;
  value: string;
  onChange: (value: string) => void;
  t: (key: MessageKey) => string;
}) {
  return (
    <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
      <span className="font-semibold">{label}</span>
      <select className="exits-input personal-profile-select" value={value} onChange={(event) => onChange(event.target.value)}>
        <option value="Private">{t("personal.profile.visibilityPrivate")}</option>
        <option value="Connections">{t("personal.profile.visibilityConnections")}</option>
        <option value="Public">{t("personal.profile.visibilityPublic")}</option>
      </select>
    </label>
  );
}
