import type { MDXComponents } from "mdx/types";
import Link from "next/link";
import type { ComponentPropsWithoutRef } from "react";

function MdxLink({ href, children, ...props }: ComponentPropsWithoutRef<"a">) {
  if (href?.startsWith("/")) {
    return (
      <Link href={href} {...props}>
        {children}
      </Link>
    );
  }

  return (
    <a href={href} rel="noopener noreferrer" {...props}>
      {children}
    </a>
  );
}

export function useMDXComponents(components: MDXComponents): MDXComponents {
  return {
    a: MdxLink,
    h2: (props) => (
      <h2
        className="mt-10 scroll-mt-24 text-xl font-semibold tracking-tight text-primary first:mt-0"
        {...props}
      />
    ),
    h3: (props) => (
      <h3
        className="mt-6 scroll-mt-24 text-lg font-semibold tracking-tight text-primary"
        {...props}
      />
    ),
    p: (props) => (
      <p className="mt-4 text-base leading-relaxed text-muted first:mt-0" {...props} />
    ),
    ul: (props) => (
      <ul className="mt-4 list-disc space-y-2 pl-5 text-base leading-relaxed text-muted" {...props} />
    ),
    ol: (props) => (
      <ol
        className="mt-4 list-decimal space-y-2 pl-5 text-base leading-relaxed text-muted"
        {...props}
      />
    ),
    li: (props) => <li className="leading-relaxed" {...props} />,
    strong: (props) => <strong className="font-semibold text-primary" {...props} />,
    em: (props) => <em className="italic text-muted" {...props} />,
    ...components,
  };
}
