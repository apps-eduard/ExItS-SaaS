import { describe, expect, it } from "vitest";
import { render, screen } from "@testing-library/react";
import { ExitsDataRecordCard } from "@/components/exits/ExitsDataRecordCard";
import { ExitsResponsiveDataView } from "@/components/exits/ExitsResponsiveDataView";
import {
  pickColumnsByPriority,
  resolveResponsiveDataLayout,
  responsiveDataMultiSelectAllowed,
  type ResponsiveColumnMeta,
} from "@/components/exits/responsive-data-view";

describe("responsive data view helpers", () => {
  it("resolves auto table vs list from width", () => {
    expect(
      resolveResponsiveDataLayout({ strategy: "auto", isWideEnoughForTable: true }),
    ).toBe("table");
    expect(
      resolveResponsiveDataLayout({ strategy: "auto", isWideEnoughForTable: false }),
    ).toBe("list");
  });

  it("maps preview reference widths to TABLE / LIST at default 1024 min", () => {
    const min = 1024;
    expect(
      resolveResponsiveDataLayout({
        strategy: "auto",
        isWideEnoughForTable: 1280 >= min,
      }),
    ).toBe("table");
    expect(
      resolveResponsiveDataLayout({
        strategy: "auto",
        isWideEnoughForTable: 834 >= min,
      }),
    ).toBe("list");
    expect(
      resolveResponsiveDataLayout({
        strategy: "auto",
        isWideEnoughForTable: 390 >= min,
      }),
    ).toBe("list");
  });

  it("forces table when horizontal-scroll exception is set", () => {
    expect(
      resolveResponsiveDataLayout({
        strategy: "auto",
        isWideEnoughForTable: false,
        allowHorizontalScroll: true,
      }),
    ).toBe("table");
    expect(
      resolveResponsiveDataLayout({ strategy: "table", isWideEnoughForTable: false }),
    ).toBe("table");
    expect(
      resolveResponsiveDataLayout({ strategy: "list", isWideEnoughForTable: true }),
    ).toBe("list");
  });

  it("keeps multi-select off in list mode by default", () => {
    expect(responsiveDataMultiSelectAllowed("table")).toBe(true);
    expect(responsiveDataMultiSelectAllowed("list")).toBe(false);
    expect(responsiveDataMultiSelectAllowed("list", true)).toBe(true);
  });

  it("filters columns by priority", () => {
    const cols: ResponsiveColumnMeta[] = [
      { id: "name", label: "Name", priority: "primary" },
      { id: "sku", label: "SKU", priority: "secondary" },
      { id: "qty", label: "Qty", priority: "metric" },
      { id: "note", label: "Note", priority: "detail" },
    ];
    expect(pickColumnsByPriority(cols, ["primary", "metric"]).map((c) => c.id)).toEqual([
      "name",
      "qty",
    ]);
  });
});

describe("ExitsResponsiveDataView", () => {
  it("shows table slot in table layout and list slot in list layout", () => {
    const { rerender } = render(
      <ExitsResponsiveDataView
        layout="table"
        table={<div>TABLE_BODY</div>}
        list={<div>LIST_BODY</div>}
      />,
    );
    expect(screen.getByTestId("exits-responsive-data")).toHaveAttribute("data-layout", "table");
    expect(screen.getByTestId("exits-responsive-data-table")).toHaveTextContent("TABLE_BODY");

    rerender(
      <ExitsResponsiveDataView
        layout="list"
        table={<div>TABLE_BODY</div>}
        list={<div>LIST_BODY</div>}
      />,
    );
    expect(screen.getByTestId("exits-responsive-data")).toHaveAttribute("data-layout", "list");
    expect(screen.getByTestId("exits-responsive-data-list")).toHaveTextContent("LIST_BODY");
  });
});

describe("ExitsDataRecordCard", () => {
  it("renders title, status, fields, and actions", () => {
    render(
      <ExitsDataRecordCard
        title="Juan Dela Cruz"
        subtitle="Personal"
        status={<span>Active</span>}
        fields={[{ label: "Balance", value: "₱1,250.00", emphasize: true }]}
        primaryAction={<button type="button">Edit</button>}
      />,
    );
    expect(screen.getByText("Juan Dela Cruz")).toBeInTheDocument();
    expect(screen.getByText("Personal")).toBeInTheDocument();
    expect(screen.getByText("Active")).toBeInTheDocument();
    expect(screen.getByText("Balance")).toBeInTheDocument();
    expect(screen.getByText("₱1,250.00")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Edit" })).toBeInTheDocument();
  });
});
