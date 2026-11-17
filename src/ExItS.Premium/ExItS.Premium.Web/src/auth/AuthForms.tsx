import { useEffect, useRef, useState, type FormEvent, type RefObject } from "react";
import { Button } from "../components/button/Button";
import { Checkbox } from "../components/checkbox/Checkbox";
import { Input } from "../components/input/Input";
import {
  passwordChecks,
  validateForgot,
  validateLogin,
  validateRegister,
  validateReset,
  type AuthSeed,
  type FieldErrors,
} from "./model";
import { PasswordField } from "./PasswordField";
import { persistRememberedEmail, readRememberedEmail } from "./remember-login";
import styles from "./AuthExperience.module.scss";

type Shared = {
  seed: AuthSeed;
  submitDelayMs: number;
  onForgot?: () => void;
  onRegister?: () => void;
  onLogin?: () => void;
  onReset?: () => void;
  onView?: (view: "form" | "instructions" | "updated") => void;
  live?: {
    login?: (input: { email: string; password: string }) => Promise<void>;
    register?: (input: { name: string; email: string }) => Promise<string>;
    forgot?: (email: string) => Promise<string>;
    reset?: (input: { password: string }) => Promise<string>;
  };
};

function failureText(error: unknown) {
  return error instanceof Error ? error.message : "The account service could not complete that request.";
}

const UNCONNECTED = "This preview is not connected. Nothing was sent, saved, or changed.";

function useBusy(locked: boolean) {
  const [busy, setBusy] = useState(locked);
  const [tracked, setTracked] = useState(locked);
  if (locked !== tracked) {
    setTracked(locked);
    setBusy(locked);
  }
  return [busy, setBusy] as const;
}

function useSubmitDelay(delayMs: number) {
  const timer = useRef<number | null>(null);

  useEffect(() => {
    return () => {
      if (timer.current != null) window.clearTimeout(timer.current);
    };
  }, []);

  function wait(done: () => void) {
    if (timer.current != null) window.clearTimeout(timer.current);
    timer.current = window.setTimeout(done, delayMs);
  }

  return wait;
}

function focusField(errors: FieldErrors, refs: Partial<Record<keyof FieldErrors, RefObject<HTMLInputElement | null>>>) {
  const order: Array<keyof FieldErrors> = ["name", "email", "password", "confirm"];
  const key = order.find((item) => errors[item]);
  if (key) refs[key]?.current?.focus();
}

function PasswordRules({ value }: { value: string }) {
  const checks = passwordChecks(value);
  const items = [
    { met: checks.length, label: "At least 10 characters" },
    { met: checks.letterAndNumber, label: "At least one letter and one number" },
  ];
  return (
    <ul className={styles.rules} aria-label="Password requirements">
      {items.map((item) => (
        <li key={item.label} className={styles.rule} data-met={item.met ? "true" : "false"}>
          <span className={styles.ruleMark} aria-hidden="true">
            {item.met ? "✓" : "–"}
          </span>
          <span className={styles.sr}>{item.met ? "Met." : "Not yet."}</span>
          <span>{item.label}</span>
        </li>
      ))}
    </ul>
  );
}

function ProviderMark({ name }: { name: "Google" | "Facebook" }) {
  if (name === "Google") {
    return (
      <svg className={styles.provider} viewBox="0 0 18 18" width="18" height="18" aria-hidden="true">
        <path fill="#4285F4" d="M17.6 9.2c0-.6-.1-1.2-.2-1.8H9v3.4h4.8a4.1 4.1 0 0 1-1.8 2.7v2.2h2.9c1.7-1.6 2.7-3.9 2.7-6.5z" />
        <path fill="#34A853" d="M9 18c2.4 0 4.5-.8 6-2.2l-2.9-2.2c-.8.6-1.9.9-3.1.9-2.4 0-4.4-1.6-5.1-3.8H.9v2.3A9 9 0 0 0 9 18z" />
        <path fill="#FBBC05" d="M3.9 10.7A5.4 5.4 0 0 1 3.6 9c0-.6.1-1.2.3-1.7V5H.9A9 9 0 0 0 0 9c0 1.4.3 2.8.9 4l3-2.3z" />
        <path fill="#EA4335" d="M9 3.6c1.3 0 2.5.5 3.4 1.3L15.1 2A9 9 0 0 0 .9 5l3 2.3C4.6 5.2 6.6 3.6 9 3.6z" />
      </svg>
    );
  }
  return (
    <svg className={styles.provider} viewBox="0 0 18 18" width="18" height="18" aria-hidden="true">
      <path
        fill="#1877F2"
        d="M18 9a9 9 0 1 0-10.4 8.9v-6.3H5.3V9h2.3V7c0-2.3 1.4-3.6 3.5-3.6.7 0 1.5.1 2.2.2v2.4h-1.2c-1.2 0-1.6.8-1.6 1.5V9h2.7l-.4 2.6h-2.3v6.3A9 9 0 0 0 18 9z"
      />
    </svg>
  );
}

function SocialSignIn() {
  return (
    <div className={styles.social}>
      <p className={styles.divider}>or</p>
      <Button type="button" variant="secondary" appearance="outline" fullWidth disabled aria-describedby="auth-providers-note" icon={<ProviderMark name="Google" />}>
        Continue with Google
      </Button>
      <Button type="button" variant="secondary" appearance="outline" fullWidth disabled aria-describedby="auth-providers-note" icon={<ProviderMark name="Facebook" />}>
        Continue with Facebook
      </Button>
      <p className={styles.fine} id="auth-providers-note">
        Google and Facebook stay unavailable until those providers are configured.
      </p>
    </div>
  );
}

export function LoginForm({ seed, submitDelayMs, onForgot, onRegister, live }: Shared) {
  const remembered = live?.login && !seed.email ? readRememberedEmail() : "";
  const [email, setEmail] = useState(seed.email || remembered);
  const [password, setPassword] = useState(seed.password);
  const [remember, setRemember] = useState(seed.remember || Boolean(remembered));
  const [errors, setErrors] = useState<FieldErrors>(() => (seed.showErrors ? validateLogin(seed) : {}));
  const [busy, setBusy] = useBusy(seed.loading);
  const [notice, setNotice] = useState<string | null>(null);
  const [problem, setProblem] = useState<string | null>(null);
  const emailRef = useRef<HTMLInputElement>(null);
  const passwordRef = useRef<HTMLInputElement>(null);
  const wait = useSubmitDelay(submitDelayMs);

  function onSubmit(event: FormEvent) {
    event.preventDefault();
    if (seed.loading) return;
    const next = validateLogin({ email, password });
    setErrors(next);
    setNotice(null);
    setProblem(null);
    if (next.email || next.password) {
      focusField(next, { email: emailRef, password: passwordRef });
      return;
    }
    setBusy(true);
    if (live?.login) {
      persistRememberedEmail(email.trim(), remember);
      void live.login({ email: email.trim(), password }).catch((error: unknown) => {
        setBusy(false);
        setProblem(failureText(error));
      });
      return;
    }
    wait(() => {
      setBusy(false);
      setNotice(UNCONNECTED);
    });
  }

  return (
    <>
      <form className={styles.form} onSubmit={onSubmit} noValidate>
        {problem ? (
          <p className={styles.alert} role="alert">
            {problem}
          </p>
        ) : null}
        {notice ? (
          <p className={styles.note} role="status">
            {notice}
          </p>
        ) : null}
        <Input
          ref={emailRef}
          label="Email"
          type="email"
          inputMode="email"
          autoComplete="username"
          autoCapitalize="off"
          spellCheck={false}
          value={email}
          required
          disabled={busy}
          error={errors.email}
          onChange={(event) => setEmail(event.target.value)}
        />
        <PasswordField
          inputRef={passwordRef}
          label="Password"
          autoComplete="current-password"
          value={password}
          revealed={seed.reveal}
          disabled={busy}
          error={errors.password}
          onChange={setPassword}
        />
        <div className={styles.assist}>
          <Checkbox label="Remember me" checked={remember} disabled={busy} onChange={(event) => setRemember(event.target.checked)} />
          <button type="button" className={styles.textButton} onClick={onForgot}>
            Forgot password?
          </button>
        </div>
        <Button type="submit" fullWidth loading={busy} disabled={busy}>
          Sign in
        </Button>
      </form>
      <SocialSignIn />
      <p className={styles.switcher}>
        New to ExItS?
        <button type="button" className={styles.textButton} onClick={onRegister}>
          Create account
        </button>
      </p>
    </>
  );
}

export function RegisterForm({ seed, submitDelayMs, onLogin, live }: Shared) {
  const [name, setName] = useState(seed.name);
  const [email, setEmail] = useState(seed.email);
  const [password, setPassword] = useState(seed.password);
  const [confirm, setConfirm] = useState(seed.confirm);
  const [errors, setErrors] = useState<FieldErrors>(() => (seed.showErrors ? validateRegister(seed) : {}));
  const [busy, setBusy] = useBusy(seed.loading);
  const [notice, setNotice] = useState<string | null>(null);
  const [problem, setProblem] = useState<string | null>(null);
  const nameRef = useRef<HTMLInputElement>(null);
  const emailRef = useRef<HTMLInputElement>(null);
  const passwordRef = useRef<HTMLInputElement>(null);
  const confirmRef = useRef<HTMLInputElement>(null);
  const wait = useSubmitDelay(submitDelayMs);

  function onSubmit(event: FormEvent) {
    event.preventDefault();
    if (seed.loading) return;
    const next = validateRegister({ name, email, password, confirm });
    setErrors(next);
    setProblem(null);
    if (Object.keys(next).length) {
      focusField(next, { name: nameRef, email: emailRef, password: passwordRef, confirm: confirmRef });
      return;
    }
    setBusy(true);
    if (live?.register) {
      void live.register({ name: name.trim(), email: email.trim() }).then((message) => {
        setBusy(false);
        setNotice(message);
      }).catch((error: unknown) => {
        setBusy(false);
        setProblem(failureText(error));
      });
      return;
    }
    wait(() => {
      setBusy(false);
      setNotice(UNCONNECTED);
    });
  }

  return (
    <form className={styles.form} onSubmit={onSubmit} noValidate>
      {problem ? (
        <p className={styles.alert} role="alert">
          {problem}
        </p>
      ) : null}
      {notice ? (
        <p className={styles.note} role="status">
          {notice}
        </p>
      ) : null}
      <Input ref={nameRef} label="Name" autoComplete="name" value={name} required disabled={busy} error={errors.name} onChange={(event) => setName(event.target.value)} />
      <Input
        ref={emailRef}
        label="Email"
        type="email"
        inputMode="email"
        autoComplete="email"
        autoCapitalize="off"
        spellCheck={false}
        value={email}
        required
        disabled={busy}
        error={errors.email}
        onChange={(event) => setEmail(event.target.value)}
      />
      <PasswordField
        inputRef={passwordRef}
        label="Password"
        autoComplete="new-password"
        value={password}
        revealed={seed.reveal}
        disabled={busy}
        error={errors.password}
        onChange={setPassword}
      />
      <PasswordRules value={password} />
      <PasswordField
        inputRef={confirmRef}
        label="Confirm password"
        autoComplete="new-password"
        value={confirm}
        revealed={seed.reveal}
        disabled={busy}
        error={errors.confirm}
        onChange={setConfirm}
      />
      <Button type="submit" fullWidth loading={busy} disabled={busy}>
        Create account
      </Button>
      <p className={styles.switcher}>
        Already have an account?
        <button type="button" className={styles.textButton} onClick={onLogin}>
          Back to sign in
        </button>
      </p>
    </form>
  );
}

export function ForgotForm({ seed, submitDelayMs, onLogin, onView, live }: Shared) {
  const [email, setEmail] = useState(seed.email);
  const [errors, setErrors] = useState<FieldErrors>(() => (seed.showErrors ? validateForgot(seed) : {}));
  const [busy, setBusy] = useBusy(seed.loading);
  const [instructions, setInstructions] = useState(seed.instructions);
  const [instructionMessage, setInstructionMessage] = useState("No email was sent. When password reset is connected, a link would go to the address you enter.");
  const [problem, setProblem] = useState<string | null>(null);
  const emailRef = useRef<HTMLInputElement>(null);
  const wait = useSubmitDelay(submitDelayMs);

  function showInstructions() {
    setInstructions(true);
    onView?.("instructions");
  }

  function onSubmit(event: FormEvent) {
    event.preventDefault();
    if (seed.loading) return;
    const next = validateForgot({ email });
    setErrors(next);
    if (next.email) {
      emailRef.current?.focus();
      return;
    }
    setBusy(true);
    if (live?.forgot) {
      void live.forgot(email.trim()).then((message) => {
        setBusy(false);
        setInstructionMessage(message);
        showInstructions();
      }).catch((error: unknown) => {
        setBusy(false);
        setProblem(failureText(error));
      });
      return;
    }
    wait(() => {
      setBusy(false);
      setInstructionMessage(`No email was sent. When password reset is connected, a link would go to ${email.trim() || "the address you enter"}.`);
      showInstructions();
    });
  }

  if (instructions) {
    return (
      <div className={styles.stack}>
        <p className={styles.note} role="status">
          {instructionMessage}
        </p>
        <Button type="button" variant="secondary" appearance="outline" fullWidth onClick={() => { setInstructions(false); onView?.("form"); }}>
          Use a different email
        </Button>
        <button type="button" className={styles.textButton} onClick={onLogin}>
          Back to sign in
        </button>
      </div>
    );
  }

  return (
    <form className={styles.form} onSubmit={onSubmit} noValidate>
      {problem ? (
        <p className={styles.alert} role="alert">
          {problem}
        </p>
      ) : null}
      <Input
        ref={emailRef}
        label="Email"
        type="email"
        inputMode="email"
        autoComplete="email"
        autoCapitalize="off"
        spellCheck={false}
        value={email}
        required
        disabled={busy}
        error={errors.email}
        onChange={(event) => setEmail(event.target.value)}
      />
      <Button type="submit" fullWidth loading={busy} disabled={busy}>
        Send reset link
      </Button>
      <button type="button" className={styles.textButton} onClick={onLogin}>
        Back to sign in
      </button>
    </form>
  );
}

export function ResetForm({ seed, submitDelayMs, onLogin, onView, live }: Shared) {
  const [password, setPassword] = useState(seed.password);
  const [confirm, setConfirm] = useState(seed.confirm);
  const [errors, setErrors] = useState<FieldErrors>(() => (seed.showErrors ? validateReset(seed) : {}));
  const [busy, setBusy] = useBusy(seed.loading);
  const [updated, setUpdated] = useState(seed.updated);
  const [updatedMessage, setUpdatedMessage] = useState("This confirmation is for review only. No password was changed.");
  const [problem, setProblem] = useState<string | null>(null);
  const passwordRef = useRef<HTMLInputElement>(null);
  const confirmRef = useRef<HTMLInputElement>(null);
  const wait = useSubmitDelay(submitDelayMs);

  function onSubmit(event: FormEvent) {
    event.preventDefault();
    if (seed.loading) return;
    const next = validateReset({ password, confirm });
    setErrors(next);
    if (next.password || next.confirm) {
      focusField(next, { password: passwordRef, confirm: confirmRef });
      return;
    }
    setBusy(true);
    if (live?.reset) {
      void live.reset({ password }).then((message) => {
        setBusy(false);
        setUpdatedMessage(message);
        setUpdated(true);
        onView?.("updated");
      }).catch((error: unknown) => {
        setBusy(false);
        setProblem(failureText(error));
      });
      return;
    }
    wait(() => {
      setBusy(false);
      setUpdatedMessage("This confirmation is for review only. No password was changed.");
      setUpdated(true);
      onView?.("updated");
    });
  }

  if (updated) {
    return (
      <div className={styles.stack}>
        <p className={styles.note} role="status">
          {updatedMessage}
        </p>
        <button type="button" className={styles.textButton} onClick={onLogin}>
          Back to sign in
        </button>
      </div>
    );
  }

  return (
    <form className={styles.form} onSubmit={onSubmit} noValidate>
      {problem ? (
        <p className={styles.alert} role="alert">
          {problem}
        </p>
      ) : null}
      <PasswordField
        inputRef={passwordRef}
        label="New password"
        autoComplete="new-password"
        value={password}
        revealed={seed.reveal}
        disabled={busy}
        error={errors.password}
        onChange={setPassword}
      />
      <PasswordRules value={password} />
      <PasswordField
        inputRef={confirmRef}
        label="Confirm password"
        autoComplete="new-password"
        value={confirm}
        revealed={seed.reveal}
        disabled={busy}
        error={errors.confirm}
        onChange={setConfirm}
      />
      <Button type="submit" fullWidth loading={busy} disabled={busy}>
        Update password
      </Button>
      <button type="button" className={styles.textButton} onClick={onLogin}>
        Back to sign in
      </button>
    </form>
  );
}
