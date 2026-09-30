import { useEffect, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Pencil } from "lucide-react";
import { useSearchParams } from "react-router-dom";
import {
  getPersonalProfile,
  updatePersonalProfile,
} from "@/api/platform/start-business-client";
import { PlatformApiError } from "@/api/platform/platform-http";
import { FormDrawer } from "@/components/exits/FormDrawer";
import { Button } from "@/components/ui/button";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingSkeleton } from "@/components/exits/FoundationStates";
import { Notice } from "@/components/exits/Notice";
import { PageHeader } from "@/components/exits/PageHeader";
import { PersonAvatar } from "@/components/exits/PersonAvatar";
import { useI18n } from "@/i18n/I18nProvider";
import { personalPageBackNav } from "@/navigation/page-back-nav";
import { useSession } from "@/session/SessionProvider";

export function PersonalProfilePage() {
  const { t } = useI18n();
  const { refreshSession } = useSession();
  const queryClient = useQueryClient();
  const [searchParams, setSearchParams] = useSearchParams();
  const startInEdit = searchParams.get("edit") === "1";

  const [editing, setEditing] = useState(startInEdit);
  const [editDisplayName, setEditDisplayName] = useState("");
  const [formError, setFormError] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);

  const profileQuery = useQuery({
    queryKey: ["personal", "profile"],
    queryFn: ({ signal }) => getPersonalProfile(signal),
  });

  useEffect(() => {
    if (profileQuery.data && editing) {
      setEditDisplayName((prev) => (prev === "" ? profileQuery.data.displayName : prev));
    }
  }, [profileQuery.data, editing]);

  useEffect(() => {
    if (startInEdit && profileQuery.data) {
      setEditing(true);
      setEditDisplayName(profileQuery.data.displayName);
    }
  }, [startInEdit, profileQuery.data]);

  const saveMutation = useMutation({
    mutationFn: (displayName: string) => updatePersonalProfile(displayName),
    onSuccess: async (data) => {
      queryClient.setQueryData(["personal", "profile"], data);
      setEditDisplayName(data.displayName);
      setFormError(null);
      setSuccessMessage(t("personal.profile.updated"));
      closeEditDrawer();
      await refreshSession();
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

  function clearEditQueryParam() {
    if (!searchParams.has("edit")) {
      return;
    }
    const next = new URLSearchParams(searchParams);
    next.delete("edit");
    setSearchParams(next, { replace: true });
  }

  function beginEdit() {
    if (!profileQuery.data || saveMutation.isPending) {
      return;
    }
    setFormError(null);
    setSuccessMessage(null);
    setEditDisplayName(profileQuery.data.displayName);
    setEditing(true);
  }

  function closeEditDrawer() {
    if (saveMutation.isPending) {
      return;
    }
    setFormError(null);
    setEditDisplayName(profileQuery.data?.displayName ?? "");
    setEditing(false);
    clearEditQueryParam();
  }

  function save() {
    if (saveMutation.isPending) {
      return;
    }
    setFormError(null);
    setSuccessMessage(null);
    saveMutation.mutate(editDisplayName);
  }

  if (profileQuery.isLoading) {
    return <LoadingSkeleton label={t("personal.profile.loading")} />;
  }

  if (profileQuery.isError || !profileQuery.data) {
    return (
      <ErrorState
        title={t("personal.profile.loadFailed")}
        detail={
          profileQuery.error instanceof PlatformApiError
            ? (profileQuery.error.problem.detail ?? profileQuery.error.message)
            : t("personal.profile.loadFailedDetail")
        }
        error={profileQuery.error}
        operation="personal.profile.load"
      />
    );
  }

  const profile = profileQuery.data;

  return (
    <div className="personal-page exits-page flex w-full min-w-0 flex-col gap-5">
      <PageHeader
        title={t("personal.profile.title")}
        description={t("personal.profile.lede")}
        backTo={personalPageBackNav.more.to}
        backLabel={t(personalPageBackNav.more.labelKey)}
        backTestId="page-header-back-profile"
        actions={
          <Button
            type="button"
            variant="outline"
            onClick={beginEdit}
            data-testid="personal-profile-edit"
            aria-label={t("personal.profile.edit")}
          >
            <Pencil className="size-4 shrink-0" aria-hidden />
            {t("personal.profile.edit")}
          </Button>
        }
      />

      {successMessage ? (
        <Notice tone="success" testId="personal-profile-success">
          {successMessage}
        </Notice>
      ) : null}

      <section
        className="rounded-[var(--exits-radius-md)] border border-border bg-surface"
        data-testid="personal-profile-card"
      >
        <div className="flex items-center gap-3 border-b border-border px-4 py-4">
          <PersonAvatar name={profile.displayName} size="lg" />
          <div className="min-w-0">
            <p className="m-0 truncate text-[length:var(--exits-text-lg)] font-semibold text-foreground">
              {profile.displayName}
            </p>
            <p className="m-0 mt-0.5 truncate text-[length:var(--exits-text-sm)] text-muted">
              {profile.email}
            </p>
          </div>
        </div>
        <div className="flex flex-col gap-4 p-4">
          <dl className="m-0 grid gap-3 text-[length:var(--exits-text-sm)]">
            <div>
              <dt className="m-0 font-semibold text-muted">{t("personal.profile.name")}</dt>
              <dd className="m-0 mt-0.5 text-foreground" data-testid="personal-profile-name-value">
                {profile.displayName}
              </dd>
            </div>
            <div>
              <dt className="m-0 font-semibold text-muted">{t("personal.profile.username")}</dt>
              <dd className="m-0 mt-0.5 text-foreground">{profile.username}</dd>
            </div>
            <div>
              <dt className="m-0 font-semibold text-muted">{t("personal.profile.email")}</dt>
              <dd className="m-0 mt-0.5 text-foreground">{profile.email}</dd>
            </div>
            <div>
              <dt className="m-0 font-semibold text-muted">{t("personal.profile.accountClass")}</dt>
              <dd className="m-0 mt-0.5 text-foreground">{profile.accountClass}</dd>
            </div>
          </dl>
        </div>
      </section>

      <FormDrawer
        open={editing}
        onOpenChange={(next) => {
          if (!next) {
            closeEditDrawer();
          }
        }}
        title={t("personal.profile.edit")}
        description={t("personal.profile.lede")}
        testId="personal-profile-edit-drawer"
        saveTestId="personal-profile-save"
        cancelTestId="personal-profile-cancel"
        closeLabel={t("personal.profile.cancel")}
        cancelLabel={t("personal.profile.cancel")}
        saveLabel={
          saveMutation.isPending ? t("personal.profile.saving") : t("personal.profile.save")
        }
        saving={saveMutation.isPending}
        saveDisabled={!editDisplayName.trim()}
        onSave={save}
        size="md"
      >
        {formError ? (
          <Notice tone="danger" testId="personal-profile-error">
            {formError}
          </Notice>
        ) : null}

        <div className="mb-4 flex items-center gap-3">
          <PersonAvatar name={editDisplayName || profile.displayName} size="lg" />
          <div className="min-w-0">
            <p className="m-0 truncate text-[length:var(--exits-text-sm)] font-semibold text-foreground">
              {editDisplayName || profile.displayName}
            </p>
            <p className="m-0 mt-0.5 truncate text-[length:var(--exits-text-sm)] text-muted">
              {profile.email}
            </p>
          </div>
        </div>

        <label className="flex flex-col gap-1.5">
          <span className="text-[length:var(--exits-text-sm)] font-semibold text-foreground">
            {t("personal.profile.name")}
          </span>
          <input
            data-testid="personal-profile-display-name"
            className="rounded-[var(--exits-radius-md)] border border-border bg-background px-3"
            value={editDisplayName}
            disabled={saveMutation.isPending}
            maxLength={100}
            autoComplete="name"
            onChange={(e) => setEditDisplayName(e.target.value)}
          />
        </label>

        <dl className="m-0 mt-4 grid gap-3 text-[length:var(--exits-text-sm)]">
          <div>
            <dt className="m-0 font-semibold text-muted">{t("personal.profile.username")}</dt>
            <dd className="m-0 mt-0.5 text-foreground">{profile.username}</dd>
          </div>
          <div>
            <dt className="m-0 font-semibold text-muted">{t("personal.profile.email")}</dt>
            <dd className="m-0 mt-0.5 text-foreground">{profile.email}</dd>
          </div>
          <div>
            <dt className="m-0 font-semibold text-muted">{t("personal.profile.accountClass")}</dt>
            <dd className="m-0 mt-0.5 text-foreground">{profile.accountClass}</dd>
          </div>
        </dl>
      </FormDrawer>
    </div>
  );
}
