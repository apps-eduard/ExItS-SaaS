import { afterEach, describe, expect, it } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { AppProviders } from "@/app/providers";
import {
  PersonalInstallHomeOffer,
  PersonalInstallMoreEntry,
} from "@/features/personal/PersonalInstallOffer";
import { readFileSync } from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import {
  PERSONAL_INSTALL_PROMPT_STORAGE_KEY,
  resetPersonalInstallCaptureForTests,
  startPersonalInstallCapture,
} from "@/pwa/personal-install-prompt";

afterEach(() => {
  resetPersonalInstallCaptureForTests();
});

describe("Personal install offer", () => {
  it("offers Install ExItS on /personal after the browser install event", async () => {
    const user = userEvent.setup();
    const prompt = viPrompt();
    capturePrompt(prompt.event);
    renderOffer("/personal");
    expect(screen.getByTestId("personal-install-offer")).toHaveTextContent("Add ExItS to your phone");
    expect(prompt.prompted).toBe(false);
    await user.click(screen.getByTestId("personal-install-accept"));
    expect(prompt.prompted).toBe(true);
    expect(screen.getByTestId("personal-install-accepted")).toBeInTheDocument();
    expect(localStorage.getItem(PERSONAL_INSTALL_PROMPT_STORAGE_KEY)).toBeNull();
    window.dispatchEvent(new Event("appinstalled"));
    expect(localStorage.getItem(PERSONAL_INSTALL_PROMPT_STORAGE_KEY)).toContain("\"installed\":true");
  });

  it("does not offer installation on the sign-in page", () => {
    capturePrompt(viPrompt().event);
    renderOffer("/sign-in");
    expect(screen.queryByTestId("personal-install-offer")).not.toBeInTheDocument();
  });

  it("hides the offer when the display is already standalone", () => {
    const original = window.matchMedia;
    window.matchMedia = ((query: string) => ({
      matches: query === "(display-mode: standalone)",
      media: query,
      addEventListener: () => undefined,
      removeEventListener: () => undefined,
      addListener: () => undefined,
      removeListener: () => undefined,
      dispatchEvent: () => false,
      onchange: null,
    })) as typeof window.matchMedia;
    capturePrompt(viPrompt().event);
    renderOffer("/personal");
    expect(screen.queryByTestId("personal-install-offer")).not.toBeInTheDocument();
    window.matchMedia = original;
  });

  it("shows Safari steps on iPhone and keeps More available after Maybe later", async () => {
    const user = userEvent.setup();
    const original = window.navigator.userAgent;
    Object.defineProperty(window.navigator, "userAgent", {
      configurable: true,
      value:
        "Mozilla/5.0 (iPhone; CPU iPhone OS 17_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.0 Mobile/15E148 Safari/604.1",
    });
    renderOffer("/personal");
    expect(screen.getByText("Tap the Share button")).toBeInTheDocument();
    expect(screen.queryByTestId("personal-install-accept")).not.toBeInTheDocument();
    await user.click(screen.getByTestId("personal-install-later"));
    expect(screen.queryByTestId("personal-install-offer")).not.toBeInTheDocument();
    renderMore();
    expect(screen.getByTestId("personal-more-install-open")).toHaveTextContent("Install ExItS");
    Object.defineProperty(window.navigator, "userAgent", { configurable: true, value: original });
  });
});

describe("Personal More install entry", () => {
  it("is mounted from the More page", () => {
    const source = readFileSync(
      path.join(path.dirname(fileURLToPath(import.meta.url)), "PersonalHubPages.tsx"),
      "utf8",
    );
    expect(source).toContain("<PersonalInstallMoreEntry />");
  });
});

function renderOffer(pathname: string) {
  render(
    <AppProviders>
      <MemoryRouter initialEntries={[pathname]}>
        <Routes>
          <Route path="*" element={<PersonalInstallHomeOffer />} />
        </Routes>
      </MemoryRouter>
    </AppProviders>,
  );
}

function renderMore() {
  render(
    <AppProviders>
      <MemoryRouter>
        <PersonalInstallMoreEntry />
      </MemoryRouter>
    </AppProviders>,
  );
}

function capturePrompt(event: Event) {
  startPersonalInstallCapture();
  window.dispatchEvent(event);
}

function viPrompt() {
  const state = {
    prompted: false,
    event: new Event("beforeinstallprompt", { cancelable: true }) as Event & {
      prompt: () => Promise<void>;
      userChoice: Promise<{ outcome: "accepted" }>;
    },
  };
  state.event.prompt = () => {
    state.prompted = true;
    return Promise.resolve();
  };
  state.event.userChoice = Promise.resolve({ outcome: "accepted" });
  return state;
}
