import type { WorkingExperience } from "@/workspace/working-experience";
import type { WorkspaceDestination } from "@/workspace/workspace-destinations";

const NOTIFICATIONS_RETURN_STORAGE_KEY = "exits.notifications.returnTo";

export type NotificationsWorkspaceReturn = {
  organizationId: string;
  organizationDisplayName: string;
  branchId: string | null;
  branchName: string | null;
  experience: WorkingExperience;
};

export type NotificationsReturnContext = {
  returnTo: string;
  workspace?: NotificationsWorkspaceReturn;
};

export type NotificationsLocationState = {
  returnTo?: string;
  workspace?: NotificationsWorkspaceReturn;
};

/** Safe in-app path to restore after closing notifications. */
export function isNotificationsReturnPath(path: string | null | undefined): path is string {
  if (!path || !path.startsWith("/") || path.startsWith("//")) {
    return false;
  }
  if (path === "/personal/notifications" || path.startsWith("/personal/notifications?")) {
    return false;
  }
  if (path === "/personal/notifications/archived" || path.startsWith("/personal/notifications/archived?")) {
    return false;
  }
  return true;
}

function isWorkingExperience(value: unknown): value is WorkingExperience {
  return value === "manage_business" || value === "operations" || value === "start_selling";
}

function parseWorkspace(value: unknown): NotificationsWorkspaceReturn | undefined {
  if (!value || typeof value !== "object") {
    return undefined;
  }
  const raw = value as Record<string, unknown>;
  if (
    typeof raw.organizationId !== "string" ||
    typeof raw.organizationDisplayName !== "string" ||
    !isWorkingExperience(raw.experience)
  ) {
    return undefined;
  }
  if (raw.branchId !== null && typeof raw.branchId !== "string") {
    return undefined;
  }
  if (raw.branchName !== null && typeof raw.branchName !== "string") {
    return undefined;
  }
  return {
    organizationId: raw.organizationId,
    organizationDisplayName: raw.organizationDisplayName,
    branchId: raw.branchId,
    branchName: raw.branchName,
    experience: raw.experience,
  };
}

function parseContext(value: unknown): NotificationsReturnContext | null {
  if (!value || typeof value !== "object") {
    return null;
  }
  const raw = value as NotificationsLocationState;
  if (!isNotificationsReturnPath(raw.returnTo)) {
    return null;
  }
  return {
    returnTo: raw.returnTo,
    workspace: parseWorkspace(raw.workspace),
  };
}

export function rememberNotificationsReturnTo(
  path: string,
  workspace?: NotificationsWorkspaceReturn | null,
): void {
  if (!isNotificationsReturnPath(path)) {
    return;
  }
  const context: NotificationsReturnContext = {
    returnTo: path,
    ...(workspace
      ? {
          workspace: {
            organizationId: workspace.organizationId,
            organizationDisplayName: workspace.organizationDisplayName,
            branchId: workspace.branchId,
            branchName: workspace.branchName,
            experience: workspace.experience,
          },
        }
      : {}),
  };
  try {
    sessionStorage.setItem(NOTIFICATIONS_RETURN_STORAGE_KEY, JSON.stringify(context));
  } catch {
    // Ignore quota / private-mode failures; history back still works.
  }
}

export function takeNotificationsReturnTo(): NotificationsReturnContext | null {
  try {
    const stored = sessionStorage.getItem(NOTIFICATIONS_RETURN_STORAGE_KEY);
    sessionStorage.removeItem(NOTIFICATIONS_RETURN_STORAGE_KEY);
    if (!stored) {
      return null;
    }
    try {
      return parseContext(JSON.parse(stored));
    } catch {
      // Legacy plain-path storage
      return isNotificationsReturnPath(stored) ? { returnTo: stored } : null;
    }
  } catch {
    return null;
  }
}

/** Read return target without consuming sessionStorage (safe for render). */
export function peekNotificationsReturnTo(
  locationState: unknown,
): NotificationsReturnContext | null {
  const fromState = parseContext(locationState);
  if (fromState) {
    return fromState;
  }
  try {
    const stored = sessionStorage.getItem(NOTIFICATIONS_RETURN_STORAGE_KEY);
    if (!stored) {
      return null;
    }
    try {
      return parseContext(JSON.parse(stored));
    } catch {
      return isNotificationsReturnPath(stored) ? { returnTo: stored } : null;
    }
  } catch {
    return null;
  }
}

/** Resolve and consume return target (call when leaving notifications). */
export function resolveNotificationsReturnTo(
  locationState: unknown,
): NotificationsReturnContext | null {
  const fromState = parseContext(locationState);
  let fromStorage: NotificationsReturnContext | null = null;
  try {
    const stored = sessionStorage.getItem(NOTIFICATIONS_RETURN_STORAGE_KEY);
    sessionStorage.removeItem(NOTIFICATIONS_RETURN_STORAGE_KEY);
    if (stored) {
      try {
        fromStorage = parseContext(JSON.parse(stored));
      } catch {
        fromStorage = isNotificationsReturnPath(stored) ? { returnTo: stored } : null;
      }
    }
  } catch {
    fromStorage = null;
  }

  if (fromState) {
    if (!fromState.workspace && fromStorage?.workspace && fromStorage.returnTo === fromState.returnTo) {
      return { returnTo: fromState.returnTo, workspace: fromStorage.workspace };
    }
    return fromState;
  }
  return fromStorage;
}

export function workspaceDestinationFromReturn(
  context: NotificationsReturnContext,
): WorkspaceDestination | null {
  const workspace = context.workspace;
  if (!workspace) {
    return null;
  }
  const labelKey =
    workspace.experience === "manage_business"
      ? "experience.manageBusiness"
      : workspace.experience === "operations"
        ? "experience.operations"
        : "experience.startSelling";
  return {
    organizationId: workspace.organizationId,
    organizationDisplayName: workspace.organizationDisplayName,
    branchId: workspace.branchId,
    branchName: workspace.branchName,
    experience: workspace.experience,
    route: context.returnTo,
    labelKey,
  };
}
