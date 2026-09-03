import { describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { RegisterInUsePanel } from "@/features/shifts/RegisterInUsePanel";

vi.mock("@/i18n/I18nProvider", () => ({
  useI18n: () => ({
    t: (key: string) => {
      if (key === "shift.registerInUseDetail") {
        return "{register} currently has an open shift by {name}.";
      }
      return key;
    },
  }),
}));

describe("RegisterInUsePanel", () => {
  it("SHIFTUSER shows register-in-use copy and choose-register action", async () => {
    const onChoose = vi.fn();
    const user = userEvent.setup();
    render(
      <MemoryRouter>
        <RegisterInUsePanel
          registerCode="REG-000001"
          registerName="Register 1"
          openedByDisplayName="Mica Uy"
          onChooseRegister={onChoose}
        />
      </MemoryRouter>,
    );

    expect(screen.getByTestId("register-in-use-panel")).toBeInTheDocument();
    expect(screen.getByTestId("register-in-use-panel-detail")).toHaveTextContent("REG-000001");
    expect(screen.getByTestId("register-in-use-panel-detail")).toHaveTextContent("Mica Uy");
    expect(screen.queryByText("shift.openConfirm")).not.toBeInTheDocument();
    await user.click(screen.getByTestId("register-in-use-panel-choose-register"));
    expect(onChoose).toHaveBeenCalledTimes(1);
  });
});
