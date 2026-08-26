import { useEffect, useState } from "react";

function readNavigatorOnline(): boolean {
  if (typeof navigator === "undefined") {
    return true;
  }
  return navigator.onLine;
}

/**
 * Browser online/offline is an advisory UX signal only.
 * It is not authentication, authorization, or server-health truth.
 */
export function subscribeBrowserOnline(onChange: (online: boolean) => void): () => void {
  const notify = (event?: Event) => {
    if (event?.type === "offline") {
      onChange(false);
      return;
    }
    if (event?.type === "online") {
      onChange(true);
      return;
    }
    onChange(readNavigatorOnline());
  };
  window.addEventListener("online", notify);
  window.addEventListener("offline", notify);
  notify();
  return () => {
    window.removeEventListener("online", notify);
    window.removeEventListener("offline", notify);
  };
}

export function useBrowserOnline(): boolean {
  const [online, setOnline] = useState(readNavigatorOnline);
  useEffect(() => subscribeBrowserOnline(setOnline), []);
  return online;
}
