import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import {
  cycleExitsTableSort,
  ExitsTable,
  ExitsTableActions,
  ExitsTableBody,
  ExitsTableCell,
  ExitsTableCheckbox,
  ExitsTableContainer,
  ExitsTableEditMenu,
  ExitsTableFooter,
  ExitsTableHead,
  ExitsTableHeader,
  ExitsTableInlineEditor,
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

  it("aligns sortable numeric headers to end and supports actions / editing presentation", async () => {
    const user = userEvent.setup();
    const onSort = vi.fn();
    const onAction = vi.fn();
    const onRowClick = vi.fn();

    render(
      <ExitsTableContainer>
        <ExitsTable>
          <ExitsTableHeader>
            <ExitsTableRow>
              <ExitsTableHead
                cellAlign="numeric"
                colSize="numeric"
                sortable
                sortDirection={null}
                onSort={onSort}
                data-testid="sort-qty"
              >
                Quantity
              </ExitsTableHead>
              <ExitsTableHead cellAlign="text" colSize="sku" data-testid="sku-head">
                SKU
              </ExitsTableHead>
              <ExitsTableHead cellAlign="actions" colSize="actions" stickyEnd>
                Actions
              </ExitsTableHead>
            </ExitsTableRow>
          </ExitsTableHeader>
          <ExitsTableBody>
            <ExitsTableRow interactive editing onClick={onRowClick} data-testid="editing-row">
              <ExitsTableCell cellAlign="numeric" colSize="numeric">
                <ExitsTableInlineEditor
                  stealth
                  invalid
                  error="Quantity must be greater than zero."
                  errorId="qty-err"
                >
                  <span>editor</span>
                </ExitsTableInlineEditor>
              </ExitsTableCell>
              <ExitsTableCell cellAlign="text" colSize="sku" truncate title="PH-FRU-APPLE">
                PH-FRU-APPLE
              </ExitsTableCell>
              <ExitsTableCell cellAlign="actions" colSize="actions" stickyEnd>
                <ExitsTableActions data-testid="row-actions">
                  <button type="button" aria-label="Edit row" onClick={onAction}>
                    Edit
                  </button>
                </ExitsTableActions>
              </ExitsTableCell>
            </ExitsTableRow>
          </ExitsTableBody>
        </ExitsTable>
      </ExitsTableContainer>,
    );

    const qtyHead = screen.getByTestId("sort-qty");
    expect(qtyHead).toHaveAttribute("data-align", "numeric");
    expect(qtyHead).toHaveAttribute("data-col-size", "numeric");
    expect(qtyHead.className).toMatch(/exits-table__cell--numeric/);
    expect(qtyHead.className).toMatch(/exits-table__col--numeric/);
    expect(screen.getByTestId("sku-head")).toHaveAttribute("data-col-size", "sku");
    expect(screen.getByRole("columnheader", { name: "Actions" })).toHaveAttribute(
      "data-sticky-end",
      "true",
    );
    expect(screen.getByTestId("editing-row")).toHaveAttribute("data-editing", "true");
    expect(screen.getByRole("alert")).toHaveTextContent("Quantity must be greater than zero.");
    expect(screen.getByText("editor").parentElement).toHaveAttribute("data-stealth", "true");

    await user.click(screen.getByTestId("sort-qty-sort"));
    expect(onSort).toHaveBeenCalledTimes(1);

    await user.click(screen.getByRole("button", { name: "Edit row" }));
    expect(onAction).toHaveBeenCalledTimes(1);
    expect(onRowClick).not.toHaveBeenCalled();
  });

  it("renders edit field menu from configured column labels", async () => {
    const user = userEvent.setup();
    const onSelectField = vi.fn();
    const onEditAll = vi.fn();

    render(
      <ExitsTableEditMenu
        fields={[
          { key: "sku", label: "SKU" },
          { key: "quantity", label: "Quantity" },
          { key: "unitCost", label: "Unit cost" },
        ]}
        ariaLabel="Edit Apple"
        onSelectField={onSelectField}
        onEditAll={onEditAll}
      />,
    );

    await user.click(screen.getByRole("button", { name: "Edit Apple" }));
    expect(screen.getByText("Edit field")).toBeInTheDocument();
    expect(screen.queryByRole("menuitem", { name: "unitCost" })).not.toBeInTheDocument();
    await user.click(screen.getByRole("menuitem", { name: "Unit cost" }));
    expect(onSelectField).toHaveBeenCalledWith("unitCost");

    await user.click(screen.getByRole("button", { name: "Edit Apple" }));
    await user.click(screen.getByRole("menuitem", { name: /Edit all/i }));
    expect(onEditAll).toHaveBeenCalledTimes(1);
  });

  it("portals the edit field menu outside the table scroll viewport", async () => {
    const user = userEvent.setup();

    render(
      <ExitsTableContainer data-testid="portal-table-wrap">
        <div className="exits-table-scroll" data-testid="portal-table-scroll" style={{ overflow: "auto", maxHeight: 120 }}>
          <ExitsTable>
            <ExitsTableBody>
              <ExitsTableRow>
                <ExitsTableCell cellAlign="actions">
                  <ExitsTableActions>
                    <ExitsTableEditMenu
                      fields={[
                        { key: "sku", label: "SKU" },
                        { key: "quantity", label: "Quantity" },
                        { key: "unitCost", label: "Unit cost" },
                      ]}
                      ariaLabel="Edit Apple"
                      onSelectField={() => undefined}
                      onEditAll={() => undefined}
                    />
                  </ExitsTableActions>
                </ExitsTableCell>
              </ExitsTableRow>
            </ExitsTableBody>
          </ExitsTable>
        </div>
      </ExitsTableContainer>,
    );

    const scroll = screen.getByTestId("portal-table-scroll");
    const beforeHeight = scroll.scrollHeight;
    const beforeClient = scroll.clientHeight;

    await user.click(screen.getByRole("button", { name: "Edit Apple" }));
    const menu = screen.getByRole("menu");
    expect(menu).toHaveAttribute("data-exits-dropdown-portal", "true");
    expect(scroll.contains(menu)).toBe(false);
    expect(document.body.contains(menu)).toBe(true);
    expect(scroll.scrollHeight).toBe(beforeHeight);
    expect(scroll.clientHeight).toBe(beforeClient);
    expect(screen.getByRole("menuitem", { name: "SKU" })).toBeInTheDocument();
    expect(screen.getByRole("menuitem", { name: /Edit all/i })).toBeInTheDocument();
  });

  it("keeps stealth editor groups and quiet danger cancel class for alignment cues", () => {
    render(
      <ExitsTableContainer>
        <ExitsTable>
          <ExitsTableBody>
            <ExitsTableRow editing>
              <ExitsTableCell cellAlign="numeric" colSize="numeric">
                <div className="exits-table__qty-edit" data-testid="qty-edit-group">
                  <ExitsTableInlineEditor stealth>
                    <span>2</span>
                  </ExitsTableInlineEditor>
                  <span className="exits-table__uom">Kg</span>
                </div>
              </ExitsTableCell>
              <ExitsTableCell cellAlign="money" colSize="money">
                <div className="exits-table__money-edit" data-testid="money-edit-group">
                  <span className="exits-table__currency-prefix">₱</span>
                  <ExitsTableInlineEditor stealth>
                    <span>76.00</span>
                  </ExitsTableInlineEditor>
                </div>
              </ExitsTableCell>
              <ExitsTableCell cellAlign="actions" colSize="actions">
                <ExitsTableActions>
                  <button type="button" className="exits-table__action-cancel" aria-label="Cancel Apple editing">
                    Cancel
                  </button>
                </ExitsTableActions>
              </ExitsTableCell>
            </ExitsTableRow>
          </ExitsTableBody>
        </ExitsTable>
      </ExitsTableContainer>,
    );

    expect(screen.getByTestId("qty-edit-group")).toHaveClass("exits-table__qty-edit");
    expect(screen.getByTestId("money-edit-group")).toHaveClass("exits-table__money-edit");
    expect(screen.getByRole("button", { name: "Cancel Apple editing" })).toHaveClass(
      "exits-table__action-cancel",
    );
  });
});
