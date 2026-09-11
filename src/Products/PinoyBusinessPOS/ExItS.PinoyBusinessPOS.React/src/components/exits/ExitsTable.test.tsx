import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import {
  cycleExitsTableSort,
  ExitsTable,
  ExitsTableBody,
  ExitsTableCell,
  ExitsTableCheckbox,
  ExitsTableContainer,
  ExitsTableFooter,
  ExitsTableHead,
  ExitsTableHeader,
  ExitsTableMobile,
  ExitsTableMobileRow,
  ExitsTableOutputActions,
  ExitsTablePagination,
  ExitsTableRow,
  ExitsTableToolbar,
} from "@/components/exits/ExitsTable";

describe("ExitsTable foundation", () => {
  it("renders header, aligned cells, footer, and mobile slot", () => {
    render(
      <ExitsTableContainer data-testid="sample-table">
        <ExitsTable>
          <ExitsTableHeader>
            <ExitsTableRow>
              <ExitsTableHead cellAlign="text">Product</ExitsTableHead>
              <ExitsTableHead cellAlign="money">Line total</ExitsTableHead>
            </ExitsTableRow>
          </ExitsTableHeader>
          <ExitsTableBody>
            <ExitsTableRow data-testid="sample-row">
              <ExitsTableCell cellAlign="text">Apple</ExitsTableCell>
              <ExitsTableCell cellAlign="money" emphasis="semibold">
                ₱900.00
              </ExitsTableCell>
            </ExitsTableRow>
          </ExitsTableBody>
          <ExitsTableFooter>
            <ExitsTableRow>
              <ExitsTableCell cellAlign="actions" emphasis="bold">
                Total
              </ExitsTableCell>
              <ExitsTableCell cellAlign="money" emphasis="bold">
                ₱900.00
              </ExitsTableCell>
            </ExitsTableRow>
          </ExitsTableFooter>
        </ExitsTable>
        <ExitsTableMobile data-testid="sample-mobile">
          <ExitsTableMobileRow>Apple · ₱900.00</ExitsTableMobileRow>
        </ExitsTableMobile>
      </ExitsTableContainer>,
    );

    expect(screen.getByTestId("sample-table")).toHaveClass("exits-table-container");
    expect(screen.getByRole("columnheader", { name: "Product" })).toHaveAttribute(
      "data-align",
      "text",
    );
    expect(screen.getByRole("columnheader", { name: "Line total" })).toHaveAttribute(
      "data-align",
      "money",
    );
    expect(screen.getByTestId("sample-row")).toBeInTheDocument();
    expect(screen.getByText("Total")).toBeInTheDocument();
    expect(screen.getByTestId("sample-mobile")).toHaveClass("exits-table-mobile");
  });

  it("supports toolbar, sortable head, selection, and pagination controls", async () => {
    const user = userEvent.setup();
    const onSort = vi.fn();
    const onPageChange = vi.fn();
    const onPageSizeChange = vi.fn();

    render(
      <ExitsTableContainer>
        <ExitsTableToolbar search={<input aria-label="Search products" />} selection="2 selected" />
        <ExitsTable>
          <ExitsTableHeader>
            <ExitsTableRow>
              <ExitsTableHead cellAlign="center">
                <ExitsTableCheckbox aria-label="Select all" />
              </ExitsTableHead>
              <ExitsTableHead
                sortable
                sortDirection={null}
                onSort={onSort}
                data-testid="sort-product"
              >
                Product
              </ExitsTableHead>
            </ExitsTableRow>
          </ExitsTableHeader>
          <ExitsTableBody>
            <ExitsTableRow selected interactive data-testid="selected-row">
              <ExitsTableCell cellAlign="center">
                <ExitsTableCheckbox aria-label="Select row" defaultChecked />
              </ExitsTableCell>
              <ExitsTableCell>Apple</ExitsTableCell>
            </ExitsTableRow>
          </ExitsTableBody>
        </ExitsTable>
        <ExitsTablePagination
          page={1}
          pageSize={25}
          total={2}
          onPageChange={onPageChange}
          onPageSizeChange={onPageSizeChange}
          rowsPerPageLabel="Rows per page"
          previousLabel="Previous"
          nextLabel="Next"
          rangeLabel="{from}–{to} of {total}"
        />
      </ExitsTableContainer>,
    );

    expect(screen.getByTestId("exits-table-toolbar")).toBeInTheDocument();
    expect(screen.getByText("2 selected")).toBeInTheDocument();
    expect(screen.getByTestId("selected-row")).toHaveAttribute("data-selected", "true");
    expect(screen.getByTestId("exits-table-pagination-range")).toHaveTextContent("1–2 of 2");
    expect(screen.getByTestId("exits-table-prev")).toBeDisabled();
    expect(screen.getByTestId("exits-table-next")).toBeDisabled();

    await user.click(screen.getByTestId("sort-product-sort"));
    expect(onSort).toHaveBeenCalledTimes(1);

    await user.selectOptions(screen.getByTestId("exits-table-page-size"), "10");
    expect(onPageSizeChange).toHaveBeenCalledWith(10);

    const pageSize = screen.getByTestId("exits-table-page-size");
    expect([...pageSize.querySelectorAll("option")].map((opt) => opt.getAttribute("value"))).toEqual([
      "10",
      "25",
      "50",
      "100",
    ]);
    expect(pageSize).toHaveValue("25");
  });

  it("cycles sort none → asc → desc → none", () => {
    expect(cycleExitsTableSort(null, null, "product")).toEqual({
      key: "product",
      direction: "asc",
    });
    expect(cycleExitsTableSort("product", "asc", "product")).toEqual({
      key: "product",
      direction: "desc",
    });
    expect(cycleExitsTableSort("product", "desc", "product")).toEqual({
      key: null,
      direction: null,
    });
  });

  it("renders Output Actions in the toolbar right slot with accessible labels", () => {
    const onCsv = vi.fn();
    const onXlsx = vi.fn();
    const onPdf = vi.fn();
    const onPrint = vi.fn();

    render(
      <ExitsTableToolbar
        search={<input aria-label="Search products" />}
        output={
          <ExitsTableOutputActions
            csvLabel="Export CSV"
            xlsxLabel="Export Excel"
            pdfLabel="Export PDF"
            printLabel="Print"
            menuLabel="Export & Print"
            onCsv={onCsv}
            onXlsx={onXlsx}
            onPdf={onPdf}
            onPrint={onPrint}
          />
        }
      />,
    );

    expect(screen.getByTestId("exits-table-toolbar-output")).toContainElement(
      screen.getByTestId("exits-table-output-actions"),
    );
    expect(screen.getByTestId("exits-table-output-csv")).toHaveAttribute("aria-label", "Export CSV");
    expect(screen.getByTestId("exits-table-output-xlsx")).toHaveAttribute(
      "aria-label",
      "Export Excel",
    );
    expect(screen.getByTestId("exits-table-output-pdf")).toHaveAttribute("aria-label", "Export PDF");
    expect(screen.getByTestId("exits-table-output-print")).toHaveAttribute("aria-label", "Print");
    expect(screen.getByTestId("exits-table-output-menu")).toHaveAttribute(
      "aria-label",
      "Export & Print",
    );
  });
});
