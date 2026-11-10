import { useId, useLayoutEffect, useRef, useState, type CSSProperties, type ReactNode } from "react";
import styles from "./Tabs.module.scss";

export type TabItem<T extends string> = {
  value: T;
  label: string;
  content: ReactNode;
  disabled?: boolean;
};

type TabsProps<T extends string> = {
  label: string;
  value: T;
  onChange: (value: T) => void;
  tabs: Array<TabItem<T>>;
};

export function Tabs<T extends string>({ label, value, onChange, tabs }: TabsProps<T>) {
  const listRef = useRef<HTMLDivElement>(null);
  const baseId = useId();
  const [box, setBox] = useState({ x: 0, width: 0 });

  useLayoutEffect(() => {
    const list = listRef.current;
    const selected = list?.querySelector<HTMLElement>("[aria-selected='true']");
    if (!list || !selected) return;
    const parent = list.getBoundingClientRect();
    const rect = selected.getBoundingClientRect();
    setBox({ x: rect.left - parent.left, width: rect.width });
  }, [value, tabs]);

  function move(from: number, direction: 1 | -1) {
    const enabled = tabs.map((tab, index) => ({ tab, index })).filter((entry) => !entry.tab.disabled);
    if (enabled.length === 0) return;
    const position = enabled.findIndex((entry) => entry.index === from);
    const next = enabled[(position + direction + enabled.length) % enabled.length];
    onChange(next.tab.value);
    listRef.current?.querySelectorAll<HTMLButtonElement>("[role='tab']")[next.index]?.focus();
  }

  const style = { "--tab-x": `${box.x}px`, "--tab-w": `${box.width}px` } as CSSProperties;

  return (
    <div className={styles.tabs}>
      <div ref={listRef} className={styles.list} role="tablist" aria-label={label} style={style}>
        <span className={styles.indicator} aria-hidden="true" />
        {tabs.map((tab, index) => (
          <button
            key={tab.value}
            type="button"
            role="tab"
            className={styles.tab}
            aria-selected={tab.value === value}
            aria-controls={`${baseId}-${index}-panel`}
            id={`${baseId}-${index}-tab`}
            tabIndex={tab.value === value ? 0 : -1}
            disabled={tab.disabled}
            onClick={() => onChange(tab.value)}
            onKeyDown={(event) => {
              if (event.key === "ArrowRight") {
                event.preventDefault();
                move(index, 1);
              } else if (event.key === "ArrowLeft") {
                event.preventDefault();
                move(index, -1);
              } else if (event.key === "Home") {
                event.preventDefault();
                const first = tabs.find((item) => !item.disabled);
                if (first) onChange(first.value);
              } else if (event.key === "End") {
                event.preventDefault();
                const last = [...tabs].reverse().find((item) => !item.disabled);
                if (last) onChange(last.value);
              }
            }}
          >
            {tab.label}
          </button>
        ))}
      </div>
      {tabs.map((tab, index) => (
        <div
          key={tab.value}
          role="tabpanel"
          id={`${baseId}-${index}-panel`}
          aria-labelledby={`${baseId}-${index}-tab`}
          className={styles.panel}
          hidden={tab.value !== value}
        >
          {tab.value === value ? tab.content : null}
        </div>
      ))}
    </div>
  );
}
