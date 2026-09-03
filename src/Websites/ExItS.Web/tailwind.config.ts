import type { Config } from "tailwindcss";

const config = {
  content: ["./src/**/*.{js,ts,jsx,tsx,mdx}"],
  theme: {
    extend: {
      colors: {
        base: "var(--color-base)",
        surface: "var(--color-surface)",
        elevated: "var(--color-elevated)",
        raised: "var(--color-raised)",
        primary: "var(--color-primary)",
        muted: "var(--color-muted)",
        brand: "var(--color-brand)",
        brandBright: "var(--color-brand-bright)",
        magenta: "var(--color-magenta)",
        secondary: "var(--color-secondary)",
        emerald: "var(--color-emerald)",
        borderDefault: "var(--color-border-default)",
        borderActive: "var(--color-border-active)",
        accentGlow: "var(--color-accent-glow)",
        night: "var(--exits-night)",
        purpleDeep: "var(--exits-purple-deep)",
      },
      borderRadius: {
        md: "0.875rem",
        lg: "1rem",
        xl: "1.25rem",
        "2xl": "1.5rem",
        "3xl": "1.75rem",
        pill: "9999px",
      },
      boxShadow: {
        elevated: "0 0 0 1px var(--color-border-default)",
        glow: "var(--shadow-glow)",
        cardHover: "var(--shadow-card-hover)",
        cta: "var(--shadow-cta)",
      },
      backgroundImage: {
        "exits-cta": "var(--gradient-cta)",
        "exits-cta-shift": "var(--gradient-cta-shift)",
        "exits-hero": "var(--gradient-hero)",
        "exits-surface": "var(--gradient-surface)",
        "exits-drawer": "var(--gradient-drawer)",
        "exits-border": "var(--gradient-border)",
        "exits-text": "var(--gradient-text)",
        "exits-footer-line": "var(--gradient-footer-line)",
      },
    },
  },
  plugins: [],
} satisfies Config;

export default config;
