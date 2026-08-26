import {
  ASSIGNMENTS_PAGE_SIZE,
  ASSIGNMENT_STATUSES,
  type AssignmentStatus,
  type RoleAssignmentsQuery,
} from "@/api/authorization/assignment-types";
import { withQuery } from "@/lib/http/query-string";

export type AssignmentsUrlState = {
  page: number;
  status: AssignmentStatus | "";
};

export function isAssignmentStatus(value: string): value is AssignmentStatus {
  return (ASSIGNMENT_STATUSES as readonly string[]).includes(value);
}

export function parseAssignmentsSearchParams(params: URLSearchParams): AssignmentsUrlState {
  const pageRaw = Number(params.get("assignmentsPage") ?? "1");
  const page = Number.isFinite(pageRaw) && pageRaw >= 1 ? Math.floor(pageRaw) : 1;
  const statusRaw = params.get("assignmentsStatus") ?? "";
  return {
    page,
    status: isAssignmentStatus(statusRaw) ? statusRaw : "",
  };
}

export function assignmentsSearchParams(state: AssignmentsUrlState): URLSearchParams {
  const params = new URLSearchParams();
  if (state.status) {
    params.set("assignmentsStatus", state.status);
  }
  if (state.page > 1) {
    params.set("assignmentsPage", String(state.page));
  }
  return params;
}

export function roleAssignmentsRequestPath(query: RoleAssignmentsQuery): string {
  return withQuery("/api/v1/platform/authorization/assignments", {
    platformUserId: query.platformUserId,
    role: query.role,
    organizationId: query.organizationId,
    status: query.status,
    page: query.page ?? 1,
    pageSize: query.pageSize ?? ASSIGNMENTS_PAGE_SIZE,
  });
}
