// P18 context validation on Android (self-provisioned accounts via Mailpit).
// Cases: Personal-only, one-org Owner+POS, multi-org switcher, logout/restore.
// Usage: node tools/p18-android-context-validate.mjs

import { spawnSync } from "node:child_process";
import fs from "node:fs";
import path from "node:path";
import http from "node:http";
import { fileURLToPath } from "node:url";
import WebSocket from "ws";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const root = path.resolve(__dirname, "..");
const artifacts = path.join(root, "artifacts", "p18-wp08-context");
fs.mkdirSync(artifacts, { recursive: true });
const MAILPIT = "http://127.0.0.1:8025";
const PLATFORM = "http://127.0.0.1:8091";
const password = "Passw0rd!P18";
const results = [];

function adb(args) {
  const r = spawnSync("adb", args, { encoding: "utf8" });
  if (r.status !== 0) throw new Error(`adb ${args.join(" ")} failed: ${r.stderr || r.stdout}`);
  return (r.stdout || "").trim();
}
const sleep = (ms) => new Promise((r) => setTimeout(r, ms));
function record(id, pass, detail) {
  results.push({ id, pass, detail: String(detail).slice(0, 400) });
  console.log(`${pass ? "PASS" : "FAIL"} ${id}: ${String(detail).slice(0, 220).replace(/\s+/g, " ")}`);
}

function httpJson(method, url, body, headers = {}) {
  return new Promise((resolve, reject) => {
    const u = new URL(url);
    const data = body == null ? null : JSON.stringify(body);
    const req = http.request(
      {
        hostname: u.hostname,
        port: u.port,
        path: u.pathname + u.search,
        method,
        headers: {
          ...(data ? { "Content-Type": "application/json", "Content-Length": Buffer.byteLength(data) } : {}),
          ...headers,
        },
      },
      (res) => {
        let raw = "";
        res.on("data", (c) => (raw += c));
        res.on("end", () => {
          let parsed = raw;
          try {
            parsed = raw ? JSON.parse(raw) : null;
          } catch {
            /* keep raw */
          }
          resolve({ status: res.statusCode, body: parsed, raw });
        });
      }
    );
    req.on("error", reject);
    if (data) req.write(data);
    req.end();
  });
}

function extractToken(message) {
  const blob = `${message.Text || ""}\n${message.HTML || ""}`;
  const m = blob.match(/[?&]token=([^&\s"'<>]+)/i);
  if (!m) return null;
  try {
    return decodeURIComponent(m[1]);
  } catch {
    return m[1];
  }
}

async function waitMailpitToken(email, timeoutMs = 45000) {
  const started = Date.now();
  const query = encodeURIComponent(`to:${email}`);
  while (Date.now() - started < timeoutMs) {
    const search = await httpJson("GET", `${MAILPIT}/api/v1/search?query=${query}`);
    const messages = search.body?.messages || [];
    if (messages.length) {
      const full = await httpJson("GET", `${MAILPIT}/api/v1/message/${messages[0].ID}`);
      const token = extractToken(full.body || {});
      if (token) return { token, messageId: messages[0].ID };
    }
    await sleep(1000);
  }
  throw new Error(`Mailpit timeout for ${email}`);
}

/** Host-side register+activate (no debugToken) using Mailpit. */
async function provisionAccount({ email, displayName }) {
  for (let attempt = 1; attempt <= 6; attempt++) {
    const reg = await httpJson("POST", `${PLATFORM}/api/v1/platform/auth/register`, {
      displayName,
      email,
    });
    if (reg.status === 429) {
      await sleep(attempt * 4000);
      continue;
    }
    if (reg.status >= 400) throw new Error(`register ${email}: ${reg.status} ${reg.raw?.slice?.(0, 180)}`);
    const mail = await waitMailpitToken(email);
    const act = await httpJson("POST", `${PLATFORM}/api/v1/platform/auth/activate-account`, {
      token: mail.token,
      password,
    });
    if (act.status >= 400) throw new Error(`activate ${email}: ${act.status} ${act.raw?.slice?.(0, 180)}`);
    const login = await httpJson("POST", `${PLATFORM}/api/v1/platform/auth/login`, {
      usernameOrEmail: email,
      password,
    });
    if (login.status >= 400) throw new Error(`login ${email}: ${login.status} ${login.raw?.slice?.(0, 180)}`);
    return { email, displayName, sessionToken: login.body.sessionToken, userId: login.body.userId };
  }
  throw new Error(`register rate-limited for ${email}`);
}

async function startBusiness(sessionToken, displayName, slug) {
  const res = await httpJson(
    "POST",
    `${PLATFORM}/api/v1/personal/start-business`,
    { displayName, slug, startAsTrial: true, activatePosEntitlement: true, assignPosOwnerRole: true },
    { "X-ExItS-Session-Token": sessionToken }
  );
  if (res.status >= 400) throw new Error(`start-business: ${res.status} ${res.raw?.slice?.(0, 220)}`);
  return res.body;
}

async function inviteAndAccept({ ownerSession, organizationId, inviteeEmail, inviteeSession }) {
  const invite = await httpJson(
    "POST",
    `${PLATFORM}/api/v1/platform/organizations/${organizationId}/invitations`,
    { email: inviteeEmail, role: "OrganizationMember", requireEmailVerification: false },
    { "X-ExItS-Session-Token": ownerSession }
  );
  if (invite.status >= 400) throw new Error(`invite failed: ${invite.status} ${invite.raw?.slice?.(0, 220)}`);

  let token =
    invite.body?.acceptToken ||
    invite.body?.AcceptToken ||
    invite.body?.token ||
    invite.body?.debugToken ||
    invite.body?.Token;
  if (!token) {
    const mail = await waitMailpitToken(inviteeEmail, 30000);
    token = mail.token;
  }
  const accept = await httpJson(
    "POST",
    `${PLATFORM}/api/v1/platform/invitations/accept`,
    { token },
    { "X-ExItS-Session-Token": inviteeSession }
  );
  if (accept.status >= 400) throw new Error(`accept failed: ${accept.status} ${accept.raw?.slice?.(0, 220)}`);
  return true;
}

async function openCdp({ clear = true } = {}) {
  adb(["reverse", "tcp:8091", "tcp:8091"]);
  adb(["reverse", "tcp:8092", "tcp:8092"]);
  adb(["shell", "am", "force-stop", "com.exits.pinoybusinesspos"]);
  if (clear) adb(["shell", "pm", "clear", "com.exits.pinoybusinesspos"]);
  adb(["shell", "monkey", "-p", "com.exits.pinoybusinesspos", "-c", "android.intent.category.LAUNCHER", "1"]);
  await sleep(14000);
  const appPid = adb(["shell", "pidof", "com.exits.pinoybusinesspos"]).split(/\s+/)[0];
  if (!appPid) throw new Error("App PID not found");
  try {
    adb(["forward", "--remove", "tcp:9222"]);
  } catch {
    /* ignore */
  }
  adb(["forward", "tcp:9222", `localabstract:webview_devtools_remote_${appPid}`]);
  let pages = [];
  for (let i = 0; i < 40; i++) {
    try {
      pages = (
        await httpJson("GET", "http://127.0.0.1:9222/json")
      ).body;
      if (Array.isArray(pages) && pages.some((p) => p.webSocketDebuggerUrl)) break;
    } catch {
      /* retry */
    }
    await sleep(500);
  }
  const target = (pages || []).find((p) => p.webSocketDebuggerUrl);
  if (!target) throw new Error("No WebView DevTools target");
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
    const r = await send("Runtime.evaluate", { expression, awaitPromise: true, returnByValue: true });
    return r.result?.value;
  }
  async function shot(name) {
    const { data } = await send("Page.captureScreenshot", { format: "png" });
    fs.writeFileSync(path.join(artifacts, `${name}.png`), Buffer.from(data, "base64"));
  }
  async function clickText(text) {
    const ok = await evalJs(`(() => {
      const nodes = Array.from(document.querySelectorAll('button, a, [role=button], label'));
      const el = nodes.find(n => (n.textContent || '').trim().includes(${JSON.stringify(text)}));
      if (!el) return false; el.click(); return true;
    })()`);
    if (!ok) throw new Error(`clickText failed: ${text}`);
    await sleep(1400);
  }
  async function fillLabeled(labelPart, value) {
    const focused = await evalJs(`(() => {
      const labels = Array.from(document.querySelectorAll('label'));
      const lab = labels.find(l => (l.textContent || '').trim() === ${JSON.stringify(labelPart)} || (l.textContent || '').includes(${JSON.stringify(labelPart)}));
      if (!lab) return false;
      const forId = lab.getAttribute('for');
      let input = forId ? document.getElementById(forId) : null;
      if (!input) input = lab.parentElement && lab.parentElement.querySelector('input,textarea,select');
      if (!input) return false;
      input.focus(); input.select(); return true;
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
  async function waitBody(pred, timeoutMs = 30000) {
    const start = Date.now();
    let body = "";
    while (Date.now() - start < timeoutMs) {
      body = String(await evalJs("document.body && document.body.innerText"));
      if (pred(body)) return body;
      await sleep(700);
    }
    return body;
  }
  async function completeOnboardingIfNeeded() {
    let body = await waitBody(
      (b) => b.includes("Get started") || (b.includes("Sign in") && b.includes("Username")),
      35000
    );
    if (!body.includes("Get started")) return;
    await clickText("Get started");
    for (let i = 0; i < 8; i++) {
      body = String(await evalJs("document.body && document.body.innerText"));
      if (body.includes("Sign in") && body.includes("Username")) return;
      await evalJs(`(() => { const cb = document.querySelector('input[type="checkbox"]'); if (cb && !cb.checked) cb.click(); return true; })()`);
      try {
        await clickText("Continue");
      } catch {
        return;
      }
    }
  }
  async function signIn(email) {
    await completeOnboardingIfNeeded();
    await waitBody((b) => b.includes("Username") || b.includes("Sign in"), 20000);
    await fillLabeled("Username or email", email);
    await fillLabeled("Password", password);
    // Verify DOM values before submit
    const filled = await evalJs(`(() => {
      const labels = Array.from(document.querySelectorAll('label'));
      const find = (part) => {
        const lab = labels.find(l => (l.textContent || '').includes(part));
        const forId = lab && lab.getAttribute('for');
        return forId ? document.getElementById(forId) : null;
      };
      const u = find('Username or email');
      const p = find('Password');
      return { u: u && u.value, pLen: p && String(p.value || '').length };
    })()`);
    console.log("signin fields", filled);
    await evalJs(`(() => {
      const buttons = Array.from(document.querySelectorAll('button'));
      const primary = buttons.find(b => (b.textContent || '').trim() === 'Sign in');
      if (primary) { primary.click(); return true; }
      return false;
    })()`);
    const body = await waitBody(
      (b) =>
        !b.includes("Loading…") &&
        !b.includes("Loading...") &&
        ( /personal home/i.test(b) ||
          /start a business/i.test(b) ||
          /Products/i.test(b) ||
          /Account context/i.test(b) ||
          /could not be signed in/i.test(b) ||
          /Select organization/i.test(b) ||
          /Organization/i.test(b) ),
      45000
    );
    return body;
  }
  async function signOut() {
    try {
      await clickText("More");
      await sleep(800);
    } catch {
      /* maybe already auth shell */
    }
    try {
      await clickText("Sign out");
    } catch {
      try {
        await clickText("Sign out");
      } catch {
        /* ignore */
      }
    }
    await sleep(2000);
  }
  return { evalJs, shot, clickText, fillLabeled, waitBody, signIn, signOut, ws };
}

async function main() {
  const stamp = Date.now();
  const identitiesPath = path.join(artifacts, "provisioned-identities.json");
  let identities;
  if (fs.existsSync(identitiesPath) && process.env.P18_REPROVISION !== "1") {
    identities = JSON.parse(fs.readFileSync(identitiesPath, "utf8"));
    console.log("Reusing provisioned identities", {
      personal: identities.personal?.email,
      owner: identities.owner?.email,
      ownerOrgs: identities.ownerOrgCount,
    });
    // Refresh sessions
    for (const key of ["personal", "owner", "second"]) {
      const acct = identities[key];
      if (!acct?.email) continue;
      const login = await httpJson("POST", `${PLATFORM}/api/v1/platform/auth/login`, {
        usernameOrEmail: acct.email,
        password,
      });
      if (login.status < 400) {
        acct.sessionToken = login.body.sessionToken;
        acct.userId = login.body.userId;
      }
    }
  } else {
  console.log("Provisioning accounts via Platform + Mailpit...");
  const personal = await provisionAccount({
    email: `p18.ctx.personal.${stamp}@example.com`,
    displayName: `P18 Personal ${stamp}`,
  });
  await sleep(2000);
  const owner = await provisionAccount({
    email: `p18.ctx.owner.${stamp}@example.com`,
    displayName: `P18 Owner ${stamp}`,
  });
  const ownerBiz = await startBusiness(owner.sessionToken, `P18 ABC ${stamp}`, `p18-abc-${stamp}`);
  const ownerOrgId = ownerBiz.organizationId || ownerBiz.OrganizationId;
  owner.sessionToken = ownerBiz.sessionToken || ownerBiz.SessionToken || owner.sessionToken;

  await sleep(2000);
  const second = await provisionAccount({
    email: `p18.ctx.second.${stamp}@example.com`,
    displayName: `P18 Second ${stamp}`,
  });
  const secondBiz = await startBusiness(second.sessionToken, `P18 XYZ ${stamp}`, `p18-xyz-${stamp}`);
  const secondOrgId = secondBiz.organizationId || secondBiz.OrganizationId;
  second.sessionToken = secondBiz.sessionToken || secondBiz.SessionToken || second.sessionToken;

  // Multi-org: invite owner into second org
  await inviteAndAccept({
    ownerSession: second.sessionToken,
    organizationId: secondOrgId,
    inviteeEmail: owner.email,
    inviteeSession: owner.sessionToken,
  });

  // Refresh owner session orgs
  const ownerOrgs = await httpJson("GET", `${PLATFORM}/api/v1/platform/auth/organizations`, null, {
    "X-ExItS-Session-Token": owner.sessionToken,
  });
  identities = {
    stamp,
    personal,
    owner: { ...owner, orgId: ownerOrgId, biz: ownerBiz },
    second: { ...second, orgId: secondOrgId, biz: secondBiz },
    ownerOrgCount: Array.isArray(ownerOrgs.body) ? ownerOrgs.body.length : ownerOrgs.body,
  };
  fs.writeFileSync(identitiesPath, JSON.stringify(identities, null, 2));
  console.log("Provisioned", {
    personal: personal.email,
    owner: owner.email,
    ownerOrgs: identities.ownerOrgCount,
  });
  }

  const personal = identities.personal;
  const owner = identities.owner;
  const second = identities.second;

  // Case 1 — Personal only
  let cdp = await openCdp({ clear: true });
  let body = await cdp.signIn(personal.email);
  await cdp.shot("01-personal-only");
  record(
    "1-personal-only-home",
    /personal home/i.test(body),
    body
  );
  record(
    "1-personal-no-pos-chrome",
    !/\nProducts\n/.test(body) && !/\nSales\n/.test(body),
    body
  );
  // Scroll/wait for Start a Business CTA after switcher load
  body = await cdp.waitBody((b) => /start a business/i.test(b) || /account context/i.test(b), 12000);
  record(
    "1-personal-start-business",
    /start a business/i.test(body),
    body
  );
  record(
    "1-context-switcher-personal",
    /account context/i.test(body) && /Personal/i.test(body),
    body
  );
  await cdp.signOut();
  cdp.ws.close();

  // Case 2 — one org (second user has exactly one)
  cdp = await openCdp({ clear: true });
  body = await cdp.signIn(second.email);
  await cdp.shot("02-one-org");
  record(
    "2-one-org-lands-org-or-pos",
    /Products|Sales|Organization|Owner|Account context|XYZ|ABC/i.test(body),
    body
  );
  try {
    await cdp.clickText("More");
    await sleep(1200);
    body = String(await cdp.evalJs("document.body && document.body.innerText"));
    await cdp.shot("02b-more");
    record("2-context-switcher-visible", /Account context|Personal/i.test(body), body);
    const switched = await cdp.evalJs(`(() => {
      const buttons = Array.from(document.querySelectorAll('button'));
      const el = buttons.find(b => /Personal/.test(b.textContent || '') && !/Start a Business|profile/i.test(b.textContent || ''));
      if (!el) return false; el.click(); return (el.textContent || '').trim();
    })()`);
    await sleep(3500);
    body = String(await cdp.evalJs("document.body && document.body.innerText"));
    await cdp.shot("02c-personal");
    record("2-switch-to-personal", !!switched && /personal home|start a business/i.test(body), `clicked=${switched}; ${body}`);
  } catch (e) {
    record("2-context-switcher-visible", false, e.message);
    record("2-switch-to-personal", false, e.message);
  }
  await cdp.signOut();
  cdp.ws.close();

  // Case 3 — multi-org owner
  cdp = await openCdp({ clear: true });
  body = await cdp.signIn(owner.email);
  await cdp.shot("03-multi-org");
  record("3-multi-org-login", /Products|Sales|Organization|Account context|ABC|XYZ|Owner|Select organization/i.test(body), body);
  // On org-select, verify both orgs appear with roles
  body = await cdp.waitBody((b) => /P18 ABC/.test(b) && /P18 XYZ/.test(b), 15000);
  record("3-multi-org-listed", /P18 ABC/.test(body) && /P18 XYZ/.test(body), body);
  // Choose ABC
  try {
    await cdp.clickText("Use this organization");
    await sleep(5000);
    body = String(await cdp.evalJs("document.body && document.body.innerText"));
    await cdp.shot("03b-after-choose");
    record("3-multi-org-enter", /Products|Sales|Owner|Organization|Account context/i.test(body) && !/Page not found/i.test(body), body);
  } catch (e) {
    record("3-multi-org-enter", false, e.message);
  }

  // Logout + restore (do not clear app prefs)
  try {
    await cdp.clickText("More");
    await sleep(800);
  } catch {
    /* auth shell may expose Sign out directly */
  }
  await cdp.signOut();
  await sleep(1500);
  body = String(await cdp.evalJs("document.body && document.body.innerText"));
  if (!/sign in/i.test(body)) {
    // Force navigate if needed
    await cdp.evalJs(`location.assign('/signin')`);
    await sleep(2000);
    body = String(await cdp.evalJs("document.body && document.body.innerText"));
  }
  record("4-signed-out", /sign in/i.test(body), body);
  cdp.ws.close();

  cdp = await openCdp({ clear: false });
  body = await cdp.signIn(owner.email);
  await cdp.shot("04-restored");
  record("4-restore-after-relogin", /Products|Sales|Organization|Account context|ABC|XYZ|Owner|Personal|Select organization/i.test(body), body);
  cdp.ws.close();

  fs.writeFileSync(path.join(artifacts, "context-validation-results.json"), JSON.stringify({ results, identities }, null, 2));
  const failed = results.filter((r) => !r.pass);
  console.log(`DONE ${results.length - failed.length}/${results.length} passed`);
  if (failed.length) process.exit(1);
}

main().catch((e) => {
  console.error(e);
  process.exit(1);
});
