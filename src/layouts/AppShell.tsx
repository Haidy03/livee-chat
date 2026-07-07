import { Outlet } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { CopilotProvider } from "@/components/copilot/CopilotProvider";

export function AppShell() {
  return (
    <CopilotProvider>
      <AppShellInner />
    </CopilotProvider>
  );
}

function AppShellInner() {
  const { i18n } = useTranslation();
  const dir = i18n.language === "ar" ? "rtl" : "ltr";

  return (
    <div data-theme="dashboard" dir={dir} className="h-screen bg-background overflow-hidden flex flex-col">
      <main className="flex-1 min-h-0 flex flex-col overflow-auto">
        <Outlet />
      </main>
    </div>
  );
}
