import { describe, expect, it, vi } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import type { ReactNode } from "react";
import { FormDrawer } from "@/components/exits/FormDrawer";
import { PreferencesProvider } from "@/hooks/usePreferences";
import { I18nProvider } from "@/i18n/I18nProvider";

function wrap(ui: ReactNode) {
  return (
    <PreferencesProvider>
      <I18nProvider>{ui}</I18nProvider>
    </PreferencesProvider>
  );
}

describe("FormDrawer", () => {
  it("opens as a right-side dialog shell without domain imports", async () => {
    const user = userEvent.setup();
    const onOpenChange = vi.fn();
    const onSave = vi.fn();

    render(
      wrap(
        <FormDrawer
          open
          onOpenChange={onOpenChange}
          title="Edit entity"
          description="Subtitle"
          onSave={onSave}
          testId="entity-form-drawer"
        >
          <label>
            Name
            <input data-testid="entity-name" defaultValue="Ada" />
          </label>
        </FormDrawer>,
      ),
    );

    const drawer = screen.getByTestId("entity-form-drawer");
    expect(drawer).toHaveAttribute("data-side", "right");
    expect(screen.getByText("Edit entity")).toBeInTheDocument();
    expect(screen.getByText("Subtitle")).toBeInTheDocument();
    expect(screen.getByTestId("entity-form-drawer-form")).toBeInTheDocument();
    expect(document.querySelector(".exits-form-drawer__panel--md")).toBeTruthy();
    expect(document.querySelector(".exits-form-drawer__footer")).toBeTruthy();

    await user.click(screen.getByTestId("entity-form-drawer-save"));
    expect(onSave).toHaveBeenCalledTimes(1);

    await user.keyboard("{Escape}");
    expect(onOpenChange).toHaveBeenCalledWith(false);
  });

  it("disables save while saving and blocks close", async () => {
    const user = userEvent.setup();
    const onOpenChange = vi.fn();

    render(
      wrap(
        <FormDrawer
          open
          onOpenChange={onOpenChange}
          title="Saving"
          onSave={() => undefined}
          saving
          saveLabel="Saving…"
          testId="saving-drawer"
        >
          <p>Body</p>
        </FormDrawer>,
      ),
    );

    expect(screen.getByTestId("saving-drawer-save")).toBeDisabled();
    expect(screen.getByTestId("saving-drawer-cancel")).toBeDisabled();
    await user.click(screen.getByTestId("saving-drawer-close"));
    expect(onOpenChange).not.toHaveBeenCalled();
  });

  it("prompts before discarding dirty changes when enabled", async () => {
    const user = userEvent.setup();
    const onOpenChange = vi.fn();

    render(
      wrap(
        <FormDrawer
          open
          onOpenChange={onOpenChange}
          title="Dirty"
          dirty
          confirmUnsavedOnClose
          onSave={() => undefined}
          testId="dirty-drawer"
        >
          <p>Changed</p>
        </FormDrawer>,
      ),
    );

    await user.click(screen.getByTestId("dirty-drawer-cancel"));
    expect(onOpenChange).not.toHaveBeenCalled();
    expect(screen.getByTestId("dirty-drawer-unsaved")).toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: "Discard" }));
    await waitFor(() => expect(onOpenChange).toHaveBeenCalledWith(false));
  });
});
