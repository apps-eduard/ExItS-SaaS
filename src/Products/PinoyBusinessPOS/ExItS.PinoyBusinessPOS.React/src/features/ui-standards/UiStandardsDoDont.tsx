import { MoreHorizontal } from "lucide-react";
import { getActionButtonStyle, getActionIcon } from "@/components/exits/action-semantics";
import { StatusChip } from "@/components/exits/StatusChip";
import { TableActionButton } from "@/components/exits/TableActionButton";
import { Button, buttonIconMotion } from "@/components/ui/button";
import { Card, CardTitle } from "@/components/ui/card";

/**
 * Compact Do / Don’t guidance for builders — production components only.
 */
export function UiStandardsDoDont() {
  const CreateIcon = getActionIcon("create");
  const AddIcon = getActionIcon("add");
  const DeleteIcon = getActionIcon("delete");

  return (
    <Card className="flex min-w-0 flex-col gap-3 p-3 lg:col-span-2" data-testid="ui-standard-card-dodont">
      <CardTitle>Do / Don’t</CardTitle>

      <div className="grid min-w-0 gap-3 md:grid-cols-2">
        <div className="flex min-w-0 flex-col gap-2 rounded-[var(--exits-radius-md)] border border-[color-mix(in_srgb,var(--exits-success)_35%,var(--exits-border))] p-2.5">
          <p className="m-0 text-[length:var(--exits-text-xs)] font-semibold uppercase tracking-wide text-[var(--exits-success)]">
            Do — one Primary
          </p>
          <div className="flex flex-wrap gap-2">
            <Button type="button" {...getActionButtonStyle("create")}>
              {CreateIcon ? (
                <CreateIcon className={`size-4 ${buttonIconMotion.add}`} aria-hidden />
              ) : null}
              Create customer
            </Button>
            <Button type="button" {...getActionButtonStyle("add", { isPrimaryInGroup: false })}>
              {AddIcon ? <AddIcon className="size-4" aria-hidden /> : null}
              Add existing
            </Button>
          </div>
          <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">
            Use one Primary action per action group.
          </p>
        </div>
        <div className="flex min-w-0 flex-col gap-2 rounded-[var(--exits-radius-md)] border border-[color-mix(in_srgb,var(--exits-danger)_35%,var(--exits-border))] p-2.5">
          <p className="m-0 text-[length:var(--exits-text-xs)] font-semibold uppercase tracking-wide text-[var(--exits-danger)]">
            Don’t — competing Primaries
          </p>
          <div className="flex flex-wrap gap-2">
            <Button type="button" intent="primary" appearance="solid">
              Create customer
            </Button>
            <Button type="button" intent="primary" appearance="solid">
              Add existing
            </Button>
          </div>
          <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">
            Do not create competing primary actions.
          </p>
        </div>
      </div>

      <div className="flex flex-col gap-2 border-t border-border pt-3">
        <p className="m-0 text-[length:var(--exits-text-xs)] font-medium uppercase tracking-wide text-muted">
          Intent vs appearance
        </p>
        <div className="grid min-w-0 gap-3 md:grid-cols-2">
          <div className="flex min-w-0 flex-col gap-2">
            <span className="text-[length:var(--exits-text-xs)] text-[var(--exits-success)]">Do</span>
            <Button type="button" intent="danger" appearance="outline">
              {DeleteIcon ? (
                <DeleteIcon className={`size-4 ${buttonIconMotion.delete}`} aria-hidden />
              ) : null}
              Delete
            </Button>
            <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">
              Intent describes meaning. Appearance describes presentation.
            </p>
          </div>
          <div className="flex min-w-0 flex-col gap-2">
            <span className="text-[length:var(--exits-text-xs)] text-[var(--exits-danger)]">
              Don’t
            </span>
            <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
              Treat <code className="text-foreground">outline</code> as an intent. Outline is an
              appearance — pair it with a semantic intent (e.g. Danger + Outline).
            </p>
          </div>
        </div>
      </div>

      <div className="flex flex-col gap-2 border-t border-border pt-3">
        <p className="m-0 text-[length:var(--exits-text-xs)] font-medium uppercase tracking-wide text-muted">
          Soft appearance ≠ Soft shape
        </p>
        <div className="flex flex-wrap items-center gap-3">
          <StatusChip tone="success" appearance="soft" shape="pill">
            Soft fill · Pill radius
          </StatusChip>
          <StatusChip tone="success" appearance="outline" shape="soft">
            Outline fill · Soft radius
          </StatusChip>
        </div>
        <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">
          Soft appearance and Soft shape are independent properties.
        </p>
      </div>

      <div className="flex flex-col gap-2 border-t border-border pt-3">
        <p className="m-0 text-[length:var(--exits-text-xs)] font-medium uppercase tracking-wide text-muted">
          Responsive data
        </p>
        <div className="grid min-w-0 gap-3 md:grid-cols-2">
          <div className="flex min-w-0 flex-col gap-1.5">
            <span className="text-[length:var(--exits-text-xs)] text-[var(--exits-success)]">Do</span>
            <p className="m-0 text-[length:var(--exits-text-sm)]">
              Desktop TABLE → mobile LIST / record cards (
              <code className="text-[length:var(--exits-text-xs)]">ExitsResponsiveDataView</code>).
            </p>
          </div>
          <div className="flex min-w-0 flex-col gap-1.5">
            <span className="text-[length:var(--exits-text-xs)] text-[var(--exits-danger)]">
              Don’t
            </span>
            <p className="m-0 text-[length:var(--exits-text-sm)]">
              Force wide horizontal table scroll on every mobile screen. Use h-scroll only as an
              explicit exception.
            </p>
          </div>
        </div>
        <div className="flex flex-wrap gap-2" aria-hidden>
          <TableActionButton action="edit" label="Example edit" />
          <TableActionButton
            action="close"
            label="Example more"
            variant="ghost"
            icon={<MoreHorizontal className="size-4" aria-hidden />}
          />
        </div>
      </div>
    </Card>
  );
}
