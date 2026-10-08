import { describe, expect, it, vi } from "vitest";
import { openNativePicker } from "@/features/customer-ordering/open-native-picker";

describe("openNativePicker", () => {
  it("opens the picker during the click", () => {
    const showPicker = vi.fn();
    const input = { showPicker, focus: vi.fn() } as unknown as HTMLInputElement;

    openNativePicker(input);

    expect(showPicker).toHaveBeenCalledTimes(1);
    expect(input.focus).not.toHaveBeenCalled();
  });

  it("focuses the field when the browser rejects showPicker", () => {
    const input = document.createElement("input");
    input.showPicker = () => {
      throw new DOMException(
        "HTMLInputElement::showPicker() requires a user gesture.",
        "NotAllowedError",
      );
    };
    const focus = vi.spyOn(input, "focus").mockImplementation(() => undefined);

    expect(() => openNativePicker(input)).not.toThrow();
    expect(focus).toHaveBeenCalledTimes(1);
  });
});
