import { afterEach, describe, expect, it } from "vitest";
import { render } from "@testing-library/react";
import { useBodyScrollLock, resetBodyScrollLockForTests } from "@/lib/use-body-scroll-lock";

function Lock({ locked }: { locked: boolean }) {
  useBodyScrollLock(locked);
  return null;
}

describe("useBodyScrollLock", () => {
  afterEach(() => {
    resetBodyScrollLockForTests();
  });

  it("restores overflow only after the last nested locker releases", () => {
    const first = render(<Lock locked />);
    expect(document.body.style.overflow).toBe("hidden");

    const second = render(<Lock locked />);
    expect(document.body.style.overflow).toBe("hidden");

    first.unmount();
    expect(document.body.style.overflow).toBe("hidden");

    second.unmount();
    expect(document.body.style.overflow).toBe("");
  });
});
