import { useEffect, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useSearchParams } from "react-router-dom";
import {
  getPersonalProfile,
  updatePersonalProfile,
} from "@/api/platform/start-business-client";
import { PlatformApiError } from "@/api/platform/platform-http";
import { Button } from "@/components/ui/button";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingSkeleton } from "@/components/exits/FoundationStates";
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
    if (profileQuery.data && editing && editDisplayName === "") {
      setEditDisplayName(profileQuery.data.displayName);
    }
  }, [profileQuery.data, editing, editDisplayName]);

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
      setEditing(false);
      if (searchParams.has("edit")) {
        const next = new URLSearchParams(searchParams);
        next.delete("edit");
        setSearchParams(next, { replace: true });
      }
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

  function beginEdit() {
    if (!profileQuery.data || saveMutation.isPending) {
      return;
    }
    setFormError(null);
    setSuccessMessage(null);
    setEditDisplayName(profileQuery.data.displayName);
    setEditing(true);
  }

  function cancelEdit() {
    if (saveMutation.isPending) {
      return;
    }
    setFormError(null);
    setEditDisplayName(profileQuery.data?.displayName ?? "");
    setEditing(false);
    if (searchParams.has("edit")) {
      const next = new URLSearchParams(searchParams);
      next.delete("edit");
      setSearchParams(next, { replace: true });
    }
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
    <div className="mx-auto flex w-full max-w-lg min-w-0 flex-col gap-5">
      <PageHeader
        title={t("personal.profile.title")}
        description={t("personal.profile.lede")}
        backTo={personalPageBackNav.more.to}
        backLabel={t(personalPageBackNav.more.labelKey)}
        backTestId="page-header-back-profile"
      />

      {successMessage ? (
        <p
          className="m-0 rounded-[var(--exits-radius-md)] border border-border bg-[var(--exits-surface-muted)] px-3 py-2 text-[length:var(--exits-text-sm)] text-foreground"
          data-testid="personal-profile-success"
          role="status"
        >
          {successMessage}
        </p>
      ) : null}

      {formError ? (
        <p
          className="m-0 rounded-[var(--exits-radius-md)] border border-destructive/40 bg-destructive/10 px-3 py-2 text-[length:var(--exits-text-sm)] text-destructive"
          data-testid="personal-profile-error"
          role="alert"
        >
          {formError}
        </p>
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
        {editing ? (
          <div className="flex flex-col gap-4 p-4">
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
                autoFocus
                onChange={(e) => setEditDisplayName(e.target.value)}
                onKeyDown={(e) => {
                  if (e.key === "Enter") {
                    e.preventDefault();
                    save();
                  }
                }}
              />
            </label>
            <dl className="m-0 grid gap-3 text-[length:var(--exits-text-sm)]">
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
            <div className="flex flex-wrap gap-2">
              <Button
                type="button"
                variant="outline"
                disabled={saveMutation.isPending}
                onClick={cancelEdit}
                data-testid="personal-profile-cancel"
              >
                {t("personal.profile.cancel")}
              </Button>
              <Button
                type="button"
                disabled={saveMutation.isPending}
                onClick={save}
                data-testid="personal-profile-save"
              >
                {saveMutation.isPending
                  ? t("personal.profile.saving")
                  : t("personal.profile.save")}
              </Button>
            </div>
          </div>
        ) : (
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
            <Button
              type="button"
              onClick={beginEdit}
              data-testid="personal-profile-edit"
            >
              {t("personal.profile.edit")}
            </Button>
          </div>
        )}
      </section>
    </div>
  );
}
