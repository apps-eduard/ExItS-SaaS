import type { ReactNode } from "react";
import { Link } from "react-router-dom";
import type { LucideIcon } from "lucide-react";
import {
  Calendar,
  Check,
  CheckCheck,
  ClipboardList,
  Clock3,
  ListTodo,
  Loader2,
  Pencil,
  RotateCcw,
  Trash2,
  TriangleAlert,
  X,
} from "lucide-react";
import type { PersonalTodoDto } from "@/api/platform/personal-todo-client";
import {
  classifyTodoDue,
  priorityTextToneClass,
  type PersonalTodoCounts,
  type TodoAgendaTab,
} from "@/api/platform/personal-todo-client";
import { Button } from "@/components/ui/button";
import { useI18n } from "@/i18n/I18nProvider";
import type { MessageKey } from "@/i18n/messages";
import { cn } from "@/lib/cn";

export type PersonalTodoExpandAllCommand = {
  expanded: boolean;
  token: number;
};

export const PERSONAL_TODO_FILTER_TABS: {
  id: TodoAgendaTab;
  labelKey: MessageKey;
  icon: LucideIcon;
}[] = [
  { id: "all", labelKey: "personal.todo.filterAll", icon: ListTodo },
  { id: "open", labelKey: "personal.todo.filterOpen", icon: ClipboardList },
  { id: "overdue", labelKey: "personal.todo.filterOverdue", icon: TriangleAlert },
  { id: "today", labelKey: "personal.todo.filterToday", icon: Clock3 },
  { id: "upcoming", labelKey: "personal.todo.filterUpcoming", icon: Calendar },
  { id: "completed", labelKey: "personal.todo.filterCompleted", icon: CheckCheck },
  { id: "cancelled", labelKey: "personal.todo.filterCancelled", icon: X },
];

function countForTab(counts: PersonalTodoCounts | null, id: TodoAgendaTab): number | null {
  if (!counts) {
    return null;
  }
  return counts[id];
}

export function PersonalTodoFilterRail({
  activeTab,
  counts,
  onChange,
}: {
  activeTab: TodoAgendaTab;
  counts: PersonalTodoCounts | null;
  onChange: (tab: TodoAgendaTab) => void;
}) {
  const { t } = useI18n();
  return (
    <nav
      className="personal-todo-tasklist-rail__nav"
      aria-label={t("personal.todo.filters")}
      data-testid="personal-todo-filters"
    >
      <p className="personal-todo-tasklist-rail__section m-0">{t("personal.todo.railSection")}</p>
      <ul className="personal-todo-tasklist-rail__list m-0 list-none p-0">
        {PERSONAL_TODO_FILTER_TABS.map((item) => {
          const Icon = item.icon;
          const count = countForTab(counts, item.id);
          const active = activeTab === item.id;
          return (
            <li key={item.id}>
              <button
                type="button"
                className={cn(
                  "personal-todo-tasklist-rail__item",
                  active && "personal-todo-tasklist-rail__item--active",
                )}
                data-testid={`todo-tab-${item.id}`}
                aria-current={active ? "page" : undefined}
                onClick={() => onChange(item.id)}
              >
                <Icon className="personal-todo-tasklist-rail__icon size-4 shrink-0" aria-hidden />
                <span className="personal-todo-tasklist-rail__label min-w-0 truncate">
                  {t(item.labelKey)}
                </span>
                {count != null ? (
                  <span className="personal-todo-tasklist-rail__count">{count}</span>
                ) : null}
              </button>
            </li>
          );
        })}
      </ul>
    </nav>
  );
}

type TodoCardTone = "primary" | "success" | "warning" | "danger";

function formatTaskDate(iso: string | null | undefined): string | null {
  if (!iso) {
    return null;
  }
  const date = new Date(iso);
  if (Number.isNaN(date.getTime())) {
    return null;
  }
  const day = String(date.getDate()).padStart(2, "0");
  const month = String(date.getMonth() + 1).padStart(2, "0");
  return `${day}-${month}-${date.getFullYear()}`;
}

function priorityLabelKey(priority: string): MessageKey {
  switch (priority) {
    case "Low":
      return "personal.todo.priorityLow";
    case "Normal":
      return "personal.todo.priorityNormal";
    case "High":
      return "personal.todo.priorityHigh";
    default:
      return "personal.todo.priorityNone";
  }
}

function todoCardTone(item: PersonalTodoDto): TodoCardTone {
  if (item.status === "Cancelled") return "danger";
  if (item.status === "Completed") return "success";
  if (item.status === "Open" && classifyTodoDue(item.dueAtUtc) === "overdue") return "warning";
  return "primary";
}

const TODO_BOARD_GROUPS: {
  id: TodoCardTone;
  labelKey: MessageKey;
  match: (item: PersonalTodoDto) => boolean;
}[] = [
  {
    id: "primary",
    labelKey: "personal.todo.statusInProgress",
    match: (item) => todoCardTone(item) === "primary",
  },
  {
    id: "warning",
    labelKey: "personal.todo.dueLabel",
    match: (item) => todoCardTone(item) === "warning",
  },
  {
    id: "success",
    labelKey: "personal.todo.statusCompleted",
    match: (item) => todoCardTone(item) === "success",
  },
  {
    id: "danger",
    labelKey: "personal.todo.statusCancelled",
    match: (item) => todoCardTone(item) === "danger",
  },
];

export function PersonalTodoBoard({
  items,
  renderItem,
}: {
  items: PersonalTodoDto[];
  renderItem: (item: PersonalTodoDto) => ReactNode;
}) {
  const { t } = useI18n();
  const groups = TODO_BOARD_GROUPS.map((group) => ({
    ...group,
    items: items.filter(group.match),
  })).filter((group) => group.items.length > 0);

  return (
    <div className="personal-todo-board" data-testid="todo-list">
      {groups.map((group) => (
        <section
          key={group.id}
          className={cn("personal-todo-board__column", `personal-todo-board__column--${group.id}`)}
        >
          <h3 className="personal-todo-board__header">
            {t(group.labelKey)}
            <span className="personal-todo-board__count">
              {" "}
              ({String(group.items.length).padStart(2, "0")})
            </span>
          </h3>
          <ul className="personal-todo-board__list m-0 list-none p-0">{group.items.map(renderItem)}</ul>
        </section>
      ))}
    </div>
  );
}

export function PersonalTodoTaskRow({
  item,
  isActing,
  isExiting,
  pendingLocal,
  offlineBlocked,
  peekMode,
  selected,
  expandAllCommand,
  onExpandedChange,
  onSelect,
  onComplete,
  onCancel,
  onReopen,
  onDelete,
}: {
  item: PersonalTodoDto;
  isActing: boolean;
  isExiting: boolean;
  pendingLocal: boolean;
  offlineBlocked: boolean;
  peekMode: boolean;
  selected: boolean;
  expandAllCommand?: PersonalTodoExpandAllCommand | null;
  onExpandedChange?: (expanded: boolean) => void;
  onSelect: () => void;
  onComplete: () => void;
  onCancel: () => void;
  onReopen: () => void;
  onDelete: () => void;
}) {
  const { t } = useI18n();
  const completed = item.status === "Completed";
  const cancelled = item.status === "Cancelled";
  const open = item.status === "Open";
  const dueLabel = formatTaskDate(item.dueAtUtc);
  void expandAllCommand;
  void onExpandedChange;

  const titleText = (
    <span
      className={cn(
        "personal-todo-tasklist-row__title",
        (completed || cancelled) && "personal-todo-tasklist-row__title--done",
      )}
    >
      {item.title}
    </span>
  );

  return (
    <li
      className={cn(isExiting && "personal-todo-list__item--exit")}
      data-testid={`todo-item-${item.id}`}
    >
      <article
        className={cn(
          "personal-todo-tasklist-row",
          `personal-todo-tasklist-row--${todoCardTone(item)}`,
          selected && "personal-todo-tasklist-row--selected",
          isExiting && "personal-todo-row--exit",
        )}
      >
        <div className="personal-todo-tasklist-row__main">
          <div className="personal-todo-tasklist-row__content min-w-0 flex-1">
            <div className="personal-todo-tasklist-row__heading">
              {peekMode ? (
                <button
                  type="button"
                  className="personal-todo-tasklist-row__title-button"
                  onClick={onSelect}
                  data-testid={`todo-select-${item.id}`}
                >
                  {titleText}
                </button>
              ) : (
                <Link
                  to={`/personal/todo/${item.id}`}
                  className="personal-todo-tasklist-row__title-link text-foreground no-underline"
                >
                  {titleText}
                </Link>
              )}
            </div>
            <div className="personal-todo-tasklist-row__facts">
              <span className={cn("personal-todo-tasklist-row__priority", priorityTextToneClass(item.priority))}>
                {t(priorityLabelKey(item.priority))}
              </span>
              <span className="personal-todo-tasklist-row__due">
                {dueLabel
                  ? `${t("personal.todo.dueDateBadge")}: ${dueLabel}`
                  : t("personal.todo.noDue")}
              </span>
            </div>
          </div>
        </div>
        {item.notes ? <p className="personal-todo-tasklist-row__notes m-0">{item.notes}</p> : null}
        <div className="personal-todo-tasklist-row__actions">
          {open ? (
            <>
              <Button asChild intent="primary" appearance="solid" emphasis="soft" size="sm">
                <Link
                  to={`/personal/todo/${item.id}?edit=1`}
                  data-testid={`todo-edit-${item.id}`}
                >
                  <Pencil className="size-3.5 shrink-0" aria-hidden />
                  {t("personal.todo.edit")}
                </Link>
              </Button>
              <Button
                type="button"
                intent="success"
                appearance="solid"
                emphasis="soft"
                size="sm"
                data-testid={`todo-check-${item.id}`}
                disabled={isActing || offlineBlocked}
                onClick={onComplete}
              >
                {isActing ? (
                  <Loader2 className="size-3.5 shrink-0 animate-spin" aria-hidden />
                ) : (
                  <Check className="size-3.5 shrink-0" aria-hidden />
                )}
                {t("personal.todo.complete")}
              </Button>
              <Button
                type="button"
                intent="warning"
                appearance="solid"
                emphasis="soft"
                size="sm"
                data-testid={`todo-cancel-${item.id}`}
                disabled={isActing || offlineBlocked}
                onClick={onCancel}
              >
                <Trash2 className="size-3.5 shrink-0" aria-hidden />
                {t("personal.todo.cancel")}
              </Button>
            </>
          ) : null}
          {completed ? (
            <>
              <Button
                type="button"
                intent="info"
                appearance="solid"
                emphasis="soft"
                size="sm"
                data-testid={`todo-reopen-${item.id}`}
                disabled={isActing || offlineBlocked}
                onClick={onReopen}
              >
                <RotateCcw className="size-3.5 shrink-0" aria-hidden />
                {t("personal.todo.reopen")}
              </Button>
              <Button
                type="button"
                intent="warning"
                appearance="solid"
                emphasis="soft"
                size="sm"
                data-testid={`todo-cancel-${item.id}`}
                disabled={isActing || offlineBlocked}
                onClick={onCancel}
              >
                <Trash2 className="size-3.5 shrink-0" aria-hidden />
                {t("personal.todo.cancel")}
              </Button>
            </>
          ) : null}
          {cancelled ? (
            <>
              <Button
                type="button"
                intent="info"
                appearance="solid"
                emphasis="soft"
                size="sm"
                data-testid={`todo-reactivate-${item.id}`}
                disabled={isActing || offlineBlocked}
                onClick={onReopen}
              >
                <RotateCcw className="size-3.5 shrink-0" aria-hidden />
                {t("personal.todo.reactivate")}
              </Button>
              <Button
                type="button"
                intent="danger"
                appearance="solid"
                emphasis="soft"
                size="sm"
                data-testid={`todo-delete-${item.id}`}
                disabled={isActing || offlineBlocked}
                onClick={onDelete}
              >
                <Trash2 className="size-3.5 shrink-0" aria-hidden />
                {t("personal.todo.delete")}
              </Button>
            </>
          ) : null}
        </div>
        {pendingLocal ? (
          <span
            className="personal-todo-tasklist-row__waiting text-[length:var(--exits-text-xs)] text-muted"
            data-testid="todo-waiting-chip"
          >
            {t("offline.personalWaitingBadge")}
          </span>
        ) : null}
      </article>
    </li>
  );
}
