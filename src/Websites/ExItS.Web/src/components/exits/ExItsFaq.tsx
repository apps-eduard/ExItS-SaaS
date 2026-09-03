"use client";

import {
  Accordion,
  AccordionContent,
  AccordionItem,
  AccordionTrigger,
} from "@/components/ui/accordion";

export type FaqItem = {
  question: string;
  answer: string;
};

export function ExItsFaq({ items }: { items: FaqItem[] }) {
  return (
    <Accordion type="single" collapsible className="w-full">
      {items.map((item, index) => {
        const id = `faq-${index}`;
        return (
          <AccordionItem key={item.question} value={id}>
            <AccordionTrigger
              className="min-h-12 px-1 text-base"
              aria-controls={`${id}-content`}
            >
              {item.question}
            </AccordionTrigger>
            <AccordionContent id={`${id}-content`} className="px-1 text-base leading-relaxed">
              {item.answer}
            </AccordionContent>
          </AccordionItem>
        );
      })}
    </Accordion>
  );
}
