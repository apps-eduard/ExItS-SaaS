import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { Boxes, ClipboardList, PackagePlus } from "lucide-react";
import { describe, expect, it } from "vitest";
import {
  ManagerSnapshotLink,
  ManagerSnapshotTable,
} from "@/features/role/ManagerHomeShared";
import { readFileSync } from "node:fs";
import { resolve } from "node:path";

describe("POS-MANAGER-HOME-OPERATIONS-SNAPSHOT-CARD-POLISH-20", () => {
  it("renders icon/title/helper snapshot cards as navigable links", () => {
    render(
      <MemoryRouter>
        <ManagerSnapshotTable>
          <ManagerSnapshotLink
            title="Inventory"
            detail="No stock issues"
            href="/inventory"
            testId="manager-snapshot-inventory"
            icon={Boxes}
          />
          <ManagerSnapshotLink
            title="Orders"
            detail="No open orders"
            href="/orders"
            testId="manager-snapshot-orders"
            icon={ClipboardList}
          />
          <ManagerSnapshotLink
            title="Purchasing"
            detail="No receivable POs"
            href="/purchasing"
            testId="manager-snapshot-purchasing"
            icon={PackagePlus}
          />
        </ManagerSnapshotTable>
      </MemoryRouter>,
    );

    const inventory = screen.getByTestId("manager-snapshot-inventory");
    expect(inventory).toHaveAttribute("href", "/inventory");
    expect(inventory).toHaveAccessibleName("Inventory — No stock issues");
    expect(inventory.className).toMatch(/manager-snapshot-card/);
    expect(inventory.className).toMatch(/exits-card/);
    expect(screen.getByText("Inventory")).toBeInTheDocument();
    expect(screen.getByText("No stock issues")).toBeInTheDocument();

    expect(screen.getByTestId("manager-snapshot-orders")).toHaveAttribute("href", "/orders");
    expect(screen.getByTestId("manager-snapshot-purchasing")).toHaveAttribute(
      "href",
      "/purchasing",
    );
  });

  it("uses Primary soft icon treatment and structural card radius (not Pill)", () => {
    const css = readFileSync(
      resolve(__dirname, "../../styles/globals.css"),
      "utf8",
    );
    expect(css).toContain(".manager-snapshot-card__icon");
    expect(css).toMatch(
      /\.manager-snapshot-card__icon\s*\{[\s\S]*?color:\s*var\(--exits-primary\)/,
    );
    expect(css).toContain("border-radius: var(--exits-radius-md) !important");
    expect(css).not.toMatch(
      /\.manager-snapshot-card\s*\{[\s\S]*?border-radius:\s*var\(--exits-control-radius\)/,
    );
    expect(css).toContain(".manager-snapshot-grid");
  });

  it("marks attention tone when domain helper indicates issues", () => {
    render(
      <MemoryRouter>
        <ManagerSnapshotLink
          title="Inventory"
          detail="2 low · 1 expiry"
          href="/inventory?lowStock=1"
          testId="manager-snapshot-inventory"
          icon={Boxes}
          tone="attention"
        />
      </MemoryRouter>,
    );
    expect(screen.getByTestId("manager-snapshot-inventory").className).toMatch(
      /manager-snapshot-card--attention/,
    );
  });
});
