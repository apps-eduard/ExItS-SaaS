import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it } from "vitest";
import { MenusActionsReview } from "./MenusActionsReview";
import { NavigationSearchReview } from "./NavigationSearchReview";
import { PresentationReview } from "./PresentationReview";
import { SelectionInputReview } from "./SelectionInputReview";
import { StatusReview } from "./StatusReview";
import { SystemExperiencesReview } from "./SystemExperiencesReview";
import { MotionReview } from "./MotionReview";
import { ToastProvider } from "../../components/toast/ToastProvider";

describe("HIG review pages", () => {
  it("records a button action and a menu choice", async () => {
    const user = userEvent.setup();
    render(<MenusActionsReview />);

    expect(screen.getByRole("heading", { name: "Menus & Actions", level: 1 })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Open Button dropdown" })).toHaveAttribute("href", "#button-dropdown");
    expect(screen.getByRole("heading", { name: "Planned", level: 2 })).toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: "Delete" }));
    expect(screen.getByRole("status")).toHaveTextContent("Destructive action recorded.");

    await user.click(screen.getByRole("button", { name: "Record" }));
    await user.click(await screen.findByRole("menuitem", { name: "Edit" }));
    expect(screen.getByRole("status")).toHaveTextContent("Menu: edit.");

    const edge = screen.getByRole("button", { name: "Edge record" });
    edge.focus();
    await user.keyboard("{Enter}");
    const longItem = await screen.findByRole("menuitem", { name: "Open the neighborhood market receipt from April" });
    expect(longItem).toBeInTheDocument();
    await user.keyboard("{Escape}");
    expect(edge).toHaveFocus();
  });

  it("filters the searchable select and keeps a disabled tab", async () => {
    const user = userEvent.setup();
    render(<NavigationSearchReview />);

    expect(screen.getByRole("heading", { name: "Navigation & Search", level: 1 })).toBeInTheDocument();
    expect(screen.getByRole("tab", { name: "Archive" })).toBeDisabled();

    await user.click(screen.getByRole("tab", { name: "Done" }));
    expect(screen.getByRole("tab", { name: "Done" })).toHaveAttribute("aria-selected", "true");
    expect(screen.getByText("Finished records in this preview.")).toBeInTheDocument();

    expect(screen.getByRole("navigation", { name: "Sample trail" })).toBeInTheDocument();
    expect(screen.getByRole("navigation", { name: "Long trail" }).querySelector("[aria-current='page']")).toHaveTextContent("Neighborhood market");

    await user.click(screen.getByRole("button", { name: "Collapse sample" }));
    expect(screen.getByRole("button", { name: "Expand sample" })).toHaveAttribute("aria-pressed", "true");

    const drawerButton = screen.getByRole("button", { name: "Open sample sections" });
    await user.click(drawerButton);
    expect(screen.getByRole("dialog", { name: "Sample sections drawer" })).toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: "Close sample sections" }));
    expect(screen.queryByRole("dialog", { name: "Sample sections drawer" })).not.toBeInTheDocument();
    expect(drawerButton).toHaveFocus();

    const filter = screen.getByRole("searchbox", { name: "Filter records" });
    await user.type(filter, "zzz");
    expect(screen.getByText("No matches")).toBeInTheDocument();
    const filterField = filter.closest("div");
    expect(filterField).not.toBeNull();
    await user.click(within(filterField!).getByRole("button", { name: "Clear search" }));
    expect(filter).toHaveValue("");
    expect(screen.getByText("Market")).toBeInTheDocument();
    expect(screen.getByRole("searchbox", { name: "Disabled" })).toBeDisabled();

    await user.click(screen.getByRole("button", { name: "Person, Ana Cruz" }));
    const search = screen.getByRole("combobox", { name: "Search Person" });
    await user.type(search, "zzz");
    expect(screen.getByText("No matches")).toBeInTheDocument();
  });

  it("previews payments, an empty list, loading, and a receipt tooltip", async () => {
    const user = userEvent.setup();
    render(<PresentationReview />);

    expect(screen.getByRole("heading", { name: "Presentation", level: 1 })).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: "Dialogs" })).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: "Sheet" })).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: "Review status", level: 2 })).toBeInTheDocument();
    expect(screen.getByText("Locked. Card, tooltip, popover, dialog, and sheet decisions are in the design system. The Components category stays unlocked.")).toBeInTheDocument();
    expect(screen.getAllByText("Paid").length).toBeGreaterThan(0);
    expect(screen.getAllByText("Pending").length).toBeGreaterThan(0);
    expect(screen.getAllByText("Failed").length).toBeGreaterThan(0);
    expect(screen.getByRole("button", { name: "Refund Neighborhood market" })).toBeDisabled();

    const details = screen.getByRole("button", { name: "Details for Neighborhood market" });
    await user.hover(details);
    await waitFor(() => expect(details.parentElement).toHaveAttribute("aria-describedby"));
    await user.unhover(details);
    await waitFor(() => expect(details.parentElement).not.toHaveAttribute("aria-describedby"));
    details.focus();
    await waitFor(() => expect(details.parentElement).toHaveAttribute("aria-describedby"));
    expect(screen.getByRole("tooltip", { name: "Opens the receipt for Neighborhood market." })).toBeInTheDocument();
    await user.keyboard("{Escape}");
    expect(details.parentElement).not.toHaveAttribute("aria-describedby");

    await user.click(details);
    expect(screen.getByRole("status")).toHaveTextContent("Opened the receipt for Neighborhood market.");

    await user.click(screen.getByRole("button", { name: "Show empty list" }));
    expect(screen.getByRole("heading", { name: "No payments yet" })).toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: "Record a payment" }));
    expect(screen.getByRole("status")).toHaveTextContent("Recorded a sample payment.");
    expect(screen.getAllByText("Neighborhood market").length).toBeGreaterThan(0);

    await user.click(screen.getByRole("button", { name: "Show loading" }));
    expect(screen.getByRole("status", { name: "Loading balance" })).toBeInTheDocument();
    expect(screen.getByRole("status", { name: "Loading Maya transfer" })).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Details for Neighborhood market" })).not.toBeInTheDocument();
  }, 15000);

  it("opens anchored popovers and non-interactive tooltips", async () => {
    const user = userEvent.setup();
    render(<PresentationReview />);

    const balance = screen.getByRole("button", { name: "Balance details" });
    await user.click(balance);
    expect(screen.getByRole("dialog", { name: "Balance details" })).toBeInTheDocument();
    await user.keyboard("{Escape}");
    await waitFor(() => expect(balance).toHaveFocus());
    expect(screen.queryByRole("dialog", { name: "Balance details" })).not.toBeInTheDocument();

    await user.click(balance);
    await user.click(screen.getByRole("heading", { name: "Popover" }));
    expect(screen.queryByRole("dialog", { name: "Balance details" })).not.toBeInTheDocument();

    const filter = screen.getByRole("button", { name: "Filter payments" });
    await user.click(filter);
    await user.click(screen.getByRole("button", { name: "Pending only" }));
    expect(screen.getByRole("status")).toHaveTextContent("Showing pending payments.");
    await waitFor(() => expect(filter).toHaveFocus());
    expect(screen.queryByRole("dialog", { name: "Filter payments" })).not.toBeInTheDocument();

    const more = screen.getByRole("button", { name: "More actions" });
    await user.click(more);
    const actions = screen.getByRole("dialog", { name: "More actions" });
    expect(actions).toBeInTheDocument();
    await user.keyboard("{Escape}");
    await waitFor(() => expect(more).toHaveFocus());

    const hint = screen.getByRole("button", { name: "Receipt hint" });
    hint.focus();
    await waitFor(() => expect(hint.parentElement).toHaveAttribute("aria-describedby"));
    expect(screen.getByRole("tooltip", { name: "Opens the receipt." })).toBeInTheDocument();
    await user.keyboard("{Escape}");
    expect(hint.parentElement).not.toHaveAttribute("aria-describedby");

    const help = screen.getByRole("button", { name: "About the available balance" });
    help.focus();
    await waitFor(() => expect(help.parentElement).toHaveAttribute("aria-describedby"));
    expect(screen.getByRole("tooltip", { name: "Available after pending payments clear." })).toBeInTheDocument();

    const note = screen.getByRole("button", { name: "Long payment note" });
    note.focus();
    await waitFor(() => expect(note.parentElement).toHaveAttribute("aria-describedby"));
    expect(screen.getByRole("tooltip", { name: /why the transfer did not complete/ })).toBeInTheDocument();
  }, 15000);

  it("edits, confirms a delete, and scrolls transaction details in the existing dialogs", async () => {
    const user = userEvent.setup();
    render(<PresentationReview />);

    const edit = screen.getByRole("button", { name: "Edit transaction" });
    await user.click(edit);
    const editor = screen.getByRole("dialog", { name: "Edit transaction" });
    expect(editor).toHaveAccessibleDescription("Update this transaction's details.");
    const description = screen.getByRole("textbox", { name: "Description" });
    expect(description).toHaveFocus();
    await user.clear(description);
    await user.type(description, "Temporary draft");
    await user.click(screen.getByRole("button", { name: "Cancel" }));
    await waitFor(() => expect(edit).toHaveFocus());
    expect(screen.getByText("No dialog action yet.")).toBeInTheDocument();

    await user.click(edit);
    expect(screen.getByRole("textbox", { name: "Description" })).toHaveValue("Neighborhood market");
    await user.clear(screen.getByRole("textbox", { name: "Description" }));
    await user.type(screen.getByRole("textbox", { name: "Description" }), "Weekend market");
    await user.click(screen.getByRole("button", { name: "Save changes" }));
    await waitFor(() => expect(edit).toHaveFocus());
    expect(screen.getByText("Saved Weekend market for ₱860.00.")).toBeInTheDocument();

    const remove = screen.getByRole("button", { name: "Delete transaction" });
    await user.click(remove);
    expect(screen.getByRole("dialog", { name: "Delete this transaction?" })).toBeInTheDocument();
    expect(screen.queryByText("Deleted the Neighborhood market sample.")).not.toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: "Delete" }));
    await waitFor(() => expect(remove).toHaveFocus());
    expect(screen.getByText("Deleted the Neighborhood market sample.")).toBeInTheDocument();

    const details = screen.getByRole("button", { name: "View transaction details" });
    await user.click(details);
    const record = screen.getByRole("dialog", { name: "Transaction details" });
    expect(record).toHaveTextContent("TX-2026-0412-0914");
    expect(screen.getByRole("button", { name: "Close details" })).toBeInTheDocument();
    await user.keyboard("{Escape}");
    await waitFor(() => expect(details).toHaveFocus());
  }, 15000);

  it("validates a transaction and updates preferences", async () => {
    const user = userEvent.setup();
    render(<SelectionInputReview />);

    expect(screen.getByRole("heading", { name: "Selection & Input", level: 1 })).toBeInTheDocument();
    expect(screen.getByText("Locked. Text fields, selects, choices, and the existing pickers stay as reviewed. The Components category stays unlocked.")).toBeInTheDocument();
    expect(screen.getByRole("textbox", { name: "Disabled" })).toBeDisabled();
    expect(screen.getAllByRole("radio", { name: "Year" }).every((radio) => radio.hasAttribute("disabled"))).toBe(true);

    await user.click(screen.getByRole("button", { name: "Save transaction" }));
    expect(screen.getByRole("status")).toHaveTextContent("Fix the highlighted fields.");
    expect(screen.getByRole("textbox", { name: "Description" })).toHaveAccessibleDescription("Enter a description.");

    await user.type(screen.getByRole("textbox", { name: "Description" }), "Market");
    await user.type(screen.getByRole("textbox", { name: "Amount" }), "12.50");
    await user.click(screen.getByRole("combobox", { name: "Category, Choose" }));
    await user.click(await screen.findByRole("option", { name: "Groceries" }));
    await user.click(screen.getByRole("button", { name: "Date" }));
    await user.click(screen.getByRole("button", { name: "Today" }));
    await user.click(screen.getByRole("button", { name: "Save transaction" }));
    expect(screen.getByRole("status")).toHaveTextContent(/Saved Market for ₱12\.50/);

    await user.click(screen.getByRole("button", { name: "Clear" }));
    expect(screen.getByRole("textbox", { name: "Description" })).toHaveValue("");
    expect(screen.getByRole("status")).toHaveTextContent("Cleared the draft.");

    await user.click(screen.getByRole("switch", { name: "Payment alerts" }));
    expect(screen.getByText(/Alerts off\. Reminder/)).toBeInTheDocument();
    await user.click(screen.getAllByRole("radio", { name: "Week" })[0]);
    expect(screen.getByText(/Summary: week/)).toBeInTheDocument();
  }, 15000);

  it("reviews badges, loading, toasts, and empty states without locking", async () => {
    const user = userEvent.setup();
    render(
      <ToastProvider>
        <StatusReview />
      </ToastProvider>,
    );

    expect(screen.getByRole("heading", { name: "Status", level: 1 })).toBeInTheDocument();
    expect(screen.getByText("Locked. Badges, loading, toasts, and empty states stay as reviewed. The Components category stays unlocked.")).toBeInTheDocument();
    expect(screen.getByText("The toast right-edge countdown is locked.")).toBeInTheDocument();
    expect(screen.getByText("Paid")).toBeInTheDocument();
    expect(screen.getByText("Pending")).toBeInTheDocument();
    expect(screen.getByText("Overdue")).toBeInTheDocument();
    expect(screen.getByText(/There is no progress bar/)).toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: "Save receipt" }));
    expect(screen.getByRole("button", { name: "Save receipt" })).toHaveAttribute("aria-busy", "true");
    await user.click(screen.getByRole("button", { name: "Stop save" }));
    expect(screen.getByRole("button", { name: "Save receipt" })).not.toHaveAttribute("aria-busy");

    await user.click(screen.getByRole("button", { name: "Show payment rows" }));
    expect(screen.getByRole("status", { name: "Loading payment name" })).toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: "Hide payment rows" }));
    expect(screen.queryByRole("status", { name: "Loading payment name" })).not.toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: "Payment saved" }));
    expect(await screen.findByText("Neighborhood market was recorded on this device.")).toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: "Dismiss notification" }));
    expect(screen.queryByText("Neighborhood market was recorded on this device.")).not.toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: "Payment failed" }));
    expect(await screen.findByRole("alert")).toHaveTextContent("April rent could not be recorded.");
    await user.click(screen.getByRole("button", { name: "Dismiss notification" }));

    await user.click(screen.getByRole("button", { name: "No records" }));
    expect(screen.getByRole("heading", { name: "No payments yet" })).toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: "Record a payment" }));
    expect(screen.getAllByText("Neighborhood market").length).toBeGreaterThan(0);

    await user.click(screen.getByRole("button", { name: "Failed loading" }));
    expect(screen.getByRole("heading", { name: "Payments could not load" })).toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: "Try again" }));

    await user.type(screen.getByRole("searchbox", { name: "Find a payment" }), "zz");
    expect(screen.getByRole("heading", { name: "No matching payments" })).toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: "Show every payment" }));
    expect(screen.getByRole("searchbox", { name: "Find a payment" })).toHaveValue("");
  }, 15000);

  it("walks confirmation, access, connection, session, and sync samples", async () => {
    const user = userEvent.setup();
    render(
      <ToastProvider>
        <SystemExperiencesReview />
      </ToastProvider>,
    );

    expect(screen.getByRole("heading", { name: "System experiences", level: 1 })).toBeInTheDocument();
    expect(screen.getByText("Locked. Confirmations, access, connection, and session recovery stay as reviewed. The Components category stays unlocked.")).toBeInTheDocument();

    const remove = screen.getByRole("button", { name: "Delete transaction" });
    await user.click(remove);
    const destroy = screen.getByRole("dialog", { name: "Delete this transaction?" });
    expect(destroy).toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: "Cancel" }));
    await waitFor(() => expect(remove).toHaveFocus());
    expect(screen.getByRole("status")).toHaveTextContent("No confirmation yet.");

    await user.click(remove);
    await user.click(screen.getByRole("button", { name: "Delete" }));
    expect(screen.getByRole("status")).toHaveTextContent("Deleted the Maya transfer sample.");

    await user.click(screen.getByRole("button", { name: "Leave with unsaved changes" }));
    await user.click(screen.getByRole("button", { name: "Cancel" }));
    expect(screen.getByRole("status")).toHaveTextContent("Stayed on the page. The draft is still here.");

    await user.click(screen.getByRole("button", { name: "Cancel transfer" }));
    const cancelTransfer = screen.getByRole("dialog", { name: "Cancel this transfer?" });
    await user.click(within(cancelTransfer).getByRole("button", { name: "Cancel transfer" }));
    expect(screen.getByRole("status")).toHaveTextContent("Canceled the transfer. The amount is still on this page.");

    await user.click(screen.getByRole("button", { name: "Permission denied" }));
    expect(screen.getByRole("heading", { name: "Permission denied" })).toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: "Use a sample account" }));
    expect(screen.getByRole("status")).toHaveTextContent("Use a sample account. No device permission was changed.");

    const note = screen.getByRole("textbox", { name: "Payment note" });
    await user.clear(note);
    await user.type(note, "Rent note");
    await user.click(screen.getByRole("button", { name: "Fail this request" }));
    expect(note).toHaveValue("Rent note");
    await user.click(screen.getByRole("button", { name: "Retry send" }));
    expect(note).toHaveValue("Rent note");
    expect(await screen.findByText("Retried the send. The note is still in the field.")).toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: "Session expired" }));
    await user.click(screen.getByRole("button", { name: "Recover draft" }));
    expect(screen.getByRole("textbox", { name: "Local draft" })).toHaveValue("Weekend market · ₱240.00");

    await user.click(screen.getByRole("button", { name: "Start sync" }));
    expect(screen.getByRole("status", { name: "Loading sync status" })).toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: "Finish sync" }));
    expect(await screen.findByText("Sync finished.")).toBeInTheDocument();
  }, 15000);

  it("reviews existing motion without replacing it", async () => {
    const user = userEvent.setup();
    render(
      <ToastProvider>
        <MotionReview />
      </ToastProvider>,
    );

    expect(screen.getByRole("heading", { name: "Motion", level: 1 })).toBeInTheDocument();
    expect(screen.getByText("Not locked. Waiting for visual approval before motion is locked.")).toBeInTheDocument();
    expect(screen.getByText(/There is no page transition/)).toBeInTheDocument();

    const openNote = screen.getByRole("button", { name: "Open note" });
    await user.click(openNote);
    expect(screen.getByRole("dialog", { name: "Payment note" })).toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: "Close note" }));
    await waitFor(() => expect(openNote).toHaveFocus());

    await user.click(screen.getByRole("button", { name: "Record" }));
    await user.click(await screen.findByRole("menuitem", { name: "Edit" }));
    expect(screen.getByRole("status")).toHaveTextContent("Menu: edit.");

    await user.click(screen.getByRole("button", { name: "Show saved toast" }));
    expect(await screen.findByText("The sample stayed on this device.")).toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: "Show payment row" }));
    expect(screen.getByRole("status", { name: "Loading payment row" })).toBeInTheDocument();
  }, 15000);
});
