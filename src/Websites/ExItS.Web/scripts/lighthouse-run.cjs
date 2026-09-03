/**
 * Windows-friendly Lighthouse runner for WEB-10 gates.
 * Uses the lighthouse package shipped with @lhci/cli and writes category scores
 * without depending on chrome-launcher temp-dir cleanup succeeding.
 */
const { spawn } = require("node:child_process");
const fs = require("node:fs");
const path = require("node:path");
const http = require("node:http");

const formFactor =
  process.argv.includes("--desktop") || process.env.LIGHTHOUSE_FORM_FACTOR === "desktop"
    ? "desktop"
    : "mobile";
const port = Number(process.env.LIGHTHOUSE_PORT || 3001);
const outDir =
  process.env.LIGHTHOUSE_OUT_DIR ||
  path.join(process.cwd(), formFactor === "desktop" ? ".lighthouseci-desktop" : ".lighthouseci");

const urls = (
  process.env.LIGHTHOUSE_URLS ||
  (formFactor === "desktop"
    ? "/, /pos, /pricing"
    : "/, /pos, /products, /pricing, /contact, /about")
)
  .split(",")
  .map((value) => value.trim())
  .filter(Boolean)
  .map((value) =>
    value.startsWith("http") ? value : `http://localhost:${port}${value === "/" ? "/" : value}`,
  );

const minScore = 0.9;

function waitForServer(url, attempts = 60) {
  return new Promise((resolve, reject) => {
    let remaining = attempts;
    const tick = () => {
      const req = http.get(url, (res) => {
        res.resume();
        resolve(undefined);
      });
      req.on("error", () => {
        remaining -= 1;
        if (remaining <= 0) {
          reject(new Error(`Server not ready at ${url}`));
          return;
        }
        setTimeout(tick, 1000);
      });
    };
    tick();
  });
}

function run(command, args, options = {}) {
  return new Promise((resolve, reject) => {
    const child = spawn(command, args, {
      stdio: ["ignore", "pipe", "pipe"],
      shell: true,
      ...options,
    });
    let stdout = "";
    let stderr = "";
    child.stdout.on("data", (chunk) => {
      stdout += chunk.toString();
    });
    child.stderr.on("data", (chunk) => {
      stderr += chunk.toString();
    });
    child.on("error", reject);
    child.on("close", (code) => {
      resolve({ code, stdout, stderr });
    });
  });
}

async function runLighthouse(url) {
  const safeName = url
    .replace(/^https?:\/\//, "")
    .replace(/[^\w.-]+/g, "_");
  const reportPath = path.join(outDir, `${safeName}.json`);
  const args = [
    url,
    "--quiet",
    "--chrome-flags=--headless --no-sandbox --disable-gpu",
    `--output=json`,
    `--output-path=${reportPath}`,
    `--form-factor=${formFactor}`,
    formFactor === "desktop"
      ? "--screenEmulation.mobile=false"
      : "--screenEmulation.mobile=true",
    formFactor === "desktop" ? "--throttling-method=provided" : "",
  ].filter(Boolean);

  const result = await run("npx", ["lighthouse", ...args], {
    env: {
      ...process.env,
      TEMP: outDir,
      TMP: outDir,
      TMPDIR: outDir,
    },
  });

  if (!fs.existsSync(reportPath)) {
    throw new Error(
      `Lighthouse did not write report for ${url}\nstdout:${result.stdout}\nstderr:${result.stderr}`,
    );
  }

  const report = JSON.parse(fs.readFileSync(reportPath, "utf8"));
  return {
    url,
    categories: {
      performance: report.categories.performance.score,
      accessibility: report.categories.accessibility.score,
      "best-practices": report.categories["best-practices"].score,
      seo: report.categories.seo?.score ?? null,
    },
    metrics: {
      lcp: report.audits["largest-contentful-paint"]?.numericValue ?? null,
      cls: report.audits["cumulative-layout-shift"]?.numericValue ?? null,
      tbt: report.audits["total-blocking-time"]?.numericValue ?? null,
      fcp: report.audits["first-contentful-paint"]?.numericValue ?? null,
    },
  };
}

async function main() {
  fs.mkdirSync(outDir, { recursive: true });

  const server = spawn("npm", ["run", "start", "--", "--port", String(port)], {
    stdio: ["ignore", "pipe", "pipe"],
    shell: true,
    env: process.env,
  });

  let serverLog = "";
  server.stdout.on("data", (chunk) => {
    serverLog += chunk.toString();
  });
  server.stderr.on("data", (chunk) => {
    serverLog += chunk.toString();
  });

  try {
    await waitForServer(`http://localhost:${port}/`);
    const results = [];
    for (const url of urls) {
      // eslint-disable-next-line no-await-in-loop
      const entry = await runLighthouse(url);
      results.push(entry);
      console.log(
        JSON.stringify(
          {
            url: entry.url,
            performance: entry.categories.performance,
            accessibility: entry.categories.accessibility,
            bestPractices: entry.categories["best-practices"],
            seo: entry.categories.seo,
            lcpMs: entry.metrics.lcp,
            cls: entry.metrics.cls,
            tbtMs: entry.metrics.tbt,
          },
          null,
          2,
        ),
      );
    }

    const summaryPath = path.join(outDir, "summary.json");
    fs.writeFileSync(summaryPath, JSON.stringify({ formFactor, results }, null, 2));

    const failures = [];
    for (const entry of results) {
      for (const key of ["performance", "accessibility", "best-practices"]) {
        const score = entry.categories[key];
        if (typeof score !== "number" || score < minScore) {
          failures.push(`${entry.url} ${key}=${score}`);
        }
      }
    }

    if (failures.length > 0) {
      console.error("Lighthouse gate failures:\n" + failures.join("\n"));
      process.exitCode = 1;
    } else {
      console.log(`Lighthouse ${formFactor} gates passed (>= ${minScore}).`);
    }
  } finally {
    if (process.platform === "win32") {
      spawn("taskkill", ["/pid", String(server.pid), "/T", "/F"], {
        stdio: "ignore",
        shell: true,
      });
    } else {
      server.kill("SIGTERM");
    }
    if (serverLog.includes("Error")) {
      console.error(serverLog.slice(-2000));
    }
  }
}

main().catch((error) => {
  console.error(error);
  process.exit(1);
});
