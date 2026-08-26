const GIB = 1024 ** 3;
const MIB = 1024 ** 2;

export function formatBytes(bytes: number): string {
  const gb = bytes / GIB;
  if (gb >= 10) {
    return `${gb.toFixed(0)} GB`;
  }
  if (gb >= 1) {
    return `${gb.toFixed(1)} GB`;
  }
  const mb = bytes / MIB;
  if (mb >= 1) {
    return `${mb.toFixed(0)} MB`;
  }
  return `${bytes} B`;
}

export function formatBytesPair(used: number | null, total: number | null): string {
  if (used == null || total == null || total <= 0) {
    return "—";
  }
  return `${formatBytes(used)} / ${formatBytes(total)}`;
}

export function formatRatioPercent(used: number | null, total: number | null): string {
  if (used == null || total == null || total <= 0) {
    return "—";
  }
  return `${Math.round((used / total) * 100)}%`;
}

export function formatCpuPercent(value: number | null): string {
  if (value == null || Number.isNaN(value)) {
    return "—";
  }
  return `${value.toFixed(1)}%`;
}

export function formatDuration(seconds: number | null): string {
  if (seconds == null || seconds < 0 || Number.isNaN(seconds)) {
    return "—";
  }
  const whole = Math.floor(seconds);
  const days = Math.floor(whole / 86400);
  const hours = Math.floor((whole % 86400) / 3600);
  const minutes = Math.floor((whole % 3600) / 60);
  if (days > 0) {
    return `${days}d ${hours}h`;
  }
  if (hours > 0) {
    return `${hours}h ${minutes}m`;
  }
  if (minutes > 0) {
    return `${minutes}m`;
  }
  return `${whole}s`;
}

export function formatLatency(ms: number | null): string {
  if (ms == null || Number.isNaN(ms)) {
    return "—";
  }
  return `${Math.round(ms)} ms`;
}
