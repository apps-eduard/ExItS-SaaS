import { createExItsOgImage, ogImageSize } from "@/lib/og-image";

export const alt = "ExItS — Business Management Platform for Filipino Businesses";
export const size = ogImageSize;
export const contentType = "image/png";

export default function OpenGraphImage() {
  return createExItsOgImage();
}
