import type { Channel } from "../models";

export const CHANNEL_LABEL: Record<Channel, string> = {
  web_chat: "Web Chat",
  mobile_app: "Mobile App",
  whatsapp: "WhatsApp",
  messenger: "Messenger",
  instagram_dm: "Instagram DM",
  instagram_comment: "Instagram Comment",
  facebook_comment: "Facebook Comment",
  twitter_dm: "X DM",
  twitter_mention: "X Mention",
  telegram: "Telegram",
};

// HSL channel color tokens. Referenced via inline style with CSS var fallback —
// keeps semantic separation without hardcoded hex in components.
export const CHANNEL_COLOR_VAR: Record<Channel, string> = {
  web_chat: "var(--channel-web, 210 70% 50%)",
  mobile_app: "var(--channel-mobile, 250 60% 55%)",
  whatsapp: "var(--channel-whatsapp, 142 70% 38%)",
  messenger: "var(--channel-messenger, 220 90% 55%)",
  instagram_dm: "var(--channel-instagram, 330 75% 55%)",
  instagram_comment: "var(--channel-instagram, 330 75% 55%)",
  facebook_comment: "var(--channel-facebook, 220 75% 45%)",
  twitter_dm: "var(--channel-twitter, 200 18% 25%)",
  twitter_mention: "var(--channel-twitter, 200 18% 25%)",
  telegram: "var(--channel-telegram, 200 80% 50%)",
};

export const channelColor = (c: Channel) => `hsl(${CHANNEL_COLOR_VAR[c]})`;
export const channelTint = (c: Channel) => `hsl(${CHANNEL_COLOR_VAR[c]} / 0.12)`;
