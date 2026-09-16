import type { Page } from "@playwright/test";

export type HitTestElementReport = {
  tag: string;
  className: string;
  testId: string | null;
  dataOpen: string | null;
  dataInteractive: string | null;
  dataActive: string | null;
  ariaHidden: string | null;
  inert: boolean;
  display: string;
  visibility: string;
  opacity: string;
  pointerEvents: string;
  position: string;
  zIndex: string;
  rect: { x: number; y: number; width: number; height: number };
};

export type ClickDeadnessSnapshot = {
  buildSha: string | null;
  serviceWorkerCount: number;
  x: number;
  y: number;
  elementsFromPoint: HitTestElementReport[];
  fullscreenCandidates: HitTestElementReport[];
  body: {
    overflow: string;
    pointerEvents: string;
    inert: boolean;
    ariaHidden: string | null;
  };
  html: {
    overflow: string;
    pointerEvents: string;
    inert: boolean;
  };
};

function reportElementScript() {
  return `(el) => {
    if (!(el instanceof Element)) {
      return null;
    }
    const style = window.getComputedStyle(el);
    const rect = el.getBoundingClientRect();
    return {
      tag: el.tagName.toLowerCase(),
      className: typeof el.className === "string" ? el.className : String(el.className ?? ""),
      testId: el.getAttribute("data-testid"),
      dataOpen: el.getAttribute("data-open"),
      dataInteractive: el.getAttribute("data-interactive"),
      dataActive: el.getAttribute("data-active"),
      ariaHidden: el.getAttribute("aria-hidden"),
      inert: el.hasAttribute("inert"),
      display: style.display,
      visibility: style.visibility,
      opacity: style.opacity,
      pointerEvents: style.pointerEvents,
      position: style.position,
      zIndex: style.zIndex,
      rect: { x: rect.x, y: rect.y, width: rect.width, height: rect.height },
    };
  }`;
}

export async function readRuntimeBuildSha(page: Page): Promise<string | null> {
  return page.evaluate(() => {
    const w = window as Window & { __EXITS_POS_BUILD_SHA__?: string };
    return (
      w.__EXITS_POS_BUILD_SHA__ ??
      document.documentElement.dataset.exitsPosBuildSha ??
      document.getElementById("root")?.dataset.exitsPosBuildSha ??
      null
    );
  });
}

export async function captureClickDeadnessSnapshot(
  page: Page,
  point?: { x: number; y: number },
): Promise<ClickDeadnessSnapshot> {
  const box = page.viewportSize() ?? { width: 1440, height: 900 };
  const x = point?.x ?? Math.floor(box.width / 2);
  const y = point?.y ?? Math.floor(box.height / 2);

  return page.evaluate(
    ({ x, y, reportFnSource }) => {
      // eslint-disable-next-line no-new-func -- e2e diagnostic bridge
      const reportEl = new Function(`return (${reportFnSource})`)() as (
        el: Element,
      ) => HitTestElementReport | null;

      const atPoint = document
        .elementsFromPoint(x, y)
        .map((el) => reportEl(el))
        .filter((v): v is HitTestElementReport => v != null);

      const selectors = [
        ".exits-side-drawer",
        ".exits-side-drawer__backdrop",
        ".exits-workspace-transition",
        ".exits-workspace-transition__backdrop",
        '[data-testid="client-error-overlay"]',
        "[data-exits-dropdown-portal]",
        ".exits-bizdoc-preview",
        ".exits-bizdoc-preview__backdrop",
        ".sell-cart-sheet-backdrop",
        ".sell-cart-sheet",
        '[class*="fixed"][class*="inset-0"]',
        '[role="dialog"]',
        '[role="alertdialog"]',
        '[role="menu"]',
      ];

      const seen = new Set<Element>();
      const fullscreenCandidates: HitTestElementReport[] = [];
      for (const selector of selectors) {
        for (const el of Array.from(document.querySelectorAll(selector))) {
          if (seen.has(el)) continue;
          seen.add(el);
          const style = window.getComputedStyle(el);
          const rect = el.getBoundingClientRect();
          const coversViewport =
            style.position === "fixed" &&
            rect.width >= window.innerWidth * 0.9 &&
            rect.height >= window.innerHeight * 0.5;
          if (!coversViewport && !el.classList.contains("exits-side-drawer") && !el.classList.contains("exits-workspace-transition")) {
            continue;
          }
          const report = reportEl(el);
          if (report) fullscreenCandidates.push(report);
        }
      }

      const bodyStyle = window.getComputedStyle(document.body);
      const htmlStyle = window.getComputedStyle(document.documentElement);
      const w = window as Window & { __EXITS_POS_BUILD_SHA__?: string };

      return {
        buildSha:
          w.__EXITS_POS_BUILD_SHA__ ??
          document.documentElement.dataset.exitsPosBuildSha ??
          document.getElementById("root")?.dataset.exitsPosBuildSha ??
          null,
        serviceWorkerCount: navigator.serviceWorker?.controller ? 1 : 0,
        x,
        y,
        elementsFromPoint: atPoint,
        fullscreenCandidates,
        body: {
          overflow: bodyStyle.overflow,
          pointerEvents: bodyStyle.pointerEvents,
          inert: document.body.hasAttribute("inert"),
          ariaHidden: document.body.getAttribute("aria-hidden"),
        },
        html: {
          overflow: htmlStyle.overflow,
          pointerEvents: htmlStyle.pointerEvents,
          inert: document.documentElement.hasAttribute("inert"),
        },
      };
    },
    { x, y, reportFnSource: reportElementScript() },
  );
}

/** First element at point that is still hit-testing while marked closed/inactive. */
export function findActualClickBlocker(
  snapshot: ClickDeadnessSnapshot,
): HitTestElementReport | null {
  for (const el of snapshot.elementsFromPoint) {
    if (el.pointerEvents === "none" || el.display === "none" || el.visibility === "hidden") {
      continue;
    }
    if (el.tag === "html" || el.tag === "body" || el.testId === "root") {
      continue;
    }
    const closed =
      el.dataInteractive === "false" ||
      el.dataOpen === "false" ||
      el.dataActive === "false" ||
      el.inert ||
      el.ariaHidden === "true";
    const looksLikeOverlay =
      el.position === "fixed" ||
      el.className.includes("backdrop") ||
      el.className.includes("overlay") ||
      el.className.includes("drawer") ||
      el.className.includes("transition") ||
      el.className.includes("inset-0");
    if (closed && looksLikeOverlay) {
      return el;
    }
    if (looksLikeOverlay && el.opacity === "0") {
      return el;
    }
  }

  for (const el of snapshot.fullscreenCandidates) {
    if (el.pointerEvents === "none" || el.display === "none") continue;
    if (
      el.dataInteractive === "false" ||
      el.dataActive === "false" ||
      el.inert ||
      (el.dataOpen === "false" && el.className.includes("backdrop"))
    ) {
      return el;
    }
  }

  return null;
}

export async function assertProbeClickable(page: Page, testId: string, clickIndex: number) {
  const probe = page.getByTestId(testId);
  await probe.waitFor({ state: "visible", timeout: 10_000 });
  const box = await probe.boundingBox();
  if (!box) {
    throw new Error(`Probe ${testId} has no bounding box at click #${clickIndex}`);
  }
  const point = { x: box.x + box.width / 2, y: box.y + box.height / 2 };
  try {
    await probe.click({ force: false, timeout: 5_000 });
  } catch (error) {
    const snapshot = await captureClickDeadnessSnapshot(page, point);
    const blocker = findActualClickBlocker(snapshot);
    const detail = JSON.stringify(
      {
        clickIndex,
        probe: testId,
        ACTUAL_CLICK_BLOCKER: blocker,
        topElementsFromPoint: snapshot.elementsFromPoint.slice(0, 8),
        fullscreenCandidates: snapshot.fullscreenCandidates.slice(0, 12),
        body: snapshot.body,
        html: snapshot.html,
        buildSha: snapshot.buildSha,
        serviceWorkerCount: snapshot.serviceWorkerCount,
      },
      null,
      2,
    );
    throw new Error(
      `Click deadness at interaction #${clickIndex} on ${testId}.\n${detail}\nOriginal: ${String(error)}`,
    );
  }
}
