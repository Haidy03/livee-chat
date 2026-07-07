/**
 * Synthesizes a short DTMF dual-tone for dialpad keypress feedback.
 * No audio assets needed — uses WebAudio oscillators.
 */

const FREQS: Record<string, [number, number]> = {
  "1": [697, 1209],
  "2": [697, 1336],
  "3": [697, 1477],
  "4": [770, 1209],
  "5": [770, 1336],
  "6": [770, 1477],
  "7": [852, 1209],
  "8": [852, 1336],
  "9": [852, 1477],
  "*": [941, 1209],
  "0": [941, 1336],
  "#": [941, 1477],
  "+": [941, 1336],
};

let ctx: AudioContext | null = null;
let dest: MediaStreamAudioDestinationNode | null = null;
let audioEl: HTMLAudioElement | null = null;

function ensureCtx(): AudioContext | null {
  try {
    if (!ctx) {
      const AC =
        window.AudioContext ||
        (window as unknown as { webkitAudioContext: typeof AudioContext }).webkitAudioContext;
      if (!AC) return null;
      ctx = new AC();
    }
    if (ctx.state === "suspended") void ctx.resume().catch(() => {});
    return ctx;
  } catch {
    return null;
  }
}

function ensureAudioRoute(c: AudioContext): MediaStreamAudioDestinationNode | null {
  try {
    if (!dest) dest = c.createMediaStreamDestination();
    if (!audioEl) {
      audioEl = new Audio();
      audioEl.autoplay = true;
    }
    if (audioEl.srcObject !== dest.stream) audioEl.srcObject = dest.stream;
    void audioEl.play().catch(() => {});
    return dest;
  } catch {
    return null;
  }
}

export function playDtmfTone(key: string, durationMs = 120) {
  try {
    const pair = FREQS[key];
    if (!pair) return;
    const c = ensureCtx();
    if (!c) return;

    const now = c.currentTime + 0.005;
    const end = now + durationMs / 1000;

    const gain = c.createGain();
    gain.gain.setValueAtTime(0, now);
    gain.gain.linearRampToValueAtTime(0.15, now + 0.01);
    gain.gain.setValueAtTime(0.15, end - 0.02);
    gain.gain.linearRampToValueAtTime(0, end);
    gain.connect(c.destination);
    const streamDest = ensureAudioRoute(c);
    if (streamDest) {
      try { gain.connect(streamDest); } catch { /* noop */ }
    }

    for (const f of pair) {
      const osc = c.createOscillator();
      osc.type = "sine";
      osc.frequency.value = f;
      osc.connect(gain);
      osc.start(now);
      osc.stop(end + 0.02);
      osc.onended = () => {
        try { osc.disconnect(); } catch { /* noop */ }
      };
    }

    window.setTimeout(() => {
      try { gain.disconnect(); } catch { /* noop */ }
    }, durationMs + 80);
  } catch {
    /* never throw from a UI event handler */
  }
}
