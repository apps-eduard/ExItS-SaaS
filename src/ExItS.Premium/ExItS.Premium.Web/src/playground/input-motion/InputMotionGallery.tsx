import { useRef, useState, type RefObject } from "react";
import { Button } from "../../components/button/Button";
import { Input } from "../../components/input/Input";
import {
  INPUT_FOCUS_MOTION_DEFAULT,
  INPUT_FOCUS_MOTION_OPTIONS,
  type InputFocusMotion,
} from "../../components/input/focus-motion";
import { SearchField } from "../../components/search/SearchField";
import { AppearanceControls } from "../AppearanceControls";
import styles from "./InputMotionGallery.module.scss";

const CARDS: Array<{ motion: InputFocusMotion; title: string; detail: string }> = [
  {
    motion: INPUT_FOCUS_MOTION_DEFAULT,
    title: "Current",
    detail: "The shipped focus style. Experimental motion stays off on product fields until one variant is approved.",
  },
  ...INPUT_FOCUS_MOTION_OPTIONS.map((option) => ({
    motion: option.id,
    title: option.title,
    detail: option.detail,
  })),
];

export function InputMotionGallery({ embedded = false }: { embedded?: boolean }) {
  const drawRef = useRef<HTMLInputElement>(null);

  function replayDraw() {
    const node = drawRef.current;
    if (!node) return;
    node.blur();
    node.focus();
  }

  const cards = (
    <div className={styles.grid}>
      {CARDS.map((card) => (
        <MotionCard
          key={card.motion}
          embedded={embedded}
          motion={card.motion}
          title={card.title}
          detail={card.detail}
          sampleRef={card.motion === "focus-draw" ? drawRef : undefined}
          onReplay={card.motion === "focus-draw" ? replayDraw : undefined}
        />
      ))}
    </div>
  );

  if (embedded) {
    return (
      <section className={styles.embedded} aria-labelledby="input-focus-motion">
        <div className={styles.cardCopy}>
          <h2 id="input-focus-motion" className="type-section">
            Focus motion
          </h2>
          <p className="type-secondary">
            Compare the three treatments by focusing and typing. Appearance switches Light and Dark, all five primaries, Standard, Soft, and Pill, and Compact, Standard, and Large. None of these is the global default.
          </p>
        </div>
        <p className={styles.note}>
          Disabled fields do not animate. Read-only fields keep a dashed border and the standard focus outline. Error and success keep their message, and the focus stroke uses that status color. Focus Draw starts at the bottom center and does not restart while typing.
        </p>
        {cards}
      </section>
    );
  }

  return (
    <div className={styles.page}>
      <div className={styles.layout}>
        <header className={styles.intro}>
          <a className={styles.back} href="/design-system#inputs">
            Design system
          </a>
          <h1 className="type-page">Input focus motion</h1>
          <p className="type-secondary">
            Compare three focus treatments on the same field states. Change theme, primary, corner shape, and density from Appearance. The radius tokens cover Standard, Soft, and Pill.
          </p>
          <p className={styles.note}>
            None of these variants is the global default. Disabled fields do not animate. Read-only fields keep a dashed border and the standard focus outline. Error and success keep their message, and the focus stroke uses that status color so the cue stays with the text.
          </p>
        </header>
        <div className={styles.workspace}>
          {cards}
          <aside className={styles.panel} aria-label="Appearance">
            <h2 className="type-section">Appearance</h2>
            <AppearanceControls name="input-motion" />
          </aside>
        </div>
      </div>
    </div>
  );
}

function MotionCard({
  embedded,
  motion,
  title,
  detail,
  sampleRef,
  onReplay,
}: {
  embedded: boolean;
  motion: InputFocusMotion;
  title: string;
  detail: string;
  sampleRef?: RefObject<HTMLInputElement | null>;
  onReplay?: () => void;
}) {
  const Title = embedded ? "h3" : "h2";
  const [typed, setTyped] = useState("");
  const [query, setQuery] = useState("Market");
  const headingId = `input-motion-${motion}`;

  return (
    <section className={styles.card} aria-labelledby={headingId}>
      <div className={styles.cardCopy}>
        <Title id={headingId} className="type-section">
          {title}
        </Title>
        <p className="type-secondary">{detail}</p>
      </div>
      {onReplay ? (
        <Button variant="secondary" onClick={onReplay}>
          Replay
        </Button>
      ) : null}
      <div className={styles.fields}>
        <Input ref={sampleRef} label={`${title} name`} focusMotion={motion} placeholder="Account name" autoComplete="off" />
        <Input
          label={`${title} typing`}
          focusMotion={motion}
          value={typed}
          onChange={(event) => setTyped(event.target.value)}
          placeholder="Focus once, then type"
          autoComplete="off"
        />
        <Input label={`${title} filled`} focusMotion={motion} defaultValue="Market day" autoComplete="off" />
        <Input label={`${title} error`} focusMotion={motion} defaultValue="a" error="Use at least two letters." autoComplete="off" />
        <Input label={`${title} success`} focusMotion={motion} defaultValue="Market" success="Name is available." autoComplete="off" />
        <Input label={`${title} disabled`} focusMotion={motion} value="Locked" disabled onChange={() => undefined} />
        <Input label={`${title} read only`} focusMotion={motion} value="Published" readOnly onChange={() => undefined} />
        <Input label={`${title} required`} focusMotion={motion} required placeholder="Needed" autoComplete="off" />
        <SearchField label={`${title} search`} value={query} onChange={setQuery} placeholder="Name or note" focusMotion={motion} />
      </div>
    </section>
  );
}
