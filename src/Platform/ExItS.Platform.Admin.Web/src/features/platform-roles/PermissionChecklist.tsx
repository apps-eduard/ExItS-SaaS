import { usePreferences } from "@/hooks/use-preferences";

export function PermissionChecklist({
  options,
  value,
  disabled,
  onChange,
}: {
  options: readonly { code: string; description: string }[];
  value: readonly string[];
  disabled?: boolean;
  onChange: (next: string[]) => void;
}) {
  const { t } = usePreferences();
  const selected = new Set(value);

  return (
    <fieldset
      className="max-h-64 overflow-y-auto rounded-[var(--exits-density-radius)] border border-border p-3"
      data-testid="platform-role-permissions"
      disabled={disabled}
    >
      <legend className="px-1 text-[length:var(--exits-text-sm)] font-medium">
        {t("platformRoles.permissions")}
      </legend>
      {options.length === 0 ? (
        <p className="text-[length:var(--exits-text-sm)] text-muted">{t("platformRoles.permissions.empty")}</p>
      ) : (
        <ul className="grid gap-2">
          {options.map((option) => {
            const checked = selected.has(option.code);
            return (
              <li key={option.code}>
                <label className="flex cursor-pointer items-start gap-2 text-[length:var(--exits-text-sm)]">
                  <input
                    type="checkbox"
                    className="mt-0.5"
                    checked={checked}
                    disabled={disabled}
                    onChange={() => {
                      if (checked) {
                        onChange(value.filter((code) => code !== option.code));
                      } else {
                        onChange([...value, option.code]);
                      }
                    }}
                  />
                  <span className="min-w-0">
                    <span className="break-all font-mono text-[length:var(--exits-text-xs)]">
                      {option.code}
                    </span>
                    {option.description ? (
                      <span className="mt-0.5 block text-muted">{option.description}</span>
                    ) : null}
                  </span>
                </label>
              </li>
            );
          })}
        </ul>
      )}
    </fieldset>
  );
}
