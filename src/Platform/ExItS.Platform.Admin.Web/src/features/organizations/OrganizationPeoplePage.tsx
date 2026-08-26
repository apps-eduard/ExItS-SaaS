import { useMemo } from "react";
import { useParams, useSearchParams } from "react-router-dom";
import { parseOrganizationId } from "@/api/organizations/organization-id";
import {
  organizationPeopleSearchParams,
  parseOrganizationPeopleSearchParams,
  type OrganizationPeopleUrlState,
} from "@/api/organizations/people-list-query";
import {
  INVITATION_STATUSES,
  MEMBERSHIP_STATUSES,
  ORGANIZATION_PEOPLE_PAGE_SIZE,
  type OrganizationInvitation,
  type OrganizationMember,
} from "@/api/organizations/organization-types";
import { PlatformApiError } from "@/api/platform-http";
import { AdminTable } from "@/components/exits/AdminTable";
import { ErrorState } from "@/components/exits/ErrorState";
import { PageHeader } from "@/components/exits/PageHeader";
import { StatusIndicator } from "@/components/exits/StatusIndicator";
import { DashboardWidgetSkeleton } from "@/components/exits/dashboard/DashboardWidgetSkeleton";
import { Button } from "@/components/ui/button";
import {
  useOrganizationInvitationsQuery,
  useOrganizationMembersQuery,
} from "@/features/organizations/use-organization-workspace-queries";
import {
  OrganizationInvitationActions,
  OrganizationMemberActions,
  OrganizationPeopleInviteButton,
} from "@/features/organizations/OrganizationPeopleOperator";
import { useMediaQuery } from "@/hooks/use-media-query";
import { usePreferences } from "@/hooks/use-preferences";
import { normalizeDiagnosticError } from "@/lib/diagnostics/normalize-diagnostic-error";
import type { MessageKey } from "@/lib/i18n/messages";

const MEMBER_STATUS_LABELS: Record<string, MessageKey> = {
  Active: "organization.people.member.status.Active",
  Suspended: "organization.people.member.status.Suspended",
  Removed: "organization.people.member.status.Removed",
};

const INVITATION_STATUS_LABELS: Record<string, MessageKey> = {
  Pending: "organization.people.invitation.status.Pending",
  Accepted: "organization.people.invitation.status.Accepted",
  Revoked: "organization.people.invitation.status.Revoked",
  Expired: "organization.people.invitation.status.Expired",
  Sent: "organization.people.invitation.status.Sent",
};

const controlClass =
  "h-[var(--exits-control-height)] min-h-[var(--exits-touch-target-min)] rounded-[var(--exits-density-radius)] border border-input bg-surface px-3 text-[length:var(--exits-text-sm)] text-foreground";

function statusTone(status: string): "success" | "warning" | "danger" | "neutral" {
  if (status === "Active" || status === "Accepted" || status === "Sent") {
    return "success";
  }
  if (status === "Suspended" || status === "Pending" || status === "Expired") {
    return "warning";
  }
  if (status === "Removed" || status === "Revoked") {
    return "danger";
  }
  return "neutral";
}

function formatInstant(value: string | undefined, language: string): string | null {
  if (!value) {
    return null;
  }
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return value;
  }
  return new Intl.DateTimeFormat(language === "fil-PH" ? "fil-PH" : "en-GB", {
    dateStyle: "medium",
  }).format(date);
}

function memberLabel(member: OrganizationMember): string {
  return member.displayName || member.email || member.username || member.userId;
}

function invitationContact(invitation: OrganizationInvitation): string {
  return invitation.inviteeDisplayName
    ? `${invitation.inviteeDisplayName} (${invitation.email})`
    : invitation.email;
}

function isForbidden(error: unknown): boolean {
  return error instanceof PlatformApiError && (error.status === 401 || error.status === 403);
}

export function OrganizationPeoplePage() {
  const { t, language } = usePreferences();
  const params = useParams();
  const organizationId = parseOrganizationId(params.organizationId);
  const showTable = useMediaQuery("(min-width: 768px)");
  const [searchParams, setSearchParams] = useSearchParams();
  const state = useMemo(() => parseOrganizationPeopleSearchParams(searchParams), [searchParams]);

  const membersQuery = useOrganizationMembersQuery(organizationId, {
    page: state.membersPage,
    status: state.membersStatus || undefined,
  });
  const invitationsQuery = useOrganizationInvitationsQuery(organizationId, {
    page: state.invitationsPage,
    status: state.invitationsStatus || undefined,
  });

  function replaceState(patch: Partial<OrganizationPeopleUrlState>) {
    const current = parseOrganizationPeopleSearchParams(
      new URLSearchParams(window.location.search),
    );
    setSearchParams(organizationPeopleSearchParams({ ...current, ...patch }), { replace: true });
  }

  return (
    <section className="grid max-w-3xl gap-4">
      <PageHeader
        title={t("organization.people.title")}
        description={t("organization.people.description")}
        actions={organizationId ? <OrganizationPeopleInviteButton organizationId={organizationId} /> : undefined}
      />

      <div
        className="flex flex-wrap gap-1"
        role="tablist"
        aria-label={t("organization.people.tabs")}
      >
        <button
          type="button"
          role="tab"
          id="people-tab-members"
          aria-selected={state.tab === "members"}
          aria-controls="people-panel-members"
          className={`rounded-[var(--exits-density-radius)] px-2 py-1 text-[length:var(--exits-text-sm)] font-medium ${
            state.tab === "members"
              ? "bg-surface-muted text-foreground"
              : "text-muted hover:bg-surface-muted/70 hover:text-foreground"
          }`}
          onClick={() => replaceState({ tab: "members" })}
        >
          {t("organization.people.tab.members")}
        </button>
        <button
          type="button"
          role="tab"
          id="people-tab-invitations"
          aria-selected={state.tab === "invitations"}
          aria-controls="people-panel-invitations"
          className={`rounded-[var(--exits-density-radius)] px-2 py-1 text-[length:var(--exits-text-sm)] font-medium ${
            state.tab === "invitations"
              ? "bg-surface-muted text-foreground"
              : "text-muted hover:bg-surface-muted/70 hover:text-foreground"
          }`}
          onClick={() => replaceState({ tab: "invitations" })}
        >
          {t("organization.people.tab.invitations")}
        </button>
      </div>

      {state.tab === "members" ? (
        <div id="people-panel-members" role="tabpanel" aria-labelledby="people-tab-members">
          <PeopleMembersPanel
            organizationId={organizationId}
            query={membersQuery}
            state={state}
            showTable={showTable}
            onStatus={(membersStatus) => replaceState({ membersStatus, membersPage: 1 })}
            onPage={(membersPage) => replaceState({ membersPage })}
          />
        </div>
      ) : (
        <div id="people-panel-invitations" role="tabpanel" aria-labelledby="people-tab-invitations">
          <PeopleInvitationsPanel
            organizationId={organizationId}
            query={invitationsQuery}
            state={state}
            showTable={showTable}
            language={language}
            onStatus={(invitationsStatus) =>
              replaceState({ invitationsStatus, invitationsPage: 1 })
            }
            onPage={(invitationsPage) => replaceState({ invitationsPage })}
          />
        </div>
      )}
    </section>
  );
}

function PeopleMembersPanel({
  organizationId,
  query,
  state,
  showTable,
  onStatus,
  onPage,
}: {
  organizationId: string | null;
  query: ReturnType<typeof useOrganizationMembersQuery>;
  state: OrganizationPeopleUrlState;
  showTable: boolean;
  onStatus: (status: OrganizationPeopleUrlState["membersStatus"]) => void;
  onPage: (page: number) => void;
}) {
  const { t } = usePreferences();
  const diagnostic = query.error
    ? normalizeDiagnosticError({
        error: query.error,
        operation: "Load organization members",
      })
    : null;
  const totalPages = query.data
    ? Math.max(1, Math.ceil(query.data.totalCount / ORGANIZATION_PEOPLE_PAGE_SIZE))
    : 1;

  return (
    <div className="grid gap-3">
      <label
        className="grid max-w-xs gap-1 text-[length:var(--exits-text-xs)] font-medium text-muted"
        htmlFor="people-members-status"
      >
        {t("organization.people.members.status")}
        <select
          id="people-members-status"
          className={controlClass}
          value={state.membersStatus}
          onChange={(event) =>
            onStatus(event.target.value as OrganizationPeopleUrlState["membersStatus"])
          }
        >
          <option value="">{t("organization.people.status.all")}</option>
          {MEMBERSHIP_STATUSES.map((status) => (
            <option key={status} value={status}>
              {t(MEMBER_STATUS_LABELS[status]!)}
            </option>
          ))}
        </select>
      </label>

      {query.isPending ? (
        <div
          className="rounded-[var(--exits-density-radius)] border border-border bg-surface px-4 py-3"
          role="status"
          aria-busy="true"
          aria-label={t("organization.people.members.loading")}
        >
          <DashboardWidgetSkeleton rows={5} />
        </div>
      ) : null}

      {query.isError && isForbidden(query.error) ? (
        <p className="rounded-[var(--exits-density-radius)] border border-border bg-surface px-4 py-3 text-[length:var(--exits-text-sm)] text-muted">
          {t("organization.people.unavailable")}
        </p>
      ) : null}

      {query.isError && !isForbidden(query.error) && diagnostic ? (
        <ErrorState
          diagnostic={diagnostic}
          title={t("organization.people.members.error")}
          headingLevel="h2"
          onRetry={() => void query.refetch()}
        />
      ) : null}

      {query.data ? (
        <>
          {showTable ? (
            <div className="rounded-[var(--exits-density-radius)] border border-border bg-surface px-4 py-3">
              <AdminTable
                caption={t("organization.people.members.caption")}
                empty={t("organization.people.members.empty")}
                columns={[
                  {
                    id: "person",
                    header: t("organization.people.column.person"),
                    cell: (member) => <span className="font-medium">{memberLabel(member)}</span>,
                  },
                  {
                    id: "identity",
                    header: t("organization.people.column.identity"),
                    cell: (member) => (
                      <span className="break-words text-muted">
                        {[member.email, member.username, member.employeeCode]
                          .filter(Boolean)
                          .join(" · ") || "—"}
                      </span>
                    ),
                  },
                  {
                    id: "role",
                    header: t("organization.people.column.role"),
                    cell: (member) => member.roleDisplay || member.role,
                  },
                  {
                    id: "status",
                    header: t("organization.people.column.status"),
                    cell: (member) => (
                      <StatusIndicator
                        tone={statusTone(member.status)}
                        label={
                          MEMBER_STATUS_LABELS[member.status]
                            ? t(MEMBER_STATUS_LABELS[member.status]!)
                            : member.status
                        }
                      />
                    ),
                  },
                  ...(organizationId
                    ? [
                        {
                          id: "actions",
                          header: t("organization.productAccess.column.actions"),
                          cell: (member: OrganizationMember) => (
                            <OrganizationMemberActions organizationId={organizationId} member={member} />
                          ),
                        },
                      ]
                    : []),
                ]}
                rows={query.data.items}
              />
            </div>
          ) : (
            <ul className="grid gap-2">
              {query.data.items.length === 0 ? (
                <li className="rounded-[var(--exits-density-radius)] border border-border bg-surface px-3 py-3 text-[length:var(--exits-text-sm)] text-muted">
                  {t("organization.people.members.empty")}
                </li>
              ) : (
                query.data.items.map((member) => (
                  <li
                    key={member.id}
                    className="rounded-[var(--exits-density-radius)] border border-border bg-surface px-3 py-2.5"
                  >
                    <p className="font-medium">{memberLabel(member)}</p>
                    {member.email || member.username ? (
                      <p className="mt-0.5 break-words text-[length:var(--exits-text-xs)] text-muted">
                        {[member.email, member.username].filter(Boolean).join(" · ")}
                      </p>
                    ) : null}
                    <div className="mt-1.5 flex flex-wrap items-center gap-2">
                      <StatusIndicator
                        tone={statusTone(member.status)}
                        label={
                          MEMBER_STATUS_LABELS[member.status]
                            ? t(MEMBER_STATUS_LABELS[member.status]!)
                            : member.status
                        }
                      />
                      <span className="text-[length:var(--exits-text-xs)] text-muted">
                        {member.roleDisplay || member.role}
                      </span>
                    </div>
                  </li>
                ))
              )}
            </ul>
          )}
          <PeoplePager page={state.membersPage} totalPages={totalPages} onPage={onPage} />
        </>
      ) : null}
    </div>
  );
}

function PeopleInvitationsPanel({
  organizationId,
  query,
  state,
  showTable,
  language,
  onStatus,
  onPage,
}: {
  organizationId: string | null;
  query: ReturnType<typeof useOrganizationInvitationsQuery>;
  state: OrganizationPeopleUrlState;
  showTable: boolean;
  language: string;
  onStatus: (status: OrganizationPeopleUrlState["invitationsStatus"]) => void;
  onPage: (page: number) => void;
}) {
  const { t } = usePreferences();
  const diagnostic = query.error
    ? normalizeDiagnosticError({
        error: query.error,
        operation: "Load organization invitations",
      })
    : null;
  const totalPages = query.data
    ? Math.max(1, Math.ceil(query.data.totalCount / ORGANIZATION_PEOPLE_PAGE_SIZE))
    : 1;

  return (
    <div className="grid gap-3">
      <label
        className="grid max-w-xs gap-1 text-[length:var(--exits-text-xs)] font-medium text-muted"
        htmlFor="people-invitations-status"
      >
        {t("organization.people.invitations.status")}
        <select
          id="people-invitations-status"
          className={controlClass}
          value={state.invitationsStatus}
          onChange={(event) =>
            onStatus(event.target.value as OrganizationPeopleUrlState["invitationsStatus"])
          }
        >
          <option value="">{t("organization.people.status.all")}</option>
          {INVITATION_STATUSES.map((status) => (
            <option key={status} value={status}>
              {t(INVITATION_STATUS_LABELS[status]!)}
            </option>
          ))}
        </select>
      </label>

      {query.isPending ? (
        <div
          className="rounded-[var(--exits-density-radius)] border border-border bg-surface px-4 py-3"
          role="status"
          aria-busy="true"
          aria-label={t("organization.people.invitations.loading")}
        >
          <DashboardWidgetSkeleton rows={5} />
        </div>
      ) : null}

      {query.isError && isForbidden(query.error) ? (
        <p className="rounded-[var(--exits-density-radius)] border border-border bg-surface px-4 py-3 text-[length:var(--exits-text-sm)] text-muted">
          {t("organization.people.unavailable")}
        </p>
      ) : null}

      {query.isError && !isForbidden(query.error) && diagnostic ? (
        <ErrorState
          diagnostic={diagnostic}
          title={t("organization.people.invitations.error")}
          headingLevel="h2"
          onRetry={() => void query.refetch()}
        />
      ) : null}

      {query.data ? (
        <>
          {showTable ? (
            <div className="rounded-[var(--exits-density-radius)] border border-border bg-surface px-4 py-3">
              <AdminTable
                caption={t("organization.people.invitations.caption")}
                empty={t("organization.people.invitations.empty")}
                columns={[
                  {
                    id: "contact",
                    header: t("organization.people.column.contact"),
                    cell: (invitation) => (
                      <span className="break-words font-medium">
                        {invitationContact(invitation)}
                      </span>
                    ),
                  },
                  {
                    id: "role",
                    header: t("organization.people.column.role"),
                    cell: (invitation) => invitation.roleDisplay || invitation.role,
                  },
                  {
                    id: "status",
                    header: t("organization.people.column.status"),
                    cell: (invitation) => {
                      const display = invitation.invitationStatus || invitation.status;
                      return (
                        <StatusIndicator
                          tone={statusTone(display)}
                          label={
                            INVITATION_STATUS_LABELS[display]
                              ? t(INVITATION_STATUS_LABELS[display]!)
                              : display
                          }
                        />
                      );
                    },
                  },
                  {
                    id: "expires",
                    header: t("organization.people.column.expires"),
                    cell: (invitation) => formatInstant(invitation.expiresAtUtc, language) || "—",
                  },
                  ...(organizationId
                    ? [
                        {
                          id: "actions",
                          header: t("organization.productAccess.column.actions"),
                          cell: (invitation: OrganizationInvitation) => (
                            <OrganizationInvitationActions
                              organizationId={organizationId}
                              invitation={invitation}
                            />
                          ),
                        },
                      ]
                    : []),
                ]}
                rows={query.data.items}
              />
            </div>
          ) : (
            <ul className="grid gap-2">
              {query.data.items.length === 0 ? (
                <li className="rounded-[var(--exits-density-radius)] border border-border bg-surface px-3 py-3 text-[length:var(--exits-text-sm)] text-muted">
                  {t("organization.people.invitations.empty")}
                </li>
              ) : (
                query.data.items.map((invitation) => {
                  const display = invitation.invitationStatus || invitation.status;
                  return (
                    <li
                      key={invitation.id}
                      className="rounded-[var(--exits-density-radius)] border border-border bg-surface px-3 py-2.5"
                    >
                      <p className="break-words font-medium">{invitationContact(invitation)}</p>
                      <div className="mt-1.5 flex flex-wrap items-center gap-2">
                        <StatusIndicator
                          tone={statusTone(display)}
                          label={
                            INVITATION_STATUS_LABELS[display]
                              ? t(INVITATION_STATUS_LABELS[display]!)
                              : display
                          }
                        />
                        <span className="text-[length:var(--exits-text-xs)] text-muted">
                          {invitation.roleDisplay || invitation.role}
                        </span>
                      </div>
                      {formatInstant(invitation.expiresAtUtc, language) ? (
                        <p className="mt-1 text-[length:var(--exits-text-xs)] text-muted">
                          {t("organization.people.column.expires")}:{" "}
                          {formatInstant(invitation.expiresAtUtc, language)}
                        </p>
                      ) : null}
                    </li>
                  );
                })
              )}
            </ul>
          )}
          <PeoplePager page={state.invitationsPage} totalPages={totalPages} onPage={onPage} />
        </>
      ) : null}
    </div>
  );
}

function PeoplePager({
  page,
  totalPages,
  onPage,
}: {
  page: number;
  totalPages: number;
  onPage: (page: number) => void;
}) {
  const { t } = usePreferences();
  return (
    <div className="flex flex-wrap items-center gap-2">
      <Button
        type="button"
        size="sm"
        variant="outline"
        disabled={page <= 1}
        onClick={() => onPage(page - 1)}
      >
        {t("organizations.previous")}
      </Button>
      <p className="text-[length:var(--exits-text-xs)] text-muted">
        {t("organizations.page")} {page} / {totalPages}
      </p>
      <Button
        type="button"
        size="sm"
        variant="outline"
        disabled={page >= totalPages}
        onClick={() => onPage(page + 1)}
      >
        {t("organizations.next")}
      </Button>
    </div>
  );
}
