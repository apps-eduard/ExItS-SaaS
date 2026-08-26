export function readPosBuildLabel(): string {
  const configured = import.meta.env.VITE_POS_BUILD_SHA;
  if (typeof configured === "string" && configured.trim().length > 0) {
    return configured.trim();
  }
  return import.meta.env.MODE;
}

export function readPosApplicationName(): string {
  return "Pinoy Business POS";
}
