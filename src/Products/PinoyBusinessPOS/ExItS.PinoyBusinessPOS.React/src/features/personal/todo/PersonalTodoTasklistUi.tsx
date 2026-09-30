import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import type { LucideIcon } from "lucide-react";
import {
  Calendar,
  CheckCheck,
  ChevronDown,
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
  priorityToneClass,
  type PersonalTodoCounts,
  type TodoAgendaTab,
} from "@/api/platform/personal-todo-client";
import { IconButton } from "@/components/ui/icon-button";
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

function formatTaskDate(iso: string | null | undefined): string | null {
  if (!iso) {
    return null;
  }
  const date = new Date(iso);
  if (Number.isNaN(date.getTime())) {
    return null;
  }
  return date.toLocaleDateString(undefined, {
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
  });
}

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
  const [expanded, setExpanded] = useState(true);
  const dueLabel = formatTaskDate(item.dueAtUtc);
  const createdLabel = formatTaskDate(item.createdAtUtc);
  const priorityClass = priorityToneClass(item.priority);
  const completed = item.status === "Completed";
  const cancelled = item.status === "Cancelled";
  const open = item.status === "Open";

  useEffect(() => {
    if (!expandAllCommand) {
      return;
    }
    setExpanded(expandAllCommand.expanded);
  }, [expandAllCommand?.token, expandAllCommand?.expanded]);

  function toggleExpanded() {
    setExpanded((openState) => {
      const next = !openState;
      onExpandedChange?.(next);
      return next;
    });
  }

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
          item.priority === "High" && "personal-todo-tasklist-row--priority-high",
          selected && "personal-todo-tasklist-row--selected",
          isExiting && "personal-todo-row--exit",
          !expanded && "personal-todo-tasklist-row--collapsed",
        )}
      >
        <div className="personal-todo-tasklist-row__main">
          {open ? (
            <button
              type="button"
              className="personal-todo-tasklist-row__check"
              data-testid={`todo-check-${item.id}`}
              aria-label={t("personal.todo.complete")}
              disabled={isActing || offlineBlocked}
              onClick={onComplete}
            >
              {isActing ? (
                <Loader2 className="size-4 animate-spin" aria-hidden />
              ) : (
                <span className="personal-todo-tasklist-row__check-box" aria-hidden />
              )}
            </button>
          ) : (
            <span
              className={cn(
                "personal-todo-tasklist-row__check personal-todo-tasklist-row__check--static",
                completed && "personal-todo-tasklist-row__check--completed",
                cancelled && "personal-todo-tasklist-row__check--cancelled",
              )}
              aria-hidden
            />
          )}

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
              <button
                type="button"
                className="personal-todo-tasklist-row__expand"
                aria-expanded={expanded}
                aria-label={
                  expanded ? t("personal.todo.collapseRow") : t("personal.todo.expandRow")
                }
                data-testid={`todo-expand-${item.id}`}
                onClick={toggleExpanded}
              >
                <ChevronDown
                  className={cn(
                    "personal-todo-tasklist-row__expand-chevron size-4 shrink-0",
                    expanded && "personal-todo-tasklist-row__expand-chevron--open",
                  )}
                  aria-hidden
                />
              </button>
            </div>

            {expanded ? (
              <>
                {item.notes ? (
                  <p className="personal-todo-tasklist-row__notes m-0">{item.notes}</p>
                ) : null}

                <div className="personal-todo-tasklist-row__meta">
                  <div className="personal-todo-tasklist-row__meta-start">
                    {dueLabel ? (
                      <span className="personal-todo-tasklist-row__date-pill">
                        <Calendar className="size-3.5 shrink-0" aria-hidden />
                        {createdLabel && createdLabel !== dueLabel ? (
                          <>
                            <span>{createdLabel}</span>
                            <span className="personal-todo-tasklist-row__date-sep" aria-hidden>
                              –
                            </span>
                          </>
                        ) : null}
                        <span>{dueLabel}</span>
                      </span>
                    ) : (
                      <span className="personal-todo-tasklist-row__date-pill personal-todo-tasklist-row__date-pill--muted">
                        {t("personal.todo.noDue")}
                      </span>
                    )}
                    <span
                      className={cn(
                        "personal-todo-meta__chip personal-todo-tasklist-row__priority",
                        priorityClass,
                      )}
                    >
                      {t(priorityLabelKey(item.priority))}
                    </span>
                    {pendingLocal ? (
                      <span
                        className="text-[length:var(--exits-text-xs)] text-muted"
                        data-testid="todo-waiting-chip"
                      >
                        {t("offline.personalWaitingBadge")}
                      </span>
                    ) : null}
                  </div>

                  <div className="personal-todo-tasklist-row__actions">
                    {open ? (
                      <>
                        <Link
                          to={`/personal/todo/${item.id}?edit=1`}
                          className="personal-todo-tasklist-row__icon-link personal-todo-tasklist-row__icon-link--edit"
                          data-testid={`todo-edit-${item.id}`}
                          aria-label={t("personal.todo.edit")}
                          title={t("personal.todo.edit")}
                        >
                          <Pencil className="size-4" aria-hidden />
                        </Link>
                        <IconButton
                          label={t("personal.todo.cancel")}
                          className="personal-todo-tasklist-row__icon-action personal-todo-tasklist-row__icon-action--cancel"
                          data-testid={`todo-cancel-${item.id}`}
                          disabled={isActing || offlineBlocked}
                          onClick={onCancel}
                        >
                          <Trash2 className="size-4" aria-hidden />
                        </IconButton>
                      </>
                    ) : null}
                    {completed ? (
                      <>
                        <IconButton
                          label={t("personal.todo.reopen")}
                          className="personal-todo-tasklist-row__icon-action personal-todo-tasklist-row__icon-action--reopen"
                          data-testid={`todo-reopen-${item.id}`}
                          disabled={isActing || offlineBlocked}
                          onClick={onReopen}
                        >
                          <RotateCcw className="size-4" aria-hidden />
                        </IconButton>
                        <IconButton
                          label={t("personal.todo.cancel")}
                          className="personal-todo-tasklist-row__icon-action personal-todo-tasklist-row__icon-action--cancel"
                          data-testid={`todo-cancel-${item.id}`}
                          disabled={isActing || offlineBlocked}
                          onClick={onCancel}
                        >
                          <Trash2 className="size-4" aria-hidden />
                        </IconButton>
                      </>
                    ) : null}
                    {cancelled ? (
                      <>
                        <IconButton
                          label={t("personal.todo.reactivate")}
                          className="personal-todo-tasklist-row__icon-action personal-todo-tasklist-row__icon-action--reactivate"
                          data-testid={`todo-reactivate-${item.id}`}
                          disabled={isActing || offlineBlocked}
                          onClick={onReopen}
                        >
                          <RotateCcw className="size-4" aria-hidden />
                        </IconButton>
                        <IconButton
                          label={t("personal.todo.delete")}
                          className="personal-todo-tasklist-row__icon-action personal-todo-tasklist-row__icon-action--delete"
                          data-testid={`todo-delete-${item.id}`}
                          disabled={isActing || offlineBlocked}
                          onClick={onDelete}
                        >
                          <Trash2 className="size-4" aria-hidden />
                        </IconButton>
                      </>
                    ) : null}
                  </div>
                </div>
              </>
            ) : null}
          </div>
        </div>
      </article>
    </li>
  );
}
