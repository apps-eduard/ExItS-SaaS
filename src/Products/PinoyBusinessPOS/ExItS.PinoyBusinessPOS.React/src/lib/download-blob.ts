/** Trigger a browser file download from a Blob or ArrayBuffer. */
export function downloadBlob(filename: string, data: Blob | ArrayBuffer, mimeType: string): void {
  const blob = data instanceof Blob ? data : new Blob([data], { type: mimeType });
  const url = URL.createObjectURL(blob);
  const anchor = document.createElement("a");
  anchor.href = url;
  anchor.download = filename;
  anchor.rel = "noopener";
  document.body.appendChild(anchor);
  anchor.click();
  anchor.remove();
  URL.revokeObjectURL(url);
}
