# Email Channel — State, Gaps & Roadmap

_Last updated: 2026-07-07. Owner: Digital Workspace → Email._

## 1. Where we are today

**Working end-to-end:** IMAP polling of one or more Gmail mailboxes (30s), RFC-correct
threading, HTML rendering (sandboxed), rich-text replies + composes with Cc and
attachments (both directions, no storage — files stream from Gmail by IMAP UID),
folders (Inbox / Snoozed / Sent / Archived / Resolved), snooze presets, per-agent
signatures, canned-response templates, per-thread local drafts, search/filter/sort,
multi-mailbox switcher, optimistic UI on every thread action, transactional email
(password reset / welcome) on a separate path.

**Architecture:** `EmailInboundWorker` (IMAP → Mongo `email_threads`/`email_messages`)
→ REST `/api/v1/email/*` → React Query UI polling at 15s. Sending via MailKit SMTP with
`In-Reply-To`/`References`.

## 2. This iteration (implemented now)

| # | Item | Notes |
|---|------|-------|
| 1 | **Pop-out reply modal** | Expand icon on the reply card opens a large modal composer (Gmail's "full screen" compose); draft carries over both ways. |
| 2 | **Collapse the whole reply card** | Gmail-style: thread shows a slim "Reply" bar; the full composer card only opens on click (auto-opens when a draft exists). Hide button collapses the entire card, preserving the draft. |
| 3 | **Folder bar redesign** | Chips wrap onto multiple lines — every folder always visible, nothing hidden behind horizontal scroll. |
| 4 | **Better template picker** | Searchable popover with title + content preview instead of a cramped menu; available in both the inline composer and the pop-out modal. |
| 5 | **Starred / favourites** | Star from the list or thread header (optimistic), `starred` persisted server-side, Starred view in the filter menu. |
| 6 | **Keyboard shortcuts** | `j`/`k` next/previous conversation, `e` archive, `s` star — ignored while typing. |

## 3. Roadmap — everything else, by layer

### Contacts
- Link `counterpartEmail` to the existing Contacts module (`IContactRepository`) — show
  known-contact card in the right panel, "create contact" for unknown senders.
- Cross-channel history: other conversations (calls, chats, emails) for the same customer.
- Auto-complete To/Cc from Contacts when composing.

### Folders & organisation
- Custom labels/tags on threads (colored, filterable) — mirrors Gmail labels.
- Spam view (ingest `[Gmail]/Spam` via `ImapFolders` config — supported; needs a UI folder).
- Pinned conversations (separate from starred).
- Server-side pagination + virtualized list (currently capped at latest 200 threads).

### Emails / reading
- **Forward** a message (quote body + re-attach originals fetched over IMAP).
- Reply-all (respect original Cc list by default).
- Server-side full-text search across message bodies (Mongo text index).
- Print / export a thread (.eml / PDF via existing report renderer).
- Inline image (`cid:`) rendering in HTML bodies.
- Collapse quoted text ("show trimmed content").

### Management / teamwork
- Assignment & transfer: assign a conversation to an agent/queue; "Mine / Unassigned"
  views. The LiveChat routing engine is the natural integration point.
- Internal notes on a thread (not emailed), @mentions.
- Collision detection ("Yasmin is replying…").
- SLA timers per queue with breach indicators (fields exist in the old mock UI).
- Audit trail via existing `IEditLogService`.

### Writing
- Template variables (`{{customer.name}}`, `{{agent.name}}`) filled at insert time.
- Per-mailbox signature (in addition to per-agent).
- Schedule send / undo send (delay queue before SMTP).
- AI-assisted drafting via the existing AiGateway (suggest reply from thread context).
- Inline image paste + drag-and-drop attachments.

### Realtime & notifications
- **Decision: no SignalR for email.** Polling stays (simple, reliable, cheap at this
  scale). If faster arrival is ever needed: (a) drop the UI poll to 5–10s only while the
  Email tab is focused, (b) or a single lightweight SSE stream that just says "something
  changed — refetch". IMAP IDLE server-side can replace the 30s mailbox poll independently.
- Browser notifications + unread badge on the workspace Email tab (works fine with polling).
- New-mail sound toggle.

### Accounts / linking other clients
- Generic IMAP/SMTP providers (Outlook 365, Zoho, etc.) — config already provider-agnostic;
  needs per-provider docs + OAuth2 (XOAUTH2) support for tenants that ban app passwords.
- Per-agent personal mailbox connect (OAuth "Sign in with Google") vs shared inboxes.
- Alias / send-as addresses per mailbox.

### Platform
- i18n (email UI is English-only; app is en/ar).
- Mobile/responsive pass for the three-column layout.
- Retention/archival policy for `email_messages`.
- Rate limiting + retry/backoff on Gmail IMAP errors (per-mailbox health indicator).

## 4. Suggested order after this iteration
1. Forward + reply-all (completes core mail actions).
2. Focus-aware faster polling + browser notifications (no SignalR — see §Realtime).
3. Assignment/queues + internal notes (teamwork).
4. Contacts linking (right panel becomes truly useful).
5. Labels, server search, pagination (scale).
6. OAuth2 / other providers (reach).
