import { createExItsOgImage } from "@/lib/og-image";

export function GET() {
  return createExItsOgImage({
    title: "Pinoy Business POS",
    subtitle: "Point of sale and business management for Filipino retailers.",
  });
}
