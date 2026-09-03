module.exports = {
  ci: {
    collect: {
      startServerCommand: "npm run start -- --port 3001",
      startServerReadyPattern: "Ready",
      startServerReadyTimeout: 120000,
      url: [
        "http://localhost:3001/",
        "http://localhost:3001/pos",
        "http://localhost:3001/pricing",
      ],
      numberOfRuns: 1,
      settings: {
        formFactor: "desktop",
        screenEmulation: {
          mobile: false,
          width: 1440,
          height: 900,
          deviceScaleFactor: 1,
          disabled: false,
        },
        throttling: {
          rttMs: 40,
          throughputKbps: 10240,
          cpuSlowdownMultiplier: 1,
        },
      },
    },
    assert: {
      assertions: {
        "categories:performance": ["error", { minScore: 0.9 }],
        "categories:accessibility": ["error", { minScore: 0.9 }],
        "categories:best-practices": ["error", { minScore: 0.9 }],
      },
    },
    upload: {
      target: "filesystem",
      outputDir: ".lighthouseci-desktop",
    },
  },
};
