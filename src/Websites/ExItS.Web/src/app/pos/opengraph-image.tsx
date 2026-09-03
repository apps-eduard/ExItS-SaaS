import { createExItsOgImage, ogImageSize } from "@/lib/og-image";

export const alt = "Pinoy Business POS by ExItS";
export const size = ogImageSize;
export const contentType = "image/png";

export default function OpenGraphImage() {
  return createExItsOgImage({
    title: "Pinoy Business POS",
    subtitle: "Point of sale and business management for Filipino retailers.",
  });
}
