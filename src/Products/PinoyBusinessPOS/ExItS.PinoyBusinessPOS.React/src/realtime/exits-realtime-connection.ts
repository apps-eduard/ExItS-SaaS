import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
} from "@microsoft/signalr";
import { platformApiUrl } from "@/api/platform/browser-session";
import {
  normalizeRealtimeEnvelope,
  type RealtimeEventEnvelope,
} from "@/realtime/realtime-event-types";

const HUB_PATH = "/hubs/exits-realtime";

type EnvelopeHandler = (envelope: RealtimeEventEnvelope) => void;
type ReconnectHandler = () => void;

/**
 * Single shared Platform SignalR connection per browser tab.
 * Prevents duplicate hub connections from PersonalAvatar + storefront hooks.
 */
class ExitsRealtimeConnectionManager {
  private connection: HubConnection | null = null;
  private startPromise: Promise<void> | null = null;
  private readonly handlers = new Set<EnvelopeHandler>();
  private readonly reconnectHandlers = new Set<ReconnectHandler>();
  private readonly joinedOrganizations = new Set<string>();
  private readonly joinedStorefrontSellers = new Set<string>();
  private refCount = 0;

  subscribe(handler: EnvelopeHandler): () => void {
    this.handlers.add(handler);
    this.refCount += 1;
    void this.ensureStarted();
    return () => {
      this.handlers.delete(handler);
      this.refCount = Math.max(0, this.refCount - 1);
      if (this.refCount === 0) {
        void this.stop();
      }
    };
  }

  /** Authoritative refetch hooks after reconnect (missed-event recovery). */
  onReconnected(handler: ReconnectHandler): () => void {
    this.reconnectHandlers.add(handler);
    return () => {
      this.reconnectHandlers.delete(handler);
    };
  }

  async joinOrganization(organizationId: string): Promise<void> {
    if (!organizationId) return;
    await this.ensureStarted();
    if (!this.connection || this.connection.state !== HubConnectionState.Connected) return;
    if (this.joinedOrganizations.has(organizationId)) return;
    try {
      await this.connection.invoke("JoinOrganization", organizationId);
      this.joinedOrganizations.add(organizationId);
    } catch {
      /* auth or network — authoritative REST still applies */
    }
  }

  async leaveOrganization(organizationId: string): Promise<void> {
    if (!organizationId || !this.connection) return;
    this.joinedOrganizations.delete(organizationId);
    if (this.connection.state !== HubConnectionState.Connected) return;
    try {
      await this.connection.invoke("LeaveOrganization", organizationId);
    } catch {
      /* ignore */
    }
  }

  async joinStorefrontSeller(organizationId: string): Promise<void> {
    if (!organizationId) return;
    await this.ensureStarted();
    if (!this.connection || this.connection.state !== HubConnectionState.Connected) return;
    if (this.joinedStorefrontSellers.has(organizationId)) return;
    try {
      await this.connection.invoke("JoinStorefrontSeller", organizationId);
      this.joinedStorefrontSellers.add(organizationId);
    } catch {
      /* ignore */
    }
  }

  async leaveStorefrontSeller(organizationId: string): Promise<void> {
    if (!organizationId || !this.connection) return;
    this.joinedStorefrontSellers.delete(organizationId);
    if (this.connection.state !== HubConnectionState.Connected) return;
    try {
      await this.connection.invoke("LeaveStorefrontSeller", organizationId);
    } catch {
      /* ignore */
    }
  }

  private async ensureStarted(): Promise<void> {
    if (import.meta.env.MODE === "test") return;
    if (this.connection?.state === HubConnectionState.Connected) return;
    if (this.startPromise) return this.startPromise;

    this.connection ??= new HubConnectionBuilder()
      .withUrl(platformApiUrl(HUB_PATH), { withCredentials: true })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .configureLogging(LogLevel.Warning)
      .build();

    this.connection.off("RealtimeEvent");
    this.connection.on("RealtimeEvent", (raw: unknown) => {
      const envelope = normalizeRealtimeEnvelope(raw);
      if (!envelope) return;
      for (const handler of this.handlers) {
        try {
          handler(envelope);
        } catch {
          /* isolate handler failures */
        }
      }
    });

    this.connection.onreconnected(() => {
      const orgs = [...this.joinedOrganizations];
      const sellers = [...this.joinedStorefrontSellers];
      this.joinedOrganizations.clear();
      this.joinedStorefrontSellers.clear();
      for (const id of orgs) void this.joinOrganization(id);
      for (const id of sellers) void this.joinStorefrontSeller(id);
      for (const handler of this.reconnectHandlers) {
        try {
          handler();
        } catch {
          /* isolate */
        }
      }
    });

    this.startPromise = this.startWithRetry();
    return this.startPromise;
  }

  private async startWithRetry(): Promise<void> {
    if (!this.connection) return;
    const delays = [0, 1000, 2000, 5000, 10000, 20000];
    for (let i = 0; i < delays.length; i++) {
      if (delays[i]! > 0) {
        await new Promise((r) => setTimeout(r, delays[i]!));
      }
      try {
        await this.connection.start();
        return;
      } catch {
        /* retry with next backoff */
      }
    }
  }

  private async stop(): Promise<void> {
    const conn = this.connection;
    this.connection = null;
    this.joinedOrganizations.clear();
    this.joinedStorefrontSellers.clear();
    if (!conn) return;
    try {
      await conn.stop();
    } catch {
      /* ignore */
    }
  }
}

export const exitsRealtimeConnection = new ExitsRealtimeConnectionManager();
