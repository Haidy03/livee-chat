import { useCallback, useEffect, useState } from "react";
import { useTranslation } from "react-i18next";
import { CheckCircle2, XCircle, AlertTriangle, Loader2, ShieldCheck, Mic, Bell, Lock, Radio, Speaker, Volume2 } from "lucide-react";
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle, DialogFooter } from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import {
  type CheckResult,
  type CheckStatus,
  checkAudioDevices,
  checkMicPermission,
  checkNotifications,
  checkSecureContext,
  checkWebRTCSupport,
  requestMic,
  requestNotifications,
  unlockAudio,
} from "../sip/permissions";

type Key =
  | "secure"
  | "webrtc"
  | "mic"
  | "audioIn"
  | "audioOut"
  | "audioCtx"
  | "notifications";

interface Row {
  key: Key;
  icon: typeof Mic;
  required: boolean;
}

const ROWS: Row[] = [
  { key: "secure", icon: Lock, required: true },
  { key: "webrtc", icon: Radio, required: true },
  { key: "mic", icon: Mic, required: true },
  { key: "audioIn", icon: Mic, required: false },
  { key: "audioOut", icon: Speaker, required: false },
  { key: "audioCtx", icon: Volume2, required: true },
  { key: "notifications", icon: Bell, required: false },
];

const VERIFIED_KEY = "softphone:permissions-verified";
const VERIFIED_TTL_MS = 24 * 60 * 60 * 1000;

type ResultsMap = Partial<Record<Key, CheckResult>>;

function statusIcon(status: CheckStatus | undefined) {
  switch (status) {
    case "checking":
      return <Loader2 className="h-4 w-4 animate-spin text-muted-foreground" />;
    case "granted":
      return <CheckCircle2 className="h-4 w-4 text-emerald-500" />;
    case "denied":
    case "unsupported":
      return <XCircle className="h-4 w-4 text-destructive" />;
    case "warning":
      return <AlertTriangle className="h-4 w-4 text-amber-500" />;
    default:
      return <div className="h-4 w-4 rounded-full border border-muted-foreground/40" />;
  }
}

interface Props {
  open: boolean;
  onClose: (verified: boolean) => void;
}

export function PermissionsCheckModal({ open, onClose }: Props) {
  const { t } = useTranslation();
  const [results, setResults] = useState<ResultsMap>({});
  const [running, setRunning] = useState(false);

  const setOne = (k: Key, r: CheckResult) =>
    setResults((prev) => ({ ...prev, [k]: r }));

  const runAll = useCallback(async () => {
    setRunning(true);
    const initial: ResultsMap = {};
    ROWS.forEach((r) => (initial[r.key] = { status: "checking" }));
    setResults(initial);

    setOne("secure", checkSecureContext());
    setOne("webrtc", checkWebRTCSupport());
    setOne("mic", await checkMicPermission());
    const devs = await checkAudioDevices();
    setOne("audioIn", devs.input);
    setOne("audioOut", devs.output);
    setOne("audioCtx", await unlockAudio());
    setOne("notifications", checkNotifications());

    setRunning(false);
  }, []);

  useEffect(() => {
    if (open) void runAll();
  }, [open, runAll]);

  const requestAll = async () => {
    setRunning(true);
    if (results.mic?.status !== "granted") setOne("mic", await requestMic());
    if (results.notifications?.status !== "granted")
      setOne("notifications", await requestNotifications());
    setOne("audioCtx", await unlockAudio());
    const devs = await checkAudioDevices();
    setOne("audioIn", devs.input);
    setOne("audioOut", devs.output);
    setRunning(false);
  };

  const requiredOk = ROWS.filter((r) => r.required).every(
    (r) => results[r.key]?.status === "granted",
  );

  const handleContinue = () => {
    if (!requiredOk) return;
    try {
      localStorage.setItem(VERIFIED_KEY, String(Date.now()));
    } catch {
      /* ignore */
    }
    onClose(true);
  };

  return (
    <Dialog open={open} onOpenChange={(o) => !o && requiredOk && onClose(true)}>
      <DialogContent
        className="max-w-xl"
        onPointerDownOutside={(e) => e.preventDefault()}
        onEscapeKeyDown={(e) => e.preventDefault()}
      >
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <ShieldCheck className="h-5 w-5 text-primary" />
            {t("softphone.permissions.title")}
          </DialogTitle>
          <DialogDescription>
            {t("softphone.permissions.description")}
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-2 max-h-[55vh] overflow-y-auto pe-1">
          {ROWS.map((row) => {
            const r = results[row.key];
            const Icon = row.icon;
            return (
              <div
                key={row.key}
                className="flex items-start gap-3 rounded-md border bg-card p-3"
              >
                <Icon className="h-4 w-4 mt-0.5 text-muted-foreground shrink-0" />
                <div className="flex-1 min-w-0">
                  <div className="flex items-center gap-2">
                    <span className="text-sm font-medium">{t(`softphone.permissions.rows.${row.key}.label`)}</span>
                    {row.required && (
                      <span className="text-[10px] uppercase tracking-wide text-muted-foreground border border-border rounded px-1">
                        {t("softphone.permissions.required")}
                      </span>
                    )}
                  </div>
                  <p className="text-xs text-muted-foreground">{t(`softphone.permissions.rows.${row.key}.desc`)}</p>
                  {r?.detail && (
                    <p className="text-[11px] text-muted-foreground/80 mt-0.5 truncate">
                      {r.detail}
                    </p>
                  )}
                </div>
                <div className="pt-0.5">{statusIcon(r?.status)}</div>
              </div>
            );
          })}
        </div>

        <DialogFooter className="gap-2 sm:gap-2">
          <Button variant="outline" onClick={() => void runAll()} disabled={running}>
            {t("softphone.permissions.recheck")}
          </Button>
          <Button variant="secondary" onClick={() => void requestAll()} disabled={running}>
            {t("softphone.permissions.requestAll")}
          </Button>
          <Button onClick={handleContinue} disabled={!requiredOk || running}>
            {t("softphone.permissions.continue")}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

export function shouldShowPermissionsModal(): boolean {
  try {
    const v = Number(localStorage.getItem(VERIFIED_KEY) ?? "0");
    if (!v) return true;
    return Date.now() - v > VERIFIED_TTL_MS;
  } catch {
    return true;
  }
}
