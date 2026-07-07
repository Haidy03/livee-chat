import type { Channel } from "../models";
import {
  Globe,
  Smartphone,
  MessageCircle,
  Facebook,
  Instagram,
  Twitter,
  Send,
  MessageSquare,
} from "lucide-react";
import { CHANNEL_LABEL, channelColor, channelTint } from "./channelTokens";
import { cn } from "@/lib/utils";

const ICONS: Record<Channel, typeof Globe> = {
  web_chat: Globe,
  mobile_app: Smartphone,
  whatsapp: MessageCircle,
  messenger: MessageSquare,
  instagram_dm: Instagram,
  instagram_comment: Instagram,
  facebook_comment: Facebook,
  twitter_dm: Twitter,
  twitter_mention: Twitter,
  telegram: Send,
};

interface Props {
  channel: Channel;
  className?: string;
  size?: number;
  withBackground?: boolean;
  showLabel?: boolean;
}

const warned = new Set<string>();
export function ChannelIcon({ channel, className, size = 14, withBackground = true, showLabel }: Props) {
  let Icon = ICONS[channel];
  if (!Icon) {
    if (!warned.has(channel as string)) {
      warned.add(channel as string);
      console.warn(`[ChannelIcon] Unknown channel "${channel}", falling back to Globe`);
    }
    Icon = Globe;
  }
  const label = CHANNEL_LABEL[channel] ?? String(channel);
  return (
    <span
      title={label}
      aria-label={label}
      className={cn(
        "inline-flex items-center gap-1.5 rounded-md",
        withBackground ? "p-1" : "",
        className,
      )}
      style={withBackground ? { backgroundColor: channelTint(channel), color: channelColor(channel) } : { color: channelColor(channel) }}
    >
      <Icon size={size} aria-hidden />
      {showLabel ? <span className="text-xs font-medium">{label}</span> : null}
    </span>
  );
}
