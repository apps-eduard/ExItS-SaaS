/** Print helpers for ExItS BusinessDocument shell (A4 / browser print-to-PDF). */

function clearPrintFlags(
  root: HTMLElement | null,
  previousHadPrintingClass: boolean,
) {
  if (!previousHadPrintingClass) {
    document.body.classList.remove("exits-printing");
  }
  delete document.body.dataset.businessDocumentPrint;
  if (root) {
    delete root.dataset.printActive;
  }
}

export function printBusinessDocument(rootSelector = "[data-business-document-root]") {
  const previous = document.body.classList.contains("exits-printing");
  document.body.classList.add("exits-printing");
  document.body.dataset.businessDocumentPrint = "true";
  const root = document.querySelector(rootSelector);
  if (root instanceof HTMLElement) {
    root.dataset.printActive = "true";
  }

  const cleanup = () => {
    window.removeEventListener("afterprint", cleanup);
    window.clearTimeout(fallbackTimer);
    clearPrintFlags(root instanceof HTMLElement ? root : null, previous);
  };

  window.addEventListener("afterprint", cleanup);
  // Fallback when afterprint is delayed/missing (some WebViews).
  const fallbackTimer = window.setTimeout(cleanup, 1000);

  window.print();
}

/** Alias: PDF export uses the same print layout (browser Save as PDF). */
export function exportBusinessDocumentPdf(rootSelector?: string) {
  printBusinessDocument(rootSelector);
}
