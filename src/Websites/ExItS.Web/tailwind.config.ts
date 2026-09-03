import type { Config } from "tailwindcss";

const config = {
  content: ["./src/**/*.{js,ts,jsx,tsx,mdx}"],
  theme: {
    extend: {
      colors: {
        base: "var(--color-base)",
        surface: "var(--color-surface)",
        elevated: "var(--color-elevated)",
        primary: "var(--color-primary)",
        muted: "var(--color-muted)",
        brand: "var(--color-brand)",
        brandBright: "var(--color-brand-bright)",
        secondary: "var(--color-secondary)",
        borderDefault: "var(--color-border-default)",
      },
      borderRadius: {
        md: "0.75rem",
        lg: "0.875rem",
        xl: "1rem",
      },
      boxShadow: {
        elevated: "0 0 0 1px var(--color-border-default)",
      },
    },
  },
  plugins: [],
} satisfies Config;

export default config;

