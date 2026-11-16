import { useState } from "react";
import { Button } from "../../components/button/Button";
import { SegmentedControl } from "../../components/segmented/SegmentedControl";
import { AuthExperience } from "../../auth/AuthExperience";
import { authPreviewHref, type AuthDemonstration, type AuthScreenId } from "../../auth/model";
import { PlaygroundShell } from "../shell/PlaygroundShell";
import styles from "./AuthenticationPlayground.module.scss";

const SCREENS: Array<{ value: AuthScreenId; label: string }> = [
  { value: "login", label: "Login" },
  { value: "register", label: "Register" },
  { value: "forgot", label: "Forgot" },
  { value: "reset", label: "Reset" },
];

const STATES: Array<{ id: AuthDemonstration; label: string; screens?: AuthScreenId[] }> = [
  { id: "empty", label: "Empty form" },
  { id: "invalid", label: "Validation errors" },
  { id: "hidden", label: "Password hidden" },
  { id: "visible", label: "Password visible" },
  { id: "loading", label: "Loading" },
  { id: "failure", label: "Authentication failure" },
  { id: "instructions", label: "Reset instructions", screens: ["forgot"] },
  { id: "updated", label: "Password updated", screens: ["reset"] },
];

export function AuthenticationPlayground() {
  const [screen, setScreen] = useState<AuthScreenId>("login");
  const [demonstration, setDemonstration] = useState<AuthDemonstration>("empty");
  const states = STATES.filter((state) => !state.screens || state.screens.includes(screen));
  const active = states.some((state) => state.id === demonstration) ? demonstration : "empty";

  function chooseScreen(next: AuthScreenId) {
    setScreen(next);
    setDemonstration("empty");
  }

  return (
    <PlaygroundShell
      title="Authentication"
      description="Preview the sign-in, account, and password screens. They do not call an API, store passwords, or send email. Open the full-screen route to review the page composition, then refresh to keep the screen."
      preview={
        <AuthExperience
          key={`${screen}-${active}`}
          frame="preview"
          screen={screen}
          demonstration={active}
          onScreenChange={chooseScreen}
          submitDelayMs={0}
        />
      }
      properties={
        <div className={styles.properties}>
          <SegmentedControl label="Screen" name="auth-screen" value={screen} options={SCREENS} onChange={chooseScreen} />
          <div className={styles.states} role="group" aria-label="Preview state">
            {states.map((state) => (
              <Button
                key={state.id}
                type="button"
                fullWidth
                variant={active === state.id ? "primary" : "secondary"}
                appearance={active === state.id ? "solid" : "outline"}
                aria-pressed={active === state.id}
                onClick={() => setDemonstration(state.id)}
              >
                {state.label}
              </Button>
            ))}
          </div>
          <a className={styles.full} href={authPreviewHref(screen)}>
            Open full-screen preview
          </a>
        </div>
      }
    />
  );
}
