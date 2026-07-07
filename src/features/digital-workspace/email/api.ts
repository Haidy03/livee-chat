import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { api, getAccessToken, getApiOrigin } from "@/lib/apiClient";

export interface EmailThread {
  id: string;
  subject: string;
  counterpartName: string;
  counterpartEmail: string;
  mailbox: string;
  lastMessageAt: string;
  lastMessageSnippet: string;
  lastMessageDirection: "inbound" | "outbound";
  lastMessageHasAttachments: boolean;
  messageCount: number;
  unreadCount: number;
  status: "open" | "resolved" | "archived";
  assignedTo?: string | null;
  snoozedUntil?: string | null;
  starred: boolean;
}

export interface EmailMessage {
  id: string;
  threadId: string;
  direction: "inbound" | "outbound";
  fromName: string;
  fromEmail: string;
  toEmail: string;
  ccEmails: string[];
  subject: string;
  textBody: string;
  htmlBody?: string | null;
  attachmentNames: string[];
  sentAt: string;
  agentId?: string | null;
  agentName?: string | null;
}

export interface EmailMailbox {
  address: string;
  displayName: string;
}

export interface AttachmentUpload {
  fileName: string;
  contentType: string;
  base64Content: string;
}

export interface ComposePayload {
  mailbox?: string;
  to: string;
  toName?: string;
  cc: string[];
  subject: string;
  body: string;
  htmlBody?: string;
  attachments: AttachmentUpload[];
}

export interface ReplyPayload {
  body: string;
  htmlBody?: string;
  cc: string[];
  attachments: AttachmentUpload[];
}

/** Reads a picked file into the base64 payload the reply/compose endpoints accept. */
export function fileToAttachment(file: File): Promise<AttachmentUpload> {
  return new Promise((resolve, reject) => {
    const reader = new FileReader();
    reader.onerror = () => reject(new Error(`Could not read ${file.name}`));
    reader.onload = () => {
      const dataUrl = reader.result as string;
      resolve({
        fileName: file.name,
        contentType: file.type || "application/octet-stream",
        base64Content: dataUrl.slice(dataUrl.indexOf(",") + 1),
      });
    };
    reader.readAsDataURL(file);
  });
}

/* -------------------- folders -------------------- */

export type EmailFolder = "inbox" | "snoozed" | "sent" | "archived" | "resolved";

export function folderOf(t: EmailThread): Exclude<EmailFolder, "sent"> {
  if (t.status === "archived") return "archived";
  if (t.status === "resolved") return "resolved";
  if (t.snoozedUntil && new Date(t.snoozedUntil) > new Date()) return "snoozed";
  return "inbox";
}

export function threadsInFolder(threads: EmailThread[], folder: EmailFolder): EmailThread[] {
  // "Sent" is a view of conversations awaiting the customer (last message was ours).
  if (folder === "sent") return threads.filter((t) => t.lastMessageDirection === "outbound");
  return threads.filter((t) => folderOf(t) === folder);
}

/* -------------------- queries -------------------- */

const THREADS_KEY = ["email", "threads"];

export function useEmailThreads() {
  return useQuery({
    queryKey: THREADS_KEY,
    queryFn: () => api.get<EmailThread[]>("/email/threads"),
    refetchInterval: 15000,
  });
}

export function useEmailMailboxes() {
  return useQuery({
    queryKey: ["email", "mailboxes"],
    queryFn: () => api.get<EmailMailbox[]>("/email/mailboxes"),
    staleTime: 5 * 60 * 1000,
  });
}

export function useEmailMessages(threadId: string | null) {
  return useQuery({
    queryKey: ["email", "messages", threadId],
    queryFn: () => api.get<EmailMessage[]>(`/email/threads/${threadId}/messages`),
    enabled: !!threadId,
    refetchInterval: 15000,
  });
}

export function useSendReply(threadId: string | null) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (payload: ReplyPayload) =>
      api.post<EmailMessage>(`/email/threads/${threadId}/reply`, payload),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["email", "messages", threadId] });
      qc.invalidateQueries({ queryKey: THREADS_KEY });
    },
  });
}

export function useComposeEmail() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (payload: ComposePayload) => api.post<EmailThread>("/email/compose", payload),
    onSuccess: () => qc.invalidateQueries({ queryKey: THREADS_KEY }),
  });
}

/**
 * Thread action that patches the cached thread immediately (optimistic), rolls back on
 * error, and reconciles with the server afterwards — so Resolve/Archive/etc. feel instant.
 */
function threadAction(path: (id: string) => string, patch: (t: EmailThread) => Partial<EmailThread>) {
  return function useAction() {
    const qc = useQueryClient();
    return useMutation({
      mutationFn: (threadId: string) => api.post(path(threadId)),
      onMutate: async (threadId: string) => {
        await qc.cancelQueries({ queryKey: THREADS_KEY });
        const prev = qc.getQueryData<EmailThread[]>(THREADS_KEY);
        qc.setQueryData<EmailThread[]>(THREADS_KEY, (old) =>
          (old ?? []).map((t) => (t.id === threadId ? { ...t, ...patch(t) } : t)));
        return { prev };
      },
      onError: (_e, _v, ctx) => {
        if (ctx?.prev) qc.setQueryData(THREADS_KEY, ctx.prev);
      },
      onSettled: () => qc.invalidateQueries({ queryKey: THREADS_KEY }),
    });
  };
}

export const useMarkThreadRead = threadAction((id) => `/email/threads/${id}/read`, () => ({ unreadCount: 0 }));
export const useMarkThreadUnread = threadAction((id) => `/email/threads/${id}/unread`, () => ({ unreadCount: 1 }));
export const useResolveThread = threadAction((id) => `/email/threads/${id}/resolve`, () => ({ status: "resolved", unreadCount: 0 }));
export const useReopenThread = threadAction((id) => `/email/threads/${id}/reopen`, () => ({ status: "open", snoozedUntil: null }));
export const useArchiveThread = threadAction((id) => `/email/threads/${id}/archive`, () => ({ status: "archived" }));

export function useStarThread() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ threadId, starred }: { threadId: string; starred: boolean }) =>
      api.post(`/email/threads/${threadId}/star`, { starred }),
    onMutate: async ({ threadId, starred }) => {
      await qc.cancelQueries({ queryKey: THREADS_KEY });
      const prev = qc.getQueryData<EmailThread[]>(THREADS_KEY);
      qc.setQueryData<EmailThread[]>(THREADS_KEY, (old) =>
        (old ?? []).map((t) => (t.id === threadId ? { ...t, starred } : t)));
      return { prev };
    },
    onError: (_e, _v, ctx) => {
      if (ctx?.prev) qc.setQueryData(THREADS_KEY, ctx.prev);
    },
    onSettled: () => qc.invalidateQueries({ queryKey: THREADS_KEY }),
  });
}

export function useSnoozeThread() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ threadId, until }: { threadId: string; until: string | null }) =>
      api.post(`/email/threads/${threadId}/snooze`, { until }),
    onMutate: async ({ threadId, until }) => {
      await qc.cancelQueries({ queryKey: THREADS_KEY });
      const prev = qc.getQueryData<EmailThread[]>(THREADS_KEY);
      qc.setQueryData<EmailThread[]>(THREADS_KEY, (old) =>
        (old ?? []).map((t) => (t.id === threadId ? { ...t, snoozedUntil: until } : t)));
      return { prev };
    },
    onError: (_e, _v, ctx) => {
      if (ctx?.prev) qc.setQueryData(THREADS_KEY, ctx.prev);
    },
    onSettled: () => qc.invalidateQueries({ queryKey: THREADS_KEY }),
  });
}

export function useEmailSignature() {
  return useQuery({
    queryKey: ["email", "signature"],
    queryFn: () => api.get<{ html: string }>("/email/signature"),
    staleTime: 5 * 60 * 1000,
  });
}

export function useSaveEmailSignature() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (html: string) => api.put("/email/signature", { html }),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["email", "signature"] }),
  });
}

/* -------------------- reply drafts (local, per thread) -------------------- */

const DRAFT_PREFIX = "vf-email-draft:";

export function loadDraft(threadId: string): string | null {
  try { return localStorage.getItem(DRAFT_PREFIX + threadId); } catch { return null; }
}

export function saveDraft(threadId: string, html: string, text: string): void {
  try {
    if (text.trim()) localStorage.setItem(DRAFT_PREFIX + threadId, html);
    else localStorage.removeItem(DRAFT_PREFIX + threadId);
  } catch { /* storage full/unavailable — drafts are best-effort */ }
}

export function clearDraft(threadId: string): void {
  try { localStorage.removeItem(DRAFT_PREFIX + threadId); } catch { /* ignore */ }
}

export function hasDraft(threadId: string): boolean {
  return loadDraft(threadId) !== null;
}

/** Downloads an inbound attachment (streamed from the mailbox over IMAP by the backend). */
export async function downloadAttachment(messageId: string, index: number, fileName: string): Promise<void> {
  const res = await fetch(`${getApiOrigin()}/api/v1/email/messages/${messageId}/attachments/${index}`, {
    headers: { Authorization: `Bearer ${getAccessToken() ?? ""}` },
  });
  if (!res.ok) throw new Error("Could not download attachment — it may no longer exist in the mailbox.");
  const blob = await res.blob();
  const url = URL.createObjectURL(blob);
  const a = document.createElement("a");
  a.href = url;
  a.download = fileName;
  a.click();
  URL.revokeObjectURL(url);
}

/* -------------------- display helpers -------------------- */

/** "20:58" for today, "Yesterday", else "Jun 24". */
export function formatEmailTime(iso: string): string {
  const d = new Date(iso);
  const now = new Date();
  if (d.toDateString() === now.toDateString())
    return d.toLocaleTimeString([], { hour: "2-digit", minute: "2-digit", hour12: false });
  const yesterday = new Date(now);
  yesterday.setDate(now.getDate() - 1);
  if (d.toDateString() === yesterday.toDateString()) return "Yesterday";
  return d.toLocaleDateString([], { month: "short", day: "numeric" });
}

export function formatSnoozeUntil(iso: string): string {
  const d = new Date(iso);
  return d.toLocaleString([], { month: "short", day: "numeric", hour: "2-digit", minute: "2-digit", hour12: false });
}

export function initialsOf(name: string): string {
  const parts = name.trim().split(/\s+/).filter(Boolean);
  if (parts.length === 0) return "?";
  if (parts.length === 1) return parts[0].slice(0, 2).toUpperCase();
  return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase();
}

const AVATAR_COLORS = ["teal", "blue", "purple", "amber", "rose", "slate", "green"] as const;
export type EmailAvatarColor = (typeof AVATAR_COLORS)[number];

export function avatarColorOf(key: string): EmailAvatarColor {
  let h = 0;
  for (let i = 0; i < key.length; i++) h = (h * 31 + key.charCodeAt(i)) | 0;
  return AVATAR_COLORS[Math.abs(h) % AVATAR_COLORS.length];
}

/** Very small HTML → text fallback for snippets when only HTML was composed. */
export function htmlToText(html: string): string {
  const el = document.createElement("div");
  el.innerHTML = html;
  return el.innerText.trim();
}
