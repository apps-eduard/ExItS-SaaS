import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it } from "vitest";
import { UiStandardsIconPlayground } from "@/features/ui-standards/UiStandardsIconPlayground";
import {
  ICON_CHIP_APPEARANCE_CATALOG,
  ICON_CHIP_DEFAULT_PREVIEW_HOST,
  iconChipPreviewHostTestId,
} from "@/features/ui-standards/icon-chip-global-default";
import { PreferencesProvider } from "@/hooks/usePreferences";

function renderPlayground() {
  return render(
    <PreferencesProvider>
      <UiStandardsIconPlayground />
    </PreferencesProvider>,
  );
}

describe("UiStandardsIconPlayground", () => {
  it("renders split preview lanes with a single selected sample host", () => {
    renderPlayground();
    const preview = screen.getByTestId("ui-standards-icon-preview");
    const context = within(preview).getByTestId("ui-standards-icon-preview-context");

    expect(within(preview).getByTestId("ui-standards-icon-preview-only")).toBeInTheDocument();
    expect(context).toBeInTheDocument();
    expect(within(preview).getByTestId("ui-standards-icon-chip-live")).toBeInTheDocument();

    expect(
      within(context).getByTestId(iconChipPreviewHostTestId(ICON_CHIP_DEFAULT_PREVIEW_HOST)),
    ).toHaveAttribute("data-preview-host", ICON_CHIP_DEFAULT_PREVIEW_HOST);
    expect(screen.getByTestId("ui-standards-icon-chip-qa-preview")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standards-icon-chip-qa-preview")).toHaveAttribute("data-shell", "org-ehg");

    expect(within(context).queryByTestId("ui-standards-icon-preview-host-card")).not.toBeInTheDocument();
    expect(screen.queryByTestId("ui-standards-icon-chip-sample-pennant")).not.toBeInTheDocument();
  });

  it("switches the right preview host when Sample changes", async () => {
    const user = userEvent.setup();
    renderPlayground();
    const preview = screen.getByTestId("ui-standards-icon-preview");
    const context = within(preview).getByTestId("ui-standards-icon-preview-context");
    const sampleRoot = screen.getByTestId("ui-standards-icon-preview-host");

    await user.click(within(sampleRoot).getByTestId("ui-standards-icon-preview-host-field"));
    await user.click(within(sampleRoot).getByTestId("ui-standards-icon-preview-host-option-card"));

    expect(within(context).getByTestId(iconChipPreviewHostTestId("card"))).toHaveAttribute(
      "data-preview-host",
      "card",
    );
    expect(screen.getByTestId("ui-standards-icon-chip-sample-pennant")).toBeInTheDocument();
    expect(screen.queryByTestId("ui-standards-icon-chip-qa-preview")).not.toBeInTheDocument();

    await user.click(within(sampleRoot).getByTestId("ui-standards-icon-preview-host-field"));
    await user.click(within(sampleRoot).getByTestId("ui-standards-icon-preview-host-option-kpi"));

    expect(within(context).getByTestId(iconChipPreviewHostTestId("kpi"))).toHaveAttribute(
      "data-preview-host",
      "kpi",
    );
    expect(screen.getByTestId("ui-standards-icon-chip-kpi-slot")).toBeInTheDocument();
  });

  it("renders Org EHG button sample with live icon chip", async () => {
    const user = userEvent.setup();
    renderPlayground();
    const preview = screen.getByTestId("ui-standards-icon-preview");
    const context = within(preview).getByTestId("ui-standards-icon-preview-context");
    const sampleRoot = screen.getByTestId("ui-standards-icon-preview-host");

    await user.click(within(sampleRoot).getByTestId("ui-standards-icon-preview-host-field"));
    await user.click(within(sampleRoot).getByTestId("ui-standards-icon-preview-host-option-button"));

    expect(within(context).getByTestId(iconChipPreviewHostTestId("button"))).toHaveAttribute(
      "data-preview-host",
      "button",
    );
    const btn = screen.getByTestId("ui-standards-icon-chip-btn-preview");
    expect(btn).toHaveAttribute("data-shell", "org-ehg");
    expect(within(btn).getByTestId("ui-standards-icon-chip-btn-slot")).toBeInTheDocument();
  });

  it("applies chip and icon size independently on the live preview", async () => {
    const user = userEvent.setup();
    renderPlayground();
    const live = screen.getByTestId("ui-standards-icon-chip-live");

    expect(live).toHaveAttribute("data-size", "md");
    expect(live).toHaveAttribute("data-icon-size", "md");

    const chipSizeRoot = screen.getByTestId("ui-standards-icon-chip-size");
    await user.click(within(chipSizeRoot).getByTestId("ui-standards-icon-chip-size-field"));
    await user.click(within(chipSizeRoot).getByTestId("ui-standards-icon-chip-size-option-xl"));

    const iconSizeRoot = screen.getByTestId("ui-standards-icon-icon-size");
    await user.click(within(iconSizeRoot).getByTestId("ui-standards-icon-icon-size-field"));
    await user.click(within(iconSizeRoot).getByTestId("ui-standards-icon-icon-size-option-sm"));

    const updatedLive = screen.getByTestId("ui-standards-icon-chip-live");
    expect(updatedLive).toHaveAttribute("data-size", "xl");
    expect(updatedLive).toHaveAttribute("data-icon-size", "sm");
    expect(updatedLive).toHaveClass("ui-standards-icon-chip--xl");
    expect(updatedLive.querySelector(".ui-standards-icon-chip__glyph--sm")).toBeInTheDocument();
    expect(updatedLive.querySelector(".ui-standards-icon-chip__glyph--xl")).not.toBeInTheDocument();
  });

  it("renders all appearance variant previews with distinct data-appearance", () => {
    renderPlayground();
    const row = screen.getByTestId("ui-standards-icon-variant-row");
    for (const entry of ICON_CHIP_APPEARANCE_CATALOG) {
      const chip = within(row).getByTestId(`ui-standards-icon-variant-${entry.id}`);
      expect(chip).toHaveAttribute("data-appearance", entry.id);
    }
    expect(within(row).getByTestId("ui-standards-icon-variant-soft-primary-ehg")).toBeInTheDocument();
    expect(within(row).getByTestId("ui-standards-icon-variant-soft-glass")).toBeInTheDocument();
  });
});