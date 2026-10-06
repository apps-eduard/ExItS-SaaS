import { Input } from "@/components/ui/input";

export function PostalCodeInput({
  value,
  onChange,
  label,
  requirementLabel,
  testId = "postal-code",
}: {
  value: string;
  onChange: (value: string) => void;
  label: string;
  requirementLabel: string;
  testId?: string;
}) {
  return (
    <Input
      label={`${label} · ${requirementLabel}`}
      value={value}
      onChange={(event) => onChange(event.target.value)}
      data-testid={testId}
    />
  );
}
