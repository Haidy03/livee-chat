import { Mail, MessageSquare, Megaphone, Phone } from "lucide-react";
import { useNavigate, useParams } from "react-router-dom";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";
import { useAgentStore } from "../stores";
import { useMyAvailableChannels } from "../hooks/useMyAvailableChannels";
import type { ChannelGroup } from "../models";

const CHANNELS: { id: ChannelGroup; slug: string; label: string; icon: typeof Phone }[] = [
  { id: "phone", slug: "phone", label: "Phone", icon: Phone },
  { id: "chat", slug: "livechat", label: "Live chat", icon: MessageSquare },
  { id: "email", slug: "email", label: "Email", icon: Mail },
  { id: "social", slug: "social", label: "Social", icon: Megaphone },
];

export function ChannelAvailabilityToggle() {
  const me = useAgentStore((s) => s.byId[s.meId]);
  const navigate = useNavigate();
  const { channel: currentSlug } = useParams<{ channel: string }>();
  const allowed = useMyAvailableChannels();

  if (!me) return null;

  const visible = allowed ? CHANNELS.filter((c) => allowed.includes(c.id)) : CHANNELS;
  if (allowed && visible.length === 0) return null;

  return (
    <div
      className="flex items-center rounded-lg border border-app-border bg-background p-0.5"
      role="tablist"
      aria-label="Channel"
      style={{ gap: "5px" }}
    >
      {visible.map(({ slug, label, icon: Icon }) => {
        const active = currentSlug === slug;
        return (
          <Button
            key={slug}
            variant="ghost"
            size="icon"
            role="tab"
            aria-selected={active}
            aria-label={label}
            title={label}
            onClick={() => navigate(`/agent/digital/${slug}`)}
            className={cn(
              "h-8 w-8 rounded transition-colors",
              active
                ? "bg-primary text-primary-foreground hover:bg-primary/90"
                : "text-muted-foreground hover:bg-muted hover:text-foreground",
            )}
          >
            <Icon className="h-5 w-5" />
          </Button>
        );
      })}
    </div>
  );
}

