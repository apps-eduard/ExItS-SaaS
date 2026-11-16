import { useEffect, useRef, useState } from "react";
import { ForgotForm, LoginForm, RegisterForm, ResetForm } from "./AuthForms";
import {
  accountHome,
  authPreviewHref,
  authSeed,
  readAuthScreen,
  type AuthDemonstration,
  type AuthScreenId,
} from "./model";
import { currentSession, loginWithPassword, registerPersonalAccount, requestPasswordReset, resetPassword, resetTokenFromLocation } from "./platform-client";
import styles from "./AuthExperience.module.scss";

type AuthView = "form" | "instructions" | "updated";

type AuthExperienceProps = {
  frame?: "page" | "preview";
  screen?: AuthScreenId;
  demonstration?: AuthDemonstration;
  onScreenChange?: (screen: AuthScreenId) => void;
  submitDelayMs?: number;
  /** Calls the account service. The design-system playground leaves this off. */
  live?: boolean;
};

const COPY: Record<AuthScreenId, { title: string; lede: string }> = {
  login: {
    title: "Welcome back",
    lede: "Use your email and password.",
  },
  register: {
    title: "Create your account",
    lede: "Choose a name, email, and password.",
  },
  forgot: {
    title: "Forgot password",
    lede: "Enter the email for your account.",
  },
  reset: {
    title: "Choose a new password",
    lede: "Set a new password for this account.",
  },
};

function titleFor(screen: AuthScreenId, view: AuthView) {
  if (screen === "forgot" && view === "instructions") return "Reset instructions";
  if (screen === "reset" && view === "updated") return "Password updated";
  return COPY[screen].title;
}

function ledeFor(screen: AuthScreenId, view: AuthView, live: boolean) {
  if (screen === "forgot" && view === "instructions") {
    return live
      ? "The account service returned a status. Email delivery is not confirmed."
      : "Review the next step. No reset email was sent.";
  }
  if (screen === "reset" && view === "updated") {
    return live ? "The account service returned a status for this password." : "The updated state is shown here for review.";
  }
  return COPY[screen].lede;
}

function initialView(screen: AuthScreenId, demonstration: AuthDemonstration | undefined): AuthView {
  const seed = authSeed(demonstration);
  if (screen === "forgot" && seed.instructions) return "instructions";
  if (screen === "reset" && seed.updated) return "updated";
  return "form";
}

const DELIVERY_NOTE = "This app cannot confirm that an email was delivered.";

export function AuthExperience({
  frame = "page",
  screen: screenProp,
  demonstration,
  onScreenChange,
  submitDelayMs = 640,
  live = false,
}: AuthExperienceProps) {
  const syncUrl = frame === "page";
  const [internalScreen, setInternalScreen] = useState<AuthScreenId>(() => screenProp ?? (syncUrl ? readAuthScreen(window.location.search) : "login"));
  const screen = screenProp ?? internalScreen;
  const [view, setView] = useState<AuthView>(() => initialView(screen, demonstration));
  const headingRef = useRef<HTMLHeadingElement>(null);
  const skipFocus = useRef(true);
  const seed = authSeed(demonstration);

  useEffect(() => {
    if (!syncUrl || screenProp) return;
    const onPop = () => {
      const next = readAuthScreen(window.location.search);
      setInternalScreen(next);
      setView("form");
    };
    window.addEventListener("popstate", onPop);
    return () => window.removeEventListener("popstate", onPop);
  }, [syncUrl, screenProp]);

  useEffect(() => {
    if (skipFocus.current) {
      skipFocus.current = false;
      return;
    }
    headingRef.current?.focus();
  }, [screen, view]);

  function go(next: AuthScreenId) {
    setView("form");
    onScreenChange?.(next);
    if (screenProp === undefined) setInternalScreen(next);
    if (!syncUrl) return;
    const href = authPreviewHref(next);
    if (`${window.location.pathname}${window.location.search}` !== href) {
      window.history.pushState(null, "", href);
    }
  }

  const title = titleFor(screen, view);
  const lede = ledeFor(screen, view, live);
  const account = live
    ? {
        login: async (input: { email: string; password: string }) => {
          await loginWithPassword(input.email, input.password);
          const session = await currentSession();
          window.location.replace(accountHome(session?.accountClass));
        },
        register: async (input: { name: string; email: string }) => {
          const message = await registerPersonalAccount(input.name, input.email);
          return `${message} ${DELIVERY_NOTE} The password on this form was not saved. Activation sets the password.`;
        },
        forgot: async (email: string) => {
          const message = await requestPasswordReset(email);
          return `${message} ${DELIVERY_NOTE} Reset links are not addressed to ExItS Premium.`;
        },
        reset: async (input: { password: string }) => {
          const token = resetTokenFromLocation(window.location.search);
          if (!token) throw new Error("A reset token is required. Email delivery is not confirmed.");
          const message = await resetPassword(token, input.password);
          return message;
        },
      }
    : undefined;

  return (
    <div className={styles.frame} data-frame={frame} data-auth-screen={screen}>
      <div className={styles.layout}>
        <aside className={styles.brand}>
          <div>
            <div className={styles.brandLockup}>
              <span className={styles.mark} aria-hidden="true">
                E
              </span>
              <span>
                <span className={styles.word}>ExItS</span>
                <span className={styles.product}>Premium</span>
              </span>
            </div>
            <p className={styles.brandTitle}>A quieter way to keep your money clear.</p>
            <p className={`${styles.brandCopy} type-secondary`}>
              Personal balances, people, and what comes next — arranged so the next step is obvious.
            </p>
            <ol className={styles.facts}>
              <li>
                <span className={styles.index}>01</span>
                <span>Your records stay with your account.</span>
              </li>
              <li>
                <span className={styles.index}>02</span>
                <span>Amounts stay readable at a glance.</span>
              </li>
              <li>
                <span className={styles.index}>03</span>
                <span>The screen keeps only what you need.</span>
              </li>
            </ol>
          </div>
          <p className={styles.fine}>{live ? "Sign-in uses your ExItS account." : "Authentication preview. Sign-in is not connected."}</p>
        </aside>
        <section className={styles.formPane} aria-label="Authentication">
          <div className={styles.column} key={screen}>
            {frame === "page" ? (
              <a className={styles.back} href="/design-system#authentication">
                Design system
              </a>
            ) : null}
            <div className={styles.mobileBrand}>
              <span className={styles.mark} aria-hidden="true">
                E
              </span>
              <span>
                <span className={styles.word}>ExItS</span>
                <span className={styles.product}>Premium</span>
              </span>
            </div>
            <header className={styles.intro}>
              <h1 ref={headingRef} tabIndex={-1} className="type-display" data-auth-heading="">
                {title}
              </h1>
              <p className="type-secondary">{lede}</p>
              {!live && view === "form" ? <p className={styles.formNote}>Preview only. Nothing is sent, saved, or changed.</p> : null}
            </header>
            {seed.failure ? (
              <p className={styles.alert} role="alert">
                This preview shows an authentication failure. No account was checked.
              </p>
            ) : null}
            {screen === "login" ? (
              <LoginForm seed={seed} submitDelayMs={submitDelayMs} live={account} onForgot={() => go("forgot")} onRegister={() => go("register")} />
            ) : null}
            {screen === "register" ? <RegisterForm seed={seed} submitDelayMs={submitDelayMs} live={account} onLogin={() => go("login")} /> : null}
            {screen === "forgot" ? (
              <ForgotForm seed={seed} submitDelayMs={submitDelayMs} live={account} onLogin={() => go("login")} onView={setView} />
            ) : null}
            {screen === "reset" ? (
              <ResetForm seed={seed} submitDelayMs={submitDelayMs} live={account} onLogin={() => go("login")} onView={setView} />
            ) : null}
          </div>
        </section>
      </div>
    </div>
  );
}
