/** Audio device enumeration & sink selection helpers. */

export interface AudioDevice {
  deviceId: string;
  label: string;
  kind: "audioinput" | "audiooutput";
}

export async function listAudioDevices(): Promise<AudioDevice[]> {
  if (!navigator.mediaDevices?.enumerateDevices) return [];
  try {
    const devices = await navigator.mediaDevices.enumerateDevices();
    return devices
      .filter((d) => d.kind === "audioinput" || d.kind === "audiooutput")
      .map((d, i) => ({
        deviceId: d.deviceId,
        label: d.label || (d.kind === "audioinput" ? `Microphone ${i + 1}` : `Speaker ${i + 1}`),
        kind: d.kind as AudioDevice["kind"],
      }));
  } catch {
    return [];
  }
}

/** Chrome-only: route an <audio> element to a specific output device. */
export async function setAudioSink(el: HTMLAudioElement, sinkId: string): Promise<boolean> {
  // setSinkId is non-standard but widely supported in Chromium browsers
  const audio = el as HTMLAudioElement & { setSinkId?: (id: string) => Promise<void> };
  if (typeof audio.setSinkId !== "function") return false;
  try {
    await audio.setSinkId(sinkId);
    return true;
  } catch {
    return false;
  }
}

export function setSinkIdSupported(): boolean {
  if (typeof document === "undefined") return false;
  const a = document.createElement("audio") as HTMLAudioElement & { setSinkId?: unknown };
  return typeof a.setSinkId === "function";
}

/** Request mic permission so subsequent enumerateDevices() returns labels. */
export async function requestMicPermission(): Promise<MediaStream | null> {
  try {
    const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
    return stream;
  } catch {
    return null;
  }
}
