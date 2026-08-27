import { describe, expect, it, vi, beforeEach, afterEach } from "vitest";
import { act, render, screen } from "@testing-library/react";
import { AppBootLoader } from "@/components/exits/loading/AppBootLoader";
import { WorkspaceTransitionOverlay } from "@/components/exits/loading/WorkspaceTransitionOverlay";
import { PageSkeleton } from "@/components/exits/loading/PageSkeleton";
import { BackgroundRefreshIndicator } from "@/components/exits/loading/BackgroundRefreshIndicator";
import { ActionButtonLoading } from "@/components/exits/loading/ActionButtonLoading";
import { useDeferredVisible } from "@/components/exits/loading/useDeferredVisible";
import { AccountContextSwitchScreen } from "@/features/account/AccountContextSwitchScreen";

function DeferredProbe({ active }: { active: boolean }) {
  const visible = useDeferredVisible(active, { delayMs: 150, minVisibleMs: 180 });
  return <div data-testid="deferred">{visible ? "yes" : "no"}</div>;
}

describe("loading UX system", () => {
  beforeEach(() => {
    vi.useFakeTimers();
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it("renders app boot loader with accessible status", () => {
    render(<AppBootLoader label="Preparing your workspace…" defer={false} />);
    expect(screen.getByTestId("app-boot-loader")).toHaveAttribute("role", "status");
    expect(screen.getByText("Preparing your workspace…")).toBeInTheDocument();
    expect(screen.getByTestId("exits-loader-mark")).toBeInTheDocument();
  });

  it("shows workspace transition overlay only while active (deferred)", () => {
    const { rerender } = render(
      <WorkspaceTransitionOverlay active={false} label="Switching workspace…" />,
    );
    expect(screen.queryByTestId("workspace-transition-overlay")).not.toBeInTheDocument();

    rerender(<WorkspaceTransitionOverlay active label="Switching workspace…" detail="Kizy Store" />);
    act(() => {
      vi.advanceTimersByTime(160);
    });
    expect(screen.getByTestId("workspace-transition-overlay")).toBeInTheDocument();
    expect(screen.getByText("Kizy Store")).toBeInTheDocument();
  });

  it("renders page skeleton without bare Loading text node", () => {
    render(<PageSkeleton label="Loading inventory" defer={false} />);
    expect(screen.getByTestId("page-skeleton")).toBeInTheDocument();
    expect(screen.queryByText(/^Loading$/)).not.toBeInTheDocument();
    expect(screen.getByText("Loading inventory", { selector: ".sr-only" })).toBeInTheDocument();
  });

  it("keeps background refresh indicator off until deferred", () => {
    const { rerender } = render(
      <BackgroundRefreshIndicator active={false} label="Updating…" />,
    );
    expect(screen.queryByTestId("background-refresh-indicator")).not.toBeInTheDocument();
    rerender(<BackgroundRefreshIndicator active label="Updating…" />);
    expect(screen.queryByTestId("background-refresh-indicator")).not.toBeInTheDocument();
    act(() => {
      vi.advanceTimersByTime(200);
    });
    expect(screen.getByTestId("background-refresh-indicator")).toHaveTextContent("Updating…");
  });

  it("disables action button and shows spinner while loading", () => {
    render(
      <ActionButtonLoading loading data-testid="save-btn">
        Save
      </ActionButtonLoading>,
    );
    const button = screen.getByTestId("save-btn");
    expect(button).toBeDisabled();
    expect(button).toHaveAttribute("aria-busy", "true");
    expect(screen.getByTestId("inline-spinner")).toBeInTheDocument();
  });

  it("defers visibility then respects min visible duration", () => {
    const { rerender } = render(<DeferredProbe active />);
    expect(screen.getByTestId("deferred")).toHaveTextContent("no");
    act(() => {
      vi.advanceTimersByTime(150);
    });
    expect(screen.getByTestId("deferred")).toHaveTextContent("yes");
    rerender(<DeferredProbe active={false} />);
    act(() => {
      vi.advanceTimersByTime(50);
    });
    expect(screen.getByTestId("deferred")).toHaveTextContent("yes");
    act(() => {
      vi.advanceTimersByTime(200);
    });
    expect(screen.getByTestId("deferred")).toHaveTextContent("no");
  });

  it("account context switch uses branded boot loader", () => {
    render(<AccountContextSwitchScreen label="Switching account…" />);
    expect(screen.getByTestId("account-context-switch")).toBeInTheDocument();
    expect(screen.getByText("Switching account…")).toBeInTheDocument();
  });
});
