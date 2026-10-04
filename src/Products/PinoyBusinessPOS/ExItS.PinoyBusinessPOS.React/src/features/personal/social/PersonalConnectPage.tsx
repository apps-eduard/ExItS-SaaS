import { useEffect, useState, type ReactNode } from "react";
import { useQuery } from "@tanstack/react-query";
import { Link, Navigate, useParams } from "react-router-dom";
import { PlatformApiError } from "@/api/platform/platform-http";
import {
  createPersonalContact,
  listPersonalConnectionRequests,
  listPersonalContacts,
  requestPersonalConnection,
  resolvePublicUserId,
} from "@/api/platform/personal-people-client";
import type { PersonalContactDto, ResolvedPublicUserDto } from "@/api/platform/personal-types";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingSkeleton } from "@/components/exits/FoundationStates";
import { useI18n } from "@/i18n/I18nProvider";
import { isPublicUserId, normalizePublicUserId } from "@/lib/exits-qr/envelope";
import { buildPersonalConnectPath } from "@/lib/personal-connect-url";
import {
  findExistingContact,
  isAlreadyAddedConflict,
  isPublicUserNotFound,
} from "@/features/personal/person-form-helpers";
import { deriveConnectionStatus } from "@/features/personal/people-status";
import {
  clearPersonalConnectIntent,
  rememberPersonalConnectIntent,
} from "@/features/personal/social/personal-connect-intent";
import { useSession, type SessionStatus } from "@/session/SessionProvider";

function isSignedOut(status: SessionStatus): boolean {
  return status !== "loading" && status !== "authenticated";
}

function initials(displayName: string): string {
  const parts = displayName.trim().split(/\s+/).filter(Boolean).slice(0, 2);
  const letters = parts.map((part) => part[0]?.toUpperCase() ?? "").join("");
  return letters || "?";
}

function isBlockedConnection(error: unknown): boolean {
  return (
    error instanceof PlatformApiError &&
    error.errorCode === "application.personal.connection.blocked"
  );
}

function isConnectionConflict(error: unknown): boolean {
  return (
    error instanceof PlatformApiError &&
    error.errorCode === "application.personal.connection_request.conflict"
  );
}

export function PersonalConnectPage() {
  const { t } = useI18n();
  const { publicUserId: rawId = "" } = useParams();
  const { status } = useSession();
  const publicUserId = normalizePublicUserId(rawId);
  const valid = isPublicUserId(publicUserId);
  const [phase, setPhase] = useState<"confirm" | "sent" | "blocked" | "conflict">("confirm");
  const [submitting, setSubmitting] = useState(false);
  const [submitError, setSubmitError] = useState<string | null>(null);

  useEffect(() => {
    if (!valid) {
      return;
    }
    if (status === "authenticated") {
      clearPersonalConnectIntent();
      return;
    }
    if (status !== "loading") {
      rememberPersonalConnectIntent(publicUserId);
    }
  }, [publicUserId, status, valid]);

  const resolveQuery = useQuery({
    queryKey: ["personal", "connect-target", publicUserId],
    enabled: valid && status === "authenticated",
    queryFn: ({ signal }) => resolvePublicUserId(publicUserId, "personal-connection", signal),
    retry: false,
  });
  const contactsQuery = useQuery({
    queryKey: ["personal", "connect-contacts"],
    enabled: valid && status === "authenticated" && resolveQuery.isSuccess,
    queryFn: ({ signal }) => listPersonalContacts(signal),
  });
  const requestsQuery = useQuery({
    queryKey: ["personal", "connect-requests"],
    enabled: valid && status === "authenticated" && resolveQuery.isSuccess,
    queryFn: ({ signal }) => listPersonalConnectionRequests(signal),
  });

  if (!valid) {
    return (
      <main className="mx-auto max-w-md p-6" data-testid="personal-connect-page">
        <ErrorState title={t("personal.connect.invalid")} detail={t("personal.connect.unavailable")} />
      </main>
    );
  }

  if (status === "loading") {
    return <LoadingSkeleton />;
  }

  if (isSignedOut(status)) {
    const continuePath = buildPersonalConnectPath(publicUserId);
    return (
      <Navigate
        to={`/sign-in?continue=${encodeURIComponent(continuePath)}`}
        replace
      />
    );
  }

  if (resolveQuery.isPending) {
    return <LoadingSkeleton />;
  }

  if (resolveQuery.isError) {
    return (
      <main className="mx-auto max-w-md p-6" data-testid="personal-connect-page">
        <ErrorState
          title={
            isPublicUserNotFound(resolveQuery.error)
              ? t("personal.connect.unavailable")
              : t("personal.connect.unavailable")
          }
          detail={t("personal.connect.unavailable")}
        />
      </main>
    );
  }

  const resolved = resolveQuery.data as ResolvedPublicUserDto;
  if (contactsQuery.isPending || requestsQuery.isPending) {
    return <LoadingSkeleton />;
  }

  if (resolved.isSelf) {
    return (
      <main className="mx-auto max-w-md p-6" data-testid="personal-connect-page">
        <Card className="flex flex-col gap-3 p-4">
          <p className="m-0 font-semibold" data-testid="personal-connect-self">
            {t("personal.connect.self")}
          </p>
          <p className="m-0" data-testid="personal-connect-id">
            {resolved.publicUserId}
          </p>
          <ContinueHome />
        </Card>
      </main>
    );
  }

  const existing = findExistingContact(contactsQuery.data, resolved);
  const connection = existing
    ? deriveConnectionStatus(existing, requestsQuery.data ?? [])
    : { status: "not_connected" as const };

  if (connection.status === "blocked" || phase === "blocked") {
    return (
      <ConnectShell name={resolved.displayName} publicId={resolved.publicUserId}>
        <p data-testid="personal-connect-blocked">{t("personal.connect.blocked")}</p>
        <ContinueHome />
      </ConnectShell>
    );
  }

  if (connection.status === "connected") {
    return (
      <ConnectShell name={resolved.displayName} publicId={resolved.publicUserId}>
        <p data-testid="personal-connect-already">{t("personal.connect.alreadyConnected")}</p>
        <ContinueHome />
      </ConnectShell>
    );
  }

  if (connection.status === "request_sent" || phase === "sent" || phase === "conflict") {
    return (
      <ConnectShell name={resolved.displayName} publicId={resolved.publicUserId}>
        <p data-testid="personal-connect-sent">
          {phase === "sent" ? t("personal.connect.sent") : t("personal.connect.requestSent")}
        </p>
        <ContinueHome />
      </ConnectShell>
    );
  }

  if (connection.status === "request_received") {
    return (
      <ConnectShell name={resolved.displayName} publicId={resolved.publicUserId}>
        <p data-testid="personal-connect-received">{t("personal.connect.requestReceived")}</p>
        <Link to="/personal/invitations" data-testid="personal-connect-invitations">
          {t("personal.connect.viewInvitations")}
        </Link>
        <ContinueHome />
      </ConnectShell>
    );
  }

  async function connect() {
    setSubmitting(true);
    setSubmitError(null);
    try {
      const contact = await ensureContact(existing, resolved);
      await requestPersonalConnection(contact.id);
      clearPersonalConnectIntent();
      setPhase("sent");
    } catch (error) {
      if (isBlockedConnection(error)) {
        setPhase("blocked");
        return;
      }
      if (isConnectionConflict(error)) {
        setPhase("conflict");
        return;
      }
      setSubmitError(t("personal.connect.unavailable"));
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <ConnectShell name={resolved.displayName} publicId={resolved.publicUserId}>
      <p className="m-0 text-[length:var(--exits-text-lg)] font-semibold" data-testid="personal-connect-confirm">
        {t("personal.connect.with")} {resolved.displayName}?
      </p>
      {submitError ? (
        <p role="alert" className="m-0 text-destructive">
          {submitError}
        </p>
      ) : null}
      <div className="flex flex-wrap gap-2">
        <Button
          type="button"
          data-testid="personal-connect-submit"
          disabled={submitting}
          onClick={() => void connect()}
        >
          {t("personal.connect.action")}
        </Button>
        <Button type="button" variant="ghost" data-testid="personal-connect-cancel" asChild>
          <Link to="/personal" onClick={() => clearPersonalConnectIntent()}>
            {t("personal.connect.cancel")}
          </Link>
        </Button>
      </div>
    </ConnectShell>
  );
}

async function ensureContact(
  existing: PersonalContactDto | null,
  resolved: ResolvedPublicUserDto,
): Promise<PersonalContactDto> {
  if (existing) {
    return existing;
  }
  try {
    return await createPersonalContact({
      displayName: resolved.displayName,
      resolvedUserIdentityId: resolved.userIdentityId,
      resolvedPublicUserId: resolved.publicUserId,
    });
  } catch (error) {
    if (!isAlreadyAddedConflict(error)) {
      throw error;
    }
    const contacts = await listPersonalContacts();
    const found = findExistingContact(contacts, resolved);
    if (!found) {
      throw error;
    }
    return found;
  }
}

function ContinueHome() {
  const { t } = useI18n();
  return (
    <Button type="button" data-testid="personal-connect-continue" asChild>
      <Link to="/personal" onClick={() => clearPersonalConnectIntent()}>
        {t("personal.connect.continue")}
      </Link>
    </Button>
  );
}

function ConnectShell({
  name,
  publicId,
  children,
}: {
  name: string;
  publicId: string;
  children: ReactNode;
}) {
  return (
    <main className="mx-auto flex max-w-md flex-col gap-3 p-6" data-testid="personal-connect-page">
      <Card className="flex flex-col gap-3 p-4">
        <div
          className="flex size-12 items-center justify-center rounded-full bg-[var(--exits-surface-muted)] font-semibold"
          aria-hidden
          data-testid="personal-connect-initials"
        >
          {initials(name)}
        </div>
        <p className="m-0 font-semibold" data-testid="personal-connect-name">
          {name}
        </p>
        <p className="m-0" data-testid="personal-connect-id">
          {publicId}
        </p>
        {children}
      </Card>
    </main>
  );
}
