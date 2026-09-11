import { useCallback, useEffect, useState } from "react";
import {
  createDefaultUiStandardsDisclosure,
  readUiStandardsDisclosure,
  setTabDisclosure,
  writeUiStandardsDisclosure,
  type UiStandardsDisclosureState,
} from "@/features/ui-standards/ui-standards-disclosure";

export function useUiStandardsDisclosure() {
  const [openMap, setOpenMap] = useState<UiStandardsDisclosureState>(() =>
    createDefaultUiStandardsDisclosure(),
  );
  const [hydrated, setHydrated] = useState(false);

  useEffect(() => {
    setOpenMap(readUiStandardsDisclosure());
    setHydrated(true);
  }, []);

  useEffect(() => {
    if (!hydrated) return;
    writeUiStandardsDisclosure(openMap);
  }, [hydrated, openMap]);

  const isOpen = useCallback(
    (id: string) => openMap[id] ?? createDefaultUiStandardsDisclosure()[id] ?? false,
    [openMap],
  );

  const setOpen = useCallback((id: string, open: boolean) => {
    setOpenMap((prev) => {
      if (prev[id] === open) return prev;
      return { ...prev, [id]: open };
    });
  }, []);

  const expandAll = useCallback((tab: "tables" | "buttons") => {
    setOpenMap((prev) => setTabDisclosure(prev, tab, true));
  }, []);

  const collapseAll = useCallback((tab: "tables" | "buttons") => {
    setOpenMap((prev) => setTabDisclosure(prev, tab, false));
  }, []);

  const resetLayout = useCallback(() => {
    setOpenMap(createDefaultUiStandardsDisclosure());
  }, []);

  return { isOpen, setOpen, expandAll, collapseAll, resetLayout, openMap };
}
