module.exports = {
  ci: {
    collect: {
      startServerCommand: "npm run start -- --port 3001",
      startServerReadyPattern: "Ready",
      startServerReadyTimeout: 120000,
      url: [
        "http://localhost:3001/",
        "http://localhost:3001/pos",
        "http://localhost:3001/products",
        "http://localhost:3001/pricing",
        "http://localhost:3001/contact",
        "http://localhost:3001/about",
      ],
      numberOfRuns: 1,
      settings: {
        // Mobile form factor is the stricter default gate for WEB-10.
        preset: "perf",
        formFactor: "mobile",
        screenEmulation: {
          mobile: true,
          width: 375,
          height: 812,
          deviceScaleFactor: 2,
          disabled: false,
        },
      },
    },
    assert: {
      assertions: {
        "categories:performance": ["error", { minScore: 0.9 }],
        "categories:accessibility": ["error", { minScore: 0.9 }],
        "categories:best-practices": ["error", { minScore: 0.9 }],
        "largest-contentful-paint": ["warn", { maxNumericValue: 2500 }],
        "cumulative-layout-shift": ["warn", { maxNumericValue: 0.1 }],
        "interactive": ["warn", { maxNumericValue: 200 }],
      },
    },
    upload: {
      target: "filesystem",
      outputDir: ".lighthouseci",
    },
  },
};
