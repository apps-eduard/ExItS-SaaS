import type { ReactElement } from "react";
import { render } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { AppProviders } from "@/app/providers";
import { SessionProvider } from "@/session/SessionProvider";

export function jsonResponse(status: number, body: unknown, delayMs = 0): Promise<Response> {
  return new Promise((resolve) => {
    window.setTimeout(() => {
      resolve({
        ok: status >= 200 && status < 300,
        status,
        text: async () => (body === null ? "" : JSON.stringify(body)),
      } as Response);
    }, delayMs);
  });
}

export function renderWithSession(ui: ReactElement, { route = "/" }: { route?: string } = {}) {
  return render(
    <AppProviders>
      <MemoryRouter initialEntries={[route]}>
        <SessionProvider>{ui}</SessionProvider>
      </MemoryRouter>
    </AppProviders>,
  );
}
