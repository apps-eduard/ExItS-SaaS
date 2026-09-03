import { notFound } from "next/navigation";

import { createExItsOgImage } from "@/lib/og-image";
import { ogImageDefinitions } from "@/lib/site-seo";

type RouteParams = {
  params: Promise<{ name: string }>;
};

export function generateStaticParams() {
  return Object.keys(ogImageDefinitions).map((name) => ({ name }));
}

export async function GET(_request: Request, { params }: RouteParams) {
  const { name } = await params;
  const definition = ogImageDefinitions[name];
  if (!definition) {
    notFound();
  }

  return createExItsOgImage(definition);
}
