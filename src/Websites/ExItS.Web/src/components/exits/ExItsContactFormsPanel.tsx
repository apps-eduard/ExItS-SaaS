"use client";

import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";

import { ExItsContactForm } from "@/components/exits/ExItsContactForm";

export function ExItsContactFormsPanel() {
  return (
    <Tabs defaultValue="general">
      <TabsList className="h-auto w-full justify-start overflow-x-auto rounded-xl p-1">
        <TabsTrigger value="general" className="min-h-11 shrink-0 rounded-lg px-4">
          General
        </TabsTrigger>
        <TabsTrigger value="sales" className="min-h-11 shrink-0 rounded-lg px-4">
          Sales
        </TabsTrigger>
        <TabsTrigger value="partnership" className="min-h-11 shrink-0 rounded-lg px-4">
          Partnership
        </TabsTrigger>
      </TabsList>
      <TabsContent value="general" className="mt-6">
        <div className="rounded-xl border border-borderDefault bg-surface p-6 sm:p-8">
          <h2 className="text-xl font-semibold text-primary">General inquiry</h2>
          <p className="mt-2 text-sm leading-relaxed text-muted">
            Questions about ExItS, the platform, or how to get started.
          </p>
          <div className="mt-6">
            <ExItsContactForm variant="general" />
          </div>
        </div>
      </TabsContent>
      <TabsContent value="sales" className="mt-6">
        <div className="rounded-xl border border-borderDefault bg-surface p-6 sm:p-8">
          <h2 className="text-xl font-semibold text-primary">Sales inquiry</h2>
          <p className="mt-2 text-sm leading-relaxed text-muted">
            Talk with us about Pinoy Business POS for your business.
          </p>
          <div className="mt-6">
            <ExItsContactForm variant="sales" />
          </div>
        </div>
      </TabsContent>
      <TabsContent value="partnership" className="mt-6">
        <div className="rounded-xl border border-borderDefault bg-surface p-6 sm:p-8">
          <h2 className="text-xl font-semibold text-primary">Partnership</h2>
          <p className="mt-2 text-sm leading-relaxed text-muted">
            Explore technology, distribution, reseller, or other partnership ideas.
          </p>
          <div className="mt-6">
            <ExItsContactForm variant="partnership" />
          </div>
        </div>
      </TabsContent>
    </Tabs>
  );
}
