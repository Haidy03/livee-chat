import { useEffect } from "react";
import { Navigate, useParams } from "react-router-dom";
import { Bell } from "lucide-react";
import { Button } from "@/components/ui/button";

import { useDigitalWorkspaceBootstrap } from "../hooks/useDigitalWorkspaceBootstrap";
import { useMyAvailableChannels } from "../hooks/useMyAvailableChannels";
import { useLiveChatTimers } from "../hooks/useLiveChatTimers";
import { useAgentConfigStore, useAgentStore, useRoomStore, useSessionStore } from "../stores";
import { ConnectionStatusPill } from "../shared/ConnectionStatusPill";
import { AgentPresenceSelector } from "../shared/AgentPresenceSelector";
import { ChannelAvailabilityToggle } from "../shared/ChannelAvailabilityToggle";
import { LiveChatWorkspace } from "../workspaces/LiveChatWorkspace";
import { PhoneWorkspace } from "../workspaces/PhoneWorkspace";
import { EmailWorkspace } from "../workspaces/EmailWorkspace";
import { SocialWorkspace } from "../workspaces/SocialWorkspace";
import type { ChannelGroup } from "../models";

const SLUG_TO_CHANNEL: Record<string, ChannelGroup> = {
  phone: "phone",
  livechat: "chat",
  email: "email",
  social: "social",
};

const CHANNEL_TO_SLUG: Record<ChannelGroup, string> = {
  phone: "phone",
  chat: "livechat",
  email: "email",
  social: "social",
};


export default function DigitalWorkspacePage() {
  useDigitalWorkspaceBootstrap();
  useLiveChatTimers();
  const me = useAgentStore((s) => s.byId[s.meId]);
  const setActiveChannel = useAgentStore((s) => s.setActiveChannel);
  const { channel: channelSlug } = useParams<{ channel: string }>();
  const allowed = useMyAvailableChannels();

  const activeChannel: ChannelGroup | undefined = channelSlug
    ? SLUG_TO_CHANNEL[channelSlug]
    : undefined;

  // URL -> store sync. URL is the source of truth for the active channel.
  useEffect(() => {
    if (activeChannel && me && !me.channelAvailability[activeChannel]) {
      setActiveChannel(activeChannel);
    }
  }, [activeChannel, me, setActiveChannel]);

  if (channelSlug && !activeChannel) {
    return <Navigate to="/agent/digital/livechat" replace />;
  }

  if (allowed && allowed.length > 0 && activeChannel && !allowed.includes(activeChannel)) {
    const fallback = CHANNEL_TO_SLUG[allowed[0]];
    if (fallback !== channelSlug) {
      return <Navigate to={`/agent/digital/${fallback}`} replace />;
    }
  }

  return (
    <div className="flex h-[calc(100vh-3.5rem)] min-h-0 w-full flex-col bg-background text-foreground">
      {/* Workspace header */}
      <div className="flex flex-wrap items-center gap-2 border-b border-app-border bg-card px-3 py-1.5">
        <h1 className="text-sm font-semibold">Digital Workspace</h1>
        {activeChannel === "chat" && me && (
          <span className="rounded-md bg-muted px-1.5 py-0.5 text-[11px] tabular-nums">
            Capacity {me.capacity.current}/{me.capacity.max}
          </span>
        )}
        {activeChannel === "chat" && <ChatSlotsBadge />}
        {activeChannel === "chat" && <ConnectionStatusPill />}
        <div className="mx-auto flex items-center justify-center">
          <ChannelAvailabilityToggle />
        </div>
        <div className="ms-auto flex items-center gap-1">
          <Button variant="ghost" size="icon" className="h-7 w-7" aria-label="Notifications">
            <Bell className="h-3.5 w-3.5" />
          </Button>
          <AgentPresenceSelector />
        </div>
      </div>

      {/* Active channel workspace */}
      {activeChannel === "phone" && <PhoneWorkspace />}
      {activeChannel === "chat" && <LiveChatWorkspace />}
      {activeChannel === "email" && <EmailWorkspace />}
      {activeChannel === "social" && <SocialWorkspace />}
    </div>
  );
}

function ChatSlotsBadge() {
  const chatSlots = useAgentConfigStore((s) => s.chatSlots);
  const loaded = useAgentConfigStore((s) => s.loaded);
  const meId = useSessionStore((s) => s.agentId || "agent-me");
  const activeCount = useRoomStore((s) => {
    let n = 0;
    for (const id of s.order) {
      const r = s.byId[id];
      if (!r) continue;
      if (r.assignedAgentId === meId && !["resolved", "closed", "spam"].includes(r.status)) n++;
    }
    return n;
  });
  if (!loaded || chatSlots <= 0) return null;
  const full = activeCount >= chatSlots;
  return (
    <span
      className={
        "rounded-md px-1.5 py-0.5 text-[11px] tabular-nums " +
        (full ? "bg-destructive text-destructive-foreground font-semibold" : "bg-muted")
      }
      title={full ? "Maximum chat slots reached" : undefined}
    >
      Chat slots {activeCount}/{chatSlots}
      {full ? " · full" : ""}
    </span>
  );
}
