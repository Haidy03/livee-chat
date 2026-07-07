/**
 * Permission & capability probes required to handle SIP calls.
 * Pure helpers — no UI. Used by PermissionsCheckModal.
 */
import { api } from "@/lib/apiClient";
import { listAudioDevices } from "./audioDevices";

export type CheckStatus = "idle" | "checking" | "granted" | "denied" | "unsupported" | "warning";

export interface CheckResult {
  status: CheckStatus;
  detail?: string;
}

export function checkSecureContext(): CheckResult {
  if (typeof window === "undefined") return { status: "unsupported" };
  return window.isSecureContext
    ? { status: "granted" }
    : { status: "denied", detail: "Page must be served over HTTPS" };
}

export function checkWebRTCSupport(): CheckResult {
  const ok =
    typeof window !== "undefined" &&
    typeof window.RTCPeerConnection === "function" &&
    typeof window.MediaStream === "function" &&
    !!navigator.mediaDevices?.getUserMedia;
  return ok ? { status: "granted" } : { status: "unsupported", detail: "Browser lacks WebRTC" };
}

export async function checkMicPermission(): Promise<CheckResult> {
  if (!navigator.mediaDevices?.getUserMedia) return { status: "unsupported" };
  const perms = (navigator as Navigator & { permissions?: { query: (d: { name: PermissionName }) => Promise<PermissionStatus> } }).permissions;
  if (perms?.query) {
    try {
      const res = await perms.query({ name: "microphone" as PermissionName });
      if (res.state === "granted") return { status: "granted" };
      if (res.state === "denied") return { status: "denied", detail: "Microphone blocked" };
      return { status: "idle", detail: "Permission not yet granted" };
    } catch {
      /* fall through */
    }
  }
  return { status: "idle" };
}

export async function requestMic(): Promise<CheckResult> {
  if (!navigator.mediaDevices?.getUserMedia) return { status: "unsupported" };
  try {
    const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
    stream.getTracks().forEach((t) => t.stop());
    return { status: "granted" };
  } catch (err) {
    return { status: "denied", detail: (err as Error).message };
  }
}

export function checkNotifications(): CheckResult {
  if (typeof Notification === "undefined") return { status: "unsupported" };
  if (Notification.permission === "granted") return { status: "granted" };
  if (Notification.permission === "denied") return { status: "denied" };
  return { status: "idle" };
}

export async function requestNotifications(): Promise<CheckResult> {
  if (typeof Notification === "undefined") return { status: "unsupported" };
  try {
    const res = await Notification.requestPermission();
    if (res === "granted") return { status: "granted" };
    if (res === "denied") return { status: "denied" };
    return { status: "warning", detail: "Dismissed" };
  } catch (err) {
    return { status: "denied", detail: (err as Error).message };
  }
}

export async function checkAudioDevices(): Promise<{ input: CheckResult; output: CheckResult }> {
  const devices = await listAudioDevices();
  const inputs = devices.filter((d) => d.kind === "audioinput");
  const outputs = devices.filter((d) => d.kind === "audiooutput");
  return {
    input: inputs.length
      ? { status: "granted", detail: `${inputs.length} found` }
      : { status: "warning", detail: "No microphone detected" },
    output: outputs.length
      ? { status: "granted", detail: `${outputs.length} found` }
      : { status: "warning", detail: "No speaker detected" },
  };
}

export async function unlockAudio(): Promise<CheckResult> {
  try {
    const AC =
      window.AudioContext ||
      (window as unknown as { webkitAudioContext?: typeof AudioContext }).webkitAudioContext;
    if (!AC) return { status: "unsupported" };
    const ctx = new AC();
    if (ctx.state === "suspended") await ctx.resume();
    const ok = ctx.state === "running";
    await ctx.close().catch(() => {});
    return ok ? { status: "granted" } : { status: "warning", detail: "Audio context suspended" };
  } catch (err) {
    return { status: "denied", detail: (err as Error).message };
  }
}

export async function checkSipAccount(): Promise<CheckResult> {
  try {
    const data = await api.get<{ sipUri?: string; wsUrl?: string }>("/sip/account");
    if (!data?.sipUri || !data?.wsUrl) {
      return { status: "warning", detail: "SIP account is incomplete" };
    }
    return { status: "granted", detail: data.sipUri };
  } catch (err) {
    return { status: "denied", detail: (err as Error).message };
  }
}
