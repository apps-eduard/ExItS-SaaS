import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { describe, expect, it } from "vitest";
import { PageHeader } from "@/components/exits/PageHeader";
import { PreferencesProvider } from "@/hooks/usePreferences";
import { I18nProvider } from "@/i18n/I18nProvider";

function renderHeader(ui: React.ReactElement) {
  return render(
    <PreferencesProvider>
      <I18nProvider>
        <MemoryRouter>{ui}</MemoryRouter>
      </I18nProvider>
    </PreferencesProvider>,
  );
}

describe("PageHeader", () => {
  it("keeps the description in an info popover beside the title", async () => {
    const user = userEvent.setup();
    renderHeader(
      <PageHeader title="Manager home" description="Operations hub" />,
    );
    expect(screen.queryByTestId("page-header-back")).not.toBeInTheDocument();
    const title = screen.getByRole("heading", { name: "Manager home" });
    const info = screen.getByTestId("page-header-info-toggle");
    expect(title.parentElement).toContainElement(info);
    expect(info).toHaveAttribute("data-intent", "info");
    expect(screen.queryByTestId("page-header-description")).not.toBeInTheDocument();
    expect(screen.getByTestId("page-header").className).toMatch(/page-header/);

    await user.click(info);
    const popover = screen.getByRole("dialog", { name: "Manager home" });
    expect(popover).not.toHaveAttribute("aria-modal");
    expect(screen.getByTestId("page-header-description")).toHaveTextContent("Operations hub");
  });

  it("supports compact operational variant without description chrome", () => {
    renderHeader(
      <PageHeader
        variant="compact"
        title="New Sale"
        description="Should not render"
        actions={<button type="button">Exit selling</button>}
      />,
    );
    const header = screen.getByTestId("page-header");
    expect(header).toHaveAttribute("data-variant", "compact");
    expect(header.className).toMatch(/page-header--compact/);
    expect(screen.queryByTestId("page-header-description")).not.toBeInTheDocument();
    expect(screen.getByTestId("page-header-actions")).toHaveTextContent("Exit selling");
  });

  it("renders canonical back link with accessible name and density-sized control", () => {
    renderHeader(
      <PageHeader
        title="Shifts"
        description="View shifts"
        backTo="/shifts"
        backLabel="Back to shifts"
        backTestId="page-header-back-shifts"
      />,
    );
    const back = screen.getByTestId("page-header-back-shifts");
    expect(back).toHaveAttribute("href", "/shifts");
    expect(back).toHaveAccessibleName("Back to shifts");
    expect(back.className).toMatch(/exits-control-height/);
  });

  it("renders actions / badge slot when provided", () => {
    renderHeader(
      <PageHeader
        title="Customers"
        description="Manage customers."
        actions={<button type="button">Add customer</button>}
      />,
    );
    expect(screen.getByTestId("page-header-actions")).toHaveTextContent("Add customer");
  });

  it("omits description when not provided", () => {
    renderHeader(<PageHeader title="Shifts" />);
    expect(screen.queryByTestId("page-header-description")).not.toBeInTheDocument();
  });

  it("closes the info popover on a second tap", async () => {
    const user = userEvent.setup();
    renderHeader(
      <PageHeader
        title="Products"
        description="Manage catalog products for this organization."
      />,
    );

    const toggle = screen.getByTestId("page-header-info-toggle");
    await user.click(toggle);
    expect(toggle).toHaveAttribute("aria-expanded", "true");
    expect(screen.getByTestId("page-header-description")).toHaveTextContent(
      "Manage catalog products for this organization.",
    );

    await user.click(toggle);
    expect(toggle).toHaveAttribute("aria-expanded", "false");
    expect(screen.queryByTestId("page-header-description")).not.toBeInTheDocument();
  });

  it("renders subtitle when provided", () => {
    renderHeader(
      <PageHeader title="Edit product" subtitle="Coke 330ml" backTo="/catalog" backLabel="Back" />,
    );
    expect(screen.getByTestId("page-header-subtitle")).toHaveTextContent("Coke 330ml");
  });

  it("still opens the info popover when an older collapsible flag is passed", async () => {
    const user = userEvent.setup();
    renderHeader(
      <PageHeader
        title="Products"
        description="Always visible lede"
        descriptionCollapsible={false}
      />,
    );
    expect(screen.queryByTestId("page-header-description")).not.toBeInTheDocument();
    await user.click(screen.getByTestId("page-header-info-toggle"));
    expect(screen.getByTestId("page-header-description")).toHaveTextContent("Always visible lede");
  });
});
