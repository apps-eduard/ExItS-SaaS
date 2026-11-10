import { useState } from "react";

export function useControllable<T>({
  value,
  defaultValue,
  onChange,
}: {
  value: T | undefined;
  defaultValue: T;
  onChange?: (value: T) => void;
}): [T, (next: T) => void] {
  const [internal, setInternal] = useState(defaultValue);
  const controlled = value !== undefined;
  const current = controlled ? value : internal;

  function set(next: T) {
    if (!controlled) setInternal(next);
    onChange?.(next);
  }

  return [current, set];
}
