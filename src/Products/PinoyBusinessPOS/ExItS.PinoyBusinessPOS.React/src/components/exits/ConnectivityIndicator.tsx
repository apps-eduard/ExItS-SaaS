export function ConnectivityIndicator({
  online,
  onlineLabel,
  offlineTitle,
  offlineDetail,
}: {
  online: boolean;
  onlineLabel: string;
  offlineTitle: string;
  offlineDetail: string;
}) {
  if (online) {
    return (
      <p
        className="m-0 text-[length:var(--exits-text-sm)] text-muted"
        data-testid="connectivity-online"
      >
        {onlineLabel}
      </p>
    );
  }

  return (
    <div
      role="status"
      data-testid="connectivity-offline"
      className="rounded-[var(--exits-radius-md)] border border-border bg-surface px-3 py-3"
    >
      <p className="m-0 font-semibold">{offlineTitle}</p>
      <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">{offlineDetail}</p>
    </div>
  );
}
