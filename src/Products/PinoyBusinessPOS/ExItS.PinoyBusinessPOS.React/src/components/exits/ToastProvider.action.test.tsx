import { describe, expect, it } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Route, Routes, useLocation } from "react-router-dom";
import { ToastNavigateBridge } from "@/components/exits/ToastNavigateBridge";
import { ToastProvider, useToast } from "@/components/exits/ToastProvider";
import { setToastNavigate } from "@/components/exits/toast-navigation";

function ToastActionProbe() {
  const { showToast } = useToast();
  return (
    <button
      type="button"
      data-testid="show-action-toast"
      onClick={() =>
        showToast({
          title: "No warehouse",
          description: "Configure branches",
          tone: "error",
          action: { label: "Open branches", href: "/org/branches" },
        })
      }
    >
      Show
    </button>
  );
}

function LocationProbe() {
  const location = useLocation();
  return <div data-testid="location">{location.pathname}</div>;
}

describe("ToastProvider action links", () => {
  it("renders toast action without Router context (no Link crash)", async () => {
    const user = userEvent.setup();
    setToastNavigate(null);

    render(
      <ToastProvider>
        <ToastActionProbe />
      </ToastProvider>,
    );

    await user.click(screen.getByTestId("show-action-toast"));

    const action = await screen.findByTestId("exits-toast-action");
    expect(action).toHaveAttribute("href", "/org/branches");
    expect(action.tagName).toBe("A");
    expect(screen.getByTestId("exits-toast")).toHaveTextContent("No warehouse");
  });

  it("SPA-navigates toast action when ToastNavigateBridge is mounted", async () => {
    const user = userEvent.setup();

    render(
      <MemoryRouter initialEntries={["/role/manager"]}>
        <ToastProvider>
          <ToastNavigateBridge />
          <Routes>
            <Route
              path="*"
              element={
                <>
                  <LocationProbe />
                  <ToastActionProbe />
                </>
              }
            />
          </Routes>
        </ToastProvider>
      </MemoryRouter>,
    );

    expect(screen.getByTestId("location")).toHaveTextContent("/role/manager");
    await user.click(screen.getByTestId("show-action-toast"));
    await user.click(await screen.findByTestId("exits-toast-action"));
    expect(screen.getByTestId("location")).toHaveTextContent("/org/branches");
  });
});
