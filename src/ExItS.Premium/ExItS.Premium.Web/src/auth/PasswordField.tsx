import { useId, useState, type Ref } from "react";
import { Input } from "../components/input/Input";
import { ExIcon } from "../icons/ExIcon";
import styles from "./AuthExperience.module.scss";

type PasswordFieldProps = {
  label: string;
  value: string;
  autoComplete: "current-password" | "new-password";
  error?: string;
  disabled?: boolean;
  revealed?: boolean;
  onChange: (value: string) => void;
  inputRef?: Ref<HTMLInputElement>;
};

export function PasswordField({
  label,
  value,
  autoComplete,
  error,
  disabled = false,
  revealed = false,
  onChange,
  inputRef,
}: PasswordFieldProps) {
  const id = useId();
  const [visible, setVisible] = useState(revealed);
  const [trackedReveal, setTrackedReveal] = useState(revealed);
  if (revealed !== trackedReveal) {
    setTrackedReveal(revealed);
    setVisible(revealed);
  }

  return (
    <div className={styles.secret}>
      <Input
        ref={inputRef}
        id={id}
        label={label}
        type={visible ? "text" : "password"}
        value={value}
        autoComplete={autoComplete}
        autoCapitalize="off"
        spellCheck={false}
        required
        disabled={disabled}
        error={error}
        className={styles.secretInput}
        onChange={(event) => onChange(event.target.value)}
      />
      <button
        type="button"
        className={styles.reveal}
        aria-label={visible ? `Hide ${label.toLowerCase()}` : `Show ${label.toLowerCase()}`}
        aria-pressed={visible}
        aria-controls={id}
        disabled={disabled}
        onClick={() => setVisible((current) => !current)}
      >
        <ExIcon name={visible ? "visibility_off" : "visibility"} />
      </button>
    </div>
  );
}
