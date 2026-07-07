# Show Timer On Every Conversation Row

Currently `ConversationTimers` is already mounted in each `RoomListItem` (line 148), but it hides itself when:
- config isn't loaded, OR
- all spans are 0 (no client/agent inactivity yet, no offline).

Result: users see no timer on rows where the conversation is currently healthy. We'll make a timer render on **every** active conversation row.

## Changes

### 1. `src/features/digital-workspace/inbox/ConversationTimers.tsx`
- Remove the `if (!anything) return null` early return.
- Keep the `!loaded || !span` guard, but render a neutral "0:00" idle timer when no span field is active.
- Show the **most relevant** timer as the primary chip:
  - Priority: `offlineSpan` (red) → `agentMessageSpan` (client-inactive) → `utteranceSpan` (agent-inactive) → idle 0:00.
- Keep the other active chips as secondary if present.
- Idle style: `text-muted-foreground` with a small clock icon (`Clock` from lucide-react).

### 2. `src/features/digital-workspace/hooks/useLiveChatTimers.ts`
- Ensure every active room gets an entry in `nextSpans` even when all computed spans are 0 (currently it does — confirmed lines 229-235). No functional change; just verify each active room key is always present so `ConversationTimers` renders.

### 3. `src/features/digital-workspace/inbox/RoomListItem.tsx`
- No structural change; timer stays in the existing slot at line 148.
- Minor: tighten spacing so the always-visible timer doesn't crowd the tags row (small `mt-0.5`).

## Out of scope
- No changes to timer math, thresholds, store shape, or backend wiring.
- Offered rows (`status === "offered"`) still won't show a timer (they aren't "active" for the agent yet) — call out in doc but keep behavior.

## Verification
- Open `/agent/digital/livechat`, confirm each conversation row shows a timer chip (idle "0:00" or live count).
- Confirm color transitions (muted → emerald → destructive) as thresholds are approached.
- Confirm no React #185 regression.
