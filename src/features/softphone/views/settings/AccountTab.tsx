/**
 * Account tab: SIP credentials view + demo toggle + auto-register toggle.
 *
 * All fields are derived client-side from the current user, agent profile,
 * JWT tenant claim, and VITE_SIP_* env vars. No backend call is made.
 */

import { useEffect, useMemo, useState } from "react";
import { useTranslation } from "react-i18next";
import { Eye, EyeOff, Phone, Plug } from "lucide-react";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Button } from "@/components/ui/button";
import { Switch } from "@/components/ui/switch";
import { getDemoAdapter, enableDemoMode } from "../../sip/useSipState";
import type { SipConfig } from "../../sip/types";
import { SipStatusPanel } from "../../components/SipStatusPanel";
import { useAuth } from "@/hooks/useAuth";
import { useAgents, agentLabel } from "@/hooks/useAgents";
import { decodeToken } from "@/lib/apiClient";

const LEGACY_PASSWORD_KEY = "softphone:sip-password";
const DEMO_KEY = "softphone:demo-mode";
const AUTO_REG_KEY = "softphone:auto-register";

export function AccountTab() {
  const { t } = useTranslation();
  const { user } = useAuth();
  const agents = useAgents();

  const cfg: SipConfig = useMemo(() => {
    const myAgent = user ? agents.find((a) => a.user_id === user.id) : undefined;
    const ext = myAgent?.extension_number ?? "";
    const tenantId = decodeToken()?.tenant_id ?? "";
    const domain = (import.meta.env.VITE_SIP_DOMAIN as string | undefined) ?? "alkhwarizmi.alkhwarizmi.ai";
    const wsUrl = (import.meta.env.VITE_SIP_WS_URL as string | undefined) ?? "wss://pbx.alkhwarizmi.cloud:8089/ws";
    const password = (import.meta.env.VITE_SIP_PASSWORD as string | undefined) ?? "soft_123";
    const stunUrls = ((import.meta.env.VITE_SIP_STUN_URLS as string | undefined) ?? "stun:stun.l.google.com:19302")
      .split(",").map((s) => s.trim()).filter(Boolean);
    const authId = ext && tenantId ? `${ext}-${tenantId}` : "";
    const sipUri = authId ? `sip:${authId}@${domain}` : "";
    return {
      displayName: agentLabel(myAgent),
      sipUri,
      authId,
      password,
      wsUrl,
      stunUrls,
      turnUrl: "",
      turnUsername: "",
      turnPassword: "",
    };
  }, [user, agents]);

  const [showPwd, setShowPwd] = useState(false);
  const [demo, setDemo] = useState(() => localStorage.getItem(DEMO_KEY) === "1");
  const [autoReg, setAutoReg] = useState(() => localStorage.getItem(AUTO_REG_KEY) === "1");

  useEffect(() => {
    try { localStorage.removeItem(LEGACY_PASSWORD_KEY); } catch { /* ignore */ }
  }, []);

  useEffect(() => {
    enableDemoMode(demo);
    localStorage.setItem(DEMO_KEY, demo ? "1" : "0");
  }, [demo]);

  return (
    <div className="space-y-5">
      <SipStatusPanel config={cfg} />

      <div className="rounded-2xl bg-card border border-border/60 p-4">
        <div className="flex items-center justify-between gap-3">
          <div className="min-w-0">
            <div className="text-sm font-semibold">
              {t("softphone.settings.autoRegister", "Auto-connect on load")}
            </div>
            <div className="text-xs text-muted-foreground">
              {t(
                "softphone.settings.autoRegisterHelp",
                "Automatically register when the app opens, using saved credentials.",
              )}
            </div>
          </div>
          <Switch
            checked={autoReg}
            onCheckedChange={(v) => {
              setAutoReg(v);
              localStorage.setItem(AUTO_REG_KEY, v ? "1" : "0");
            }}
          />
        </div>
      </div>

      <div className="rounded-2xl bg-card border border-border/60 p-4 space-y-3">
        <div className="flex items-center justify-between">
          <div>
            <div className="text-sm font-semibold">
              {t("softphone.settings.demoMode", "Demo mode")}
            </div>
            <div className="text-xs text-muted-foreground">
              {t(
                "softphone.settings.demoModeHelp",
                "Simulate registration and calls without a real SIP server.",
              )}
            </div>
          </div>
          <Switch checked={demo} onCheckedChange={setDemo} />
        </div>
        {demo && (
          <div className="flex flex-wrap gap-2 pt-2">
            <Button size="sm" variant="outline" onClick={() => getDemoAdapter().register(cfg)}>
              <Plug className="h-4 w-4 me-2" />
              {t("softphone.settings.simulateRegister", "Simulate register")}
            </Button>
            <Button
              size="sm"
              variant="outline"
              onClick={() => getDemoAdapter().simulateInbound("+15555550123", "Demo Caller")}
            >
              <Phone className="h-4 w-4 me-2" />
              {t("softphone.settings.simulateInbound", "Simulate inbound call")}
            </Button>
          </div>
        )}
      </div>

      <div className="rounded-2xl bg-card border border-border/60 p-5 space-y-3">
        <div className="text-sm font-semibold">{t("softphone.settings.credentials", "SIP credentials")}</div>

        <Field label={t("softphone.settings.displayName", "Display name")}>
          <Input value={cfg.displayName} readOnly />
        </Field>
        <Field label="SIP URI">
          <Input value={cfg.sipUri} readOnly dir="ltr" />
        </Field>
        <Field label={t("softphone.settings.authId", "Auth ID")}>
          <Input value={cfg.authId} readOnly dir="ltr" />
        </Field>
        <Field label={t("softphone.settings.password", "Password")}>
          <div className="relative">
            <Input
              type={showPwd ? "text" : "password"}
              value={cfg.password}
              readOnly
              dir="ltr"
            />
            <button
              type="button"
              onClick={() => setShowPwd((v) => !v)}
              className="absolute end-2 top-1/2 -translate-y-1/2 text-muted-foreground"
              aria-label={t("softphone.settings.togglePassword")}
            >
              {showPwd ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
            </button>
          </div>
        </Field>
        <Field label="WebSocket URL">
          <Input value={cfg.wsUrl} readOnly dir="ltr" />
        </Field>
        <Field label="STUN servers">
          <Input value={cfg.stunUrls.join(",")} readOnly dir="ltr" />
        </Field>
      </div>
    </div>
  );
}

function Field({ label, hint, children }: { label: string; hint?: string; children: React.ReactNode }) {
  return (
    <div className="space-y-1.5">
      <div className="flex items-baseline justify-between">
        <Label className="text-xs">{label}</Label>
        {hint && <span className="text-[10px] text-muted-foreground">{hint}</span>}
      </div>
      {children}
    </div>
  );
}
