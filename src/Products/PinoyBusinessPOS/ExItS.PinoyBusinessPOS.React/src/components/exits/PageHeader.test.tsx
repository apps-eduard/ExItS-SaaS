import { fireEvent, render, screen } from "@testing-library/react";
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
  it("renders title and optional always-visible description by default", () => {
    renderHeader(
      <PageHeader title="Manager home" description="Operations hub" />,
    );
    expect(screen.queryByTestId("page-header-back")).not.toBeInTheDocument();
    expect(screen.getByRole("heading", { name: "Manager home" })).toBeInTheDocument();
    expect(screen.getByTestId("page-header-description")).toHaveTextContent("Operations hub");
    expect(screen.queryByTestId("page-header-info-toggle")).not.toBeInTheDocument();
    expect(screen.getByTestId("page-header").className).toMatch(/page-header/);
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

  it("reveals description on hover when collapsible", () => {
    renderHeader(
      <PageHeader
        title="Products"
        description="Manage catalog products for this organization."
        descriptionCollapsible
      />,
    );

    const toggle = screen.getByTestId("page-header-info-toggle");
    const shell = screen.getByTestId("page-header-description-shell");
    expect(shell).toHaveAttribute("aria-hidden", "true");

    fireEvent.mouseEnter(toggle);
    expect(shell).toHaveAttribute("aria-hidden", "false");
    expect(screen.getByTestId("page-header-description")).toHaveTextContent(
      "Manage catalog products for this organization.",
    );

    fireEvent.mouseLeave(screen.getByText("Products").closest(".page-header__main")!);
    expect(shell).toHaveAttribute("aria-hidden", "true");
  });

  it("pins description open on tap until tapped again when collapsible", async () => {
    const user = userEvent.setup();
    renderHeader(
      <PageHeader
        title="Products"
        description="Manage catalog products for this organization."
        descriptionCollapsible
      />,
    );

    const toggle = screen.getByTestId("page-header-info-toggle");
    const shell = screen.getByTestId("page-header-description-shell");

    await user.click(toggle);
    expect(shell).toHaveAttribute("aria-hidden", "false");
    expect(toggle).toHaveAttribute("aria-expanded", "true");

    fireEvent.mouseLeave(screen.getByText("Products").closest(".page-header__main")!);
    expect(shell).toHaveAttribute("aria-hidden", "false");

    await user.click(toggle);
    expect(shell).toHaveAttribute("aria-hidden", "true");
    expect(toggle).toHaveAttribute("aria-expanded", "false");
  });

  it("renders subtitle when provided", () => {
    renderHeader(
      <PageHeader title="Edit product" subtitle="Coke 330ml" backTo="/catalog" backLabel="Back" />,
    );
    expect(screen.getByTestId("page-header-subtitle")).toHaveTextContent("Coke 330ml");
  });

  it("can keep description always visible when collapsible is disabled", () => {
    renderHeader(
      <PageHeader
        title="Products"
        description="Always visible lede"
        descriptionCollapsible={false}
      />,
    );
    expect(screen.queryByTestId("page-header-info-toggle")).not.toBeInTheDocument();
    expect(screen.getByTestId("page-header-description")).toHaveTextContent("Always visible lede");
  });
});
