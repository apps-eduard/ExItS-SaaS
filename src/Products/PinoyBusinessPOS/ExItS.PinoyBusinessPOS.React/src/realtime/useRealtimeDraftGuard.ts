import { useCallback, useRef, useState } from "react";

/**
 * When a settings form has unsaved local edits, realtime invalidation must not
 * silently overwrite the draft. Components set dirty=true while editing and
 * clear after successful save or explicit reload.
 */
export function useRealtimeDraftGuard() {
  const dirtyRef = useRef(false);
  const [remoteChangePending, setRemoteChangePending] = useState(false);

  const setDirty = useCallback((dirty: boolean) => {
    dirtyRef.current = dirty;
    if (!dirty) {
      setRemoteChangePending(false);
    }
  }, []);

  const noteRemoteChange = useCallback(() => {
    if (dirtyRef.current) {
      setRemoteChangePending(true);
      return true;
    }
    return false;
  }, []);

  const clearRemoteChange = useCallback(() => {
    setRemoteChangePending(false);
  }, []);

  return {
    isDirty: () => dirtyRef.current,
    setDirty,
    remoteChangePending,
    noteRemoteChange,
    clearRemoteChange,
  };
}
