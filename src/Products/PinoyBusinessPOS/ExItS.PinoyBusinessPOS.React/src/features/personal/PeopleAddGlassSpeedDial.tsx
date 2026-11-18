import { EllipsisVertical, IdCard, Plus, UserPlus } from "lucide-react";
import { useState, type ReactNode } from "react";
import { useNavigate } from "react-router-dom";
import {
  ExitsFileFormatIcon,
  ExitsPrintFormatIcon,
} from "@/components/exits/ExitsTable";
import { ExitsTooltip } from "@/components/exits/ExitsTooltip";
import { BottomSheet } from "@/components/exits/SheetDialog";
import { Button } from "@/components/ui/button";
import { useI18n } from "@/i18n/I18nProvider";
import { cn } from "@/lib/cn";

export type PeopleSpeedDialExportActions = {
  disabled?: boolean;
  onCsv: () => void;
  onXlsx: () => void;
  onPdf: () => void;
  onPrint: () => void;
};

type PeopleAddGlassSpeedDialProps = {
  onAddLocal: () => void;
  onAddExits: () => void;
  exportActions?: PeopleSpeedDialExportActions | null;
  className?: string;
};

/**
 * People list header ⋮ (vertical) — plain icon at the trailing end.
 * Opens a bottom drawer of actions (add / connections + optional export).
 */
export function PeopleAddGlassSpeedDial({
  onAddLocal,
  onAddExits,
  exportActions = null,
  className,
}: PeopleAddGlassSpeedDialProps) {
  const { t } = useI18n();
  const navigate = useNavigate();
  const [open, setOpen] = useState(false);

  function close() {
    setOpen(false);
  }

  function run(action: () => void) {
    close();
    action();
  }

  const primaryActions = [
    {
      id: "local" as const,
      label: t("people.addLocal"),
      intent: "info" as const,
      Icon: Plus,
      testId: "people-add-toggle",
      onSelect: onAddLocal,
    },
    {
      id: "exits" as const,
      label: t("people.addExits"),
      intent: "primary" as const,
      Icon: IdCard,
      testId: "people-add-exits",
      onSelect: onAddExits,
    },
    {
      id: "requests" as const,
      label: t("people.connectionInbox"),
      intent: "neutral" as const,
      Icon: UserPlus,
      testId: "people-connection-requests",
      onSelect: () => {
        void navigate("/personal/invitations");
      },
    },
  ];

  const exportItems: Array<{
    id: "csv" | "xlsx" | "pdf" | "print";
    label: string;
    testId: string;
    icon: ReactNode;
    onSelect: () => void;
  }> = exportActions
    ? [
        {
          id: "csv",
          label: t("exitsTable.exportCsv"),
          testId: "people-export-csv",
          icon: <ExitsFileFormatIcon format="csv" />,
          onSelect: exportActions.onCsv,
        },
        {
          id: "xlsx",
          label: t("exitsTable.exportExcel"),
          testId: "people-export-xlsx",
          icon: <ExitsFileFormatIcon format="xlsx" />,
          onSelect: exportActions.onXlsx,
        },
        {
          id: "pdf",
          label: t("exitsTable.exportPdf"),
          testId: "people-export-pdf",
          icon: <ExitsFileFormatIcon format="pdf" />,
          onSelect: exportActions.onPdf,
        },
        {
          id: "print",
          label: t("exitsTable.print"),
          testId: "people-export-print",
          icon: <ExitsPrintFormatIcon />,
          onSelect: exportActions.onPrint,
        },
      ]
    : [];

  const exportDisabled = Boolean(exportActions?.disabled);

  return (
    <div
      className={cn("people-add-glass-speed-dial", className)}
      data-open={open ? "true" : "false"}
      data-testid="people-add-speed-dial"
    >
      <ExitsTooltip content={t("people.menu.action")}>
        <button
          type="button"
          className="people-add-glass-speed-dial__trigger"
          aria-label={t("people.addMenu")}
          aria-expanded={open}
          aria-haspopup="dialog"
          data-testid="people-add-speed-dial-trigger"
          onClick={() => setOpen(true)}
        >
          <EllipsisVertical className="size-5 shrink-0" aria-hidden strokeWidth={2.25} />
        </button>
      </ExitsTooltip>

      <BottomSheet
        open={open}
        onClose={close}
        title={t("people.menu.action")}
        panelId="people-add-menu-sheet"
        testId="people-add-speed-dial-actions"
        closeLabel={t("people.add.cancel")}
        presentation="sheet"
        panelClassName="people-add-menu-sheet"
      >
        <div className="people-add-menu-sheet__body flex min-w-0 flex-col gap-2">
          <div className="people-add-menu-sheet__row">
            {primaryActions.slice(0, 2).map((action) => {
              const ActionIcon = action.Icon;
              return (
                <Button
                  key={action.id}
                  type="button"
                  intent={action.intent}
                  appearance="outline"
                  size="sm"
                  aria-label={action.label}
                  data-testid={action.testId}
                  onClick={() => run(action.onSelect)}
                >
                  <ActionIcon className="size-3.5 shrink-0" aria-hidden />
                  {action.label}
                </Button>
              );
            })}
          </div>
          <div className="people-add-menu-sheet__row people-add-menu-sheet__row--solo">
            {primaryActions.slice(2).map((action) => {
              const ActionIcon = action.Icon;
              return (
                <Button
                  key={action.id}
                  type="button"
                  intent={action.intent}
                  appearance="outline"
                  size="sm"
                  aria-label={action.label}
                  data-testid={action.testId}
                  onClick={() => run(action.onSelect)}
                >
                  <ActionIcon className="size-3.5 shrink-0" aria-hidden />
                  {action.label}
                </Button>
              );
            })}
          </div>
          {exportItems.length > 0 ? (
            <div
              className="people-add-menu-sheet__export"
              role="group"
              aria-label={t("exitsTable.exportPrintMenu")}
              data-testid="people-list-export"
            >
              {exportItems.map((item) => (
                <Button
                  key={item.id}
                  type="button"
                  intent="neutral"
                  appearance="outline"
                  size="sm"
                  className="people-add-menu-sheet__export-btn"
                  aria-label={item.label}
                  disabled={exportDisabled}
                  data-testid={item.testId}
                  onClick={() => {
                    if (exportDisabled) {
                      return;
                    }
                    run(item.onSelect);
                  }}
                >
                  {item.icon}
                </Button>
              ))}
            </div>
          ) : null}
        </div>
      </BottomSheet>
    </div>
  );
}
