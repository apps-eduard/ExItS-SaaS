// P18-WP08 Android WebView CDP driver (raw WebSocket; DEBUG + WebView debugging).
// Registration activation tokens are pulled from Mailpit (Local Validation), never logged.
// Usage: node tools/p18-android-webview-drive.mjs

import { spawnSync } from "node:child_process";
import fs from "node:fs";
import path from "node:path";
import http from "node:http";
import { fileURLToPath } from "node:url";
import WebSocket from "ws";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const root = path.resolve(__dirname, "..");
const artifacts = path.join(root, "artifacts", "p18-wp08");
fs.mkdirSync(artifacts, { recursive: true });

const MAILPIT = "http://127.0.0.1:8025";

function adb(args) {
  const r = spawnSync("adb", args, { encoding: "utf8" });
  if (r.status !== 0) throw new Error(`adb ${args.join(" ")} failed: ${r.stderr || r.stdout}`);
  return (r.stdout || "").trim();
}

function httpGetJson(url) {
  return new Promise((resolve, reject) => {
    http
      .get(url, (res) => {
        let data = "";
        res.on("data", (c) => (data += c));
        res.on("end", () => {
          try {
            resolve(JSON.parse(data));
          } catch {
            reject(new Error(`Bad JSON from ${url}: ${data.slice(0, 300)}`));
          }
        });
      })
      .on("error", reject);
  });
}

const sleep = (ms) => new Promise((r) => setTimeout(r, ms));

/** Extract opaque activation token from Mailpit message body. Token value is not logged. */
function extractActivationToken(message) {
  const blob = `${message.Text || ""}\n${message.HTML || ""}`;
  const m = blob.match(/[?&]token=([^&\s"'<>]+)/i);
  if (!m) return null;
  try {
    return decodeURIComponent(m[1]);
  } catch {
    return m[1];
  }
}

async function waitForMailpitActivationToken(email, timeoutMs = 45000) {
  const started = Date.now();
  const query = encodeURIComponent(`to:${email}`);
  while (Date.now() - started < timeoutMs) {
    const search = await httpGetJson(`${MAILPIT}/api/v1/search?query=${query}`);
    const messages = search.messages || [];
    if (messages.length > 0) {
      const id = messages[0].ID;
      const full = await httpGetJson(`${MAILPIT}/api/v1/message/${id}`);
      const token = extractActivationToken(full);
      if (token) {
        return { messageId: id, created: messages[0].Created, token };
      }
    }
    await sleep(1000);
  }
  throw new Error(`Mailpit: no activation mail for ${email} within ${timeoutMs}ms`);
}

async function main() {
  adb(["reverse", "tcp:8091", "tcp:8091"]);
  adb(["reverse", "tcp:8092", "tcp:8092"]);

  adb(["shell", "am", "force-stop", "com.exits.pinoybusinesspos"]);
  adb(["shell", "pm", "clear", "com.exits.pinoybusinesspos"]);
  adb([
    "shell",
    "monkey",
    "-p",
    "com.exits.pinoybusinesspos",
    "-c",
    "android.intent.category.LAUNCHER",
    "1",
  ]);
  await sleep(12000);

  const appPid = adb(["shell", "pidof", "com.exits.pinoybusinesspos"]).split(/\s+/)[0];
  if (!appPid) throw new Error("App PID not found");
  console.log("pid", appPid);

  try {
    adb(["forward", "--remove", "tcp:9222"]);
  } catch {
    /* ignore */
  }
  adb(["forward", "tcp:9222", `localabstract:webview_devtools_remote_${appPid}`]);

  let pages = [];
  for (let i = 0; i < 40; i++) {
    try {
      pages = await httpGetJson("http://127.0.0.1:9222/json");
      if (Array.isArray(pages) && pages.some((p) => p.webSocketDebuggerUrl)) break;
    } catch {
      /* retry */
    }
    await sleep(500);
  }
  const target = pages.find((p) => p.webSocketDebuggerUrl);
  if (!target) throw new Error("No WebView DevTools target");
  console.log("url", target.url);

  const ws = new WebSocket(target.webSocketDebuggerUrl);
  await new Promise((res, rej) => {
    ws.once("open", res);
    ws.once("error", rej);
  });

  let nextId = 0;
  function send(method, params = {}) {
    const id = ++nextId;
    return new Promise((resolve, reject) => {
      const onMsg = (data) => {
        const msg = JSON.parse(data.toString());
        if (msg.id !== id) return;
        ws.off("message", onMsg);
        if (msg.error) reject(new Error(JSON.stringify(msg.error)));
        else resolve(msg.result);
      };
      ws.on("message", onMsg);
      ws.send(JSON.stringify({ id, method, params }));
    });
  }

  await send("Runtime.enable");
  await send("Page.enable");

  async function evalJs(expression) {
    const r = await send("Runtime.evaluate", {
      expression,
      awaitPromise: true,
      returnByValue: true,
    });
    return r.result?.value;
  }

  async function shot(name) {
    const { data } = await send("Page.captureScreenshot", { format: "png" });
    fs.writeFileSync(path.join(artifacts, `${name}.png`), Buffer.from(data, "base64"));
    console.log("shot", name);
  }

  async function clickText(text) {
    const ok = await evalJs(`(() => {
      const nodes = Array.from(document.querySelectorAll('button, a, [role=button], label'));
      const el = nodes.find(n => (n.textContent || '').trim().includes(${JSON.stringify(text)}));
      if (!el) return false;
      el.click();
      return true;
    })()`);
    if (!ok) throw new Error(`clickText failed: ${text}`);
    await sleep(1600);
  }

  async function fillLabeled(labelPart, value) {
    const focused = await evalJs(`(() => {
      const labels = Array.from(document.querySelectorAll('label'));
      const lab = labels.find(l => (l.textContent || '').includes(${JSON.stringify(labelPart)}));
      if (!lab) return false;
      const forId = lab.getAttribute('for');
      let input = forId ? document.getElementById(forId) : null;
      if (!input) input = lab.parentElement && lab.parentElement.querySelector('input,textarea,select');
      if (!input) return false;
      input.focus();
      input.select();
      return true;
    })()`);
    if (!focused) throw new Error(`fillLabeled failed: ${labelPart}`);
    await send("Input.dispatchKeyEvent", { type: "keyDown", windowsVirtualKeyCode: 8, nativeVirtualKeyCode: 8 });
    await send("Input.dispatchKeyEvent", { type: "keyUp", windowsVirtualKeyCode: 8, nativeVirtualKeyCode: 8 });
    await send("Input.insertText", { text: value });
    await sleep(250);
    const got = await evalJs(`(() => {
      const labels = Array.from(document.querySelectorAll('label'));
      const lab = labels.find(l => (l.textContent || '').includes(${JSON.stringify(labelPart)}));
      const forId = lab && lab.getAttribute('for');
      const input = forId ? document.getElementById(forId) : null;
      return input ? String(input.value || '') : '';
    })()`);
    if (String(got) !== String(value)) {
      // Fallback: native setter + InputEvent for Blazor @oninput.
      await evalJs(`(() => {
        const labels = Array.from(document.querySelectorAll('label'));
        const lab = labels.find(l => (l.textContent || '').includes(${JSON.stringify(labelPart)}));
        const forId = lab && lab.getAttribute('for');
        const input = forId ? document.getElementById(forId) : null;
        if (!input) return false;
        const setter = Object.getOwnPropertyDescriptor(window.HTMLInputElement.prototype, 'value').set;
        setter.call(input, ${JSON.stringify(value)});
        input.dispatchEvent(new InputEvent('input', { bubbles: true, data: ${JSON.stringify(value)}, inputType: 'insertText' }));
        input.dispatchEvent(new Event('change', { bubbles: true }));
        return true;
      })()`);
      await sleep(200);
    }
  }

  await shot("cdp-01-welcome");
  let body = "";
  for (let i = 0; i < 30; i++) {
    body = String(await evalJs("document.body && document.body.innerText"));
    if (body.includes("Get started") || body.includes("unhandled error")) break;
    await sleep(1000);
  }
  console.log("body:", body.slice(0, 500));
  if (body.includes("unhandled error")) {
    throw new Error("Blazor unhandled error still present on Welcome — aborting journey");
  }
  if (!body.includes("Get started")) {
    throw new Error("Welcome did not become interactive (no Get started)");
  }

  await clickText("Get started");
  await shot("cdp-02-language");
  body = String(await evalJs("document.body && document.body.innerText"));
  console.log("after get started:", body.slice(0, 300));

  await clickText("Continue");
  await shot("cdp-03-theme");
  await clickText("Continue");
  await shot("cdp-04-density");
  await clickText("Continue");
  await shot("cdp-05-dev");
  await evalJs(`(() => {
    const cb = document.querySelector('input[type="checkbox"]');
    if (cb && !cb.checked) cb.click();
    return !!cb;
  })()`);
  await sleep(400);
  await clickText("Continue");
  await shot("cdp-06-signin");

  await clickText("Create account");
  await shot("cdp-07-register");

  const stamp = Date.now();
  const email = `p18.wp08.${stamp}@example.com`;
  const display = `P18 Owner ${stamp}`;
  const password = "Passw0rd!P18";

  await fillLabeled("Display", display);
  await fillLabeled("Email", email);
  await clickText("Register");
  await sleep(4000);
  await shot("cdp-08-registered");
  body = String(await evalJs("document.body && document.body.innerText"));
  console.log("after register:", body.slice(0, 700));

  if (body.toLowerCase().includes("could not be completed") || body.toLowerCase().includes("registration could not")) {
    throw new Error("Register still failed in UI — check AllowedHosts / adb reverse / BaseUrl");
  }

  // Local Validation: ExposeDebugTokens is off — wait for Mailpit verification email.
  console.log("mailpit: waiting for verification mail to", email);
  const mail = await waitForMailpitActivationToken(email);
  console.log("mailpit: verification mail received", { messageId: mail.messageId, created: mail.created, tokenPresent: true });
  fs.writeFileSync(
    path.join(artifacts, "cdp-mailpit-receipt.json"),
    JSON.stringify({ email, messageId: mail.messageId, created: mail.created, tokenPresent: true }, null, 2)
  );

  // Navigate to activate if still on register ack / elsewhere
  const pathNow = String(await evalJs("location.pathname"));
  if (!pathNow.includes("activate")) {
    for (const t of ["Continue to activation", "Continue", "Activate account"]) {
      try {
        await clickText(t);
        break;
      } catch {
        /* next */
      }
    }
    const still = String(await evalJs("location.pathname"));
    if (!still.includes("activate")) {
      await evalJs(`Blazor.navigateTo('/activate', true)`).catch(() => {});
      await sleep(1200);
      // If Blazor helper missing, click any known nav — force via history
      if (!String(await evalJs("location.pathname")).includes("activate")) {
        await evalJs(`location.assign('/activate')`);
        await sleep(2000);
      }
    }
  }
  await shot("cdp-09-activate");

    await fillLabeled("Activation token", mail.token);
  await fillLabeled("Password", password);
  await fillLabeled("Confirm password", password);
  for (const t of ["Activate account", "Activate", "Submit"]) {
    try {
      await clickText(t);
      break;
    } catch {
      /* next */
    }
  }
  await sleep(4000);
  await shot("cdp-10-activated");
  body = String(await evalJs("document.body && document.body.innerText"));
  console.log("after activate:", body.slice(0, 500));

  // Sign in with the new account
  {
    const pathNow2 = String(await evalJs("location.pathname"));
    if (!pathNow2.includes("signin")) {
      for (const t of ["Sign in"]) {
        try {
          await clickText(t);
          break;
        } catch {
          /* next */
        }
      }
      await sleep(1000);
    }
    await fillLabeled("Username or email", email);
    await fillLabeled("Password", password);
    // Prefer primary password sign-in (avoid Dev GUID button).
    await evalJs(`(() => {
      const buttons = Array.from(document.querySelectorAll('button'));
      const primary = buttons.find(b => (b.textContent || '').trim() === 'Sign in');
      if (primary) { primary.click(); return true; }
      return false;
    })()`);
    for (let i = 0; i < 20; i++) {
      await sleep(1000);
      body = String(await evalJs("document.body && document.body.innerText"));
      if (!body.includes("Loading…") && !body.includes("Loading...")) break;
    }
    await shot("cdp-11-signed-in");
    body = String(await evalJs("document.body && document.body.innerText"));
    console.log("after sign-in:", body.slice(0, 700));
    if (body.toLowerCase().includes("sign in with your platform") || body.toLowerCase().includes("could not")) {
      throw new Error("Sign-in did not leave the auth screen");
    }
  }

  // Start a Business if personal home
  if (body.toLowerCase().includes("start a business") || body.toLowerCase().includes("start business")) {
    for (const t of ["Start a Business", "Start a business", "Start Business"]) {
      try {
        await clickText(t);
        break;
      } catch {
        /* next */
      }
    }
    await sleep(1500);
    await shot("cdp-12-start-business");
    const orgName = `P18 Biz ${stamp}`;
    const slug = `p18-biz-${stamp}`;
    await fillLabeled("Business name", orgName);
    await fillLabeled("URL slug", slug);
    await clickText("Create organization");
    for (let i = 0; i < 30; i++) {
      await sleep(1000);
      body = String(await evalJs("document.body && document.body.innerText"));
      if (!body.includes("Loading…") && !body.includes("Loading...")) break;
    }
    await shot("cdp-13-after-start-business");
    body = String(await evalJs("document.body && document.body.innerText"));
    console.log("after start business:", body.slice(0, 700));
    if (body.toLowerCase().includes("are required")) {
      throw new Error("Start business validation still failing after fill");
    }
    if (body.toLowerCase().includes("start a business") && body.toLowerCase().includes("business name")) {
      throw new Error("Start business did not navigate away from create form");
    }
  }

  fs.writeFileSync(
    path.join(artifacts, "cdp-register-identity.json"),
    JSON.stringify({ email, display, password, stamp, mailpitMessageId: mail.messageId }, null, 2)
  );
  console.log("DONE", { email, display, mailpitMessageId: mail.messageId });
  ws.close();
}

main().catch((e) => {
  console.error(e);
  process.exit(1);
});
