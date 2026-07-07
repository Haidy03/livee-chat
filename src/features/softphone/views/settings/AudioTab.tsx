/**
 * Audio settings: mic / speaker / ringtone device pickers, processing toggles,
 * mic level meter, ringtone preview.
 */

import { useEffect, useRef, useState } from "react";
import { useTranslation } from "react-i18next";
import { Mic, Speaker, Bell } from "lucide-react";
import { Switch } from "@/components/ui/switch";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import { listAudioDevices, requestMicPermission, setSinkIdSupported, type AudioDevice } from "../../sip/audioDevices";
import { previewRingtone, setRingtoneSink } from "../../sip/ringtone";
import { getSipAdapter } from "../../sip/useSipState";

const PREFS_KEY = "softphone:audio-prefs";

interface AudioPrefs {
  micId: string;
  speakerId: string;
  ringtoneSinkId: string;
  echoCancellation: boolean;
  noiseSuppression: boolean;
  autoGainControl: boolean;
  ringtone: "classic" | "modern" | "marimba";
}

const defaults: AudioPrefs = {
  micId: "",
  speakerId: "",
  ringtoneSinkId: "",
  echoCancellation: true,
  noiseSuppression: true,
  autoGainControl: true,
  ringtone: "classic",
};

function loadPrefs(): AudioPrefs {
  try {
    return { ...defaults, ...JSON.parse(localStorage.getItem(PREFS_KEY) ?? "{}") };
  } catch {
    return defaults;
  }
}

export function AudioTab() {
  const { t } = useTranslation();
  const [devices, setDevices] = useState<AudioDevice[]>([]);
  const [prefs, setPrefs] = useState<AudioPrefs>(() => loadPrefs());
  const [meter, setMeter] = useState(0);
  const sinkSupported = setSinkIdSupported();
  const streamRef = useRef<MediaStream | null>(null);
  const rafRef = useRef<number | null>(null);

  const refresh = async () => {
    await requestMicPermission();
    const list = await listAudioDevices();
    setDevices(list);
  };

  useEffect(() => {
    refresh();
    return () => {
      if (rafRef.current) cancelAnimationFrame(rafRef.current);
      streamRef.current?.getTracks().forEach((t) => t.stop());
    };
  }, []);

  // Persist + apply
  useEffect(() => {
    localStorage.setItem(PREFS_KEY, JSON.stringify(prefs));
    if (prefs.speakerId) getSipAdapter().setSinkId?.(prefs.speakerId).catch(() => {});
    setRingtoneSink(prefs.ringtoneSinkId).catch(() => {});
  }, [prefs]);

  // Mic level meter
  useEffect(() => {
    let cancelled = false;
    let audioCtx: AudioContext | null = null;
    (async () => {
      try {
        const stream = await navigator.mediaDevices.getUserMedia({
          audio: {
            deviceId: prefs.micId ? { exact: prefs.micId } : undefined,
            echoCancellation: prefs.echoCancellation,
            noiseSuppression: prefs.noiseSuppression,
            autoGainControl: prefs.autoGainControl,
          },
        });
        if (cancelled) {
          stream.getTracks().forEach((t) => t.stop());
          return;
        }
        streamRef.current?.getTracks().forEach((t) => t.stop());
        streamRef.current = stream;
        const Ctx = window.AudioContext || (window as unknown as { webkitAudioContext: typeof AudioContext }).webkitAudioContext;
        audioCtx = new Ctx();
        const source = audioCtx.createMediaStreamSource(stream);
        const analyser = audioCtx.createAnalyser();
        analyser.fftSize = 256;
        source.connect(analyser);
        const data = new Uint8Array(analyser.frequencyBinCount);
        const tick = () => {
          analyser.getByteFrequencyData(data);
          let sum = 0;
          for (let i = 0; i < data.length; i++) sum += data[i];
          const avg = sum / data.length / 255;
          setMeter(avg);
          rafRef.current = requestAnimationFrame(tick);
        };
        rafRef.current = requestAnimationFrame(tick);
      } catch {
        /* mic permission denied */
      }
    })();
    return () => {
      cancelled = true;
      if (rafRef.current) cancelAnimationFrame(rafRef.current);
      audioCtx?.close().catch(() => {});
    };
  }, [prefs.micId, prefs.echoCancellation, prefs.noiseSuppression, prefs.autoGainControl]);

  const inputs = devices.filter((d) => d.kind === "audioinput");
  const outputs = devices.filter((d) => d.kind === "audiooutput");

  return (
    <div className="space-y-5">
      <Card title={t("softphone.settings.microphone", "Microphone")} icon={Mic}>
        <Select
          value={prefs.micId}
          onChange={(v) => setPrefs({ ...prefs, micId: v })}
          options={inputs.map((d) => ({ value: d.deviceId, label: d.label }))}
          placeholder={t("softphone.settings.systemDefault", "System default")}
        />
        <div className="mt-3">
          <div className="text-xs text-muted-foreground mb-1">{t("softphone.settings.micLevel", "Input level")}</div>
          <div className="h-2 rounded-full bg-muted overflow-hidden">
            <div
              className="h-full bg-emerald-500 transition-all duration-75"
              style={{ width: `${Math.min(100, meter * 200)}%` }}
            />
          </div>
        </div>
        <div className="grid grid-cols-1 sm:grid-cols-3 gap-3 mt-4">
          <Toggle
            label={t("softphone.settings.echoCancellation", "Echo cancellation")}
            checked={prefs.echoCancellation}
            onChange={(v) => setPrefs({ ...prefs, echoCancellation: v })}
          />
          <Toggle
            label={t("softphone.settings.noiseSuppression", "Noise suppression")}
            checked={prefs.noiseSuppression}
            onChange={(v) => setPrefs({ ...prefs, noiseSuppression: v })}
          />
          <Toggle
            label={t("softphone.settings.autoGain", "Auto gain")}
            checked={prefs.autoGainControl}
            onChange={(v) => setPrefs({ ...prefs, autoGainControl: v })}
          />
        </div>
      </Card>

      <Card title={t("softphone.settings.speaker", "Speaker")} icon={Speaker}>
        <Select
          value={prefs.speakerId}
          onChange={(v) => setPrefs({ ...prefs, speakerId: v })}
          options={outputs.map((d) => ({ value: d.deviceId, label: d.label }))}
          placeholder={t("softphone.settings.systemDefault", "System default")}
          disabled={!sinkSupported}
        />
        {!sinkSupported && (
          <p className="text-xs text-muted-foreground mt-2">
            {t(
              "softphone.settings.sinkUnsupported",
              "Output device selection is only supported in Chromium-based browsers.",
            )}
          </p>
        )}
      </Card>

      <Card title={t("softphone.settings.ringtone", "Ringtone")} icon={Bell}>
        <Select
          value={prefs.ringtoneSinkId}
          onChange={(v) => setPrefs({ ...prefs, ringtoneSinkId: v })}
          options={outputs.map((d) => ({ value: d.deviceId, label: d.label }))}
          placeholder={t("softphone.settings.systemDefault", "System default")}
          disabled={!sinkSupported}
        />
        <div className="grid grid-cols-3 gap-2 mt-3">
          {(["classic", "modern", "marimba"] as const).map((r) => (
            <button
              key={r}
              onClick={() => {
                setPrefs({ ...prefs, ringtone: r });
                previewRingtone(r);
              }}
              className={`rounded-xl border p-3 text-sm capitalize transition-colors ${
                prefs.ringtone === r ? "border-primary bg-primary/5" : "border-border/60 hover:bg-muted"
              }`}
            >
              {r}
            </button>
          ))}
        </div>
      </Card>

      <div className="flex justify-end">
        <Button variant="outline" onClick={refresh}>
          {t("softphone.settings.refreshDevices", "Refresh devices")}
        </Button>
      </div>
    </div>
  );
}

function Card({ title, icon: Icon, children }: { title: string; icon: typeof Mic; children: React.ReactNode }) {
  return (
    <div className="rounded-2xl bg-card border border-border/60 p-5">
      <div className="flex items-center gap-2 mb-3 text-sm font-semibold">
        <Icon className="h-4 w-4" />
        {title}
      </div>
      {children}
    </div>
  );
}

function Select({
  value,
  onChange,
  options,
  placeholder,
  disabled,
}: {
  value: string;
  onChange: (v: string) => void;
  options: { value: string; label: string }[];
  placeholder: string;
  disabled?: boolean;
}) {
  return (
    <select
      value={value}
      onChange={(e) => onChange(e.target.value)}
      disabled={disabled}
      className="w-full h-10 rounded-md border border-input bg-background px-3 text-sm disabled:opacity-50"
    >
      <option value="">{placeholder}</option>
      {options.map((o) => (
        <option key={o.value} value={o.value}>
          {o.label}
        </option>
      ))}
    </select>
  );
}

function Toggle({ label, checked, onChange }: { label: string; checked: boolean; onChange: (v: boolean) => void }) {
  return (
    <label className="flex items-center justify-between gap-2 rounded-xl border border-border/60 p-3 cursor-pointer">
      <span className="text-xs">{label}</span>
      <Switch checked={checked} onCheckedChange={onChange} />
    </label>
  );
}
