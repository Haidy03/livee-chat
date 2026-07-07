import { useState } from "react";
import { Voicemail as VoicemailIcon, Phone, Check, Loader2, PhoneOutgoing } from "lucide-react";
import { Button } from "@/components/ui/button";
import { sipActions } from "../sip/useSipController";
import {
  useVoicemails,
  useClaimVoicemail,
  useResolveVoicemail,
  type Voicemail,
} from "../voicemail/api";

function fmtDuration(sec: number): string {
  const m = Math.floor(sec / 60);
  const s = sec % 60;
  return `${m}:${s.toString().padStart(2, "0")}`;
}

function fmtWhen(iso: string): string {
  const d = new Date(iso);
  if (Number.isNaN(d.getTime())) return "";
  return d.toLocaleString(undefined, { month: "short", day: "numeric", hour: "2-digit", minute: "2-digit" });
}

function ownerLabel(v: Voicemail): string {
  const t = v.ownerType === "queue" ? "Queue" : v.ownerType === "group" ? "Group" : v.ownerType === "agent" ? "Direct" : "Flow";
  return t;
}

export function VoicemailView() {
  const { data: voicemails = [], isLoading } = useVoicemails();
  const claim = useClaimVoicemail();
  const resolve = useResolveVoicemail();
  const [expanded, setExpanded] = useState<string | null>(null);

  if (isLoading) {
    return (
      <div className="flex-1 flex items-center justify-center text-muted-foreground">
        <Loader2 className="h-6 w-6 animate-spin" />
      </div>
    );
  }

  if (voicemails.length === 0) {
    return (
      <div className="flex-1 flex flex-col items-center justify-center text-center gap-4 p-8">
        <div className="h-24 w-24 rounded-full bg-primary/10 flex items-center justify-center">
          <VoicemailIcon className="h-12 w-12 text-primary" />
        </div>
        <div className="text-lg font-semibold">No voicemails</div>
        <div className="text-sm text-muted-foreground max-w-sm">
          Messages left for your queues will appear here.
        </div>
      </div>
    );
  }

  return (
    <div className="flex-1 overflow-y-auto divide-y">
      {voicemails.map((v) => {
        const isOpen = expanded === v.id;
        const number = v.callerIdNumber || "Unknown";
        return (
          <div key={v.id} className="p-3 flex flex-col gap-2">
            <div className="flex items-start gap-3">
              {v.status === "new" && <span className="mt-2 h-2 w-2 shrink-0 rounded-full bg-primary" aria-label="unread" />}
              <button
                className="flex-1 min-w-0 text-left"
                onClick={() => setExpanded(isOpen ? null : v.id)}
              >
                <div className="flex items-center gap-2">
                  <span className="font-medium truncate">{number}</span>
                  <span className="text-[10px] uppercase tracking-wide rounded bg-muted px-1.5 py-0.5 text-muted-foreground">
                    {ownerLabel(v)}
                  </span>
                </div>
                <div className="text-xs text-muted-foreground">
                  {fmtWhen(v.timestamp)} · {fmtDuration(v.durationSeconds)}
                  {v.claimedBy && v.status !== "new" ? ` · Claimed` : ""}
                </div>
                {v.transcript && (
                  <div className={`text-xs text-muted-foreground mt-1 ${isOpen ? "" : "line-clamp-2"}`}>
                    {v.transcript}
                  </div>
                )}
              </button>
            </div>

            <div className="flex items-center gap-2 pl-5">
              <Button size="sm" variant="secondary" onClick={() => sipActions.call(number)}>
                <PhoneOutgoing className="h-3.5 w-3.5 mr-1" /> Call back
              </Button>
              {v.status === "new" && (
                <Button
                  size="sm"
                  variant="outline"
                  disabled={claim.isPending}
                  onClick={() => claim.mutate(v.id)}
                >
                  <Phone className="h-3.5 w-3.5 mr-1" /> Claim
                </Button>
              )}
              {v.status !== "done" && (
                <Button
                  size="sm"
                  variant="ghost"
                  disabled={resolve.isPending}
                  onClick={() => resolve.mutate(v.id)}
                >
                  <Check className="h-3.5 w-3.5 mr-1" /> Done
                </Button>
              )}
            </div>

            {isOpen && v.summary && (
              <div className="pl-5 text-xs">
                <div className="font-medium text-muted-foreground">Summary</div>
                <div className="text-muted-foreground">{v.summary}</div>
                {v.sentiment && <div className="mt-1 text-muted-foreground">Sentiment: {v.sentiment}</div>}
              </div>
            )}
          </div>
        );
      })}
    </div>
  );
}
