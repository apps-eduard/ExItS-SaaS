import { describe, expect, it } from "vitest";
import {
  DIALOG_TRANSITION_DEFAULT,
  DIALOG_TRANSITIONS,
  isDialogTransition,
  resolveDialogMotion,
} from "./dialog-transition";

describe("dialog-transition", () => {
  it("defaults to spring", () => {
    expect(DIALOG_TRANSITION_DEFAULT).toBe("spring");
    expect(DIALOG_TRANSITIONS).toEqual(["spring", "fade", "scale", "slide"]);
    expect(isDialogTransition("fade")).toBe(true);
    expect(isDialogTransition("flip")).toBe(false);
  });

  it("resolves spring sheet and fade recipes", () => {
    const spring = resolveDialogMotion({
      transition: "spring",
      sheet: true,
      reduced: false,
      closing: false,
    });
    expect(spring.variants.open).toMatchObject({ y: 0 });

    const fade = resolveDialogMotion({
      transition: "fade",
      sheet: false,
      reduced: false,
      closing: false,
    });
    expect(fade.variants.initial).toMatchObject({ opacity: 0, y: 0, scale: 1 });
  });

  it("forces reduced motion regardless of selected transition", () => {
    const reduced = resolveDialogMotion({
      transition: "spring",
      sheet: true,
      reduced: true,
      closing: false,
    });
    expect(reduced.transition).toMatchObject({ duration: 0.01 });
  });
});
