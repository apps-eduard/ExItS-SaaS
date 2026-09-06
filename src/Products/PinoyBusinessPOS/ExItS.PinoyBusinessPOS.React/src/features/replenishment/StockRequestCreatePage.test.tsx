import { describe, expect, it } from "vitest";
import { render, screen } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { StockRequestCreatePage } from "@/features/replenishment/StockRequestCreatePage";

describe("StockRequestCreatePage redirect", () => {
  it("redirects to retail warehouse request-stock", () => {
    render(
      <MemoryRouter initialEntries={["/inventory/stock-requests/new"]}>
        <Routes>
          <Route path="/inventory/stock-requests/new" element={<StockRequestCreatePage />} />
          <Route
            path="/warehouse/request-stock"
            element={<div data-testid="warehouse-request-stock">ok</div>}
          />
        </Routes>
      </MemoryRouter>,
    );
    expect(screen.getByTestId("warehouse-request-stock")).toBeInTheDocument();
  });
});
