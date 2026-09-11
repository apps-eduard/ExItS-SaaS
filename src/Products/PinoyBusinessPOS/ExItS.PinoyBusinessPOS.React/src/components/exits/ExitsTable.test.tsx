import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import {
  ExitsTable,
  ExitsTableBody,
  ExitsTableCell,
  ExitsTableContainer,
  ExitsTableFooter,
  ExitsTableHead,
  ExitsTableHeader,
  ExitsTableMobile,
  ExitsTableMobileRow,
  ExitsTableRow,
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
});
