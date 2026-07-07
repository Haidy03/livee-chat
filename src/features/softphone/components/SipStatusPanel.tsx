/**
 * Live SIP registration status panel.
 *
 * Surfaces:
 *  - Current registration state (unregistered / connecting / registered / failed)
 *  - Last error with friendly hint + extracted SIP/HTTP code
 *  - Last state-change timestamp (live "x seconds ago")
 *  - WSS endpoint and SIP URI in use
 *  - "Test register" button with 10s timeout watchdog
 *  - Re-register / unregister buttons
 */

import { useEffect, useMemo, useRef, useState } from "react";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import {
  AlertCircle,
  CheckCircle2,
  Loader2,
  PlayCircle,
  PowerOff,
  RefreshCw,
  WifiOff,
} from "lucide-react";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";
import { getSipAdapter, useSipState } from "../sip/useSipState";
import type { SipConfig, SipDebugEvent, RegistrationStatus } from "../sip/types";

interface Props {
  config?: SipConfig;
  className?: string;
}

const META: Record<RegistrationStatus, { label: string; tone: string; ring: string; Icon: typeof CheckCircle2 }> = {
  unregistered: { label: "Unregistered", tone: "text-muted-foreground", ring: "bg-muted-foreground/40", Icon: WifiOff },
  connecting:   { label: "Connecting…", tone: "text-amber-600 dark:text-amber-400", ring: "bg-amber-500 animate-pulse", Icon: Loader2 },
  registered:   { label: "Registered", tone: "text-emerald-600 dark:text-emerald-400", ring: "bg-emerald-500", Icon: CheckCircle2 },
  failed:       { label: "Failed", tone: "text-destructive", ring: "bg-destructive", Icon: AlertCircle },
};

function extractErrorCode(msg?: string): string | null {
  if (!msg) return null;
  const m = msg.match(/\b([1-6]\d{2})\b/);
  return m ? m[1] : null;
}

function friendlyHint(reason?: string): string | null {
  if (!reason) return null;
  const r = reason.toLowerCase();
  if (r.includes("authentication")) return "Wrong username or password (401/403). Double-check Auth ID and Password.";
  if (r.includes("request timeout") || r.includes("408")) return "Server didn't respond in time. Verify the WSS URL is reachable and SIP is enabled on the PBX.";
  if (r.includes("connection error") || r.includes("websocket")) return "WebSocket failed to connect. Check the WSS URL, TLS certificate, and that port 8089 is open.";
  if (r.includes("not found") || r.includes("404")) return "User not found on the server. Check the SIP URI's username part.";
  if (r.includes("forbidden") || r.includes("403")) return "Server rejected the credentials (403 Forbidden).";
  if (r.includes("service unavailable") || r.includes("503")) return "Server is temporarily unavailable (503).";
  if (r.includes("rejected")) return "The server rejected the registration request.";
  return null;
}

const REGISTER_TIMEOUT_MS = 10_000;

export function SipStatusPanel({ config, className }: Props) {
  const { t } = useTranslation();
  const sip = useSipState();
  const meta = META[sip.registration];
  const [busy, setBusy] = useState(false);
  const [timeoutHit, setTimeoutHit] = useState<string | null>(null);
  const watchdogRef = useRef<number | null>(null);

  const [changedAt, setChangedAt] = useState<number>(() => Date.now());
  const lastStatusRef = useRef<RegistrationStatus>(sip.registration);
  useEffect(() => {
    if (lastStatusRef.current !== sip.registration) {
      lastStatusRef.current = sip.registration;
      setChangedAt(Date.now());
      if (sip.registration !== "connecting" && watchdogRef.current) {
        window.clearTimeout(watchdogRef.current);
        watchdogRef.current = null;
      }
      if (sip.registration === "registered" || sip.registration === "failed") {
        setTimeoutHit(null);
      }
    }
  }, [sip.registration]);

  const [, force] = useState(0);
  useEffect(() => {
    const id = window.setInterval(() => force((n) => n + 1), 1000);
    return () => window.clearInterval(id);
  }, []);

  const lastError: SipDebugEvent | undefined = useMemo(() => {
    for (let i = sip.debugLog.length - 1; i >= 0; i--) {
      const e = sip.debugLog[i];
      if (e.level === "error" || e.level === "warn") return e;
    }
    return undefined;
  }, [sip.debugLog]);

  // Last 12 SIP wire-trace lines + lifecycle messages for diagnostic panel.
  const sipTrace = useMemo(() => {
    return sip.debugLog.slice(-12);
  }, [sip.debugLog]);

  const errorText = sip.registrationReason || timeoutHit || (sip.registration === "failed" ? lastError?.message : undefined);
  const errorCode = extractErrorCode(errorText);
  const hint = friendlyHint(errorText);

  const startWatchdog = () => {
    if (watchdogRef.current) window.clearTimeout(watchdogRef.current);
    watchdogRef.current = window.setTimeout(() => {
      setTimeoutHit(`No response from server after ${REGISTER_TIMEOUT_MS / 1000}s. Check WSS reachability.`);
      toast.error("Registration timed out — no response from server.");
    }, REGISTER_TIMEOUT_MS);
  };

  const validate = (): string | null => {
    if (!config) return "No configuration available.";
    if (!config.wsUrl?.startsWith("wss://")) return "WebSocket URL must start with wss://";
    if (!config.sipUri) return "SIP URI is required.";
    if (!config.password) return "Password is required (re-type it in the form).";
    return null;
  };

  const testRegister = async () => {
    const err = validate();
    if (err) {
      toast.error(err);
      return;
    }
    setBusy(true);
    setTimeoutHit(null);
    try {
      const adapter = getSipAdapter();
      try { await adapter.unregister(); } catch { /* ignore */ }
      startWatchdog();
      await adapter.register(config!);
    } catch (e) {
      toast.error((e as Error).message);
    } finally {
      setBusy(false);
    }
  };

  const unregister = async () => {
    setBusy(true);
    try { await getSipAdapter().unregister(); } finally { setBusy(false); }
  };

  const Icon = meta.Icon;

  return (
    <div className={cn("rounded-2xl bg-card border border-border/60 p-4 space-y-3", className)}>
      <div className="flex items-start justify-between gap-3 flex-wrap">
        <div className="flex items-center gap-3 min-w-0">
          <span className="relative flex h-9 w-9 items-center justify-center rounded-full bg-muted shrink-0">
            <Icon className={cn("h-4 w-4", meta.tone, sip.registration === "connecting" && "animate-spin")} />
            <span className={cn("absolute -bottom-0.5 -end-0.5 h-2.5 w-2.5 rounded-full ring-2 ring-card", meta.ring)} />
          </span>
          <div className="min-w-0">
            <div className={cn("text-sm font-semibold", meta.tone)}>
              {t(`softphone.settings.regStatus.${sip.registration}`, meta.label)}
            </div>
            <div className="text-xs text-muted-foreground truncate">{timeAgo(changedAt)}</div>
          </div>
        </div>
        <div className="flex gap-2 shrink-0 flex-wrap">
          {config && sip.registration !== "registered" && (
            <Button size="sm" onClick={testRegister} disabled={busy}>
              <PlayCircle className={cn("h-3.5 w-3.5 me-1.5", busy && "animate-spin")} />
              {t("softphone.settings.testRegister", "Test register")}
            </Button>
          )}
          {config && sip.registration === "registered" && (
            <Button size="sm" variant="outline" onClick={testRegister} disabled={busy}>
              <RefreshCw className={cn("h-3.5 w-3.5 me-1.5", busy && "animate-spin")} />
              {t("softphone.settings.reRegister", "Re-register")}
            </Button>
          )}
          {sip.registration === "registered" && (
            <Button size="sm" variant="ghost" onClick={unregister} disabled={busy}>
              <PowerOff className="h-3.5 w-3.5 me-1.5" />
              {t("softphone.settings.unregister", "Unregister")}
            </Button>
          )}
        </div>
      </div>

      <div className="grid grid-cols-1 sm:grid-cols-2 gap-2 text-xs">
        <DetailRow label="SIP URI" value={config?.sipUri || "—"} />
        <DetailRow label="WebSocket" value={config?.wsUrl || "—"} />
      </div>

      {errorText && (sip.registration === "failed" || timeoutHit) && (
        <div className="rounded-lg border border-destructive/30 bg-destructive/5 p-3 space-y-1.5">
          <div className="flex items-center gap-2 text-xs font-semibold text-destructive">
            <AlertCircle className="h-3.5 w-3.5" />
            {t("softphone.settings.lastError", "Last error")}
            {errorCode && (
              <span className="ms-auto rounded-md bg-destructive/15 px-1.5 py-0.5 font-mono text-[10px]">
                {errorCode}
              </span>
            )}
          </div>
          <div className="text-xs text-destructive/90 break-words font-mono">{errorText}</div>
          {hint && (
            <div className="text-xs text-foreground/80">
              <span className="font-semibold">Hint: </span>{hint}
            </div>
          )}
          {lastError && (
            <div className="text-[10px] text-muted-foreground">
              {new Date(lastError.at).toLocaleTimeString()}
            </div>
          )}
        </div>
      )}
      <div className="rounded-lg border border-border/60 bg-muted/30 p-2.5 space-y-1">
        <div className="flex items-center justify-between gap-2">
          <div className="text-[10px] uppercase tracking-wide text-muted-foreground font-semibold">
            SIP wire trace {sipTrace.length > 0 ? `(last ${sipTrace.length})` : ""}
          </div>
          {sipTrace.length === 0 && (
            <span className="inline-flex items-center gap-1 text-[10px] text-muted-foreground">
              <span className="h-1.5 w-1.5 rounded-full bg-muted-foreground/50 animate-pulse" />
              waiting for REGISTER…
            </span>
          )}
        </div>
        {sipTrace.length === 0 ? (
          <div className="font-mono text-[10px] text-muted-foreground/80 leading-tight py-1">
            No SIP messages captured yet. Click <span className="font-semibold">Test register</span> to send a REGISTER and watch traffic here.
          </div>
        ) : (
          <div className="space-y-0.5 max-h-48 overflow-auto">
            {sipTrace.map((e, i) => {
              const isOut = e.message.startsWith("SIP →");
              const isIn = e.message.startsWith("SIP ←");
              const isSip = isOut || isIn;
              const arrow = isOut ? "→" : isIn ? "←" : e.level === "error" ? "✕" : e.level === "warn" ? "!" : "•";
              const arrowColor = isOut
                ? "text-blue-600 dark:text-blue-400"
                : isIn
                  ? "text-emerald-600 dark:text-emerald-400"
                  : e.level === "error"
                    ? "text-destructive"
                    : e.level === "warn"
                      ? "text-amber-600 dark:text-amber-400"
                      : "text-muted-foreground";
              const text = isSip ? e.message.slice(6) : e.message;
              return (
                <div key={i} className="font-mono text-[10px] leading-tight flex gap-1.5" dir="ltr">
                  <span className={cn("shrink-0 w-3 text-center", arrowColor)}>{arrow}</span>
                  <span className="break-all text-foreground/80">{text}</span>
                </div>
              );
            })}
          </div>
        )}
        {sipTrace.length > 0 &&
          sipTrace.some((e) => e.message.startsWith("SIP →")) &&
          !sipTrace.some((e) => e.message.startsWith("SIP ←")) && (
            <div className="text-[10px] text-amber-600 dark:text-amber-400 pt-1">
              Outbound only — server is not replying. Likely WSS sub-protocol (<code>sip</code>) missing on Asterisk, or REGISTER blocked by ACL/transport.
            </div>
          )}
      </div>
    </div>
  );
}

function DetailRow({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-lg bg-muted/40 px-2.5 py-1.5 min-w-0">
      <div className="text-[10px] uppercase tracking-wide text-muted-foreground">{label}</div>
      <div className="font-mono text-xs truncate" dir="ltr" title={value}>{value}</div>
    </div>
  );
}

function timeAgo(ts: number): string {
  const s = Math.max(0, Math.floor((Date.now() - ts) / 1000));
  if (s < 60) return `${s}s ago`;
  const m = Math.floor(s / 60);
  if (m < 60) return `${m}m ${s % 60}s ago`;
  const h = Math.floor(m / 60);
  return `${h}h ${m % 60}m ago`;
}
