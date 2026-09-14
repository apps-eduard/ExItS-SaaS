/** Print helpers for ExItS BusinessDocument shell (A4 / browser print-to-PDF). */

export function printBusinessDocument(rootSelector = "[data-business-document-root]") {
  const previous = document.body.classList.contains("exits-printing");
  document.body.classList.add("exits-printing");
  document.body.dataset.businessDocumentPrint = "true";
  const root = document.querySelector(rootSelector);
  if (root instanceof HTMLElement) {
    root.dataset.printActive = "true";
  }
  window.print();
  window.setTimeout(() => {
    if (!previous) {
      document.body.classList.remove("exits-printing");
    }
    delete document.body.dataset.businessDocumentPrint;
    if (root instanceof HTMLElement) {
      delete root.dataset.printActive;
    }
  }, 300);
}

/** Alias: PDF export uses the same print layout (browser Save as PDF). */
export function exportBusinessDocumentPdf(rootSelector?: string) {
  printBusinessDocument(rootSelector);
}
