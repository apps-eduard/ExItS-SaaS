import { DataTableDemo } from "./DataTableDemo";
import styles from "./DataTablePlayground.module.scss";

export function DataTablePreviewPage() {
  return (
    <div className={styles.previewPage}>
      <DataTableDemo />
    </div>
  );
}
