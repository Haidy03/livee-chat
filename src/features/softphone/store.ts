import { create } from "zustand";
import { persist } from "zustand/middleware";
import { mockHistory } from "./data/mockHistory";

export type SoftphoneView = "dialer" | "contacts" | "recents" | "voicemail" | "messages" | "settings";
export type AgentStatus = "available" | "busy" | "away" | "offline";

export interface ActiveCall {
  contactId?: string;
  number: string;
  startedAt: number;
  direction: "in" | "out";
  muted: boolean;
  onHold: boolean;
  recording: boolean;
  speaker: boolean;
  showKeypad: boolean;
}

export interface CallRecord {
  id: string;
  contactId?: string;
  number: string;
  type: "in" | "out" | "missed";
  at: number;
  durationSec: number;
}

interface State {
  view: SoftphoneView;
  dialed: string;
  pipelineTarget: string | null;
  status: AgentStatus;
  activeCall: ActiveCall | null;
  history: CallRecord[];
  selectedContactId: string | null;
  selectedHistoryId: string | null;
  notes: Record<string, string>;
  callTags: Record<string, string[]>;
  callerId: string | null;
  setView: (v: SoftphoneView) => void;
  setDialed: (d: string) => void;
  setPipelineTarget: (t: string | null) => void;
  setCallerId: (v: string | null) => void;
  appendDigit: (d: string) => void;
  backspace: () => void;
  startCall: (number: string, contactId?: string, direction?: "in" | "out") => void;
  endCall: () => void;
  toggle: (k: "muted" | "onHold" | "recording" | "speaker" | "showKeypad") => void;
  setStatus: (s: AgentStatus) => void;
  setSelectedContact: (id: string | null) => void;
  setSelectedHistory: (id: string | null) => void;
  setNote: (contactId: string, value: string) => void;
  removeHistory: (id: string) => void;
  /** Used by the SIP controller to mirror live SIP call state into the legacy shape. */
  setActiveCall: (c: ActiveCall | null) => void;
  /** Add a record to history (used by SIP controller and legacy mock flow). */
  pushHistoryRecord: (rec: CallRecord) => void;
  setCallTags: (callId: string, ids: string[]) => void;
  toggleCallTag: (callId: string, id: string) => void;
}


export const useSoftphone = create<State>()(
  persist(
    (set, get) => ({
      view: "dialer",
      dialed: "",
      pipelineTarget: null,
      status: "available",
      activeCall: null,
      history: mockHistory,
      selectedContactId: null,
      selectedHistoryId: null,
      notes: {},
      callTags: {},
      callerId: null,
      setView: (v) => set({ view: v }),
      setDialed: (d) => set({ dialed: d, pipelineTarget: null }),
      setPipelineTarget: (t) => set({ pipelineTarget: t }),
      setCallerId: (v) => set({ callerId: v }),
      appendDigit: (d) => set({ dialed: get().dialed + d, pipelineTarget: null }),
      backspace: () => set({ dialed: get().dialed.slice(0, -1), pipelineTarget: null }),

      startCall: (number, contactId, direction = "out") =>
        set({
          activeCall: {
            number,
            contactId,
            direction,
            startedAt: Date.now(),
            muted: false,
            onHold: false,
            recording: false,
            speaker: false,
            showKeypad: false,
          },
          view: "dialer",
        }),
      endCall: () => {
        const c = get().activeCall;
        if (!c) return;
        const durationSec = Math.floor((Date.now() - c.startedAt) / 1000);
        const rec: CallRecord = {
          id: "r" + Date.now(),
          contactId: c.contactId,
          number: c.number,
          type: c.direction,
          at: c.startedAt,
          durationSec,
        };
        set({ activeCall: null, history: [rec, ...get().history], dialed: "" });
      },
      toggle: (k) => {
        const c = get().activeCall;
        if (!c) return;
        set({ activeCall: { ...c, [k]: !c[k] } });
      },
      setStatus: (s) => set({ status: s }),
      setSelectedContact: (id) => set({ selectedContactId: id }),
      setSelectedHistory: (id) => set({ selectedHistoryId: id }),
      setNote: (contactId, value) => set({ notes: { ...get().notes, [contactId]: value } }),
      removeHistory: (id) => set({ history: get().history.filter((h) => h.id !== id) }),
      setActiveCall: (c) => set({ activeCall: c }),
      pushHistoryRecord: (rec) => set({ history: [rec, ...get().history] }),
      setCallTags: (callId, ids) =>
        set({ callTags: { ...get().callTags, [callId]: ids } }),
      toggleCallTag: (callId, id) => {
        const cur = get().callTags[callId] ?? [];
        const next = cur.includes(id) ? cur.filter((x) => x !== id) : [...cur, id];
        set({ callTags: { ...get().callTags, [callId]: next } });
      },
    }),
    {
      name: "softphone-state",
      partialize: (s) => ({ history: s.history, status: s.status, notes: s.notes, callTags: s.callTags, callerId: s.callerId }),
    },
  ),
);
