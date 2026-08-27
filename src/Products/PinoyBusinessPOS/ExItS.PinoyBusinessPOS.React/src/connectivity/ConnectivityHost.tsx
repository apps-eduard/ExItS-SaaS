import { ConnectivityNotice } from "@/connectivity/ConnectivityNotice";
import { useConnectivity } from "@/connectivity/ConnectivityProvider";

export function ConnectivityHost() {
  const { phase, showBackOnline, isOnline } = useConnectivity();
  const offline = !isOnline;
  const reconnecting = phase === "reconnecting" || phase === "checking";

  return (
    <ConnectivityNotice
      offline={offline}
      reconnecting={reconnecting}
      backOnline={showBackOnline && isOnline}
    />
  );
}
