import { useState, type ReactNode } from "react";
import { Link } from "react-router-dom";
import type { LucideIcon } from "lucide-react";
import {
  Calendar,
  Check,
  CheckCheck,
  ChevronDown,
  CircleX,
  ClipboardList,
  Clock3,
  Ellipsis,
  ListTodo,
  Loader2,
  MoreVertical,
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
import { ConfirmationDialog } from "@/components/exits/SheetDialog";
import { DropdownMenu, MenuItem, useDismissibleOpen } from "@/components/ui/dropdown-menu";
import { useI18n } from "@/i18n/I18nProvider";
import type { MessageKey } from "@/i18n/messages";
import { cn } from "@/lib/cn";

function capitalFirst(value: string): string {
  if (!value) {
    return value;
  }
  return value.charAt(0).toLocaleUpperCase() + value.slice(1);
}

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
  const moreMenu = useDismissibleOpen(false);

  return (
    <nav
      className="personal-todo-tasklist-rail__nav personal-todo-tasklist-rail__nav--scroll-more"
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
      {/* Mobile: scrollable row + vertical more (ext-tabs-basic-scrollable-more). Hidden on desktop rail. */}
      <DropdownMenu
        align="end"
        open={moreMenu.open}
        onOpenChange={moreMenu.setOpen}
        menuLabel={t("personal.todo.filters")}
        menuClassName="personal-todo-tasklist-rail__more-menu"
        trigger={({ id, expanded, controls, onClick, onKeyDown }) => (
          <button
            id={id}
            type="button"
            className="personal-todo-tasklist-rail__more"
            aria-label={t("personal.todo.filtersMore")}
            aria-haspopup="menu"
            aria-expanded={expanded}
            aria-controls={controls}
            data-testid="todo-tab-more"
            onClick={onClick}
            onKeyDown={onKeyDown}
          >
            <MoreVertical className="personal-todo-tasklist-rail__more-icon size-5" aria-hidden />
          </button>
        )}
      >
        {PERSONAL_TODO_FILTER_TABS.map((item) => {
          const Icon = item.icon;
          const count = countForTab(counts, item.id);
          const active = activeTab === item.id;
          return (
            <MenuItem
              key={item.id}
              data-testid={`todo-tab-more-item-${item.id}`}
              className={cn(active && "personal-todo-tasklist-rail__more-item--active")}
              onSelect={() => {
                onChange(item.id);
                moreMenu.close();
              }}
            >
              <Icon className="size-4 shrink-0" aria-hidden />
              <span className="min-w-0 flex-1 truncate">{t(item.labelKey)}</span>
              {count != null ? (
                <span className="personal-todo-tasklist-rail__more-count tabular-nums">{count}</span>
              ) : null}
            </MenuItem>
          );
        })}
      </DropdownMenu>
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
  return `${day}.${month}.${date.getFullYear()}`;
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

function relatedLabelKey(type: string): MessageKey {
  switch (type) {
    case "PersonalContact":
      return "personal.todo.relatedContact";
    case "PersonalUtangRelationship":
      return "personal.todo.relatedUtang";
    case "CustomerOrder":
      return "personal.todo.relatedOrder";
    case "Organization":
      return "personal.todo.relatedOrg";
    default:
      return "personal.todo.relatedLinkLabel";
  }
}

function relatedHref(type: string, id: string): string | null {
  if (type === "PersonalContact") {
    return `/personal/people/${id}`;
  }
  if (type === "PersonalUtangRelationship") {
    return `/personal/utang/relationships/${id}`;
  }
  return null;
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
  grouped = false,
  renderItem,
}: {
  items: PersonalTodoDto[];
  grouped?: boolean;
  renderItem: (item: PersonalTodoDto) => ReactNode;
}) {
  const { t } = useI18n();
  const [closed, setClosed] = useState<Record<string, boolean>>({});
  if (!grouped) {
    return (
      <div className="personal-todo-board personal-todo-board--flat" data-testid="todo-list">
        <ul className="personal-todo-board__list m-0 list-none p-0">{items.map(renderItem)}</ul>
      </div>
    );
  }

  const groups = TODO_BOARD_GROUPS.map((group) => ({
    ...group,
    items: items.filter(group.match),
  })).filter((group) => group.items.length > 0);

  return (
    <div className="personal-todo-board personal-todo-board--grouped" data-testid="todo-list">
      {groups.map((group) => {
        const open = !closed[group.id];
        return (
          <section
            key={group.id}
            className={cn("personal-todo-tasklist-group", `personal-todo-tasklist-group--${group.id}`)}
          >
            <button
              type="button"
              className="personal-todo-tasklist-group__toggle"
              aria-expanded={open}
              onClick={() => setClosed((current) => ({ ...current, [group.id]: open }))}
            >
              <span className="personal-todo-tasklist-group__title">
                {String(group.items.length)} {t(group.labelKey)}
              </span>
              <ChevronDown
                className={cn(
                  "personal-todo-tasklist-group__chevron size-4 shrink-0",
                  open && "personal-todo-tasklist-group__chevron--open",
                )}
                aria-hidden
              />
            </button>
            {open ? (
              <ul className="personal-todo-board__list m-0 list-none p-0">{group.items.map(renderItem)}</ul>
            ) : null}
          </section>
        );
      })}
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
  const [moreOpen, setMoreOpen] = useState(false);
  const [resetConfirm, setResetConfirm] = useState(false);
  const completed = item.status === "Completed";
  const cancelled = item.status === "Cancelled";
  const open = item.status === "Open";
  const dueLabel = formatTaskDate(item.dueAtUtc);
  const createdLabel = formatTaskDate(item.createdAtUtc);
  const hasReminder = Boolean(item.reminderAtUtc);
  const hasRelated = Boolean(item.relatedEntityType && item.relatedEntityId);
  const hasExtra = hasReminder || hasRelated;
  const linkedHref = hasRelated ? relatedHref(item.relatedEntityType ?? "", item.relatedEntityId ?? "") : null;
  void expandAllCommand;
  void onExpandedChange;

  const titleText = (
    <span
      className={cn(
        "personal-todo-tasklist-row__title",
        (completed || cancelled) && "personal-todo-tasklist-row__title--done",
      )}
    >
      {capitalFirst(item.title)}
    </span>
  );
  const statusLabel = completed
    ? t("personal.todo.reopen")
    : cancelled
      ? t("personal.todo.reactivate")
      : t("personal.todo.complete");

  return (
    <li
      className={cn(isExiting && "personal-todo-list__item--exit")}
      data-testid={`todo-item-${item.id}`}
    >
      <article
        className={cn(
          "personal-todo-tasklist-row",
          selected && "personal-todo-tasklist-row--selected",
          isExiting && "personal-todo-row--exit",
        )}
      >
        <div className="personal-todo-tasklist-row__line">
          <button
            type="button"
            className={cn(
              "personal-todo-tasklist-row__check",
              completed && "personal-todo-tasklist-row__check--completed",
              cancelled && "personal-todo-tasklist-row__check--cancelled",
            )}
            aria-pressed={completed}
            aria-label={statusLabel}
            data-testid={
              completed
                ? `todo-reopen-${item.id}`
                : cancelled
                  ? `todo-reactivate-${item.id}`
                  : `todo-check-${item.id}`
            }
            disabled={isActing || offlineBlocked}
            onClick={open ? onComplete : onReopen}
          >
            {isActing ? (
              <Loader2 className="size-3.5 shrink-0 animate-spin" aria-hidden />
            ) : cancelled ? (
              <CircleX className="size-4 shrink-0" aria-hidden />
            ) : completed ? (
              <Check className="size-3 shrink-0" aria-hidden />
            ) : (
              <span className="personal-todo-tasklist-row__check-box" />
            )}
          </button>
          <div className="personal-todo-tasklist-row__body min-w-0">
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
            {item.notes ? (
              <p className="personal-todo-tasklist-row__notes" data-testid={`todo-notes-${item.id}`}>
                {item.notes}
              </p>
            ) : null}
            <div className="personal-todo-tasklist-row__dates">
              <span className="personal-todo-tasklist-row__date">
                {t("personal.todo.startLabel")}
                <span className="personal-todo-tasklist-row__date-value">{createdLabel}</span>
              </span>
              <span className="personal-todo-tasklist-row__date">
                {t("personal.todo.dueLabel")}
                <span className="personal-todo-tasklist-row__date-value">
                  {dueLabel ?? t("personal.todo.noDue")}
                </span>
              </span>
              <span className={cn("personal-todo-tasklist-row__priority", priorityTextToneClass(item.priority))}>
                {t(priorityLabelKey(item.priority))}
              </span>
              {hasExtra ? (
                <div className="personal-todo-tasklist-row__more">
                  <Button
                    type="button"
                    intent="info"
                    appearance="solid"
                    emphasis="soft"
                    size="sm"
                    aria-label={moreOpen ? t("personal.todo.hideMoreOptions") : t("personal.todo.moreOptions")}
                    aria-expanded={moreOpen}
                    aria-controls={`todo-more-panel-${item.id}`}
                    data-testid={`todo-more-${item.id}`}
                    onClick={() => setMoreOpen((open) => !open)}
                  >
                    <Ellipsis className="size-4 shrink-0" aria-hidden />
                  </Button>
                  {moreOpen ? (
                    <div
                      id={`todo-more-panel-${item.id}`}
                      className="personal-todo-tasklist-row__more-panel"
                      data-testid={`todo-more-panel-${item.id}`}
                    >
                      {hasReminder && item.reminderAtUtc ? (
                        <p className="personal-todo-tasklist-row__more-line">
                          {t("personal.todo.reminderAt")}: {new Date(item.reminderAtUtc).toLocaleString()}
                          {" · "}
                          {item.reminderNotifiedAtUtc
                            ? t("personal.todo.reminderDelivered")
                            : t("personal.todo.reminderPending")}
                        </p>
                      ) : null}
                      {hasRelated ? (
                        <p className="personal-todo-tasklist-row__more-line">
                          {t("personal.todo.relatedLinkLabel")}:{" "}
                          {linkedHref ? (
                            <Link className="font-semibold text-primary no-underline" to={linkedHref}>
                              {t(relatedLabelKey(item.relatedEntityType ?? ""))}
                            </Link>
                          ) : (
                            <span>{t(relatedLabelKey(item.relatedEntityType ?? ""))}</span>
                          )}
                        </p>
                      ) : null}
                    </div>
                  ) : null}
                </div>
              ) : null}
            </div>
          </div>
          <div className="personal-todo-tasklist-row__tools">
            <Button
              asChild
              intent="primary"
              appearance="ghost"
              size="sm"
              className="personal-todo-tasklist-row__icon-btn personal-todo-tasklist-row__icon-btn--edit"
              aria-label={t("personal.todo.edit")}
            >
              <Link to={`/personal/todo/${item.id}?edit=1`} data-testid={`todo-edit-${item.id}`}>
                <Pencil className="size-4 shrink-0" aria-hidden />
              </Link>
            </Button>
            {cancelled ? (
              <Button
                type="button"
                intent="info"
                appearance="ghost"
                size="sm"
                className="personal-todo-tasklist-row__icon-btn personal-todo-tasklist-row__icon-btn--reset"
                aria-label={t("personal.todo.reactivate")}
                data-testid={`todo-reset-${item.id}`}
                disabled={isActing || offlineBlocked}
                onClick={() => setResetConfirm(true)}
              >
                <RotateCcw className="size-4 shrink-0" aria-hidden />
              </Button>
            ) : null}
            {cancelled ? (
              <Button
                type="button"
                intent="danger"
                appearance="ghost"
                size="sm"
                className="personal-todo-tasklist-row__icon-btn personal-todo-tasklist-row__icon-btn--delete"
                aria-label={t("personal.todo.delete")}
                data-testid={`todo-delete-${item.id}`}
                disabled={isActing || offlineBlocked}
                onClick={onDelete}
              >
                <Trash2 className="size-4 shrink-0" aria-hidden />
              </Button>
            ) : (
              <Button
                type="button"
                intent="danger"
                appearance="ghost"
                size="sm"
                className="personal-todo-tasklist-row__icon-btn personal-todo-tasklist-row__icon-btn--delete"
                aria-label={t("personal.todo.cancel")}
                data-testid={`todo-cancel-${item.id}`}
                disabled={isActing || offlineBlocked}
                onClick={onCancel}
              >
                <Trash2 className="size-4 shrink-0" aria-hidden />
              </Button>
            )}
          </div>
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
      <ConfirmationDialog
        open={resetConfirm}
        title={t("personal.todo.resetConfirmTitle")}
        detail={t("personal.todo.resetConfirmDetail")}
        confirmLabel={t("personal.todo.reactivate")}
        cancelLabel={t("personal.todo.cancel")}
        busy={isActing}
        onCancel={() => {
          if (!isActing) {
            setResetConfirm(false);
          }
        }}
        onConfirm={() => {
          onReopen();
          setResetConfirm(false);
        }}
        testId={`todo-reset-confirm-${item.id}`}
      />
    </li>
  );
}
