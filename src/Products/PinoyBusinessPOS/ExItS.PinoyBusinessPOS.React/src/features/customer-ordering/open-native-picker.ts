/** Opens a date/time input's picker during the current click. A later call throws NotAllowedError. */
export function openNativePicker(input: HTMLInputElement | null): void {
  if (!input) {
    return;
  }

  try {
    if (typeof input.showPicker === "function") {
      input.showPicker();
      return;
    }
  } catch {
    // Browsers require the click itself. Falling back to focus must not surface as a page error.
  }

  input.focus();
}
