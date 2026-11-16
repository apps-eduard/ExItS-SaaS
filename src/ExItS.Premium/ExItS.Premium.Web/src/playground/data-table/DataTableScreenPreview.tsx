import { useLayoutEffect, useRef, useState } from "react";
import { Select } from "../../components/select/Select";
import {
  DATA_TABLE_PREVIEW_PATH,
  DEFAULT_SCREEN_PREVIEW,
  SCREEN_PREVIEWS,
  screenPreviewById,
  screenPreviewScale,
  type ScreenPreviewId,
} from "./screen-preview";
import styles from "./DataTablePlayground.module.scss";

export function DataTableScreenPreview() {
  const stageRef = useRef<HTMLDivElement>(null);
  const [preview, setPreview] = useState<ScreenPreviewId>(DEFAULT_SCREEN_PREVIEW);
  const [available, setAvailable] = useState<number | null>(null);
  const size = screenPreviewById(preview);
  const scale = available == null ? 1 : screenPreviewScale(available, size.width);

  useLayoutEffect(() => {
    const stage = stageRef.current;
    if (!stage) return;
    const update = () => setAvailable(stage.clientWidth);
    update();
    if (typeof ResizeObserver === "undefined") return;
    const observer = new ResizeObserver(update);
    observer.observe(stage);
    return () => observer.disconnect();
  }, []);

  return (
    <div className={styles.preview}>
      <div className={styles.previewBar}>
        <div className={styles.previewSelect}>
          <Select label="Screen Preview" value={preview} onChange={(event) => setPreview(screenPreviewById(event.target.value).id)}>
            {SCREEN_PREVIEWS.map((item) => (
              <option key={item.id} value={item.id}>
                {item.label}
              </option>
            ))}
          </Select>
        </div>
        <p className={styles.viewportLabel} id="data-table-viewport-width">
          Viewport width {size.width}px
        </p>
      </div>
      <div className={styles.stage} ref={stageRef}>
        <div
          className={styles.frame}
          style={available == null ? undefined : { width: `${size.width * scale}px`, height: `${size.height * scale}px` }}
        >
          <iframe
            className={styles.viewport}
            title={`Data Table preview at ${size.width}px`}
            src={DATA_TABLE_PREVIEW_PATH}
            data-viewport-width={size.width}
            style={{ width: `${size.width}px`, height: `${size.height}px`, transform: `scale(${scale})` }}
          />
        </div>
      </div>
    </div>
  );
}
