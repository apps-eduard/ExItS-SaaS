import { useEffect, useRef, useState, type MouseEvent, type ReactNode } from "react";

type Wave = {
  id: number;
  diameter: number;
  x: number;
  y: number;
};

export function useQbtnRipple(enabled: boolean, waveTestId = "ui-standard-qbtn-ripple-wave") {
  const [waves, setWaves] = useState<readonly Wave[]>([]);
  const timers = useRef<number[]>([]);
  useEffect(() => {
    const pending = timers.current;
    return () => {
      for (const timer of pending) window.clearTimeout(timer);
    };
  }, []);

  function showRipple(event: MouseEvent<HTMLButtonElement>) {
    if (!enabled || event.currentTarget.disabled) return;
    const rect = event.currentTarget.getBoundingClientRect();
    const x = event.clientX - rect.left;
    const y = event.clientY - rect.top;
    const farthest = Math.max(
      Math.hypot(x, y),
      Math.hypot(rect.width - x, y),
      Math.hypot(x, rect.height - y),
      Math.hypot(rect.width - x, rect.height - y),
    );
    const diameter = farthest * 2;
    const id = Date.now() + Math.random();
    setWaves((current) => [
      ...current,
      { id, diameter, x: x - diameter / 2, y: y - diameter / 2 },
    ]);
    const timer = window.setTimeout(() => {
      setWaves((current) => current.filter((wave) => wave.id !== id));
    }, 750);
    timers.current.push(timer);
  }

  const layer: ReactNode = enabled ? (
    <span className="ui-standards-qbtn-ripple-host" aria-hidden>
      {waves.map((wave) => (
        <span
          key={wave.id}
          className="ui-standards-qbtn-ripple"
          style={{ width: wave.diameter, height: wave.diameter, left: wave.x, top: wave.y }}
          data-testid={waveTestId}
        />
      ))}
    </span>
  ) : null;

  return { showRipple, layer };
}
