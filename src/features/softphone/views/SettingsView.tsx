import { useEffect, useState } from "react";
import { useTranslation } from "react-i18next";
import { Moon, Sun, Phone, ExternalLink } from "lucide-react";
import { Switch } from "@/components/ui/switch";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { useSoftphone, type AgentStatus } from "../store";
import { cn } from "@/lib/utils";
import { AccountTab } from "./settings/AccountTab";
import { AudioTab } from "./settings/AudioTab";
import { DebugTab } from "./settings/DebugTab";
import { getSoftphoneOpenMode, setSoftphoneOpenMode } from "../utils/openSoftphonePopup";

export function SettingsView() {
  const { t } = useTranslation();
  const { status, setStatus } = useSoftphone();
  const [dark, setDark] = useState(() => document.documentElement.classList.contains("dark"));
  const [popupMode, setPopupMode] = useState(() => getSoftphoneOpenMode() === "popup");

  useEffect(() => {
    document.documentElement.classList.toggle("dark", dark);
  }, [dark]);

  const statuses: { id: AgentStatus; color: string }[] = [
    { id: "available", color: "hsl(158 84% 39%)" },
    { id: "busy", color: "hsl(0 84% 60%)" },
    { id: "away", color: "hsl(38 92% 50%)" },
  ];

  return (
    <div className="flex-1 overflow-auto">
      <div className="max-w-2xl mx-auto p-6">
        <Tabs defaultValue="account" className="w-full">
          <TabsList className="grid grid-cols-4 w-full">
            <TabsTrigger value="account">{t("softphone.settings.tabs.account", "Account")}</TabsTrigger>
            <TabsTrigger value="audio">{t("softphone.settings.tabs.audio", "Audio")}</TabsTrigger>
            <TabsTrigger value="appearance">{t("softphone.settings.tabs.appearance", "Appearance")}</TabsTrigger>
            <TabsTrigger value="debug">{t("softphone.settings.tabs.debug", "Debug")}</TabsTrigger>
          </TabsList>

          <TabsContent value="account" className="mt-5">
            <AccountTab />
          </TabsContent>

          <TabsContent value="audio" className="mt-5">
            <AudioTab />
          </TabsContent>

          <TabsContent value="appearance" className="mt-5 space-y-5">
            <Section title={t("softphone.settings.status")}>
              <div className="grid grid-cols-3 gap-2">
                {statuses.map((s) => (
                  <button
                    key={s.id}
                    onClick={() => setStatus(s.id)}
                    className={cn(
                      "rounded-xl border p-3 flex items-center gap-2 text-sm transition-colors",
                      status === s.id ? "border-primary bg-primary/5" : "border-border/60 hover:bg-muted",
                    )}
                  >
                    <span className="h-2.5 w-2.5 rounded-full" style={{ background: s.color }} />
                    <span className="capitalize">{t(`softphone.status.${s.id}`)}</span>
                  </button>
                ))}
              </div>
            </Section>

            <Section title={t("softphone.settings.appearance")}>
              <div className="flex items-center justify-between">
                <div className="flex items-center gap-3">
                  {dark ? <Moon className="h-4 w-4" /> : <Sun className="h-4 w-4" />}
                  <div className="text-sm font-medium">{t("softphone.settings.darkMode")}</div>
                </div>
                <Switch checked={dark} onCheckedChange={setDark} />
              </div>
            </Section>

            <Section title={t("softphone.settings.window", "Window")}>
              <div className="flex items-center justify-between">
                <div className="flex items-center gap-3">
                  <ExternalLink className="h-4 w-4" />
                  <div>
                    <div className="text-sm font-medium">
                      {t("softphone.settings.openInPopup", "Open softphone in popup window")}
                    </div>
                    <div className="text-xs text-muted-foreground">
                      {t(
                        "softphone.settings.openInPopupHint",
                        "Launch a compact dialer window instead of navigating in-app.",
                      )}
                    </div>
                  </div>
                </div>
                <Switch
                  checked={popupMode}
                  onCheckedChange={(v) => {
                    setPopupMode(v);
                    setSoftphoneOpenMode(v ? "popup" : "page");
                  }}
                />
              </div>
            </Section>
          </TabsContent>

          <TabsContent value="debug" className="mt-5">
            <DebugTab />
          </TabsContent>
        </Tabs>
      </div>
    </div>
  );
}

function Section({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <div className="rounded-2xl bg-card border border-border/60 p-5">
      <div className="text-sm font-semibold mb-3">{title}</div>
      {children}
    </div>
  );
}

// Re-export for legacy callers
export { Phone };
