import { setAudioSink } from "./audioDevices";

/**
 * Synthesizes a classic ringback/ringtone using WebAudio so we don't need
 * to ship audio assets. Supports a separate output sink via an offscreen
 * <audio> element fed by a MediaStreamDestination (Chromium only).
 */

type Pattern = "classic" | "modern" | "marimba";

let ctx: AudioContext | null = null;
let osc: OscillatorNode | null = null;
let gain: GainNode | null = null;
let interval: number | null = null;
let dest: MediaStreamAudioDestinationNode | null = null;
let audioEl: HTMLAudioElement | null = null;
let currentSinkId = "";

function ensureCtx(): AudioContext {
  if (!ctx) ctx = new (window.AudioContext || (window as unknown as { webkitAudioContext: typeof AudioContext }).webkitAudioContext)();
  return ctx;
}

export async function setRingtoneSink(sinkId: string) {
  currentSinkId = sinkId;
  if (audioEl && sinkId) await setAudioSink(audioEl, sinkId);
}

export function startRingtone(pattern: Pattern = "classic") {
  if (osc) return;
  const c = ensureCtx();

  osc = c.createOscillator();
  gain = c.createGain();
  gain.gain.value = 0;
  osc.frequency.value = pattern === "modern" ? 523.25 : pattern === "marimba" ? 659.25 : 440;
  osc.type = "sine";

  // Route through a stream destination so we can pipe to a chosen output.
  dest = c.createMediaStreamDestination();
  osc.connect(gain).connect(dest);
  // Also connect to default output as a fallback so it's audible without sinkId.
  gain.connect(c.destination);

  osc.start();

  if (!audioEl) {
    audioEl = new Audio();
    audioEl.autoplay = true;
  }
  audioEl.srcObject = dest.stream;
  if (currentSinkId) setAudioSink(audioEl, currentSinkId).catch(() => {});

  // Pulse pattern: 1s on, 2s off (classic)
  let on = false;
  const pulse = () => {
    if (!gain) return;
    on = !on;
    const target = on ? 0.18 : 0;
    gain.gain.cancelScheduledValues(c.currentTime);
    gain.gain.linearRampToValueAtTime(target, c.currentTime + 0.05);
  };
  pulse();
  interval = window.setInterval(pulse, 1000);
}

export function stopRingtone() {
  if (interval) {
    clearInterval(interval);
    interval = null;
  }
  if (gain && ctx) {
    gain.gain.cancelScheduledValues(ctx.currentTime);
    gain.gain.linearRampToValueAtTime(0, ctx.currentTime + 0.05);
  }
  if (osc) {
    try {
      osc.stop();
    } catch {
      /* already stopped */
    }
    osc.disconnect();
    osc = null;
  }
  if (gain) {
    gain.disconnect();
    gain = null;
  }
  if (dest) {
    dest.disconnect();
    dest = null;
  }
  if (audioEl) {
    audioEl.srcObject = null;
  }
}

export function previewRingtone(pattern: Pattern = "classic", durationMs = 1500) {
  startRingtone(pattern);
  window.setTimeout(stopRingtone, durationMs);
}

/* =================== Ringback (outbound dialing) =================== */

let rbCtx: AudioContext | null = null;
let rbOsc1: OscillatorNode | null = null;
let rbOsc2: OscillatorNode | null = null;
let rbGain: GainNode | null = null;
let rbInterval: number | null = null;
let rbDest: MediaStreamAudioDestinationNode | null = null;
let rbAudioEl: HTMLAudioElement | null = null;

function ensureRbCtx(): AudioContext {
  if (!rbCtx) rbCtx = new (window.AudioContext || (window as unknown as { webkitAudioContext: typeof AudioContext }).webkitAudioContext)();
  return rbCtx;
}

export function startRingback() {
  if (rbOsc1) return;
  const c = ensureRbCtx();

  rbGain = c.createGain();
  rbGain.gain.value = 0;

  // Classic US ringback: 440 Hz + 480 Hz
  rbOsc1 = c.createOscillator();
  rbOsc1.type = "sine";
  rbOsc1.frequency.value = 440;
  rbOsc2 = c.createOscillator();
  rbOsc2.type = "sine";
  rbOsc2.frequency.value = 480;

  rbDest = c.createMediaStreamDestination();
  rbOsc1.connect(rbGain);
  rbOsc2.connect(rbGain);
  rbGain.connect(rbDest);
  rbGain.connect(c.destination);

  rbOsc1.start();
  rbOsc2.start();

  if (!rbAudioEl) {
    rbAudioEl = new Audio();
    rbAudioEl.autoplay = true;
  }
  rbAudioEl.srcObject = rbDest.stream;
  if (currentSinkId) setAudioSink(rbAudioEl, currentSinkId).catch(() => {});

  // 2s on, 4s off
  let phase = -1;
  const tick = () => {
    if (!rbGain) return;
    phase = (phase + 1) % 2;
    const on = phase === 0;
    rbGain.gain.cancelScheduledValues(c.currentTime);
    rbGain.gain.linearRampToValueAtTime(on ? 0.16 : 0, c.currentTime + 0.05);
    if (rbInterval) clearInterval(rbInterval);
    rbInterval = window.setTimeout(tick, on ? 2000 : 4000) as unknown as number;
  };
  tick();
}

export function stopRingback() {
  if (rbInterval) {
    clearTimeout(rbInterval);
    rbInterval = null;
  }
  if (rbGain && rbCtx) {
    rbGain.gain.cancelScheduledValues(rbCtx.currentTime);
    rbGain.gain.linearRampToValueAtTime(0, rbCtx.currentTime + 0.05);
  }
  for (const o of [rbOsc1, rbOsc2]) {
    if (o) {
      try { o.stop(); } catch { /* already stopped */ }
      o.disconnect();
    }
  }
  rbOsc1 = null;
  rbOsc2 = null;
  if (rbGain) { rbGain.disconnect(); rbGain = null; }
  if (rbDest) { rbDest.disconnect(); rbDest = null; }
  if (rbAudioEl) rbAudioEl.srcObject = null;
}

/* =================== Soft inbound ring (StageCard) =================== */

let srCtx: AudioContext | null = null;
let srOsc1: OscillatorNode | null = null;
let srOsc2: OscillatorNode | null = null;
let srGain: GainNode | null = null;
let srTimer: number | null = null;

function ensureSrCtx(): AudioContext {
  if (!srCtx) srCtx = new (window.AudioContext || (window as unknown as { webkitAudioContext: typeof AudioContext }).webkitAudioContext)();
  return srCtx;
}

/** Soft two-tone inbound ring for the StageCard. Honors the soundOn toggle.
 *  Stays silent until the user has interacted with the page (browser autoplay). */
export function startSoftRing(soundOn: boolean) {
  if (!soundOn || srOsc1) return;
  const c = ensureSrCtx();
  if (c.state === "suspended") c.resume().catch(() => {});

  srGain = c.createGain();
  srGain.gain.value = 0;
  srOsc1 = c.createOscillator(); srOsc1.type = "sine"; srOsc1.frequency.value = 440;
  srOsc2 = c.createOscillator(); srOsc2.type = "sine"; srOsc2.frequency.value = 480;
  srOsc1.connect(srGain); srOsc2.connect(srGain);
  srGain.connect(c.destination);
  srOsc1.start(); srOsc2.start();

  let on = false;
  const tick = () => {
    if (!srGain) return;
    on = !on;
    srGain.gain.cancelScheduledValues(c.currentTime);
    srGain.gain.linearRampToValueAtTime(on ? 0.06 : 0, c.currentTime + 0.05);
    srTimer = window.setTimeout(tick, on ? 2000 : 4000) as unknown as number;
  };
  tick();
}

export function stopSoftRing() {
  if (srTimer) { clearTimeout(srTimer); srTimer = null; }
  if (srGain && srCtx) {
    srGain.gain.cancelScheduledValues(srCtx.currentTime);
    srGain.gain.linearRampToValueAtTime(0, srCtx.currentTime + 0.05);
  }
  for (const o of [srOsc1, srOsc2]) {
    if (o) { try { o.stop(); } catch { /* ignore */ } o.disconnect(); }
  }
  srOsc1 = null; srOsc2 = null;
  if (srGain) { srGain.disconnect(); srGain = null; }
}

/** Short connect blip when a call is answered. */
export function playConnectBlip() {
  try {
    const c = ensureSrCtx();
    if (c.state === "suspended") c.resume().catch(() => {});
    const o = c.createOscillator();
    const g = c.createGain();
    o.type = "sine"; o.frequency.value = 880;
    g.gain.value = 0;
    o.connect(g).connect(c.destination);
    o.start();
    g.gain.linearRampToValueAtTime(0.05, c.currentTime + 0.01);
    g.gain.linearRampToValueAtTime(0, c.currentTime + 0.09);
    window.setTimeout(() => { try { o.stop(); } catch { /* ignore */ } o.disconnect(); g.disconnect(); }, 120);
  } catch { /* ignore */ }
}

