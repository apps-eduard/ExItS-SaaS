export const PERSONAL_INSTALL_PROMPT_STORAGE_KEY = "exits.pwa.installPrompt";
export const PERSONAL_INSTALL_COOLDOWN_MS = 7 * 24 * 60 * 60 * 1000;

const ALLOWED_RECORD_KEYS = new Set(["dismissedAtUtc", "installed"]);

export type PersonalInstallPromptRecord = {
  dismissedAtUtc?: string;
  installed?: boolean;
};

export type BeforeInstallPromptEvent = Event & {
  prompt: () => Promise<void>;
  userChoice: Promise<{ outcome: "accepted" | "dismissed" }>;
};

export type PersonalInstallMode = "installed" | "installable" | "ios-safari" | "unsupported";

let captureStarted = false;
let deferredPrompt: BeforeInstallPromptEvent | null = null;
const captureListeners = new Set<() => void>();

export function startPersonalInstallCapture(): void {
  if (captureStarted || typeof window === "undefined") {
    return;
  }
  captureStarted = true;
  window.addEventListener("beforeinstallprompt", onBeforeInstallPrompt);
  window.addEventListener("appinstalled", onAppInstalled);
}

export function subscribePersonalInstallCapture(listener: () => void): () => void {
  captureListeners.add(listener);
  return () => captureListeners.delete(listener);
}

export function peekDeferredInstallPrompt(): BeforeInstallPromptEvent | null {
  return deferredPrompt;
}

export async function runDeferredInstallPrompt(): Promise<"accepted" | "dismissed" | "unavailable"> {
  const promptEvent = deferredPrompt;
  if (!promptEvent) {
    return "unavailable";
  }
  deferredPrompt = null;
  notifyCaptureListeners();
  try {
    await promptEvent.prompt();
    const choice = await promptEvent.userChoice;
    return choice.outcome === "accepted" ? "accepted" : "dismissed";
  } catch {
    return "unavailable";
  }
}

export function resetPersonalInstallCaptureForTests(): void {
  if (typeof window !== "undefined" && captureStarted) {
    window.removeEventListener("beforeinstallprompt", onBeforeInstallPrompt);
    window.removeEventListener("appinstalled", onAppInstalled);
  }
  captureStarted = false;
  deferredPrompt = null;
  captureListeners.clear();
}

function onBeforeInstallPrompt(event: Event) {
  event.preventDefault();
  deferredPrompt = event as BeforeInstallPromptEvent;
  notifyCaptureListeners();
}

function onAppInstalled() {
  deferredPrompt = null;
  markPersonalInstallInstalled();
  notifyCaptureListeners();
}

function notifyCaptureListeners() {
  for (const listener of captureListeners) {
    listener();
  }
}

export function isStandaloneDisplay(win: Window = window): boolean {
  const nav = win.navigator as Navigator & { standalone?: boolean };
  if (nav.standalone === true) {
    return true;
  }
  return win.matchMedia?.("(display-mode: standalone)")?.matches === true;
}

export function isIosSafari(win: Window = window): boolean {
  const ua = win.navigator.userAgent ?? "";
  const iPadOs =
    win.navigator.platform === "MacIntel" && (win.navigator.maxTouchPoints ?? 0) > 1;
  const ios = /iPad|iPhone|iPod/i.test(ua) || iPadOs;
  if (!ios) {
    return false;
  }
  const safari = /Safari/i.test(ua);
  const otherBrowser = /CriOS|FxiOS|EdgiOS|OPiOS|Chrome|Chromium|Android|Edg\//i.test(ua);
  return safari && !otherBrowser;
}

export function readPersonalInstallPrompt(): PersonalInstallPromptRecord | null {
  if (typeof localStorage === "undefined") {
    return null;
  }
  try {
    const raw = localStorage.getItem(PERSONAL_INSTALL_PROMPT_STORAGE_KEY);
    if (!raw) {
      return null;
    }
    const parsed = JSON.parse(raw) as unknown;
    if (!parsed || typeof parsed !== "object" || Array.isArray(parsed)) {
      localStorage.removeItem(PERSONAL_INSTALL_PROMPT_STORAGE_KEY);
      return null;
    }
    const record = parsed as Record<string, unknown>;
    for (const key of Object.keys(record)) {
      if (!ALLOWED_RECORD_KEYS.has(key)) {
        localStorage.removeItem(PERSONAL_INSTALL_PROMPT_STORAGE_KEY);
        return null;
      }
    }
    const result: PersonalInstallPromptRecord = {};
    if (record.installed === true) {
      result.installed = true;
    }
    if (
      typeof record.dismissedAtUtc === "string" &&
      !Number.isNaN(Date.parse(record.dismissedAtUtc))
    ) {
      result.dismissedAtUtc = record.dismissedAtUtc;
    }
    if (!result.installed && !result.dismissedAtUtc) {
      localStorage.removeItem(PERSONAL_INSTALL_PROMPT_STORAGE_KEY);
      return null;
    }
    return result;
  } catch {
    return null;
  }
}

export function isPersonalInstallCoolingDown(nowMs = Date.now()): boolean {
  const record = readPersonalInstallPrompt();
  if (!record?.dismissedAtUtc || record.installed) {
    return false;
  }
  const dismissedAt = Date.parse(record.dismissedAtUtc);
  return nowMs - dismissedAt < PERSONAL_INSTALL_COOLDOWN_MS;
}

export function markPersonalInstallInstalled(): void {
  writePersonalInstallPrompt({ installed: true });
}

export function dismissPersonalInstallPrompt(now = new Date()): void {
  if (readPersonalInstallPrompt()?.installed || isStandaloneDisplay()) {
    markPersonalInstallInstalled();
    return;
  }
  writePersonalInstallPrompt({ dismissedAtUtc: now.toISOString() });
}

export function isAutomaticPersonalInstallPath(pathname: string): boolean {
  return pathname === "/personal";
}

export function resolvePersonalInstallMode(input?: {
  standalone?: boolean;
  iosSafari?: boolean;
  hasDeferredPrompt?: boolean;
}): PersonalInstallMode {
  const standalone = input?.standalone ?? isStandaloneDisplay();
  const storedInstalled = readPersonalInstallPrompt()?.installed === true;
  if (standalone || storedInstalled) {
    return "installed";
  }
  const hasDeferredPrompt = input?.hasDeferredPrompt ?? deferredPrompt !== null;
  if (hasDeferredPrompt) {
    return "installable";
  }
  if (input?.iosSafari ?? isIosSafari()) {
    return "ios-safari";
  }
  return "unsupported";
}

export function shouldShowAutomaticPersonalInstall(input: {
  pathname: string;
  online: boolean;
  mode: PersonalInstallMode;
  nowMs?: number;
}): boolean {
  if (!input.online || !isAutomaticPersonalInstallPath(input.pathname)) {
    return false;
  }
  if (input.mode !== "installable" && input.mode !== "ios-safari") {
    return false;
  }
  return !isPersonalInstallCoolingDown(input.nowMs);
}

export function shouldShowPersonalMoreInstall(mode: PersonalInstallMode): boolean {
  return mode !== "installed";
}

function writePersonalInstallPrompt(record: PersonalInstallPromptRecord): void {
  if (typeof localStorage === "undefined") {
    return;
  }
  try {
    localStorage.setItem(PERSONAL_INSTALL_PROMPT_STORAGE_KEY, JSON.stringify(record));
  } catch {
    // Private mode or a full store must not block Personal.
  }
}
