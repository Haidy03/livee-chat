/**
 * SoftphoneWorkspace — visually mirrors the /relay page. ZERO logic changes:
 * every action, hook, store, and SIP call binding from the original
 * /softphone page is preserved. The /relay mock engine is never invoked.
 *
 * Bindings:
 *  - useSoftphone (dialed, status, activeCall, history, tags) — unchanged store
 *  - sipActions.* — unchanged SIP controller
 *  - useAgents, useSoftphoneContacts, usePipelineUsers — unchanged data sources
 *  - useAuth() — for display name + Sign out
 *  - openSoftphonePopup + WorkspaceSheet — unchanged Popup/Settings/Voicemail/Messages
 */
import { useEffect, useMemo, useRef, useState } from "react";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import {
  Phone, PhoneOff, PhoneIncoming, PhoneOutgoing, Video, X, Mic, MicOff, Pause, Play,
  Grip, ArrowRightLeft, Users as UsersIcon, Settings as SettingsIcon, Voicemail as VmIcon,
  MessageSquare, ExternalLink, Volume2, VolumeX, Copy, ChevronDown, Search,
  ChevronsLeft, Grid3x3, Delete,
} from "lucide-react";
import { cn } from "@/lib/utils";
import { useSoftphone, type AgentStatus, type CallRecord } from "../store";
import { useSipState, getSipAdapter } from "../sip/useSipState";
import { sipActions } from "../sip/useSipController";
import { startSoftRing, stopSoftRing, playConnectBlip } from "../sip/ringtone";
import { useSoftphoneContacts, useFindContactByNumber } from "../hooks/useSoftphoneContacts";
import { useInboundCaller } from "../sip/inboundCallerLookup";
import { useAgents, agentLabel } from "@/hooks/useAgents";
import { useAuth } from "@/hooks/useAuth";
import { usePipelineUsers } from "./usePipelineUsers";
import { useDialpadKeyboard } from "../hooks/useDialpadKeyboard";
import { playDtmfTone } from "../sip/dtmfTone";
import { formatPhone } from "../utils/formatPhone";
import { getCurrentTenantId } from "@/lib/tenant";
import { openSoftphonePopup, isSoftphonePopupWindow } from "../utils/openSoftphonePopup";
import { WorkspaceSheet, type SheetKey } from "./WorkspaceSheet";
import { useWrapUpCodes as useTenantWrapUpCodes } from "@/features/wrapup-codes/api";
import { TagChip } from "./TagChip";
import { TagEditor } from "./TagEditor";
import { useTagsStore } from "./useTagsStore";
import { useTenantNumbers } from "../data/tenantNumbers";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { saveWrapUp } from "../api/wrapup";

// ============================================================
// Tokens (mirrors /relay)
// ============================================================
const INK = "#1F2630";
const MUTE = "#646E7B";
const FAINT = "#9AA3AD";
const BORDER = "#E7E4DB";
const BG = "#F3F1EB";
const SOFT = "#F8F7F2";
const CARD = "bg-white border border-[#E7E4DB] rounded-[14px]";
const PANEL = "bg-white border border-[#E7E4DB]";
const CARD_SOFT = "bg-white border border-[#E7E4DB] rounded-[12px]";

const PALETTE = [
  "#6366F1", "#0EA572", "#F97316", "#E11D48",
  "#0EA5E9", "#9333EA", "#EF4444", "#0891B2",
];
function hash(s: string) { let h = 0; for (let i = 0; i < s.length; i++) h = (h * 31 + s.charCodeAt(i)) | 0; return Math.abs(h); }
function avatarColor(name: string) { return PALETTE[hash(name) % PALETTE.length]; }
function initials(name: string) {
  return String(name || "?").split(/[\s@.]/).filter(Boolean).slice(0, 2).map((s) => s[0]).join("").toUpperCase() || "?";
}
function fmtDur(sec: number) {
  const m = Math.floor(sec / 60); const s = Math.floor(sec % 60);
  return `${m.toString().padStart(2, "0")}:${s.toString().padStart(2, "0")}`;
}

// ============================================================
// Presence (bound to softphone store)
// ============================================================
type PresenceKey = "available" | "break" | "lunch" | "meeting" | "admin" | "offline" | "incoming" | "on_call" | "wrap_up";
const PRESENCE_LABEL: Record<PresenceKey, string> = {
  available: "Available", break: "Break", lunch: "Lunch", meeting: "Meeting", admin: "Admin work", offline: "Offline",
  incoming: "Incoming", on_call: "On call", wrap_up: "Wrap-up",
};
const PRESENCE_COLOR: Record<PresenceKey, string> = {
  available: "#16A34A", break: "#B45309", lunch: "#B45309", meeting: "#B45309", admin: "#B45309", offline: "#9AA3AD",
  incoming: "#2563EB", on_call: "#16A34A", wrap_up: "#D97706",
};
/** Map between the softphone AgentStatus and the richer relay presence vocab.
 *  We persist the AgentStatus the store already understands; non-ready reasons
 *  are tracked locally only (// TODO: persist reason via presence API). */
function statusToPresence(s: AgentStatus): PresenceKey {
  if (s === "available") return "available";
  if (s === "offline") return "offline";
  return "meeting"; // away/busy folded into a generic "not ready"
}

// ============================================================
// Shared primitives
// ============================================================
function Avatar({ name, size = 36, ring }: { name: string; size?: number; ring?: string }) {
  return (
    <div
      className="rounded-full inline-flex items-center justify-center text-white font-bold select-none shrink-0"
      style={{
        width: size, height: size, background: avatarColor(name), fontSize: size * 0.38,
        boxShadow: ring ? `0 0 0 3px ${ring}` : undefined,
      }}
    >
      {initials(name)}
    </div>
  );
}
function Pill({ kind, className = "" }: { kind: "SOFTPHONE" | "CTI"; className?: string }) {
  const isSp = kind === "SOFTPHONE";
  return (
    <span
      className={`inline-flex items-center text-[10px] font-bold tracking-wider px-2 py-0.5 rounded-md ${className}`}
      style={{ background: isSp ? "#ECF2FE" : "#E8F6EE", color: isSp ? "#2563EB" : "#16A34A" }}
    >
      {kind}
    </span>
  );
}
function CountPill({ n, active }: { n: number; active?: boolean }) {
  return (
    <span
      className="inline-flex items-center justify-center text-[10px] font-bold rounded-full h-5 min-w-[20px] px-1.5"
      style={{ background: active ? "#E8F6EE" : BG, color: active ? "#16A34A" : MUTE }}
    >{n}</span>
  );
}
function RedBadge({ n }: { n: number }) {
  if (!n) return null;
  return (
    <span className="ml-auto inline-flex items-center justify-center text-[10px] font-bold rounded-full h-5 min-w-[20px] px-1.5 text-white" style={{ background: "#DC2626" }}>
      {n}
    </span>
  );
}
function useTicker(ms = 1000) { const [, setN] = useState(0); useEffect(() => { const id = setInterval(() => setN((x) => x + 1), ms); return () => clearInterval(id); }, [ms]); }

// ============================================================
// Header (presence pill + sound + CRM)
// ============================================================
function Header({ soundOn, onToggleSound, onOpenCrm, wrapupActive }: { soundOn: boolean; onToggleSound: () => void; onOpenCrm: () => void; wrapupActive: boolean }) {
  const status = useSoftphone((s) => s.status);
  const setStatus = useSoftphone((s) => s.setStatus);
  const sip = useSipState();
  const { signOut } = useAuth();
  const [menu, setMenu] = useState(false);
  const [reason, setReason] = useState<PresenceKey | null>(null);
  const callPhase = sip.call?.phase;
  const callPresence: PresenceKey | null =
    callPhase === "inbound_ringing"
      ? "incoming"
      : sip.call && (callPhase === "active" || callPhase === "outbound_dialing" || callPhase === "outbound_ringing")
        ? "on_call"
        : wrapupActive
          ? "wrap_up"
          : null;
  const presence: PresenceKey = callPresence ?? reason ?? statusToPresence(status);
  const callDriven = callPresence !== null;
  const menuRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const onDown = (e: MouseEvent) => { if (!menuRef.current?.contains(e.target as Node)) setMenu(false); };
    if (menu) document.addEventListener("mousedown", onDown);
    return () => document.removeEventListener("mousedown", onDown);
  }, [menu]);

  const isRinging = callPhase === "inbound_ringing";
  const pick = (p: PresenceKey) => {
    setMenu(false);
    if (isRinging) { toast("Answer or decline the current call first"); return; }
    if (p === "available") { setStatus("available"); setReason(null); }
    else if (p === "offline") { setStatus("offline"); setReason(null); }
    else {
      // not-ready reasons: store as "busy" in the existing AgentStatus enum,
      // keep reason locally for the chip label. // TODO: persist reason via presence API
      setStatus("busy"); setReason(p);
    }
  };
  const onPresenceClick = () => {
    if (isRinging) { toast("Answer or decline the current call first"); return; }
    setMenu((v) => !v);
  };
  const isDemo = typeof window !== "undefined" && localStorage.getItem("softphone:demo-mode") === "1";

  // queue/SLA — // TODO: wire to real queue/SLA API when one exists
  return (
    <header className="h-14 px-4 flex items-center justify-between bg-white border-b border-[#E7E4DB]">
      <div className="flex items-center gap-3">
        <div className="h-8 w-8 rounded-md inline-flex items-center justify-center text-white font-extrabold" style={{ background: "#16A34A" }}>S</div>
        <div className="leading-tight">
          <div className="text-sm font-bold" style={{ color: INK }}>Softphone</div>
          <div className="text-[10px] tracking-wider" style={{ color: MUTE }}>SOFTPHONE · WORKSPACE</div>
        </div>
        <div className="relative ml-3" ref={menuRef}>
          <button
            onClick={onPresenceClick}
            disabled={isRinging}
            className={cn(
              "inline-flex items-center gap-2 h-8 px-3 rounded-full border border-[#E7E4DB] text-sm font-medium bg-white",
              isRinging && "opacity-60 cursor-not-allowed",
            )}
            style={{ color: INK }}
          >
            <span
              className={cn("h-2 w-2 rounded-full", presence === "incoming" && "animate-pulse")}
              style={{ background: PRESENCE_COLOR[presence] }}
            />
            {PRESENCE_LABEL[presence]}
            <ChevronDown className="h-3.5 w-3.5 opacity-60" />
          </button>
          {menu && (
            <div className="absolute z-30 mt-1 left-0 w-56 bg-white border border-[#E7E4DB] rounded-lg shadow-lg p-1">
              <MenuItem onClick={() => pick("available")} dot="#16A34A" label="Available" />
              <div className="px-2 pt-2 pb-1 text-[10px] uppercase tracking-wider" style={{ color: FAINT }}>Not ready</div>
              <MenuItem onClick={() => pick("break")} dot="#B45309" label="Break" />
              <MenuItem onClick={() => pick("lunch")} dot="#B45309" label="Lunch" />
              <MenuItem onClick={() => pick("meeting")} dot="#B45309" label="Meeting" />
              <MenuItem onClick={() => pick("admin")} dot="#B45309" label="Admin work" />
              <div className="my-1 border-t border-[#F3F1EB]" />
              <MenuItem onClick={() => { setMenu(false); void signOut(); }} dot="#9AA3AD" label="Sign out" />
            </div>
          )}
        </div>
      </div>

      <div className="flex items-center gap-3">
        {isDemo && (
          <button
            onClick={() => {
              const adapter = getSipAdapter() as unknown as { simulateInbound?: (n: string, name: string) => void };
              if (adapter.simulateInbound) {
                adapter.simulateInbound("+15551234567", "Demo Caller");
              } else {
                toast.message("Enable demo mode to simulate inbound");
              }
            }}
            className="h-8 px-3 rounded-md inline-flex items-center gap-1.5 text-[12px] font-semibold border border-[#E7E4DB] bg-white"
            style={{ color: INK }}
            title="Simulate inbound call"
          >
            <PhoneIncoming className="h-3.5 w-3.5" /> Simulate inbound
          </button>
        )}
        {/* // TODO: queue/SLA API */}
        <div className="text-[12px] font-medium hidden lg:block" style={{ color: MUTE }}>
          Queue <span className="font-bold" style={{ color: INK }}>—</span> waiting · SLA <span className="font-bold" style={{ color: INK }}>—</span>
        </div>
        <button
          onClick={onToggleSound}
          aria-label="Toggle sound"
          className="h-8 w-8 rounded-full border border-[#E7E4DB] inline-flex items-center justify-center bg-white"
          style={{ color: MUTE }}
          title={soundOn ? "Sound on" : "Muted"}
        >
          {soundOn ? <Volume2 className="h-4 w-4" /> : <VolumeX className="h-4 w-4" />}
        </button>
        <button
          onClick={onOpenCrm}
          className="h-8 px-3 rounded-md inline-flex items-center gap-2 text-sm font-semibold text-white"
          style={{ background: "#16A34A" }}
        >
          <ExternalLink className="h-3.5 w-3.5" />
          CRM
        </button>
      </div>
    </header>
  );
}
function MenuItem({ onClick, dot, label }: { onClick: () => void; dot: string; label: string }) {
  return (
    <button onClick={onClick} className="w-full flex items-center gap-2 px-2 py-2 rounded-md hover:bg-[#F8F7F2] text-sm">
      <span className="h-2 w-2 rounded-full" style={{ background: dot }} />
      <span style={{ color: INK }}>{label}</span>
    </button>
  );
}



// ============================================================
// Left rail — title, nav, today stats, account footer
// ============================================================
function LeftRail({
  onSheet,
  active = "dialpad",
  collapsed,
  onToggleCollapse,
}: {
  onSheet: (k: Exclude<SheetKey, null>) => void;
  active?: "dialpad" | "popup" | "voicemail" | "messages" | "settings";
  collapsed: boolean;
  onToggleCollapse: () => void;
}) {
  const history = useSoftphone((s) => s.history);
  const sip = useSipState();
  const { user } = useAuth();
  const agents = useAgents();
  const myAgent = agents.find((a) => a.user_id === user?.id);
  const name = agentLabel(myAgent) || user?.email || "You";
  const myExt = myAgent?.extension_number;

  // Today stats from real history (no fake data).
  const startOfDay = useMemo(() => {
    const d = new Date(); d.setHours(0, 0, 0, 0); return d.getTime();
  }, []);
  const todayRows = history.filter((h) => h.at >= startOfDay);
  const totalCalls = todayRows.length;
  const missed = todayRows.filter((h) => h.type === "missed").length;
  const talkSec = todayRows.reduce((sum, h) => sum + (h.durationSec || 0), 0);
  const answeredCount = todayRows.filter((h) => h.type !== "missed").length;
  const avgSec = answeredCount > 0 ? Math.round(talkSec / answeredCount) : 0;
  const fmtHM = (s: number) => {
    if (s <= 0) return "0m";
    const h = Math.floor(s / 3600);
    const m = Math.floor((s % 3600) / 60);
    return h ? `${h}h ${m}m` : `${m}m`;
  };
  const fmtMS = (s: number) => {
    const m = Math.floor(s / 60); const r = s % 60;
    return `${m}:${r.toString().padStart(2, "0")}`;
  };

  const items: Array<{ id: "dialpad" | "popup" | "voicemail" | "messages" | "settings"; label: string; icon: any; badge?: number; onClick: () => void }> = [
    { id: "dialpad",   label: "Dialpad",   icon: Grid3x3,        onClick: () => { /* current view */ } },
    { id: "popup",     label: "Popup",     icon: ExternalLink,   onClick: () => openSoftphonePopup() },
    { id: "voicemail", label: "Voicemail", icon: VmIcon,         badge: 0, onClick: () => onSheet("voicemail") },
    { id: "messages",  label: "Messages",  icon: MessageSquare,  badge: 0, onClick: () => onSheet("messages") },
    { id: "settings",  label: "Settings",  icon: SettingsIcon,   onClick: () => onSheet("settings") },
  ];

  const sipReg = sip.registration;
  const sipDot = SIP_REG_COLOR[sipReg];
  const sipLabel = sipReg === "registered" ? "SIP registered" : SIP_REG_LABEL[sipReg];

  if (collapsed) {
    return (
      <aside className={`${PANEL} h-full min-h-0 flex flex-col items-center py-3 gap-2`}>
        <button onClick={onToggleCollapse} className="h-8 w-8 rounded-md inline-flex items-center justify-center hover:bg-[#F8F7F2]" aria-label="Expand">
          <ChevronsLeft className="h-4 w-4 rotate-180" style={{ color: MUTE }} />
        </button>
        <div className="w-full border-t border-[#F0EEE7] my-1" />
        {items.map((it) => {
          const isActive = active === it.id;
          return (
            <button
              key={it.id}
              onClick={it.onClick}
              className="h-10 w-10 rounded-lg inline-flex items-center justify-center relative"
              style={{
                background: isActive ? "#E8F6EE" : "transparent",
                color: isActive ? "#16A34A" : INK,
              }}
              title={it.label}
            >
              <it.icon className="h-4 w-4" />
              {it.badge ? (
                <span className="absolute -top-0.5 -right-0.5 h-4 min-w-[16px] px-1 rounded-full text-[9px] font-bold text-white inline-flex items-center justify-center" style={{ background: "#DC2626" }}>{it.badge}</span>
              ) : null}
            </button>
          );
        })}
      </aside>
    );
  }

  return (
    <aside className={`${PANEL} h-full min-h-0 flex flex-col overflow-hidden`}>
      {/* Title bar */}
      <div className="px-4 pt-4 pb-3 flex items-center justify-between">
        <div className="text-[15px] font-bold" style={{ color: INK }}>Softphone</div>
        <div className="flex items-center gap-1">
          <button
            onClick={onToggleCollapse}
            className="h-7 w-7 rounded-md inline-flex items-center justify-center hover:bg-[#F8F7F2]"
            aria-label="Collapse"
            title="Collapse"
          >
            <ChevronsLeft className="h-3.5 w-3.5" style={{ color: MUTE }} />
          </button>
        </div>
      </div>

      {/* Nav */}
      <nav className="px-3 flex flex-col gap-1">
        {items.map((it) => {
          const isActive = active === it.id;
          return (
            <button
              key={it.id}
              onClick={it.onClick}
              className="relative h-10 w-full inline-flex items-center gap-3 px-3 rounded-lg text-[13px] font-medium transition"
              style={{
                background: isActive ? "#E8F6EE" : "transparent",
                color: isActive ? "#16A34A" : INK,
              }}
              onMouseEnter={(e) => { if (!isActive) e.currentTarget.style.background = "#F8F7F2"; }}
              onMouseLeave={(e) => { if (!isActive) e.currentTarget.style.background = "transparent"; }}
            >
              <it.icon className="h-4 w-4 shrink-0" />
              <span className="flex-1 text-left">{it.label}</span>
              {it.badge ? <RedBadge n={it.badge} /> : null}
            </button>
          );
        })}
      </nav>

      {/* Today stats */}
      <div className="px-4 pt-5 pb-3">
        <div className="text-[10px] uppercase tracking-[0.18em] font-semibold mb-2" style={{ color: FAINT }}>Today</div>
        <div className="grid grid-cols-2 gap-2">
          <StatTile value={String(totalCalls)} label="Calls" icon={Phone} />
          <StatTile value={fmtHM(talkSec)} label="Talk time" icon={Pause /* clock-ish */} />
          <StatTile value={String(missed)} label="Missed" icon={PhoneIncoming} valueColor={missed > 0 ? "#DC2626" : INK} />
          <StatTile value={fmtMS(avgSec)} label="Avg" icon={ArrowRightLeft} />
        </div>
      </div>

      <div className="flex-1" />

      {/* User footer */}
      <div className="px-4 py-3 border-t border-[#F0EEE7] flex items-center gap-2.5">
        <Avatar name={name} size={32} />
        <div className="min-w-0 flex-1 leading-tight">
          <div className="text-[12px] font-semibold truncate" style={{ color: INK }}>{name}</div>
          <div className="text-[10.5px] truncate flex items-center gap-1.5" style={{ color: MUTE }}>
            {myExt != null && <span className="font-mono">Ext {myExt}</span>}
            {myExt != null && <span className="opacity-50">·</span>}
            <span className="inline-flex items-center gap-1">
              <span className="h-1.5 w-1.5 rounded-full" style={{ background: sipDot }} />
              {sipLabel}
            </span>
          </div>
        </div>
      </div>
    </aside>
  );
}

function StatTile({ value, label, icon: Icon, valueColor = INK }: { value: string; label: string; icon: any; valueColor?: string }) {
  return (
    <div className="rounded-xl px-3 py-2.5" style={{ background: SOFT, border: `1px solid ${BORDER}` }}>
      <div className="text-[18px] font-bold leading-none" style={{ color: valueColor }}>{value}</div>
      <div className="mt-1.5 text-[10.5px] inline-flex items-center gap-1" style={{ color: MUTE }}>
        <Icon className="h-3 w-3" />
        {label}
      </div>
    </div>
  );
}


// ============================================================
// Center stage
// ============================================================
function CenterStage({ onTransfer, wrapup, onSaveWrapup, onCancelWrapup, soundOn, onToggleSound, onOpenCrm }: {
  onTransfer: () => void;
  wrapup: { startedAt: number; lastNumber: string; lastName: string } | null;
  onSaveWrapup: (d: { disposition: string; notes: string; callback: boolean }) => Promise<void> | void;
  onCancelWrapup: () => void;
  soundOn: boolean;
  onToggleSound: () => void;
  onOpenCrm: () => void;
}) {
  const activeCall = useSoftphone((s) => s.activeCall);
  const sip = useSipState();
  const phase = sip.call?.phase;
  if (wrapup) return <section className={`${PANEL} flex flex-col h-full min-h-0 overflow-hidden`}><WrapupStage data={wrapup} onSave={onSaveWrapup} onCancel={onCancelWrapup} /></section>;
  if (activeCall && phase === "inbound_ringing") {
    return <section className={`${PANEL} flex flex-col h-full min-h-0 overflow-hidden`}><RingingStage soundOn={soundOn} /></section>;
  }
  if (activeCall) {
    return <section className={`${PANEL} flex flex-col h-full min-h-0 overflow-hidden`}><ActiveStage onTransfer={onTransfer} /></section>;
  }
  return <section className="border border-[#E7E4DB] flex flex-col h-full min-h-0 overflow-hidden bg-transparent"><IdleStage soundOn={soundOn} onToggleSound={onToggleSound} onOpenCrm={onOpenCrm} /></section>;
}

const SIP_REG_LABEL: Record<import("../sip/types").RegistrationStatus, string> = {
  registered: "Registered",
  connecting: "Connecting",
  failed: "Failed",
  unregistered: "Unregistered",
};
const SIP_REG_COLOR: Record<import("../sip/types").RegistrationStatus, string> = {
  registered: "#16A34A",
  connecting: "#D97706",
  failed: "#DC2626",
  unregistered: "#646E7B",
};

function IdentityHeader() {
  const sip = useSipState();
  const reg = sip.registration;
  const { user } = useAuth();
  const agents = useAgents();
  const myAgent = agents.find((a) => a.user_id === user?.id);
  const name = agentLabel(myAgent) || user?.email || "You";
  const myExt = myAgent?.extension_number;
  return (
    <div className="px-5 py-4 border-b border-[#E7E4DB] flex items-center gap-3">
      <Avatar name={name} size={40} />
      <div className="flex-1 min-w-0">
        <div className="text-sm font-semibold truncate" style={{ color: INK }}>{name}</div>
        <div className="text-[12px] font-mono" style={{ color: MUTE }}>Ext {myExt ?? "—"}</div>
      </div>
      <span
        className="inline-flex items-center gap-1.5 h-7 px-2.5 rounded-full text-[12px] font-semibold"
        style={{ background: `${SIP_REG_COLOR[reg]}1A`, color: SIP_REG_COLOR[reg] }}
      >
        <span className="h-1.5 w-1.5 rounded-full" style={{ background: SIP_REG_COLOR[reg] }} />
        {SIP_REG_LABEL[reg]}
      </span>
    </div>
  );
}

function IdleStage({ soundOn, onToggleSound, onOpenCrm }: { soundOn: boolean; onToggleSound: () => void; onOpenCrm: () => void }) {
  const dialed = useSoftphone((s) => s.dialed);
  const setDialed = useSoftphone((s) => s.setDialed);
  const appendDigit = useSoftphone((s) => s.appendDigit);
  const backspace = useSoftphone((s) => s.backspace);
  const pipelineTarget = useSoftphone((s) => s.pipelineTarget);
  const callerId = useSoftphone((s) => s.callerId);
  const setCallerId = useSoftphone((s) => s.setCallerId);
  const sip = useSipState();
  const { numbers: tenantNumbers, loading: tenantNumbersLoading } = useTenantNumbers();
  const { user } = useAuth();
  const agents = useAgents();
  const myAgent = agents.find((a) => a.user_id === user?.id);
  const name = agentLabel(myAgent) || user?.email || "You";
  const myExt = myAgent?.extension_number;
  const reg = sip.registration;

  const { t } = useTranslation();

  useEffect(() => {
    if (!callerId && tenantNumbers.length > 0) setCallerId(tenantNumbers[0].number);
  }, [callerId, tenantNumbers, setCallerId]);

  const handleCall = () => {
    if (!dialed) return;
    const target = pipelineTarget ?? dialed.replace(/[^\d+#*@.a-zA-Z:]/g, "");
    if (sip.registration !== "registered") {
      toast.error(t("softphone.notRegistered"));
      return;
    }
    sipActions.call(target).then(() => toast(`Calling ${formatPhone(dialed)}`)).catch((e: Error) => toast.error(e.message));
  };
  useDialpadKeyboard(handleCall);

  useEffect(() => {
    const onPaste = (e: ClipboardEvent) => {
      const tgt = e.target as HTMLElement | null;
      if (tgt && (tgt.tagName === "INPUT" || tgt.tagName === "TEXTAREA" || tgt.isContentEditable)) return;
      const raw = e.clipboardData?.getData("text") ?? "";
      const v = raw.replace(/[^\d+#*]/g, "").slice(0, 32);
      if (!v) return;
      e.preventDefault();
      setDialed(v);
    };
    window.addEventListener("paste", onPaste);
    return () => window.removeEventListener("paste", onPaste);
  }, [setDialed]);

  const canCall = !!dialed && sip.registration === "registered";

  const keys: { d: string; sub: string }[] = [
    { d: "1", sub: "" }, { d: "2", sub: "ABC" }, { d: "3", sub: "DEF" },
    { d: "4", sub: "GHI" }, { d: "5", sub: "JKL" }, { d: "6", sub: "MNO" },
    { d: "7", sub: "PQRS" }, { d: "8", sub: "TUV" }, { d: "9", sub: "WXYZ" },
    { d: "*", sub: "" }, { d: "0", sub: "+" }, { d: "#", sub: "" },
  ];

  return (
    <>
      {/* In-card header: identity + queue + sound + CRM */}
      <div className="px-5 py-4 border-b border-[#E7E4DB] flex items-center gap-3" style={{ background: "#FFFFFF" }}>
        <Avatar name={name} size={40} />
        <div className="flex-1 min-w-0 leading-tight">
          <div className="flex items-center gap-2">
            <div className="text-[15px] font-bold truncate" style={{ color: INK }}>{name}</div>
            <span
              className="inline-flex items-center gap-1.5 h-6 px-2.5 rounded-full text-[11px] font-semibold"
              style={{ background: `${SIP_REG_COLOR[reg]}1A`, color: SIP_REG_COLOR[reg] }}
            >
              <span className="h-1.5 w-1.5 rounded-full" style={{ background: SIP_REG_COLOR[reg] }} />
              {SIP_REG_LABEL[reg]}
            </span>
          </div>
          <div className="text-[12px] mt-0.5 truncate" style={{ color: MUTE }}>
            <span className="font-mono">Ext {myExt ?? "—"}</span>
            <span className="opacity-50"> · </span>
            Queue: <span style={{ color: INK }} className="font-medium">waiting</span>
            <span className="opacity-50"> · </span>
            SLA <span style={{ color: INK }} className="font-medium">—</span>
          </div>
        </div>
        <button
          onClick={onToggleSound}
          className="h-9 w-9 rounded-md inline-flex items-center justify-center hover:bg-[#F8F7F2]"
          aria-label="Toggle sound"
          title={soundOn ? "Sound on" : "Muted"}
        >
          {soundOn ? <Volume2 className="h-4 w-4" style={{ color: MUTE }} /> : <VolumeX className="h-4 w-4" style={{ color: MUTE }} />}
        </button>
        <button
          onClick={onOpenCrm}
          className="h-9 px-3.5 rounded-lg inline-flex items-center gap-2 text-[13px] font-semibold text-white"
          style={{ background: "#16A34A" }}
        >
          <ExternalLink className="h-3.5 w-3.5" />
          CRM
        </button>
      </div>

      <div className="flex-1 min-h-0 overflow-y-auto">
        <div className="max-w-[480px] mx-auto px-6 pt-5 pb-6">
          {/* Outbound number */}
          <div className="text-[10px] uppercase tracking-[0.18em] font-semibold mb-2" style={{ color: FAINT }}>
            Outbound number
          </div>
          <Select value={callerId ?? undefined} onValueChange={(v) => setCallerId(v)}>
            <SelectTrigger
              className="h-12 w-full rounded-xl text-[14px] font-mono"
              style={{ background: "#FFFFFF", border: `1px solid ${BORDER}`, color: INK }}
            >
              <div className="flex items-center gap-2.5 min-w-0 flex-1">
                <span className="h-6 w-6 rounded-full inline-flex items-center justify-center shrink-0" style={{ background: "#E8F6EE" }}>
                  <Phone className="h-3 w-3" style={{ color: "#16A34A" }} />
                </span>
                {callerId ? (
                  <>
                    <span dir="ltr" className="truncate" style={{ color: INK }}>{formatPhone(callerId)}</span>
                    <span className="text-[11px] font-sans" style={{ color: FAINT }}>Default</span>
                  </>
                ) : (
                  <SelectValue placeholder={tenantNumbersLoading ? "Loading…" : "Select number"} />
                )}
              </div>
            </SelectTrigger>
            <SelectContent>
              {tenantNumbers.length === 0 ? (
                <div className="px-2 py-1.5 text-xs text-muted-foreground">
                  {tenantNumbersLoading ? "Loading…" : "No numbers available"}
                </div>
              ) : (
                tenantNumbers.map((n) => (
                  <div key={n.id} className="flex items-center gap-2 pe-1">
                    <SelectItem value={n.number} className="flex-1">
                      <span className="font-mono" dir="ltr">{formatPhone(n.number)}</span>
                      <span className="ms-2 text-xs text-muted-foreground">{n.label}</span>
                    </SelectItem>
                    <button
                      type="button"
                      onPointerDown={(e) => { e.preventDefault(); e.stopPropagation(); }}
                      onClick={(e) => {
                        e.preventDefault();
                        e.stopPropagation();
                        const numberWithoutPlus = n.number.replace(/\+/g, "");
                        setDialed(numberWithoutPlus);
                        if (sip.registration !== "registered") {
                          toast.error(t("softphone.notRegistered"));
                          return;
                        }
                        const target = numberWithoutPlus.replace(/[^\d+#*@.a-zA-Z:]/g, "");
                        sipActions.call(target).then(() => toast(`Calling ${formatPhone(numberWithoutPlus)}`)).catch((err: Error) => toast.error(err.message));
                      }}
                      className="h-7 w-7 rounded-full inline-flex items-center justify-center text-white shrink-0"
                      style={{ background: "#16A34A" }}
                      aria-label="Dial this number"
                      title="Dial this number"
                    >
                      <Phone className="h-3.5 w-3.5" />
                    </button>
                  </div>
                ))
              )}
            </SelectContent>
          </Select>

          {/* Enter number */}
          <div className="relative mt-5">
            <input
              value={dialed}
              onChange={(e) => setDialed(e.target.value)}
              placeholder="Enter number"
              className="w-full h-14 px-4 pr-12 rounded-xl outline-none font-mono text-[24px] placeholder:text-[#9AA3AD] placeholder:font-sans placeholder:font-normal"
              style={{ background: "#FFFFFF", border: `1px solid ${BORDER}`, color: INK }}
              dir="ltr"
            />
            {dialed && (
              <button
                onClick={backspace}
                className="absolute right-2 top-1/2 -translate-y-1/2 h-9 w-9 rounded-lg inline-flex items-center justify-center hover:bg-[#F0EEE7]"
                aria-label="Backspace"
              >
                <Delete className="h-4 w-4" style={{ color: MUTE }} />
              </button>
            )}
          </div>


          {/* Keypad */}
          <div className="grid grid-cols-3 gap-2.5 mt-5">
            {keys.map((k) => (
              <button
                key={k.d}
                onClick={() => { playDtmfTone(k.d); appendDigit(k.d); }}
                className="h-[58px] rounded-xl bg-white hover:bg-[#F8F7F2] transition-colors flex flex-col items-center justify-center gap-0.5 active:scale-[0.98]"
                style={{ border: `1px solid ${BORDER}` }}
              >
                <span className="font-mono text-[20px] font-semibold leading-none" style={{ color: INK }}>{k.d}</span>
                <span className="text-[9px] leading-none tracking-[0.18em] uppercase" style={{ color: FAINT }}>
                  {k.sub || "\u00A0"}
                </span>
              </button>
            ))}
          </div>

          {/* Call button */}
          <button
            disabled={!canCall}
            onClick={handleCall}
            className="mt-5 w-full h-14 rounded-2xl text-white text-[15px] font-semibold inline-flex items-center justify-center gap-2 transition active:scale-[0.99]"
            style={{ background: canCall ? "#16A34A" : "#ACD9BD", cursor: canCall ? "pointer" : "not-allowed" }}
          >
            <Phone className="h-5 w-5" />
            Call
          </button>
        </div>
      </div>
    </>
  );
}


function RingingStage({ soundOn }: { soundOn: boolean }) {
  const activeCall = useSoftphone((s) => s.activeCall)!;
  const sip = useSipState();
  const findContact = useFindContactByNumber();
  const contact = findContact(activeCall.number);
  const caller = useInboundCaller(sip.call?.fromUri, activeCall.number, activeCall.direction);

  const isInternal = caller.kind === "internal";
  const isAi = caller.kind === "ai-pipeline";
  const matched = !!contact || isInternal || isAi || (caller.kind === "external" && !!caller.name);

  const name =
    caller.name ??
    contact?.name ??
    sip.call?.remoteDisplayName ??
    "Unknown caller";

  useTicker(1000);
  const wait = Math.max(0, Math.floor((Date.now() - activeCall.startedAt) / 1000));

  // // TODO: real queue/priority from inbound call payload when backend provides it.
  const queue = isInternal ? "Internal" : "General";
  const priority: "Normal" | "High" | "VIP" =
    caller.kind === "external" && caller.isVip ? "VIP" : "Normal";
  const priorityAmber = priority !== "Normal";

  // Soft ring tone (StageCard local) — gated by sound toggle.
  useEffect(() => {
    startSoftRing(soundOn);
    return () => stopSoftRing();
  }, [soundOn]);

  const onAnswer = () => {
    stopSoftRing();
    playConnectBlip();
    sipActions.answer().catch((e: Error) => toast.error(e.message));
  };
  const onDecline = () => {
    stopSoftRing();
    sipActions.reject();
    toast("Call declined");
  };

  // Keyboard: Enter = answer, Esc = decline.
  useEffect(() => {
    const onKey = (e: KeyboardEvent) => {
      const tgt = e.target as HTMLElement | null;
      if (tgt && (tgt.tagName === "INPUT" || tgt.tagName === "TEXTAREA" || tgt.isContentEditable)) return;
      if (e.key === "Enter") { e.preventDefault(); onAnswer(); }
      else if (e.key === "Escape") { e.preventDefault(); onDecline(); }
    };
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const subline = isInternal
    ? `Extension ${caller.extension}`
    : formatPhone(activeCall.number);

  return (
    <div className="flex-1 flex flex-col items-center justify-center px-6 py-10 text-center animate-in fade-in slide-in-from-bottom-2 duration-150">
      <div className="flex items-center gap-2 mb-5">
        <Pill kind="SOFTPHONE" />
        <span className="text-[11px]" style={{ color: MUTE }}>answer / decline</span>
        {isInternal ? (
          <span className="text-[10px] font-bold tracking-wider px-2 py-0.5 rounded-md" style={{ background: "#1E40AF22", color: "#1E40AF" }}>INTERNAL</span>
        ) : isAi ? (
          <span className="text-[10px] font-bold tracking-wider px-2 py-0.5 rounded-md" style={{ background: "#7C3AED22", color: "#7C3AED" }}>AI</span>
        ) : matched ? (
          <>
            <Pill kind="CTI" />
            <span className="text-[11px]" style={{ color: MUTE }}>caller matched · screen pop</span>
          </>
        ) : caller.loading ? (
          <span className="text-[10px] font-bold tracking-wider px-2 py-0.5 rounded-md" style={{ background: BG, color: MUTE }}>LOOKING UP…</span>
        ) : (
          <span className="text-[10px] font-bold tracking-wider px-2 py-0.5 rounded-md" style={{ background: BG, color: MUTE }}>NO CRM</span>
        )}
      </div>
      <div className="relative mb-5" style={{ width: 92, height: 92 }}>
        <span className="absolute inset-0 rounded-full animate-ping" style={{ background: "#16A34A33", animationDuration: "1.6s" }} />
        <span className="absolute -inset-2 rounded-full animate-ping" style={{ background: "#16A34A22", animationDuration: "1.6s", animationDelay: "0.4s" }} />
        <span className="absolute -inset-3 rounded-full ring-2" style={{ borderColor: "#16A34A55" }} />
        <div className="relative">
          <Avatar name={name} size={92} ring="#16A34A33" />
        </div>
      </div>
      <div className="text-[21px] font-bold leading-tight" style={{ color: INK }}>{name}</div>
      <div className="font-mono mt-1" style={{ color: MUTE }}>{subline}</div>
      <div className="text-[12px] mt-2" style={{ color: MUTE }}>
        <span style={{ color: INK }}>{queue}</span> queue ·{" "}
        <span style={{ color: priorityAmber ? "#B45309" : MUTE, fontWeight: priorityAmber ? 700 : 500 }}>{priority}</span>
        {" "}· waiting <span className="font-mono tabular-nums">{fmtDur(wait)}</span>
      </div>
      <div className="mt-8 flex items-start gap-12">
        <div className="flex flex-col items-center gap-1.5">
          <button
            onClick={onDecline}
            className="h-[60px] w-[60px] rounded-full text-white inline-flex items-center justify-center shadow-md active:scale-95 transition"
            style={{ background: "#DC2626" }} aria-label="Decline"
          >
            <PhoneOff className="h-6 w-6 rotate-[135deg]" />
          </button>
          <span className="text-[11px]" style={{ color: MUTE }}>Decline</span>
        </div>
        <div className="flex flex-col items-center gap-1.5">
          <button
            onClick={onAnswer}
            className="h-[60px] w-[60px] rounded-full text-white inline-flex items-center justify-center shadow-md active:scale-95 transition animate-pulse"
            style={{ background: "#16A34A" }} aria-label="Answer" autoFocus
          >
            <Phone className="h-6 w-6" />
          </button>
          <span className="text-[11px] font-semibold" style={{ color: "#16A34A" }}>Answer</span>
        </div>
      </div>
    </div>
  );
}


function ActiveStage({ onTransfer }: { onTransfer: () => void }) {
  const activeCall = useSoftphone((s) => s.activeCall)!;
  const findContact = useFindContactByNumber();
  const sip = useSipState();
  const phase = sip.call?.phase ?? "active";
  const isConnected = phase === "active";
  const dialing = phase === "outbound_dialing" || phase === "outbound_ringing";
  useTicker(1000);
  const contact = findContact(activeCall.number);
  const name = contact?.name ?? activeCall.number;
  const liveSec = isConnected ? Math.floor((Date.now() - activeCall.startedAt) / 1000) : 0;
  const [showDtmf, setShowDtmf] = useState(false);

  return (
    <div className="flex-1 flex flex-col">
      <div className="px-5 py-4 border-b border-[#E7E4DB] flex items-center gap-3">
        <Avatar name={name} size={40} />
        <div className="flex-1 min-w-0">
          <div className="flex items-center gap-2">
            <div className="text-sm font-semibold truncate" style={{ color: INK }}>{name}</div>
            <span className="inline-flex items-center gap-1 px-1.5 py-0.5 rounded text-[10px] font-bold"
              style={{ background: activeCall.direction === "in" ? "#ECF2FE" : "#E8F6EE", color: activeCall.direction === "in" ? "#2563EB" : "#16A34A" }}>
              {activeCall.direction === "in" ? "Inbound" : "Outbound"}
            </span>
          </div>
          <div className="text-[12px] font-mono truncate" style={{ color: MUTE }}>{formatPhone(activeCall.number)}</div>
        </div>
        <div className="text-right">
          <div className="font-mono text-2xl font-semibold tabular-nums" style={{ color: INK }}>
            {dialing ? "· · ·" : fmtDur(liveSec)}
          </div>
          {activeCall.recording && (
            <div className="mt-1 text-[10px] font-bold inline-flex items-center gap-1 text-[#DC2626]">
              <span className="h-1.5 w-1.5 rounded-full animate-pulse" style={{ background: "#DC2626" }} /> REC
            </div>
          )}
        </div>
      </div>

      <div className="px-5 pt-3">
        <Pill kind="SOFTPHONE" /><span className="text-[11px] ml-2" style={{ color: MUTE }}>in-call controls — mute · hold · transfer · DTMF</span>
      </div>

      {activeCall.onHold && (
        <div className="mx-5 mt-3 px-3 py-2 rounded-lg flex items-center justify-between text-[12px]" style={{ background: "#FEF3E0", color: "#B45309" }}>
          <span>Customer on hold</span>
        </div>
      )}

      <div className="px-5 pt-3 grid grid-cols-3 gap-2">
        <CtlBtn icon={activeCall.muted ? MicOff : Mic} label={activeCall.muted ? "Unmute" : "Mute"}
          onClick={() => sipActions.toggleMute(!activeCall.muted)} active={activeCall.muted} disabled={!isConnected} />
        <CtlBtn icon={activeCall.onHold ? Play : Pause} label={activeCall.onHold ? "Resume" : "Hold"}
          onClick={() => {
            const next = !activeCall.onHold;
            sipActions.toggleHold(next).then(() => toast(next ? "Call on hold" : "Call resumed")).catch((e: Error) => toast.error(e.message));
          }} active={activeCall.onHold} amber={activeCall.onHold} disabled={!isConnected} />
        <CtlBtn icon={Grip} label="Keypad" onClick={() => setShowDtmf((v) => !v)} active={showDtmf} disabled={!isConnected} />
        <CtlBtn icon={ArrowRightLeft} label="Transfer" onClick={onTransfer} disabled={!isConnected} />
        <CtlBtn icon={UsersIcon} label="Conference" onClick={() => toast.message("Conference — pending backend")} disabled />
        <div /> {/* spacer to keep grid tidy without faking record */}
      </div>

      {showDtmf && <InlineDtmf />}

      <div className="px-5 pt-4 pb-4 mt-auto">
        <button
          onClick={() => sipActions.hangup()}
          className="w-full h-12 rounded-xl text-white text-[15px] font-semibold inline-flex items-center justify-center gap-2"
          style={{ background: "#DC2626" }}
        >
          <PhoneOff className="h-4 w-4" /> End call
        </button>
      </div>
    </div>
  );
}

function CtlBtn({ icon: Icon, label, onClick, active, amber, red, disabled }: { icon: any; label: string; onClick: () => void; active?: boolean; amber?: boolean; red?: boolean; disabled?: boolean }) {
  const bg = disabled ? SOFT : active ? (red ? "#FDECEC" : amber ? "#FEF3E0" : "#E8F6EE") : "#FFFFFF";
  const color = disabled ? FAINT : active ? (red ? "#DC2626" : amber ? "#B45309" : "#16A34A") : INK;
  return (
    <button onClick={onClick} disabled={disabled}
      className="h-16 rounded-xl border flex flex-col items-center justify-center gap-1 text-[12px] font-medium transition"
      style={{ background: bg, color, borderColor: BORDER, cursor: disabled ? "not-allowed" : "pointer" }}
    >
      <Icon className="h-4 w-4" />{label}
    </button>
  );
}

function InlineDtmf() {
  const [val, setVal] = useState("");
  const keys = ["1","2","3","4","5","6","7","8","9","*","0","#"];
  const send = (k: string) => { playDtmfTone(k); sipActions.dtmf(k); setVal((v) => v + k); };
  return (
    <div className="mx-5 mt-3 p-3 rounded-xl" style={{ background: SOFT, border: `1px solid ${BORDER}` }}>
      <div className="h-9 mb-2 px-2 rounded-md bg-white font-mono text-[16px] flex items-center" style={{ color: INK, border: `1px solid ${BORDER}` }}>{val || <span style={{ color: FAINT }}>DTMF</span>}</div>
      <div className="grid grid-cols-3 gap-1.5">
        {keys.map((k) => (
          <button key={k} onClick={() => send(k)} className="h-10 rounded-md bg-white font-mono text-[15px] font-semibold hover:bg-[#F0EEE7]" style={{ border: `1px solid ${BORDER}`, color: INK }}>{k}</button>
        ))}
      </div>
    </div>
  );
}

// ============================================================
// Wrap-up (new — additive, client-side; // TODO: persist disposition)
// ============================================================
const FALLBACK_DISPOSITIONS = ["Resolved", "Follow-up", "Escalated", "Sale", "No answer", "Wrong number"];

function WrapupStage({ data, onSave, onCancel }: { data: { startedAt: number; lastNumber: string; lastName: string }; onSave: (d: { disposition: string; notes: string; callback: boolean }) => Promise<void> | void; onCancel: () => void }) {
  const [disposition, setDisposition] = useState<string | null>(null);
  const [notes, setNotes] = useState("");
  const [callback, setCallback] = useState(false);
  const [saving, setSaving] = useState(false);
  const { data: tenantCodes = [] } = useTenantWrapUpCodes({ activeOnly: true });
  const dispositions = tenantCodes.length > 0
    ? tenantCodes.map((c) => ({ label: c.label, color: c.color }))
    : FALLBACK_DISPOSITIONS.map((d) => ({ label: d, color: "#16A34A" }));
  useTicker(1000);
  const wrapSec = Math.floor((Date.now() - data.startedAt) / 1000);
  const handleSave = async () => {
    if (!disposition || saving) return;
    setSaving(true);
    try {
      await onSave({ disposition, notes, callback });
    } finally {
      setSaving(false);
    }
  };
  return (
    <div className="flex-1 flex flex-col">
      <div className="px-5 py-4 border-b border-[#E7E4DB] flex items-center gap-3">
        <Avatar name={data.lastName} size={40} />
        <div className="flex-1 min-w-0">
          <div className="text-sm font-semibold truncate" style={{ color: INK }}>{data.lastName}</div>
          <div className="text-[12px] font-mono" style={{ color: MUTE }}>{formatPhone(data.lastNumber)}</div>
        </div>
        <div className="text-right">
          <div className="text-[10px] font-bold tracking-wider" style={{ color: MUTE }}>AFTER-CALL</div>
          <div className="font-mono text-xl font-semibold tabular-nums" style={{ color: "#2563EB" }}>{fmtDur(wrapSec)}</div>
        </div>
      </div>
      <div className="px-5 pt-3 flex items-center gap-2">
        <Pill kind="CTI" /><span className="text-[11px]" style={{ color: MUTE }}>after-call work — /* TODO: persist to CRM */</span>
      </div>
      <div className="px-5 pt-3">
        <div className="text-[12px] font-semibold mb-2" style={{ color: INK }}>Disposition <span className="text-[#DC2626]">*</span></div>
        <div className="flex flex-wrap gap-1.5">
          {dispositions.map((d) => {
            const active = disposition === d.label;
            return (
              <button key={d.label} onClick={() => setDisposition(d.label)} className="h-8 px-3 rounded-full text-[12px] font-semibold border transition"
                style={{ background: active ? d.color : "#FFFFFF", color: active ? "#FFFFFF" : INK, borderColor: active ? d.color : BORDER }}>
                {d.label}
              </button>
            );
          })}
        </div>
      </div>
      <div className="px-5 pt-3">
        <div className="text-[12px] font-semibold mb-2" style={{ color: INK }}>Notes</div>
        <textarea value={notes} onChange={(e) => setNotes(e.target.value)} rows={4}
          placeholder="Outcome, next steps, important details…"
          className="w-full rounded-lg px-3 py-2 text-sm bg-white outline-none resize-none"
          style={{ border: `1px solid ${BORDER}` }}
        />
      </div>
      <label className="px-5 pt-3 flex items-center gap-2 text-[12px]" style={{ color: INK }}>
        <input type="checkbox" checked={callback} onChange={(e) => setCallback(e.target.checked)} /> Schedule a callback task
      </label>
      <div className="px-5 pt-4 pb-4 mt-auto flex gap-2">
        <button onClick={onCancel} disabled={saving} className="h-12 px-4 rounded-xl border text-sm font-medium" style={{ borderColor: BORDER, color: INK, background: "white" }}>Skip</button>
        <button
          onClick={handleSave}
          disabled={!disposition || saving}
          className="flex-1 h-12 rounded-xl text-white text-[15px] font-semibold"
          style={{ background: disposition && !saving ? "#16A34A" : "#BFE6CB", cursor: disposition && !saving ? "pointer" : "not-allowed" }}
        >
          {saving ? "Saving…" : disposition ? "Save & go available" : "Pick a disposition to save"}
        </button>
      </div>
    </div>
  );
}

// ============================================================
// Right panel — screen-pop + tabbed list (live data)
// ============================================================
function ScreenPopCard({ number, name, contactId, caseId }: { number: string; name?: string; contactId?: string; caseId?: string }) {
  const findContact = useFindContactByNumber();
  const c = findContact(number);
  const sip = useSipState();
  const activeCall = useSoftphone((s) => s.activeCall);
  const caller = useInboundCaller(sip.call?.fromUri, number, activeCall?.direction);

  if (caller.kind === "internal" || caller.kind === "ai-pipeline") {
    const isAi = caller.kind === "ai-pipeline";
    return (
      <div className={`${CARD} p-4 mb-3`}>
        <div className="flex items-center gap-2 mb-2">
          <Pill kind="CTI" />
          <span className="text-[11px]" style={{ color: MUTE }}>Caller</span>
          {isAi ? (
            <span className="text-[10px] font-bold tracking-wider px-2 py-0.5 rounded-md" style={{ background: "#7C3AED22", color: "#7C3AED" }}>AI</span>
          ) : (
            <span className="text-[10px] font-bold tracking-wider px-2 py-0.5 rounded-md" style={{ background: "#1E40AF22", color: "#1E40AF" }}>INTERNAL</span>
          )}
        </div>
        <div className="flex items-center gap-3">
          <Avatar name={caller.name ?? "?"} size={40} />
          <div className="flex-1 min-w-0">
            <div className="text-sm font-semibold truncate" style={{ color: INK }}>{caller.name}</div>
            <div className="text-[12px] font-mono truncate" style={{ color: MUTE }}>Extension {caller.extension}</div>
          </div>
        </div>
        <div className="text-[12px] mt-3" style={{ color: MUTE }}>{isAi ? "AI pipeline" : "Internal call"}</div>
      </div>
    );
  }

  if (!c) {
    const displayName = name ?? caller.name;
    return (
      <div className={`${CARD} p-4 mb-3`}>
        <div className="flex items-center gap-2 mb-2"><Pill kind="CTI" /><span className="text-[11px]" style={{ color: MUTE }}>Caller</span></div>
        <div className="text-sm font-semibold" style={{ color: INK }}>{displayName ?? (caller.loading ? "Looking up…" : "Unknown number")}</div>
        <div className="text-[12px] font-mono mt-0.5" style={{ color: MUTE }}>{formatPhone(number)}</div>
        <div className="text-[12px] mt-2" style={{ color: MUTE }}>No CRM record — will be logged as new.</div>
        {(contactId || caseId) && (
          <div className="mt-3 pt-3 border-t border-[#F0EEE7] flex items-center justify-between text-[12px]">
            <div className="font-mono" style={{ color: MUTE }}>🔗 {contactId}{caseId ? ` · ${caseId}` : ""}</div>
            <span className="text-[10px] font-bold tracking-wider" style={{ color: "#16A34A" }}>FROM CRM</span>
          </div>
        )}
      </div>
    );
  }
  return (
    <div className={`${CARD} p-4 mb-3`}>
      <div className="flex items-center gap-2 mb-3"><Pill kind="CTI" /><span className="text-[11px]" style={{ color: MUTE }}>Caller</span></div>
      <div className="flex items-center gap-3">
        <Avatar name={c.name} size={40} />
        <div className="flex-1 min-w-0">
          <div className="text-sm font-semibold truncate" style={{ color: INK }}>{c.name}</div>
          {c.email && <div className="text-[12px] truncate" style={{ color: MUTE }}>{c.email}</div>}
        </div>
        <button
          onClick={() => sipActions.call(c.numbers[0]?.value ?? number, c.name).then(() => toast(`Calling ${c.name}`)).catch((e: Error) => toast.error(e.message))}
          className="h-7 px-2.5 rounded-md text-white text-[12px] font-semibold inline-flex items-center gap-1" style={{ background: "#16A34A" }}
        >
          <Phone className="h-3 w-3" /> Call
        </button>
      </div>
      <div className="mt-3 text-[12px] font-mono" style={{ color: INK }}>{formatPhone(c.numbers[0]?.value ?? number)}</div>
      {(contactId || caseId) && (
        <div className="mt-3 pt-3 border-t border-[#F0EEE7] flex items-center justify-between text-[12px]">
          <div className="font-mono" style={{ color: MUTE }}>🔗 {contactId ?? c.id}{caseId ? ` · ${caseId}` : ""}</div>
          <span className="text-[10px] font-bold tracking-wider" style={{ color: "#16A34A" }}>FROM CRM</span>
        </div>
      )}
    </div>
  );
}

// ============================================================
// Tabbed panel — live data (Users / Contacts / Recents)
// ============================================================
type Tab = "users" | "contacts" | "recents";

function RightPanel() {
  const [tab, setTab] = useState<Tab>("users");
  const [query, setQuery] = useState("");
  const { contacts } = useSoftphoneContacts();
  const history = useSoftphone((s) => s.history);
  const agents = useAgents();
  const { user } = useAuth();
  const { data: pipelines = [] } = usePipelineUsers();

  const userRows = useMemo(() => {
    const profileRows = agents
      .filter((a) => a.extension_number != null && a.user_id !== user?.id)
      .map((a) => ({
        rowId: `user:${a.user_id}`, name: agentLabel(a), ext: String(a.extension_number), team: "",
        isPipeline: false,
      }));
    const pipelineRows = pipelines.map((p) => ({
      rowId: `pipeline:${p.id}`, name: p.name, ext: p.extension, team: "AI",
      isPipeline: true,
    }));
    return [...profileRows, ...pipelineRows];
  }, [agents, user?.id, pipelines]);

  const counts = { users: userRows.length, contacts: contacts.length, recents: history.length };
  const q = query.trim().toLowerCase();
  const matches = (s?: string) => !q || (!!s && s.toLowerCase().includes(q));

  const callUser = (row: { ext: string; name: string; isPipeline?: boolean }) => {
    let target = row.ext;
    try {
      const tid = getCurrentTenantId();
      target = row.isPipeline ? `AI-${row.ext}-${tid}` : `${row.ext}-${tid}`;
    } catch { /* fall back to bare ext */ }
    sipActions.call(target, row.name).then(() => toast(`Calling ${row.name}`)).catch((e: Error) => toast.error(e.message));
  };

  return (
    <div className={`${PANEL} flex flex-col flex-1 min-h-0 overflow-hidden`}>
      <div className="px-4 pt-3 border-b border-[#E7E4DB]">
        <div className="flex items-center gap-1">
          {(["users","contacts","recents"] as const).map((id) => {
            const active = tab === id;
            const label = id[0].toUpperCase() + id.slice(1);
            return (
              <button key={id} onClick={() => setTab(id)} className="relative inline-flex items-center gap-2 px-3 py-2.5 text-sm font-medium"
                style={{ color: active ? INK : MUTE }}>
                {label}
                <CountPill n={counts[id]} active={active} />
                {active && <span className="absolute left-0 right-0 -bottom-px h-0.5 rounded-full" style={{ background: "#16A34A" }} />}
              </button>
            );
          })}
        </div>
      </div>

      <div className="px-4 pt-3">
        <div className="relative">
          <Search className="absolute top-1/2 -translate-y-1/2 left-3 h-4 w-4" style={{ color: FAINT }} />
          <input value={query} onChange={(e) => setQuery(e.target.value)} placeholder="Search names or numbers"
            className="w-full pl-9 pr-3 h-10 rounded-xl text-sm outline-none"
            style={{ background: SOFT, border: `1px solid ${BORDER}`, color: INK }}
          />
        </div>
      </div>

      <div className="flex-1 min-h-0 overflow-y-auto p-3 space-y-1.5">
        {tab === "users" && userRows.filter((r) => matches(r.name) || matches(r.ext)).map((r) => (
          <UserRow key={r.rowId} rowId={r.rowId} name={r.name} ext={r.ext} team={r.team} onCall={() => callUser(r)} />
        ))}
        {tab === "contacts" && contacts.filter((c) => matches(c.name) || matches(c.numbers[0]?.value)).map((c) => (
          <ContactRow key={c.id}
            name={c.name}
            sub={c.email ?? ""}
            phone={c.numbers[0]?.value ?? ""}
            onCall={() => sipActions.call(c.numbers[0]?.value ?? "", c.name).then(() => toast(`Calling ${c.name}`)).catch((e: Error) => toast.error(e.message))}
          />
        ))}
        {tab === "recents" && history.filter((h) => matches(h.number) || matches(contacts.find((c) => c.numbers.some((n) => n.value === h.number))?.name)).map((h) => {
          const nm = contacts.find((c) => c.numbers.some((n) => n.value === h.number))?.name ?? h.number;
          return <RecentRow key={h.id} record={h} name={nm} onRedial={() => sipActions.call(h.number, nm).then(() => toast(`Calling ${nm}`)).catch((e: Error) => toast.error(e.message))} />;
        })}
        {tab === "users" && userRows.length === 0 && <Empty label="No teammates yet." />}
        {tab === "contacts" && contacts.length === 0 && <Empty label="No contacts yet." />}
        {tab === "recents" && history.length === 0 && <Empty label="No recent calls." />}
      </div>
    </div>
  );
}

function Empty({ label }: { label: string }) {
  return <div className="text-center text-sm py-8" style={{ color: MUTE }}>{label}</div>;
}

function UserRow({ rowId, name, ext, team, onCall }: { rowId: string; name: string; ext: string; team: string; onCall: () => void }) {
  const tags = useTagsStore((s) => s.tags[rowId]) ?? [];
  const removeTag = useTagsStore((s) => s.remove);
  return (
    <div className="flex items-center gap-2 p-2 rounded-lg hover:bg-[#F8F7F2] border border-transparent hover:border-[#E7E4DB] transition">
      <Avatar name={name} size={32} />
      <div className="flex-1 min-w-0">
        <div className="text-[13px] font-semibold truncate" style={{ color: INK }}>{name}</div>
        <div className="text-[11px] truncate" style={{ color: MUTE }}>
          {team ? <>{team} · </> : null}<span className="font-mono">Ext {ext}</span>
        </div>
        <div className="flex flex-wrap gap-1 mt-1 items-center">
          {tags.map((tg) => <TagChip key={tg} tag={tg} onRemove={() => removeTag(rowId, tg)} />)}
          <TagEditor rowId={rowId} />
        </div>
      </div>
      <button onClick={onCall} className="h-8 w-8 rounded-full text-white inline-flex items-center justify-center shrink-0" style={{ background: "#16A34A" }} aria-label="Call">
        <Phone className="h-3.5 w-3.5" />
      </button>
    </div>
  );
}

function ContactRow({ name, sub, phone, onCall }: { name: string; sub: string; phone: string; onCall: () => void }) {
  return (
    <div className="flex items-center gap-2 p-2 rounded-lg hover:bg-[#F8F7F2] border border-transparent hover:border-[#E7E4DB] transition">
      <Avatar name={name} size={32} />
      <div className="flex-1 min-w-0">
        <div className="text-[13px] font-semibold truncate" style={{ color: INK }}>{name}</div>
        <div className="text-[11px] truncate" style={{ color: MUTE }}>{sub ? <>{sub} · </> : null}<span className="font-mono">{formatPhone(phone)}</span></div>
      </div>
      <button onClick={onCall} className="h-8 w-8 rounded-full text-white inline-flex items-center justify-center shrink-0" style={{ background: "#16A34A" }} aria-label="Call">
        <Phone className="h-3.5 w-3.5" />
      </button>
    </div>
  );
}

function RecentRow({ record, name, onRedial }: { record: CallRecord; name: string; onRedial: () => void }) {
  const isIn = record.type === "in";
  const Icon = isIn ? PhoneIncoming : PhoneOutgoing;
  const d = new Date(record.at);
  const time = d.toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" });
  return (
    <div className="flex items-center gap-2 p-2 rounded-lg hover:bg-[#F8F7F2] border border-transparent hover:border-[#E7E4DB] transition">
      <div className="h-8 w-8 rounded-lg inline-flex items-center justify-center shrink-0"
        style={{ background: isIn ? "#E8F6EE" : "#ECF2FE", color: isIn ? "#16A34A" : "#2563EB" }}>
        <Icon className="h-4 w-4" />
      </div>
      <div className="flex-1 min-w-0">
        <div className="text-[13px] font-semibold truncate" style={{ color: INK }}>{name}</div>
        <div className="text-[11px] truncate" style={{ color: MUTE }}>{time} · <span className="font-mono">{fmtDur(record.durationSec)}</span></div>
      </div>
      <button onClick={onRedial} className="h-8 px-2.5 rounded-md text-[12px] font-semibold border hover:bg-white" style={{ borderColor: BORDER, color: INK }}>Redial</button>
    </div>
  );
}

// ============================================================
// Modals (Transfer reuses real sipActions.transfer; CRM is UI-only)
// ============================================================
function ModalShell({ open, onClose, title, subtitle, width = 560, children }: { open: boolean; onClose: () => void; title: string; subtitle?: string; width?: number; children: React.ReactNode }) {
  if (!open) return null;
  return (
    <div className="fixed inset-0 z-40 bg-black/30 flex items-center justify-center p-4" onClick={onClose}>
      <div onClick={(e) => e.stopPropagation()} className="bg-white border border-[#E7E4DB] rounded-xl shadow-2xl w-full" style={{ maxWidth: width }}>
        <div className="px-5 py-4 border-b border-[#E7E4DB] flex items-center justify-between">
          <div>
            <div className="text-base font-semibold" style={{ color: INK }}>{title}</div>
            {subtitle && <div className="text-[12px] mt-0.5" style={{ color: MUTE }}>{subtitle}</div>}
          </div>
          <button onClick={onClose} className="h-8 w-8 inline-flex items-center justify-center rounded hover:bg-[#F8F7F2]">
            <X className="h-4 w-4" style={{ color: MUTE }} />
          </button>
        </div>
        {children}
      </div>
    </div>
  );
}

function TransferModalUI({ open, onClose }: { open: boolean; onClose: () => void }) {
  const [target, setTarget] = useState("");
  const submit = () => {
    const t = target.trim();
    if (!t) return;
    sipActions.transfer(t)
      .then(() => { toast.success("Transferred"); setTarget(""); onClose(); })
      .catch((e: Error) => toast.error(e.message));
  };
  return (
    <ModalShell open={open} onClose={onClose} title="Transfer call" subtitle="Blind transfer — type an extension or number.">
      <div className="p-5 space-y-3">
        <input
          value={target} onChange={(e) => setTarget(e.target.value)}
          onKeyDown={(e) => e.key === "Enter" && submit()}
          placeholder="Extension or number" autoFocus
          className="w-full h-11 px-3 rounded-lg outline-none font-mono"
          style={{ background: SOFT, border: `1px solid ${BORDER}`, color: INK }}
        />
        <div className="flex gap-2">
          <button onClick={onClose} className="flex-1 h-10 rounded-lg border text-sm font-medium" style={{ borderColor: BORDER, color: INK }}>Cancel</button>
          <button onClick={submit} disabled={!target.trim()} className="flex-1 h-10 rounded-lg text-white text-sm font-semibold" style={{ background: target.trim() ? "#16A34A" : "#BFE6CB" }}>
            Blind transfer
          </button>
        </div>
        <div className="text-[11px]" style={{ color: MUTE }}>Conference / warm transfer — pending backend.</div>
      </div>
    </ModalShell>
  );
}

function CrmModal({ open, onClose, onDial, onScreenpop }: { open: boolean; onClose: () => void; onDial: (n: string, name?: string) => void; onScreenpop: (p: { number: string; name?: string; contactId?: string; caseId?: string }) => void }) {
  const base = typeof window !== "undefined" ? `${window.location.origin}/softphone` : "/softphone";
  const cards = [
    { title: "Click-to-dial", desc: "Place an outbound call from your CRM record.",
      url: `${base}?action=dial&number=%2B14155550142&name=Marcus%20Delgado&contactId=CRM-10234`,
      run: () => onDial("+14155550142", "Marcus Delgado") },
    { title: "Screen-pop only", desc: "Open the matched customer without dialing.",
      url: `${base}?action=screenpop&number=%2B14155550142&contactId=CRM-10455&caseId=CS-8841`,
      run: () => onScreenpop({ number: "+14155550142", contactId: "CRM-10455", caseId: "CS-8841" }) },
  ];
  const snippet = `// From any CRM iframe / parent window:
window.postMessage({
  type: 'relay-cti',
  action: 'dial',
  number: '+14155550142',
  contactId: 'CRM-10234'
}, '*');`;
  const vars = [
    { k: "action", v: "dial · screenpop" },
    { k: "number", v: "E.164 phone number" },
    { k: "name", v: "Display name (optional)" },
    { k: "contactId", v: "Your CRM contact id" },
    { k: "caseId", v: "Your CRM case id" },
  ];
  return (
    <ModalShell open={open} onClose={onClose} title="CRM integration" subtitle="Drive the softphone from any CRM via URL or postMessage." width={680}>
      <div className="p-5 space-y-4 max-h-[75vh] overflow-y-auto">
        <div className="text-[12px]" style={{ color: MUTE }}>
          The softphone accepts commands from your CRM through <strong>URL params</strong> on load, the browser <strong>postMessage</strong> API, and the green <strong>Test</strong> button below. Inbound routing is <em>pending backend</em>.
        </div>
        <div className="space-y-3">
          {cards.map((c) => (
            <div key={c.title} className={`${CARD_SOFT} p-3`}>
              <div className="flex items-center justify-between gap-2 mb-1">
                <div className="text-[13px] font-semibold" style={{ color: INK }}>{c.title}</div>
                <button onClick={() => { onClose(); window.setTimeout(c.run, 120); }}
                  className="h-7 px-2.5 rounded-md text-white text-[12px] font-semibold" style={{ background: "#16A34A" }}>Test</button>
              </div>
              <div className="text-[11px] mb-2" style={{ color: MUTE }}>{c.desc}</div>
              <div className="flex items-center gap-2 rounded-md px-2 py-1.5" style={{ background: SOFT, border: `1px solid ${BORDER}` }}>
                <code className="font-mono text-[11px] truncate flex-1" style={{ color: INK }}>{c.url}</code>
                <button onClick={() => { navigator.clipboard.writeText(c.url); toast.success("Copied"); }} className="h-6 w-6 rounded inline-flex items-center justify-center hover:bg-white">
                  <Copy className="h-3.5 w-3.5" style={{ color: MUTE }} />
                </button>
              </div>
            </div>
          ))}
        </div>
        <div>
          <div className="text-[12px] font-semibold mb-1" style={{ color: INK }}>postMessage</div>
          <pre className="rounded-md p-3 text-[11px] leading-relaxed overflow-x-auto" style={{ background: "#1F2630", color: "#E8F6EE" }}><code>{snippet}</code></pre>
        </div>
        <div>
          <div className="text-[12px] font-semibold mb-2" style={{ color: INK }}>Recognised variables</div>
          <div className="grid grid-cols-2 gap-2">
            {vars.map((v) => (
              <div key={v.k} className="rounded-md p-2" style={{ background: SOFT, border: `1px solid ${BORDER}` }}>
                <div className="font-mono text-[11px] font-bold" style={{ color: "#2563EB" }}>{v.k}</div>
                <div className="text-[11px]" style={{ color: MUTE }}>{v.v}</div>
              </div>
            ))}
          </div>
        </div>
      </div>
    </ModalShell>
  );
}

// ============================================================
// Top-level workspace shell
// ============================================================
export function SoftphoneWorkspace() {
  const sip = useSipState();
  const activeCall = useSoftphone((s) => s.activeCall);
  const findContact = useFindContactByNumber();
  const [sheet, setSheet] = useState<SheetKey>(null);
  const [transferOpen, setTransferOpen] = useState(false);
  const [crmOpen, setCrmOpen] = useState(false);
  const [soundOn, setSoundOn] = useState(true);
  const [pop, setPop] = useState<{ number: string; name?: string; contactId?: string; caseId?: string } | null>(null);

  // Wrap-up state — additive, client-side after the call ends.
  const [wrapup, setWrapup] = useState<{ startedAt: number; lastNumber: string; lastName: string; lastRecordId: string | null; sipCallId: string | null } | null>(null);
  const prevActiveRef = useRef<typeof activeCall>(null);
  // Mirror the live SIP callId so we still have it after the call clears.
  const lastSipCallIdRef = useRef<string | null>(null);
  useEffect(() => {
    if (sip.call?.callId) lastSipCallIdRef.current = sip.call.callId;
  }, [sip.call?.callId]);
  useEffect(() => {
    // When activeCall transitions from set → null, the SIP controller already
    // pushed a history record; start wrap-up referencing the prior call.
    if (prevActiveRef.current && !activeCall) {
      const prev = prevActiveRef.current;
      const c = findContact(prev.number);
      setWrapup({ startedAt: Date.now(), lastNumber: prev.number, lastName: c?.name ?? prev.number, lastRecordId: null, sipCallId: lastSipCallIdRef.current });
    }
    prevActiveRef.current = activeCall;
  }, [activeCall, findContact]);

  // Screen-pop: mirror the live SIP call's number while ringing/active.
  useEffect(() => {
    if (activeCall) {
      const c = findContact(activeCall.number);
      setPop({ number: activeCall.number, name: c?.name });
    }
  }, [activeCall, findContact]);

  // CRM launch engine — URL params + postMessage.
  useEffect(() => {
    const apply = (action: string, params: Record<string, string | undefined>) => {
      const number = params.number ?? "";
      if (action === "dial" && number) {
        sipActions.call(number, params.name).then(() => toast(`Calling ${formatPhone(number)}`)).catch((e: Error) => toast.error(e.message));
      } else if (action === "screenpop" && number) {
        setPop({ number, name: params.name, contactId: params.contactId, caseId: params.caseId });
      }
      // action === "incoming": no-op (// TODO: real inbound routing API)
    };
    try {
      const u = new URL(window.location.href);
      const action = u.searchParams.get("action");
      if (action) {
        apply(action, {
          number: u.searchParams.get("number") ?? undefined,
          name: u.searchParams.get("name") ?? undefined,
          contactId: u.searchParams.get("contactId") ?? undefined,
          caseId: u.searchParams.get("caseId") ?? undefined,
        });
        // Clear params so a reload doesn't redial.
        ["action","number","name","contactId","caseId","queue","priority"].forEach((k) => u.searchParams.delete(k));
        window.history.replaceState({}, "", u.toString());
      }
    } catch { /* ignore */ }
    const onMsg = (ev: MessageEvent) => {
      const d = ev.data as { type?: string; action?: string; number?: string; name?: string; contactId?: string; caseId?: string };
      if (d?.type === "relay-cti" && d.action) apply(d.action, d);
    };
    window.addEventListener("message", onMsg);
    return () => window.removeEventListener("message", onMsg);
  }, []);

  const isPopup = isSoftphonePopupWindow();

  const [collapsed, setCollapsed] = useState(false);

  return (
    <div className={cn("h-full w-full overflow-hidden flex flex-col", isPopup && "popup")} style={{ background: BG, fontFamily: "Inter, ui-sans-serif, system-ui" }}>
      <style>{`
        @import url('https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700;800&family=JetBrains+Mono:wght@400;500;600&display=swap');
        .font-mono { font-family: 'JetBrains Mono', ui-monospace, SFMono-Regular, Menlo, monospace; }
      `}</style>

      <main
        className="w-full h-full grid flex-1 min-h-0 overflow-hidden"
        style={{ gridTemplateColumns: collapsed ? "64px minmax(0, 750px) 1fr" : "260px minmax(0, 750px) 1fr" }}
      >
        <LeftRail
          onSheet={(k) => setSheet(k)}
          active="dialpad"
          collapsed={collapsed}
          onToggleCollapse={() => setCollapsed((v) => !v)}
        />
        <CenterStage
          onTransfer={() => setTransferOpen(true)}
          wrapup={wrapup ? { startedAt: wrapup.startedAt, lastNumber: wrapup.lastNumber, lastName: wrapup.lastName } : null}
          onSaveWrapup={async (d) => {
            if (!wrapup) return;
            const acwSeconds = Math.max(0, Math.round((Date.now() - wrapup.startedAt) / 1000));
            try {
              await saveWrapUp({
                sipCallId: wrapup.sipCallId ?? "",
                disposition: d.disposition,
                notes: d.notes,
                callbackScheduled: d.callback,
                acwSeconds,
              });
              toast.success(`Saved: ${d.disposition}${d.callback ? " · callback scheduled" : ""}`);
              setWrapup(null);
            } catch (e) {
              toast.error((e as Error).message || "Failed to save wrap-up");
              throw e;
            }
          }}
          onCancelWrapup={() => setWrapup(null)}
          soundOn={soundOn}
          onToggleSound={() => setSoundOn((v) => !v)}
          onOpenCrm={() => setCrmOpen(true)}
        />
        <div className="flex flex-col gap-3 h-full min-h-0 overflow-hidden">
          {pop && activeCall && <ScreenPopCard number={pop.number} name={pop.name} contactId={pop.contactId} caseId={pop.caseId} />}
          <RightPanel />
        </div>
      </main>


      <TransferModalUI open={transferOpen} onClose={() => setTransferOpen(false)} />
      <CrmModal
        open={crmOpen}
        onClose={() => setCrmOpen(false)}
        onDial={(n, name) => sipActions.call(n, name).then(() => toast(`Calling ${formatPhone(n)}`)).catch((e: Error) => toast.error(e.message))}
        onScreenpop={(p) => setPop(p)}
      />
      <WorkspaceSheet open={sheet} onClose={() => setSheet(null)} theme="light" />
    </div>
  );
}
