/**
 * Persists softphone calls via VoiceFlow `POST /calls/sip-upsert`.
 */

import { api } from "@/lib/apiClient";
import type { CallRecordEvent } from "./types";
import { resolveSipParty } from "./identityResolver";

function mapDirection(d: "in" | "out" | undefined): string | undefined {
  if (!d) return undefined;
  return d;
}

// Remembers from/to URIs per sipCallId across patches.
const uriMemory = new Map<string, { from?: string; to?: string }>();
const TERMINAL_STATUSES = new Set(["completed", "failed", "missed", "rejected", "busy"]);

// Per-sipCallId verdict for inbound-internal dedupe. When an inbound call's
// caller resolves to a tenant agent or AI pipeline, we know the outbound leg
// of the same conversation is already reporting to /calls/sip-upsert from
// the other agent's softphone. The PBX (B2BUA) gives each leg a different
// Call-ID, so without this skip the backend would store two rows per
// internal call. We let the outbound leg be the single source of truth.
type UpsertVerdict = "skip" | "send";
const callVerdict = new Map<string, UpsertVerdict>();

function clearCallMemory(sipCallId: string): void {
  uriMemory.delete(sipCallId);
  callVerdict.delete(sipCallId);
}

export async function persistCallEvent(evt: CallRecordEvent): Promise<void> {
  const { callId, patch } = evt;

  const body: Record<string, unknown> = {
    sipCallId: callId,
  };

  if (patch.direction) body.direction = mapDirection(patch.direction);
  if (patch.status) body.status = patch.status;
  if (patch.from_uri !== undefined) {
    body.fromUri = patch.from_uri;
    if (patch.caller === undefined) body.caller = patch.from_uri;
  }
  if (patch.from_display !== undefined) body.fromDisplay = patch.from_display ?? "";
  if (patch.to_uri !== undefined) {
    body.toUri = patch.to_uri;
    if (patch.called === undefined) body.called = patch.to_uri;
  }
  if (patch.to_display !== undefined) body.toDisplay = patch.to_display ?? "";
  if (patch.caller !== undefined) body.caller = patch.caller;
  if (patch.called !== undefined) body.called = patch.called;
  if (patch.started_at) body.startedAt = patch.started_at;
  if (patch.answered_at) body.answeredAt = patch.answered_at;
  if (patch.ended_at) body.endedAt = patch.ended_at;
  if (patch.hangup_cause) body.hangupCause = patch.hangup_cause;
  if (patch.recording_url !== undefined) body.recordingUrl = patch.recording_url;
  if (patch.has_recording !== undefined) body.hasRecording = patch.has_recording;
  if (patch.ring_seconds !== undefined) body.ringSeconds = patch.ring_seconds;
  if (patch.hold_seconds !== undefined) body.holdSeconds = patch.hold_seconds;
  if (patch.active_seconds !== undefined) body.activeSeconds = patch.active_seconds;
  if (patch.total_hold_seconds !== undefined) body.totalHoldSeconds = patch.total_hold_seconds;
  if (patch.total_seconds !== undefined) body.totalSeconds = patch.total_seconds;

  // Defense in depth: drop POSTs whose body carries only durations / no
  // meaningful lifecycle field. Prevents future regressions where someone
  // re-introduces a mid-call durations-only emit and floods the backend.
  const MEANINGFUL_KEYS = [
    "status", "direction", "fromUri", "toUri", "fromDisplay", "toDisplay",
    "caller", "called", "startedAt", "answeredAt", "endedAt",
    "hangupCause", "recordingUrl", "hasRecording",
  ];
  if (!MEANINGFUL_KEYS.some((k) => k in body)) {
    console.info("[sip-upsert] skipped (no meaningful fields)", { sipCallId: body.sipCallId });
    return;
  }

  // Remember per-call URIs so identity enrichment runs on every patch, not
  // just the first event that carried from_uri / to_uri.
  const sipCallIdKey = String(body.sipCallId);
  const remembered = uriMemory.get(sipCallIdKey) ?? {};
  if (patch.from_uri) remembered.from = patch.from_uri;
  if (patch.to_uri) remembered.to = patch.to_uri;
  if (patch.caller && !remembered.from) remembered.from = patch.caller;
  if (patch.called && !remembered.to) remembered.to = patch.called;
  uriMemory.set(sipCallIdKey, remembered);

  // Enrich with resolved caller/called identities (tenant user or AI pipeline).
  let callerParty: Awaited<ReturnType<typeof resolveSipParty>> = null;
  let calledParty: Awaited<ReturnType<typeof resolveSipParty>> = null;
  try {
    [callerParty, calledParty] = await Promise.all([
      resolveSipParty(remembered.from),
      resolveSipParty(remembered.to),
    ]);
    if (callerParty) {
      if (callerParty.id !== undefined) body.callerId = callerParty.id;
      if (callerParty.name !== undefined) body.callerName = callerParty.name;
      if (callerParty.extension !== undefined) body.callerExtension = callerParty.extension;
      body.callerIsAiAgent = callerParty.isAiAgent;
    }
    if (calledParty) {
      if (calledParty.id !== undefined) body.calledId = calledParty.id;
      if (calledParty.name !== undefined) body.calledName = calledParty.name;
      if (calledParty.extension !== undefined) body.calledExtension = calledParty.extension;
      body.calledIsAiAgent = calledParty.isAiAgent;
    }
  } catch (e) {
    console.debug("[sip-upsert] identity resolve failed", e);
  }

  // Inbound-internal dedupe. Once we've decided "skip" for this Call-ID,
  // every subsequent patch (answered, ended, durations) is also dropped so
  // we don't leak a partial row. Decision is made on the first inbound
  // event whose caller URI resolves to a known tenant party.
  let verdict = callVerdict.get(sipCallIdKey);
  if (!verdict) {
    const isInbound = body.direction === "in" || patch.direction === "in";
    if (isInbound && callerParty) {
      // AI pipeline (always internal) OR tenant agent (has id) -> skip.
      if (callerParty.isAiAgent || callerParty.id) {
        verdict = "skip";
        callVerdict.set(sipCallIdKey, "skip");
      }
    }
  }

  if (verdict === "skip") {
    console.info("[sip-upsert] skipped (internal inbound leg)", {
      sipCallId: sipCallIdKey,
      caller: remembered.from,
    });
    if (typeof body.status === "string" && TERMINAL_STATUSES.has(body.status)) {
      clearCallMemory(sipCallIdKey);
    }
    return;
  }

  console.info("[sip-upsert] →", {
    eventCallId: callId,
    sipCallId: body.sipCallId,
    status: body.status,
    direction: body.direction,
  });

  try {
    const res = await api.post("/calls/sip-upsert", body);
    console.info("[sip-upsert] ←", { sipCallId: body.sipCallId, response: res });
  } catch (e) {
    console.error("[sip-upsert] failed", { sipCallId: body.sipCallId, error: e, body });
  }

  if (typeof body.status === "string" && TERMINAL_STATUSES.has(body.status)) {
    clearCallMemory(sipCallIdKey);
  }
}

// Tracks the last tagIds payload sent per sipCallId to suppress duplicates
// (e.g. ActiveCallTags re-running its effect with no user change).
const lastSentTags = new Map<string, string>();

export async function persistCallTags(sipCallId: string, tagIds: string[]): Promise<void> {
  if (!sipCallId) return;
  // If we never created a row for this Call-ID (internal inbound leg),
  // don't post tags for it either — they'd land on nothing.
  if (callVerdict.get(sipCallId) === "skip") return;
  const key = JSON.stringify(tagIds ?? []);
  if (lastSentTags.get(sipCallId) === key) {
    return;
  }
  // Skip the initial empty payload — no need to upsert just to set [].
  if (!lastSentTags.has(sipCallId) && (!tagIds || tagIds.length === 0)) {
    lastSentTags.set(sipCallId, key);
    return;
  }
  lastSentTags.set(sipCallId, key);
  try {
    await api.post("/calls/sip-upsert", { sipCallId, tagIds });
  } catch (e) {
    console.error("[sip-upsert tags] failed", { sipCallId, error: e });
  }
}
