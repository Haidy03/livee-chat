import { useEffect, useRef, useState } from "react";
import { Button } from "@/components/ui/button";
import { Textarea } from "@/components/ui/textarea";
import {
  Tabs,
  TabsList,
  TabsTrigger,
} from "@/components/ui/tabs";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSub,
  DropdownMenuSubContent,
  DropdownMenuSubTrigger,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";

import { cn } from "@/lib/utils";
import {
  BookOpen,
  ChevronDown,
  Languages,
  Mic,
  Paperclip,
  Send,
  Smile,
  Sparkles,
  Variable,
  Zap,
} from "lucide-react";
import type { Room } from "../models";
import { usePreferencesStore, useSessionStore } from "../stores";
import { sendMessageOptimistic, sendTyping, sendAttachmentOptimistic } from "../services/messageService";
import { useToast } from "@/hooks/use-toast";
import { supabase } from "@/integrations/supabase/client";
import type { MessageAttachment } from "../models";
import { useCannedResponses } from "@/features/canned-responses/useCannedResponses";
import { AiSuggestPanel } from "./ai-suggest/AiSuggestPanel";
import { markSuggestionUsed } from "@/features/ai-suggest/api";
import { getCurrentTenantId } from "@/lib/tenant";

function detectKind(mime: string): MessageAttachment["kind"] {
  if (mime.startsWith("image/")) return "image";
  if (mime.startsWith("video/")) return "video";
  if (mime.startsWith("audio/")) return "audio";
  return "document";
}

interface Props {
  room: Room;
  onResolve?: () => void;
}

export function MessageComposer({ room, onResolve }: Props) {
  const draft = usePreferencesStore((s) => s.drafts[room.id] ?? "");
  const setDraft = usePreferencesStore((s) => s.setDraft);
  const clearDraft = usePreferencesStore((s) => s.clearDraft);
  const sendOnEnter = usePreferencesStore((s) => s.sendOnEnter);
  const tenantId = useSessionStore((s) => s.tenantId);
  const agentId = useSessionStore((s) => s.agentId);
  const [mode, setMode] = useState<"reply" | "note">("reply");
  const [publicReply, setPublicReply] = useState(true);
  const ref = useRef<HTMLTextAreaElement>(null);
  const fileRef = useRef<HTMLInputElement>(null);
  const [uploading, setUploading] = useState(false);
  const { toast } = useToast();
  const { data: cannedAll, isLoading: cannedLoading, error: cannedError } = useCannedResponses();
  const cannedList = cannedAll ?? [];
  const [showAiSuggest, setShowAiSuggest] = useState(false);
  const [lastAiSuggestionId, setLastAiSuggestionId] = useState<string | null>(null);





  const handleFiles = async (files: FileList | null) => {
    if (!files || files.length === 0) return;
    setUploading(true);
    const caption = draft.trim() || undefined;
    try {
      for (const file of Array.from(files)) {
        const path = `${room.id}/${Date.now()}-${Math.random().toString(36).slice(2, 8)}-${file.name}`;
        const { error } = await supabase.storage
          .from("chat-attachments")
          .upload(path, file, { contentType: file.type, upsert: false });
        if (error) throw error;
        const { data: signed, error: signErr } = await supabase.storage
          .from("chat-attachments")
          .createSignedUrl(path, 60 * 60 * 24 * 7);
        if (signErr || !signed?.signedUrl) throw signErr ?? new Error("sign_failed");
        await sendAttachmentOptimistic({
          roomId: room.id,
          tenantId,
          senderId: agentId,
          channel: room.channel,
          attachment: {
            id: path,
            kind: detectKind(file.type || ""),
            url: signed.signedUrl,
            name: file.name,
            sizeBytes: file.size,
            mime: file.type || undefined,
          },
          caption,
        });
      }
      if (caption) clearDraft(room.id);
    } catch (e) {
      toast({ title: "Failed to send attachment", variant: "destructive" });
    } finally {
      setUploading(false);
      if (fileRef.current) fileRef.current.value = "";
    }
  };

  useEffect(() => {
    ref.current?.focus();
  }, [room.id]);

  const send = async (extra?: { resolveAfter?: boolean; pendingAfter?: boolean }) => {
    const text = draft.trim();
    if (!text) return;
    clearDraft(room.id);
    try {
      await sendMessageOptimistic({
        roomId: room.id,
        tenantId,
        senderId: agentId,
        channel: room.channel,
        text,
        internal: mode === "note",
        publicReply,
      });
      if (lastAiSuggestionId) {
        const pid = (() => { try { return getCurrentTenantId(); } catch { return null; } })();
        if (pid) void markSuggestionUsed(pid, lastAiSuggestionId, text).catch(() => {});
        setLastAiSuggestionId(null);
      }
      if (extra?.resolveAfter) onResolve?.();
      if (extra?.pendingAfter) toast({ title: "Room set to pending" });
    } catch (e) {
      toast({ title: "Failed to send", variant: "destructive" });
    }
  };

  const onKey = (e: React.KeyboardEvent<HTMLTextAreaElement>) => {
    if (e.key === "Enter" && !e.shiftKey && sendOnEnter) {
      e.preventDefault();
      void send();
    }
  };

  const charLimit = room.channel === "twitter_dm" ? 10000 : room.channel === "twitter_mention" ? 280 : undefined;

  return (
    <div className={cn(
      "flex flex-col gap-2 border-t border-app-border bg-card p-3",
      mode === "note" && "bg-amber-50/40 dark:bg-amber-950/10",
    )}>
      <div className="flex items-center justify-between">
        <Tabs value={mode} onValueChange={(v) => setMode(v as any)}>
          <TabsList className="h-7">
            <TabsTrigger value="reply" className="h-6 px-2 text-xs">Reply to Customer</TabsTrigger>
            <TabsTrigger value="note" className="h-6 px-2 text-xs">Internal Note</TabsTrigger>
          </TabsList>
        </Tabs>
        {mode === "reply" && (room.channel === "facebook_comment" || room.channel === "instagram_comment" || room.channel === "twitter_mention") && (
          <div className="flex items-center gap-1 text-[11px]">
            <button
              onClick={() => setPublicReply(true)}
              className={cn("rounded px-2 py-0.5", publicReply ? "bg-primary/10 text-primary font-semibold" : "text-muted-foreground")}
            >
              Public reply
            </button>
            <button
              onClick={() => setPublicReply(false)}
              className={cn("rounded px-2 py-0.5", !publicReply ? "bg-primary/10 text-primary font-semibold" : "text-muted-foreground")}
            >
              Private message
            </button>
          </div>
        )}
      </div>

      {showAiSuggest && (
        <AiSuggestPanel
          roomId={room.id}
          currentDraft={draft}
          onClose={() => setShowAiSuggest(false)}
          onUseReply={(text, suggestionId) => {
            setDraft(room.id, text);
            setLastAiSuggestionId(suggestionId);
            ref.current?.focus();
          }}
        />
      )}


      <Textarea
        ref={ref}
        value={draft}
        onChange={(e) => {
          setDraft(room.id, e.target.value);
          if (mode === "reply") void sendTyping(room.id, e.target.value.length > 0);
        }}
        onBlur={() => {
          if (mode === "reply") void sendTyping(room.id, false);
        }}
        onKeyDown={onKey}
        placeholder={mode === "note" ? "Add an internal note (not visible to the customer)…" : "Write a reply…"}
        className="min-h-[72px] resize-y text-sm"
        dir="auto"
        maxLength={charLimit}
        aria-label="Message"
      />

      <div className="flex flex-wrap items-center gap-1">
        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <Button variant="ghost" size="sm" className="h-7 px-2 text-xs">
              <Zap className="me-1 h-3 w-3" /> Canned
            </Button>
          </DropdownMenuTrigger>
          <DropdownMenuContent>
            <DropdownMenuLabel>Canned responses</DropdownMenuLabel>
            {cannedLoading ? (
              <DropdownMenuItem disabled>Loading…</DropdownMenuItem>
            ) : cannedError ? (
              <DropdownMenuItem disabled>Failed to load</DropdownMenuItem>
            ) : cannedList.length === 0 ? (
              <DropdownMenuItem disabled>No canned responses</DropdownMenuItem>
            ) : (
              cannedList.map((c) => {
                const msgs = c.messages ?? [];
                if (msgs.length <= 1) {
                  const only = msgs[0] ?? "";
                  return (
                    <DropdownMenuItem
                      key={c._id}
                      disabled={!only}
                      onClick={() => only && setDraft(room.id, only)}
                    >
                      <span className="truncate">{c.title}</span>
                    </DropdownMenuItem>
                  );
                }
                return (
                  <DropdownMenuSub key={c._id}>
                    <DropdownMenuSubTrigger>
                      <span className="truncate">{c.title}</span>
                      <span className="ms-2 text-[10px] text-muted-foreground">{msgs.length}</span>
                    </DropdownMenuSubTrigger>
                    <DropdownMenuSubContent className="max-w-sm">
                      {msgs.map((m, i) => (
                        <DropdownMenuItem key={i} onClick={() => setDraft(room.id, m)}>
                          <span className="line-clamp-2 whitespace-normal text-xs">{m}</span>
                        </DropdownMenuItem>
                      ))}
                    </DropdownMenuSubContent>
                  </DropdownMenuSub>
                );
              })
            )}

          </DropdownMenuContent>
        </DropdownMenu>
        <Button
          variant={showAiSuggest ? "secondary" : "ghost"}
          size="sm"
          className="h-7 px-2 text-xs"
          onClick={() => setShowAiSuggest((v) => !v)}
        >
          <Sparkles className="me-1 h-3 w-3" /> AI suggest
        </Button>
        <Button variant="ghost" size="sm" className="h-7 px-2 text-xs">
          <BookOpen className="me-1 h-3 w-3" /> Knowledge
        </Button>
        <Button variant="ghost" size="sm" className="h-7 px-2 text-xs">
          <Languages className="me-1 h-3 w-3" /> Translate
        </Button>
        <Button variant="ghost" size="sm" className="h-7 px-2 text-xs">
          <Variable className="me-1 h-3 w-3" /> Variables
        </Button>
        <span className="mx-1 h-4 w-px bg-app-border" />
        <input
          ref={fileRef}
          type="file"
          multiple
          className="hidden"
          onChange={(e) => void handleFiles(e.target.files)}
        />
        <Button
          variant="ghost"
          size="icon"
          className="h-7 w-7"
          aria-label="Attach"
          disabled={uploading}
          onClick={() => fileRef.current?.click()}
        >
          <Paperclip className="h-3.5 w-3.5" />
        </Button>
        <Button variant="ghost" size="icon" className="h-7 w-7" aria-label="Emoji"><Smile className="h-3.5 w-3.5" /></Button>
        <Button variant="ghost" size="icon" className="h-7 w-7" aria-label="Voice note"><Mic className="h-3.5 w-3.5" /></Button>

        <div className="ms-auto flex items-center gap-1">
          {charLimit && (
            <span className="text-[10px] tabular-nums text-muted-foreground">
              {draft.length}/{charLimit}
            </span>
          )}
          <Button size="sm" className="h-7" onClick={() => void send()} disabled={!draft.trim()}>
            <Send className="me-1 h-3 w-3" /> Send
          </Button>
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button size="sm" className="h-7 px-1.5" variant="secondary" aria-label="More send options">
                <ChevronDown className="h-3 w-3" />
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end">
              <DropdownMenuItem onClick={() => void send({ resolveAfter: true })}>Send & Resolve</DropdownMenuItem>
              <DropdownMenuItem onClick={() => void send({ pendingAfter: true })}>Send & Set Pending</DropdownMenuItem>
              <DropdownMenuItem onClick={() => clearDraft(room.id)}>Cancel draft</DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>
        </div>
      </div>
    </div>
  );
}
