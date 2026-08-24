import { platformRequest } from "@/api/platform-http";

export type ActionCenterCategory =
  | "payment"
  | "subscription"
  | "usage"
  | "account"
  | "job"
  | "health"
  | "organization";

export type ActionCenterItem = {
  id: string;
  category: ActionCenterCategory;
  severity: "warning" | "danger" | "neutral";
  title: string;
  reason: string;
  organizationId?: string | null;
  organizationDisplayName?: string | null;
  productCode?: string | null;
  subscriptionId?: string | null;
  paymentId?: string | null;
  jobId?: string | null;
  occurredAtUtc?: string | null;
};

export type ActionCenterResponse = {
  items: ActionCenterItem[];
  totalCount: number;
};

export const ACTION_CENTER_PATH = "/api/v1/platform/admin/action-center";

export function getActionCenter(
  baseUrl: string,
  signal?: AbortSignal,
): Promise<ActionCenterResponse> {
  return platformRequest<ActionCenterResponse>(baseUrl, {
    path: ACTION_CENTER_PATH,
    signal,
  });
}

export function actionCenterItemHref(item: ActionCenterItem): string {
  if (item.category === "payment") {
    if (item.paymentId) {
      return `/admin/payments/${item.paymentId}`;
    }
    if (item.id.startsWith("summary-pending")) {
      return "/admin/payments/issues?issueType=pending-payment";
    }
    if (item.id.startsWith("summary-rejected")) {
      return "/admin/payments/issues?issueType=rejected-payment";
    }
    return "/admin/payments/issues";
  }
  if (item.category === "subscription") {
    if (item.subscriptionId) {
      return `/admin/subscriptions/${item.subscriptionId}`;
    }
    if (item.id.includes("grace")) {
      return "/admin/subscriptions?status=GracePeriod";
    }
    return "/admin/subscriptions?status=PastDue";
  }
  if (item.category === "usage") {
    if (item.organizationId && item.productCode) {
      return `/admin/usage?organizationId=${item.organizationId}&productCode=${encodeURIComponent(item.productCode)}`;
    }
    return "/admin/usage";
  }
  if (item.category === "account") {
    return "/admin/users?directory=Unassigned";
  }
  if (item.category === "job") {
    if (item.jobId) {
      return `/admin/global-catalog/imports/${item.jobId}`;
    }
    return "/admin/operations/jobs?status=Failed";
  }
  if (item.category === "health") {
    return "/admin/system-health";
  }
  return "/admin";
}
