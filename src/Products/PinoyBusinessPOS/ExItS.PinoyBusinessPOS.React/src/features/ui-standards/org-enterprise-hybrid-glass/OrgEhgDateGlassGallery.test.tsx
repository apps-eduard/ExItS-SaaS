import { describe, expect, it } from "vitest";
import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { ToastProvider } from "@/components/exits/ToastProvider";
import { PreferencesProvider } from "@/hooks/usePreferences";
import { OrgEhgDateGlassGallery } from "@/features/ui-standards/org-enterprise-hybrid-glass/OrgEhgDateGlassGallery";

function wrap(ui: React.ReactNode) {
  return (
    <MemoryRouter>
      <PreferencesProvider>
        <ToastProvider>{ui}</ToastProvider>
      </PreferencesProvider>
    </MemoryRouter>
  );
}

async function chooseDropdown(
  user: ReturnType<typeof userEvent.setup>,
  testId: string,
  optionSlug: string,
) {
  const root = screen.getByTestId(testId);
  const field = within(root).getByTestId(`${testId}-field`);
  if (field.getAttribute("aria-expanded") !== "true") {
    await user.click(field);
  }
  await user.click(within(root).getByTestId(`${testId}-option-${optionSlug}`));
}

describe("OrgEhgDateGlassGallery", () => {
  it("defaults to Quasar date-time glass with light/dark samples", () => {
    render(wrap(<OrgEhgDateGlassGallery />));
    const root = screen.getByTestId("ui-standard-org-ehg-date-glass");
    const playground = within(root).getByTestId("ui-standard-card-org-ehg-date-glass-playground");
    const light = within(root).getByTestId("org-ehg-date-glass-light");
    expect(light).toHaveAttribute("data-org-ehg-theme", "light");
    expect(within(root).getByTestId("org-ehg-date-glass-dark")).toHaveAttribute(
      "data-org-ehg-theme",
      "dark",
    );
    expect(playground.compareDocumentPosition(light) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy();

    const preview = screen.getByTestId("ui-standard-org-ehg-date-pg-preview-field");
    expect(preview).toHaveAttribute("data-variety", "quasar");
    expect(preview).toHaveClass("org-ehg-date-glass-field--kind-datetime");
    expect(preview).toHaveClass("org-ehg-date-glass-field--quasar");

    expect(within(light).getByTestId("org-ehg-date-glass-light-quasar-default")).toHaveAttribute(
      "data-variety",
      "quasar",
    );
    expect(within(light).getByTestId("org-ehg-date-glass-light-soft")).toBeInTheDocument();
    expect(within(light).getByTestId("org-ehg-date-glass-light-focused")).toHaveClass(
      "org-ehg-date-glass-field--focus-thick-below",
    );
    expect(within(light).getByTestId("org-ehg-date-glass-light-popover-date")).toHaveAttribute(
      "data-variety",
      "android",
    );
    expect(within(light).getByTestId("org-ehg-date-glass-light-popover-datetime")).toHaveAttribute(
      "data-variety",
      "android",
    );
    expect(within(light).getAllByText("Select date & time").length).toBeGreaterThan(0);
    expect(within(light).getAllByText("OK").length).toBeGreaterThan(0);
    expect(within(light).getByTestId("org-ehg-date-glass-light-popover-quasar-range")).toHaveAttribute(
      "aria-label",
      "Quasar date range picker",
    );
  });

  it("updates variety, kind, intensity, focus animation, and opens Quasar popover", async () => {
    const user = userEvent.setup();
    render(wrap(<OrgEhgDateGlassGallery />));

    const card = screen.getByTestId("ui-standard-card-org-ehg-date-glass-playground");
    expect(card).toHaveAttribute("data-bg-option", "soft-ambient");

    const preview = screen.getByTestId("ui-standard-org-ehg-date-pg-preview-field");
    expect(preview).toHaveClass("org-ehg-date-glass-field--medium");
    expect(preview).toHaveClass("org-ehg-date-glass-field--focus-thick-below");

    await chooseDropdown(user, "ui-standard-org-ehg-date-pg-variety", "sidebar");
    expect(preview).toHaveClass("org-ehg-date-glass-field--sidebar");
    expect(preview).toHaveAttribute("data-variety", "sidebar");

    await chooseDropdown(user, "ui-standard-org-ehg-date-pg-variety", "android");
    expect(preview).toHaveAttribute("data-variety", "android");

    await chooseDropdown(user, "ui-standard-org-ehg-date-pg-variety", "quasar-(default)");
    expect(preview).toHaveClass("org-ehg-date-glass-field--quasar");
    expect(preview).toHaveAttribute("data-variety", "quasar");

    await chooseDropdown(user, "ui-standard-org-ehg-date-pg-kind", "date");
    expect(preview).toHaveClass("org-ehg-date-glass-field--kind-date");

    await chooseDropdown(user, "ui-standard-org-ehg-date-pg-kind", "date-and-time-glass");
    expect(preview).toHaveClass("org-ehg-date-glass-field--kind-datetime");
    expect(preview).toHaveClass("org-ehg-date-glass-field--medium");
    expect(preview).toHaveAttribute("data-variety", "quasar");

    await chooseDropdown(user, "ui-standard-org-ehg-date-pg-kind", "time-only");
    expect(preview).toHaveClass("org-ehg-date-glass-field--kind-time");

    await chooseDropdown(user, "ui-standard-org-ehg-date-pg-kind", "date-range");
    expect(preview).toHaveClass("org-ehg-date-glass-field--kind-range");

    await chooseDropdown(user, "ui-standard-org-ehg-date-pg-kind", "date-and-time-glass");
    expect(preview).toHaveClass("org-ehg-date-glass-field--kind-datetime");

    await chooseDropdown(user, "ui-standard-org-ehg-date-pg-intensity", "ultra-glass");
    expect(preview).toHaveClass("org-ehg-date-glass-field--ultra");

    await chooseDropdown(user, "ui-standard-org-ehg-date-pg-focus-animation", "glow");
    expect(preview).toHaveClass("org-ehg-date-glass-field--focus-glow");

    await chooseDropdown(user, "ui-standard-org-ehg-date-pg-background", "5.-soft-vignette");
    expect(card).toHaveAttribute("data-bg-option", "soft-vignette");

    await user.click(screen.getByTestId("ui-standard-org-ehg-date-pg-theme"));
    expect(card).toHaveAttribute("data-org-ehg-theme", "dark");

    await user.click(screen.getByTestId("ui-standard-org-ehg-date-pg-preview-field-trigger"));
    const popover = await screen.findByTestId("ui-standard-org-ehg-date-pg-preview-field-popover");
    expect(popover).toHaveAttribute("data-variety", "quasar");
    expect(popover).toHaveAttribute("data-intensity", "ultra");
    expect(popover).toHaveClass("org-ehg-date-glass-popover--ultra");
    expect(within(popover).getByRole("textbox", { name: "Hour" })).toHaveClass(
      "org-ehg-date-glass-popover__quasar-time-value",
    );
    expect(within(popover).getByText("Set")).toBeInTheDocument();
    await user.click(within(popover).getByText("Clear"));
    expect(screen.getByTestId("ui-standard-org-ehg-date-pg-preview-field-trigger")).toHaveAttribute(
      "aria-expanded",
      "false",
    );
    const closed = screen.queryByTestId("ui-standard-org-ehg-date-pg-preview-field-popover");
    if (closed) {
      expect(closed).toHaveAttribute("data-state", "closed");
    }
  });

  it("opens Quasar month and year pickers without shrinking header/footer, and supports range", async () => {
    const user = userEvent.setup();
    render(wrap(<OrgEhgDateGlassGallery />));

    const light = screen.getByTestId("org-ehg-date-glass-light");
    const quasarDate = within(light).getByTestId("org-ehg-date-glass-light-popover-quasar-date");
    expect(within(quasarDate).getByLabelText("Previous month")).toBeInTheDocument();
    expect(within(quasarDate).getByLabelText("Next month")).toBeInTheDocument();
    expect(within(quasarDate).getByLabelText("Previous year")).toBeInTheDocument();
    expect(within(quasarDate).getByLabelText("Next year")).toBeInTheDocument();

    await user.click(within(quasarDate).getByLabelText("Select month"));
    expect(within(quasarDate).getByLabelText("Select month")).toHaveAttribute("aria-pressed", "true");
    expect(within(quasarDate).getByLabelText("Previous month")).toBeInTheDocument();
    expect(within(quasarDate).getByLabelText("Next year")).toBeInTheDocument();
    expect(within(quasarDate).getByRole("button", { name: "Feb" })).toBeInTheDocument();
    await user.click(within(quasarDate).getByLabelText("Select month"));
    expect(within(quasarDate).getByLabelText("Select month")).toHaveAttribute(
      "aria-pressed",
      "false",
    );

    await user.click(within(quasarDate).getByLabelText("Select year"));
    expect(within(quasarDate).getByLabelText("Select year")).toHaveAttribute("aria-pressed", "true");
    expect(within(quasarDate).getByLabelText("Previous years")).toBeInTheDocument();
    expect(within(quasarDate).getByLabelText("Next years")).toBeInTheDocument();
    expect(
      within(quasarDate).getByRole("button", { name: String(new Date().getFullYear()) }),
    ).toBeInTheDocument();
    expect(
      within(quasarDate)
        .getAllByRole("button")
        .filter((btn) => /^\d{4}$/.test(btn.textContent ?? "")).length,
    ).toBe(18);

    const quasarDatetime = within(light).getByTestId(
      "org-ehg-date-glass-light-popover-quasar-datetime",
    );
    expect(within(quasarDatetime).getByLabelText("Time")).toBeInTheDocument();
    expect(within(quasarDatetime).getByRole("textbox", { name: "Hour" })).toBeInTheDocument();
    expect(within(quasarDatetime).getByRole("textbox", { name: "Minute" })).toBeInTheDocument();
    expect(within(quasarDatetime).getByText("Hours")).toBeInTheDocument();
    expect(within(quasarDatetime).getByText("Minutes")).toBeInTheDocument();
    expect(within(quasarDatetime).getByText("Now")).toBeInTheDocument();
    expect(within(quasarDatetime).getByText("Set")).toBeInTheDocument();
    await user.click(within(quasarDatetime).getByText("Now"));
    const now = new Date();
    const hourPad = String(now.getHours() % 12 || 12).padStart(2, "0");
    const minute = String(now.getMinutes()).padStart(2, "0");
    const period = now.getHours() >= 12 ? "PM" : "AM";
    const monthGrid = within(quasarDatetime).getByRole("grid", {
      name: `${now.toLocaleString("en-US", { month: "long" })} ${now.getFullYear()}`,
    });
    expect(
      within(monthGrid).getByRole("button", { name: String(now.getDate()) }),
    ).toHaveAttribute("aria-pressed", "true");
    const timeBlock = within(quasarDatetime).getByLabelText("Time");
    const hourInput = within(timeBlock).getByRole("textbox", { name: "Hour" });
    const minuteInput = within(timeBlock).getByRole("textbox", { name: "Minute" });
    expect(hourInput).toHaveValue(hourPad);
    expect(minuteInput).toHaveValue(minute);
    expect(timeBlock).toHaveTextContent(period);
    await user.clear(hourInput);
    await user.type(hourInput, "09");
    expect(hourInput).toHaveValue("09");
    await user.clear(minuteInput);
    await user.type(minuteInput, "45");
    expect(minuteInput).toHaveValue("45");
    await user.click(within(quasarDatetime).getByLabelText("Select month"));
    expect(within(quasarDatetime).getByLabelText("Time")).toBeInTheDocument();
    expect(within(quasarDatetime).getByText("Set")).toBeInTheDocument();
    expect(within(quasarDatetime).getByLabelText("Previous month")).toBeInTheDocument();
    await user.click(within(quasarDatetime).getByLabelText("Select year"));
    expect(within(quasarDatetime).getByLabelText("Time")).toBeInTheDocument();
    expect(within(quasarDatetime).getByText("Set")).toBeInTheDocument();
    expect(within(quasarDatetime).getByLabelText("Previous years")).toBeInTheDocument();

    const quasarTime = within(light).getByTestId("org-ehg-date-glass-light-popover-quasar-time");
    expect(quasarTime).toHaveAttribute("aria-label", "Quasar time picker");
    expect(within(quasarTime).getByText("Now")).toBeInTheDocument();
    expect(within(quasarTime).queryByLabelText("Select month")).not.toBeInTheDocument();

    const quasarRange = within(light).getByTestId("org-ehg-date-glass-light-popover-quasar-range");
    expect(within(quasarRange).getByRole("button", { name: "20" })).toHaveAttribute(
      "aria-pressed",
      "true",
    );
    expect(within(quasarRange).getByRole("button", { name: "27" })).toHaveAttribute(
      "aria-pressed",
      "true",
    );
    expect(within(quasarRange).getByText("Set")).toBeInTheDocument();
  });
});
