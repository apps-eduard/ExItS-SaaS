import { describe, expect, it, beforeEach } from "vitest";
import { renderHook, act } from "@testing-library/react";
import {
  PERSONAL_HOME_SURFACE_STYLE_DEFAULT,
  PERSONAL_HOME_SURFACE_STYLE_STORAGE_KEY,
  loadPersonalHomeSurfaceStyle,
  parsePersonalHomeSurfaceStyle,
  savePersonalHomeSurfaceStyle,
  usePersonalHomeSurfaceStyle,
} from "@/features/personal/personal-home-surface-style";

describe("parsePersonalHomeSurfaceStyle", () => {
  it("defaults unknown values to modern-solid", () => {
    expect(parsePersonalHomeSurfaceStyle(null)).toBe("modern-solid");
    expect(parsePersonalHomeSurfaceStyle("")).toBe("modern-solid");
    expect(parsePersonalHomeSurfaceStyle("soft-glass")).toBe("modern-solid");
    expect(parsePersonalHomeSurfaceStyle("ehg-glass")).toBe("ehg-glass");
    expect(parsePersonalHomeSurfaceStyle("modern-solid")).toBe("modern-solid");
  });
});

describe("personal home surface style storage", () => {
  beforeEach(() => {
    window.localStorage.clear();
  });

  it("defaults to modern-solid when storage is empty", () => {
    expect(loadPersonalHomeSurfaceStyle()).toBe(PERSONAL_HOME_SURFACE_STYLE_DEFAULT);
    expect(PERSONAL_HOME_SURFACE_STYLE_DEFAULT).toBe("modern-solid");
  });

  it("persists selection under the Personal Home key", () => {
    savePersonalHomeSurfaceStyle("ehg-glass");
    expect(window.localStorage.getItem(PERSONAL_HOME_SURFACE_STYLE_STORAGE_KEY)).toBe("ehg-glass");
    expect(loadPersonalHomeSurfaceStyle()).toBe("ehg-glass");
  });

  it("ignores corrupt storage and falls back to default", () => {
    window.localStorage.setItem(PERSONAL_HOME_SURFACE_STYLE_STORAGE_KEY, "not-a-style");
    expect(loadPersonalHomeSurfaceStyle()).toBe("modern-solid");
  });
});

describe("usePersonalHomeSurfaceStyle", () => {
  beforeEach(() => {
    window.localStorage.clear();
  });

  it("updates immediately and writes localStorage", () => {
    const { result } = renderHook(() => usePersonalHomeSurfaceStyle());
    expect(result.current[0]).toBe("modern-solid");

    act(() => {
      result.current[1]("ehg-glass");
    });

    expect(result.current[0]).toBe("ehg-glass");
    expect(window.localStorage.getItem(PERSONAL_HOME_SURFACE_STYLE_STORAGE_KEY)).toBe("ehg-glass");
  });
});
