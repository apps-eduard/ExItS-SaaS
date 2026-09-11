import { ChevronsDown, ChevronsUp, RotateCcw } from "lucide-react";
import { Button } from "@/components/ui/button";

export type UiStandardsDisclosureToolbarProps = {
  onExpandAll: () => void;
  onCollapseAll: () => void;
  onReset: () => void;
  expandLabel: string;
  collapseLabel: string;
  resetLabel: string;
};

export function UiStandardsDisclosureToolbar({
  onExpandAll,
  onCollapseAll,
  onReset,
  expandLabel,
  collapseLabel,
  resetLabel,
}: UiStandardsDisclosureToolbarProps) {
  return (
    <div
      className="flex flex-wrap items-center gap-2"
      data-testid="ui-standards-disclosure-toolbar"
    >
      <Button type="button" variant="ghost" onClick={onExpandAll} data-testid="ui-standards-expand-all">
        <ChevronsDown className="size-4" aria-hidden />
        {expandLabel}
      </Button>
      <Button
        type="button"
        variant="ghost"
        onClick={onCollapseAll}
        data-testid="ui-standards-collapse-all"
      >
        <ChevronsUp className="size-4" aria-hidden />
        {collapseLabel}
      </Button>
      <Button type="button" variant="ghost" onClick={onReset} data-testid="ui-standards-reset-layout">
        <RotateCcw className="size-4" aria-hidden />
        {resetLabel}
      </Button>
    </div>
  );
}
