import { useState, type CSSProperties } from "react";
import { QuantityStepper } from "@/components/exits/MoneyQuantity";
import { UiStandardsCopyCommand } from "@/features/ui-standards/UiStandardsCopyCommand";
import { PlaygroundSection } from "@/features/ui-standards/UiStandardsSnippetBlock";
import {
  typographyCursorCommand,
  typographyFromOriginal,
  type TypographyProperty,
} from "@/features/ui-standards/relative-typography";

export function UiStandardsSidenavTypography() {
  const [amount, setAmount] = useState(1);
  const [fontWeightPercent, setFontWeightPercent] = useState(0);
  const [fontSizePercent, setFontSizePercent] = useState(0);
  const [bothPercent, setBothPercent] = useState(0);
  const value = typographyFromOriginal(fontWeightPercent, fontSizePercent, bothPercent);
  const previewStyle = {
    "--sidenav-font-weight": String(value.fontWeight),
    "--sidenav-font-size": value.fontSize,
  } as CSSProperties;

  function apply(property: TypographyProperty, direction: 1 | -1) {
    if (!Number.isFinite(amount) || amount <= 0) return;
    const delta = direction * amount;
    if (property === "font-weight") setFontWeightPercent((current) => current + delta);
    else if (property === "font-size") setFontSizePercent((current) => current + delta);
    else setBothPercent((current) => current + delta);
  }

  function reset() {
    setFontWeightPercent(0);
    setFontSizePercent(0);
    setBothPercent(0);
  }

  return (
    <PlaygroundSection testId="ui-standards-typography" className="">
      <div className="ui-standards-type-controls">
        <QuantityStepper
          compact
          variant="outline"
          value={amount}
          onChange={setAmount}
          min={1}
          step={1}
          precision={0}
          ariaLabel="Adjustment percent"
          decreaseLabel="Decrease adjustment percent"
          increaseLabel="Increase adjustment percent"
          valueTestId="ui-standards-sidenav-type-amount"
        />
        <button type="button" data-testid="ui-standards-sidenav-type-fw-up" onClick={() => apply("font-weight", 1)}>
          FW+
        </button>
        <button type="button" data-testid="ui-standards-sidenav-type-fw-down" onClick={() => apply("font-weight", -1)}>
          FW-
        </button>
        <button type="button" data-testid="ui-standards-sidenav-type-fz-up" onClick={() => apply("font-size", 1)}>
          FZ+
        </button>
        <button type="button" data-testid="ui-standards-sidenav-type-fz-down" onClick={() => apply("font-size", -1)}>
          FZ-
        </button>
        <button type="button" data-testid="ui-standards-sidenav-type-fwz-up" onClick={() => apply("both", 1)}>
          FWZ+
        </button>
        <button type="button" data-testid="ui-standards-sidenav-type-fwz-down" onClick={() => apply("both", -1)}>
          FWZ-
        </button>
        <button type="button" data-testid="ui-standards-sidenav-type-reset" onClick={reset}>
          Reset
        </button>
      </div>
      <div className="ui-standards-type-stage">
        <article
          className="ui-standards-type-sample"
          data-testid="ui-standards-sidenav-type-preview"
          data-font-weight={value.fontWeight}
          data-font-size={value.fontSize}
          style={previewStyle}
        >
          <p className="ui-standards-type-sample__text">Navigation</p>
          <p className="ui-standards-type-sample__values">
            <span data-testid="ui-standards-sidenav-type-weight">weight: {value.fontWeight}</span>
            <span data-testid="ui-standards-sidenav-type-size">size: {value.fontSize}</span>
          </p>
        </article>
      </div>
      <UiStandardsCopyCommand
        standard="Typography"
        standardStatus="locked"
        command={typographyCursorCommand(fontWeightPercent, fontSizePercent, bothPercent)}
        context="Example: table header fw+10"
      />
    </PlaygroundSection>
  );
}
