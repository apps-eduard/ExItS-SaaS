export function areDevelopmentToolsAllowed(): boolean {
  return import.meta.env.MODE !== "production";
}
