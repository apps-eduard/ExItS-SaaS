import { IconTile } from "../components/icon-tile/IconTile";
import { ExIcon } from "../icons/ExIcon";
import { FEATURE_ICONS, FEATURE_TONES, ICON_NAMES } from "../icons/registry";
import styles from "./Playground.module.scss";

export function IconsSection() {
  return (
    <>
      <header className={styles.intro}>
        <h1 className="type-page">Icons</h1>
        <p className="type-secondary">
          Material Symbols Rounded is self-hosted. Navigation stays monochrome. Colored tiles are reserved for feature actions.
        </p>
      </header>
      <section className={styles.stageBlock}>
        <h2 className="type-section">Size, weight, and fill</h2>
        <div className={styles.stage}>
          <ExIcon name="search" size={20} label="20 pixel search" />
          <ExIcon name="search" size={24} label="24 pixel search" />
          <ExIcon name="search" size={40} label="40 pixel search" />
          <ExIcon name="notifications" size={24} weight={600} label="Heavier notification" />
          <ExIcon name="notifications" size={24} fill={1} label="Filled notification" />
        </div>
      </section>
      <section className={styles.stageBlock}>
        <h2 className="type-section">Feature tiles</h2>
        <div className={styles.stage}>
          {FEATURE_TONES.map((tone) => (
            <IconTile key={tone} name={FEATURE_ICONS[tone]} variant="feature" tone={tone} label={tone} />
          ))}
          <IconTile name="dashboard" variant="neutral" label="Neutral" />
          <IconTile name="palette" variant="primary" label="Primary" />
          <IconTile name="check" variant="solid" label="Solid" />
        </div>
      </section>
      <section className={styles.stageBlock}>
        <h2 className="type-section">Registry</h2>
        <div className={styles.iconGrid}>
          {ICON_NAMES.map((name) => (
            <span key={name} className={styles.iconCell}>
              <ExIcon name={name} size={24} />
              <span className="type-caption">{name.replaceAll("_", " ")}</span>
            </span>
          ))}
        </div>
      </section>
    </>
  );
}
