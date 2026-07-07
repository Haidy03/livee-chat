import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { BrowserRouter, Navigate, Route, Routes } from "react-router-dom";
import { Toaster as Sonner } from "@/components/ui/sonner";
import { Toaster } from "@/components/ui/toaster";
import { TooltipProvider } from "@/components/ui/tooltip";
import { AuthProvider } from "@/hooks/useAuth";
import { AppShell } from "./layouts/AppShell";
import DigitalWorkspacePage from "./features/digital-workspace/pages/DigitalWorkspacePage";

const queryClient = new QueryClient();

const App = () => (
  <QueryClientProvider client={queryClient}>
    <TooltipProvider>
      <Toaster />
      <Sonner />
      <BrowserRouter>
        <AuthProvider>
          <Routes>
            <Route path="/" element={<Navigate to="/agent/digital/livechat" replace />} />
            <Route element={<AppShell />}>
              <Route path="/agent/digital" element={<Navigate to="/agent/digital/livechat" replace />} />
              <Route path="/agent/digital/:channel" element={<DigitalWorkspacePage />} />
            </Route>
            <Route path="*" element={<Navigate to="/agent/digital/livechat" replace />} />
          </Routes>
        </AuthProvider>
      </BrowserRouter>
    </TooltipProvider>
  </QueryClientProvider>
);

export default App;
