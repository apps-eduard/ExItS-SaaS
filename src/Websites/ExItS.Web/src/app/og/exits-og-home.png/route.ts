import { createExItsOgImage } from "@/lib/og-image";

export function GET() {
  return createExItsOgImage({
    title: "Business management for Filipino businesses",
    subtitle: "Pinoy Business POS — available now. Other products coming soon.",
  });
}
