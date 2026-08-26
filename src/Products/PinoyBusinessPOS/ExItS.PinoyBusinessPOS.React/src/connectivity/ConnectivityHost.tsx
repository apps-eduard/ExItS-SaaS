import { useBrowserOnline } from "@/connectivity/browser-online";
import { ConnectivityNotice } from "@/connectivity/ConnectivityNotice";

export function ConnectivityHost() {
  const online = useBrowserOnline();
  return <ConnectivityNotice offline={!online} />;
}
