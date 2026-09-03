import { defineConfig, globalIgnores } from "eslint/config";
import nextVitals from "eslint-config-next/core-web-vitals";
import nextTs from "eslint-config-next/typescript";

const eslintConfig = defineConfig([
  ...nextVitals,
  ...nextTs,
  // Override default ignores of eslint-config-next.
  globalIgnores([
    // Default ignores of eslint-config-next:
    ".next/**",
    "out/**",
    "build/**",
    "next-env.d.ts",
    // WEB-10 tooling (CommonJS Node runners / LHCI configs)
    "scripts/**",
    "lighthouserc.cjs",
    "lighthouserc.desktop.cjs",
    ".lighthouseci/**",
    ".lighthouseci-desktop/**",
  ]),
]);

export default eslintConfig;
