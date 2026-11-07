import { BrowserRouter, Navigate, Route, Routes } from "react-router-dom";
import { Toaster } from "sonner";
import { AppShell } from "@/components/layout/app-shell";
import { TooltipProvider } from "@/components/ui/tooltip";
import { SignInPage } from "@/features/auth/SignInPage";
import { DesignSystemPage } from "@/features/design-system/DesignSystemPage";
import { HomePage } from "@/features/home/HomePage";
import { RequireAuth } from "@/session/RequireAuth";
import { SessionProvider } from "@/session/SessionProvider";
import { ThemeProvider } from "@/theme/theme-provider";

export default function App() {
  return (
    <ThemeProvider>
      <TooltipProvider>
        <BrowserRouter>
          <SessionProvider>
            <Routes>
              <Route path="/sign-in" element={<SignInPage />} />
              <Route
                path="/*"
                element={
                  <RequireAuth>
                    <AppShell>
                      <Routes>
                        <Route path="/" element={<HomePage />} />
                        <Route
                          path="/design-system"
                          element={<DesignSystemPage />}
                        />
                        <Route path="*" element={<Navigate to="/" replace />} />
                      </Routes>
                    </AppShell>
                  </RequireAuth>
                }
              />
            </Routes>
            <Toaster
              richColors
              closeButton
              position="top-center"
              toastOptions={{ className: "font-sans" }}
            />
          </SessionProvider>
        </BrowserRouter>
      </TooltipProvider>
    </ThemeProvider>
  );
}
