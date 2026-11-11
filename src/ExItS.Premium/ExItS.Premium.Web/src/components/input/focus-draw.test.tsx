import { fireEvent, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { useState } from "react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { SearchField } from "../search/SearchField";
import { focusDrawGeometry } from "./focus-draw";
import { INPUT_FOCUS_DRAW_EASING, INPUT_FOCUS_DRAW_MS, INPUT_FOCUS_DRAW_RELEASE_MS } from "./focus-motion";
import { Input } from "./Input";

type DrawCall = {
  keyframes: Keyframe[];
  options: KeyframeAnimationOptions;
};

function installAnimate() {
  const calls: DrawCall[] = [];
  const original = Element.prototype.animate;
  Element.prototype.animate = function (keyframes, options) {
    calls.push({
      keyframes: Array.isArray(keyframes) ? (keyframes as Keyframe[]) : [],
      options: (options ?? {}) as KeyframeAnimationOptions,
    });
    return {
      cancel() {},
      onfinish: null,
      finished: Promise.resolve(),
    } as unknown as Animation;
  };
  return {
    calls,
    restore() {
      Element.prototype.animate = original;
    },
  };
}

function draws(calls: DrawCall[]) {
  return calls.filter((call) => {
    const first = Number((call.keyframes[0] as { strokeDashoffset?: number })?.strokeDashoffset);
    const last = Number((call.keyframes[1] as { strokeDashoffset?: number })?.strokeDashoffset);
    return first > 0 && last === 0;
  });
}

function fades(calls: DrawCall[]) {
  return calls.filter((call) => call.options.duration === INPUT_FOCUS_DRAW_RELEASE_MS);
}

describe("focus draw geometry", () => {
  it("starts both strokes at the bottom center and finishes them at the top center", () => {
    const geometry = focusDrawGeometry(280, 44, 12);

    expect(geometry.left).toBe(
      "M 140 42.75 L 13.25 42.75 A 12 12 0 0 1 1.25 30.75 L 1.25 13.25 A 12 12 0 0 1 13.25 1.25 L 140 1.25",
    );
    expect(geometry.right).toBe(
      "M 140 42.75 L 266.75 42.75 A 12 12 0 0 0 278.75 30.75 L 278.75 13.25 A 12 12 0 0 0 266.75 1.25 L 140 1.25",
    );
    expect(geometry.left.startsWith(`M ${geometry.startX} ${geometry.startY}`)).toBe(true);
    expect(geometry.right.startsWith(`M ${geometry.startX} ${geometry.startY}`)).toBe(true);
    expect(geometry.left.endsWith(`L ${geometry.endX} ${geometry.endY}`)).toBe(true);
    expect(geometry.right.endsWith(`L ${geometry.endX} ${geometry.endY}`)).toBe(true);
    expect(geometry.startX).toBe(geometry.endX);
    expect(geometry.startY).toBeGreaterThan(geometry.endY);
    expect(geometry.left).not.toBe(geometry.right);
  });

  it("follows standard, soft, and pill corners at compact, standard, and large heights", () => {
    const standard = focusDrawGeometry(280, 44, 8);
    const soft = focusDrawGeometry(280, 44, 12);
    const pill = focusDrawGeometry(280, 44, 999);
    const compact = focusDrawGeometry(240, 36, 12);
    const large = focusDrawGeometry(320, 52, 12);

    expect(standard.radius).toBe(8);
    expect(soft.radius).toBe(12);
    expect(standard.left).not.toBe(soft.left);
    expect(pill.radius).toBe(20.75);
    expect(pill.left).toContain("A 20.75 20.75");
    expect(pill.right).toContain("A 20.75 20.75");
    expect(compact.height).toBe(36);
    expect(large.height).toBe(52);
    expect(compact.startY).not.toBe(large.startY);
    expect(compact.left).not.toBe(large.left);
    expect(large.startY).toBeGreaterThan(large.endY);
  });
});

describe("focus draw interaction", () => {
  afterEach(() => {
    vi.restoreAllMocks();
    vi.unstubAllGlobals();
  });

  function mockBox() {
    const prototype = (window.SVGElement?.prototype ?? window.Element.prototype) as { getTotalLength?: () => number };
    prototype.getTotalLength = () => 148;
    vi.spyOn(HTMLElement.prototype, "getBoundingClientRect").mockReturnValue({
      width: 280,
      height: 44,
      top: 0,
      left: 0,
      right: 280,
      bottom: 44,
      x: 0,
      y: 0,
      toJSON() {
        return {};
      },
    });
  }

  it("starts both strokes together on focus, holds the outline, and does not restart while typing", async () => {
    mockBox();
    const motion = installAnimate();
    const user = userEvent.setup();
    render(<Input label="Name" focusMotion="focus-draw" placeholder="Account name" />);
    const field = screen.getByRole("textbox", { name: "Name" });
    const shell = field.parentElement;
    expect(shell?.querySelector("svg")).toBeTruthy();
    expect(shell?.querySelectorAll(":scope > span")).toHaveLength(0);

    fireEvent.focus(field);

    const started = draws(motion.calls);
    expect(started).toHaveLength(2);
    expect(started.map((call) => (call.keyframes[0] as { strokeDashoffset: number }).strokeDashoffset)).toEqual([148, 148]);
    expect(started.every((call) => call.options.duration === INPUT_FOCUS_DRAW_MS)).toBe(true);
    expect(started.every((call) => call.options.easing === INPUT_FOCUS_DRAW_EASING)).toBe(true);
    expect(started.every((call) => call.options.delay === 0)).toBe(true);
    expect(started.every((call) => call.options.fill === "forwards")).toBe(true);
    expect(INPUT_FOCUS_DRAW_MS).toBe(1100);
    expect(INPUT_FOCUS_DRAW_EASING).toBe("linear");
    expect(shell).toHaveAttribute("data-drawn", "true");
    expect(shell?.querySelector("[data-stroke='left']")).toHaveAttribute("d", focusDrawGeometry(280, 44, 0).left);
    expect(shell?.querySelector("[data-stroke='right']")).toHaveAttribute("d", focusDrawGeometry(280, 44, 0).right);

    await user.type(field, "ab");
    expect(field).toHaveValue("ab");
    expect(draws(motion.calls)).toHaveLength(2);
    expect(shell).toHaveAttribute("data-drawn", "true");
    motion.restore();
  });

  it("keeps controlled and uncontrolled values without a second draw", async () => {
    mockBox();
    const motion = installAnimate();
    const user = userEvent.setup();

    function Controlled() {
      const [value, setValue] = useState("M");
      return <Input label="Controlled" focusMotion="focus-draw" value={value} onChange={(event) => setValue(event.target.value)} />;
    }

    const view = render(<Controlled />);
    const controlled = screen.getByRole("textbox", { name: "Controlled" });
    fireEvent.focus(controlled);
    await user.type(controlled, "a");
    expect(controlled).toHaveValue("Ma");
    expect(draws(motion.calls)).toHaveLength(2);

    view.rerender(<Input label="Open" focusMotion="focus-draw" defaultValue="Market" />);
    const open = screen.getByRole("textbox", { name: "Open" });
    fireEvent.focus(open);
    await user.type(open, "!");
    expect(open).toHaveValue("Market!");
    expect(draws(motion.calls)).toHaveLength(4);
    motion.restore();
  });

  it("resets on blur and replays on the next focus", () => {
    mockBox();
    const motion = installAnimate();
    render(<Input label="Name" focusMotion="focus-draw" />);
    const field = screen.getByRole("textbox", { name: "Name" });
    const shell = field.parentElement;

    fireEvent.focus(field);
    fireEvent.blur(field);
    expect(shell).not.toHaveAttribute("data-drawn");
    expect(fades(motion.calls)).toHaveLength(2);

    fireEvent.focus(field);
    expect(shell).toHaveAttribute("data-drawn", "true");
    expect(draws(motion.calls)).toHaveLength(4);
    expect(fades(motion.calls)).toHaveLength(2);
    motion.restore();
  });

  it("uses the status color and keeps the validation message", () => {
    mockBox();
    render(
      <>
        <Input label="Email" defaultValue="a" error="Use at least two letters." focusMotion="focus-draw" />
        <Input label="Store" defaultValue="Market" success="Name is available." focusMotion="focus-draw" />
      </>,
    );
    const email = screen.getByRole("textbox", { name: "Email" });
    const store = screen.getByRole("textbox", { name: "Store" });
    expect(email.parentElement).toHaveAttribute("data-state", "error");
    expect(email.parentElement).toHaveAttribute("data-motion", "focus-draw");
    expect(email).toHaveAttribute("aria-invalid", "true");
    expect(screen.getByText("Use at least two letters.")).toBeVisible();
    expect(store.parentElement).toHaveAttribute("data-state", "success");
    expect(screen.getByText("Name is available.")).toBeVisible();
    fireEvent.focus(email);
    expect(email.parentElement?.querySelectorAll("path")).toHaveLength(2);
    expect(email.parentElement?.querySelector(":scope > span")).not.toBeInTheDocument();
  });

  it("shows the solid outline immediately when motion is reduced", () => {
    mockBox();
    const motion = installAnimate();
    vi.stubGlobal("matchMedia", (query: string) => ({
      matches: query.includes("reduce"),
      media: query,
      addEventListener() {},
      removeEventListener() {},
      dispatchEvent() {
        return false;
      },
      onchange: null,
    }));
    render(<Input label="Name" focusMotion="focus-draw" />);
    const field = screen.getByRole("textbox", { name: "Name" });
    fireEvent.focus(field);
    const left = field.parentElement?.querySelector("[data-stroke='left']") as SVGPathElement;
    expect(motion.calls).toHaveLength(0);
    expect(left.style.opacity).toBe("1");
    expect(left.style.strokeDashoffset).toBe("0");
    expect(field.parentElement).toHaveAttribute("data-drawn", "true");
    motion.restore();
  });

  it("draws on a search field and skips read-only and disabled fields", async () => {
    mockBox();
    const user = userEvent.setup();
    const onChange = vi.fn();
    render(
      <>
        <SearchField label="Search" value="Market" onChange={onChange} focusMotion="focus-draw" />
        <Input label="Nickname" value="Published" readOnly focusMotion="focus-draw" onChange={() => undefined} />
        <Input label="Locked" value="Locked" disabled focusMotion="focus-draw" onChange={() => undefined} />
      </>,
    );
    const search = screen.getByRole("searchbox", { name: "Search" });
    expect(search.parentElement).toHaveAttribute("data-motion", "focus-draw");
    expect(search.parentElement?.querySelector("[data-focus-draw]")).toBeTruthy();
    fireEvent.focus(search);
    expect(search.parentElement).toHaveAttribute("data-drawn", "true");
    await user.click(screen.getByRole("button", { name: "Clear search" }));
    expect(onChange).toHaveBeenCalledWith("");

    const nickname = screen.getByRole("textbox", { name: "Nickname, read-only" });
    expect(nickname.parentElement).toHaveAttribute("data-quiet", "true");
    expect(nickname.parentElement?.querySelector("svg")).not.toBeInTheDocument();
    expect(screen.getByRole("textbox", { name: "Locked" }).parentElement).not.toHaveAttribute("data-motion");
  });
});
