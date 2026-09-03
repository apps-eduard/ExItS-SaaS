import { ImageResponse } from "next/og";

export const ogImageSize = { width: 1200, height: 630 };

export function createExItsOgImage({
  title,
  subtitle,
}: {
  title: string;
  subtitle: string;
}) {
  return new ImageResponse(
    (
      <div
        style={{
          width: "100%",
          height: "100%",
          display: "flex",
          flexDirection: "column",
          justifyContent: "center",
          backgroundColor: "#080e0a",
          color: "#f0f4f1",
          padding: 80,
        }}
      >
        <div style={{ fontSize: 28, color: "#34d399", letterSpacing: 4 }}>ExItS</div>
        <div
          style={{
            fontSize: 58,
            fontWeight: 700,
            marginTop: 28,
            lineHeight: 1.1,
            maxWidth: 900,
          }}
        >
          {title}
        </div>
        <div style={{ fontSize: 24, color: "#8aa690", marginTop: 28, maxWidth: 900 }}>
          {subtitle}
        </div>
      </div>
    ),
    ogImageSize,
  );
}
