import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { BrowserRouter, Route, Routes } from "react-router-dom";
import { AppErrorBoundary } from "@/app/AppErrorBoundary";
import { TooltipProvider } from "@/components/ui/tooltip";
import { ScaffoldPage } from "@/features/scaffold/ScaffoldPage";
import { PreferencesProvider } from "@/hooks/use-preferences";
import { RootLayout } from "@/layouts/RootLayout";

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      retry: false,
      refetchOnWindowFocus: false,
    },
  },
});

export function App() {
  return (
    <AppErrorBoundary>
      <PreferencesProvider>
        <TooltipProvider>
          <QueryClientProvider client={queryClient}>
            <BrowserRouter>
              <Routes>
                <Route element={<RootLayout />}>
                  <Route path="/" element={<ScaffoldPage />} />
                </Route>
              </Routes>
            </BrowserRouter>
          </QueryClientProvider>
        </TooltipProvider>
      </PreferencesProvider>
    </AppErrorBoundary>
  );
}
