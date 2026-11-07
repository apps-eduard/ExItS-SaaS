import type { BrowserSessionSnapshot } from "./auth";
import { getPersonalDashboard, listPersonalTodos, summarizeTodoCounts } from "./personal";
import {
  listProductAffiliations,
  listWorkplaces,
  type PersonalBusinessCard,
} from "./personal-businesses";
import { readGuideHomeDismissed } from "./personal-guide";
import { loadStoresToPayPreview, type StoresToPayPreview } from "./personal-stores";
import {
  buildHomeAttentionItems,
  listBorrowedRelationships,
  listLentRelationships,
  listPersonalContacts,
  type UtangAttentionItem,
} from "./personal-utang";
import { type HomeWidget, widgetError } from "./widget";

export type PersonalHomeData = {
  profile: {
    userId?: string;
    displayName: string;
    email?: string;
    initials: string;
  };
  tracker: HomeWidget<{
    myBorrowed: number;
    myLent: number;
    sharedBorrowed: number;
    sharedLent: number;
    contactCount: number;
    activeMine: number;
    activeShared: number;
  }>;
  attention: HomeWidget<{ items: UtangAttentionItem[] }>;
  storesToPay: HomeWidget<StoresToPayPreview>;
  businesses: HomeWidget<{ items: PersonalBusinessCard[] }>;
  todos: HomeWidget<{
    today: number;
    upcoming: number;
    overdue: number;
    open: number;
  }>;
  showGuide: boolean;
};

function initialsFromName(name: string): string {
  const parts = name.split(/\s+/).filter(Boolean);
  if (parts.length >= 2) {
    return `${parts[0]![0] ?? ""}${parts[1]![0] ?? ""}`.toUpperCase();
  }
  return name.slice(0, 2).toUpperCase() || "?";
}

export async function loadPersonalHome(
  session: BrowserSessionSnapshot,
  signal?: AbortSignal,
): Promise<PersonalHomeData> {
  const displayName =
    session.displayName?.trim() ||
    session.username?.trim() ||
    session.email?.trim() ||
    "Signed in";

  const profile = {
    userId: session.userId,
    displayName,
    email: session.email,
    initials: initialsFromName(displayName),
  };

  const showGuide = !readGuideHomeDismissed(session.userId);

  const dashboardResult = await Promise.allSettled([getPersonalDashboard(signal)]);
  const dash = dashboardResult[0];

  let tracker: PersonalHomeData["tracker"];
  let pendingCount = 0;
  if (dash.status === "fulfilled") {
    pendingCount = dash.value.pendingConfirmationCount;
    tracker = {
      status: "ready",
      data: {
        myBorrowed: dash.value.totalBorrowedBalance,
        myLent: dash.value.totalLentBalance,
        sharedBorrowed: dash.value.sharedWithMeBorrowedBalance,
        sharedLent: dash.value.sharedWithMeLentBalance,
        contactCount: dash.value.contactCount,
        activeMine: dash.value.activeRelationshipCount,
        activeShared: dash.value.sharedWithMeActiveCount,
      },
    };
  } else {
    tracker = widgetError(dash.reason);
  }

  const [attentionBundle, storesResult, ownedResult, workplacesResult, todosResult] =
    await Promise.allSettled([
      Promise.all([
        listLentRelationships(signal),
        listBorrowedRelationships(signal),
        listPersonalContacts(signal),
      ]),
      loadStoresToPayPreview(signal),
      listProductAffiliations(signal),
      listWorkplaces(signal),
      listPersonalTodos(signal),
    ]);

  let attention: PersonalHomeData["attention"];
  if (attentionBundle.status === "fulfilled") {
    const [lent, borrowed, contacts] = attentionBundle.value;
    const items = buildHomeAttentionItems({
      pendingConfirmationCount: pendingCount,
      lent,
      borrowed,
      contacts,
    });
    attention = {
      status: items.length === 0 ? "empty" : "ready",
      data: { items },
    };
  } else if (pendingCount > 0) {
    attention = {
      status: "ready",
      data: {
        items: [
          {
            key: "pending",
            kind: "pendingConfirmation",
            count: pendingCount,
          },
        ],
      },
    };
  } else {
    attention = widgetError(attentionBundle.reason);
  }

  const storesToPay: PersonalHomeData["storesToPay"] =
    storesResult.status === "fulfilled"
      ? {
          status:
            storesResult.value.storeCount === 0 &&
            storesResult.value.preview.length === 0
              ? "empty"
              : "ready",
          data: storesResult.value,
        }
      : widgetError(storesResult.reason);

  let businesses: PersonalHomeData["businesses"];
  if (ownedResult.status === "fulfilled" || workplacesResult.status === "fulfilled") {
    const owned = ownedResult.status === "fulfilled" ? ownedResult.value : [];
    const ownedIds = new Set(owned.map((item) => item.organizationId));
    const workplaces =
      workplacesResult.status === "fulfilled"
        ? workplacesResult.value.filter((item) => !ownedIds.has(item.organizationId))
        : [];
    const items = [...owned, ...workplaces];
    businesses = {
      status: items.length === 0 ? "empty" : "ready",
      data: { items },
    };
  } else {
    businesses = widgetError(
      ownedResult.status === "rejected"
        ? ownedResult.reason
        : (workplacesResult as PromiseRejectedResult).reason,
    );
  }

  const todos: PersonalHomeData["todos"] =
    todosResult.status === "fulfilled"
      ? {
          status: "ready",
          data: summarizeTodoCounts(todosResult.value),
        }
      : widgetError(todosResult.reason);

  return {
    profile,
    tracker,
    attention,
    storesToPay,
    businesses,
    todos,
    showGuide,
  };
}
