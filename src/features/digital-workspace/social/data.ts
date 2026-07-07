import type { AvatarColor, Platform, Sentiment } from "./ui/primitives";

export type EngageItem = {
  id: string;
  name: string;
  avatarColor: AvatarColor;
  initials: string;
  platform: Platform;
  sub: string;
  preview: string;
  time: string;
  sentiment: Sentiment;
  tags?: { label: string; variant?: "default" | "red" | "amber" }[];
  pills?: { label: string; variant: "default" | "green" | "purple" | "amber" | "red" | "outline"; icon?: "check" }[];
  sla?: { text: string; breached?: boolean };
  unread?: boolean;
  unreadCount?: number;
};

export const ENGAGE_ITEMS: EngageItem[] = [
  {
    id: "jenna",
    name: "Jenna Doyle",
    initials: "JD",
    avatarColor: "rose",
    platform: "x",
    sub: "@jenna_d · mentioned you",
    preview: `"@YourBrand ordered 2 weeks ago and still nothing. This is unacceptable 😡"`,
    time: "4 min",
    sentiment: "negative",
    tags: [{ label: "complaint", variant: "red" }],
    sla: { text: "-2m 14s", breached: true },
  },
  {
    id: "mike",
    name: "Mike Kowalski",
    initials: "MK",
    avatarColor: "blue",
    platform: "facebook",
    sub: `commented on "Summer Sale is live ☀️"`,
    preview: `"Does the 30% code work on bundles too?"`,
    time: "22 min",
    sentiment: "neutral",
    tags: [{ label: "question" }],
    unread: true,
  },
  {
    id: "rania",
    name: "Rania Patel",
    initials: "RP",
    avatarColor: "rose",
    platform: "instagram",
    sub: "DM · @rania.makes",
    preview: `"Loved the unboxing! Can I feature you? 💕"`,
    time: "38 min",
    sentiment: "positive",
    pills: [{ label: "influencer", variant: "purple" }],
  },
  {
    id: "david",
    name: "David Tran",
    initials: "DT",
    avatarColor: "blue",
    platform: "linkedin",
    sub: "commented on company post",
    preview: `"Great hire announcement — congrats to the team!"`,
    time: "1 hour",
    sentiment: "positive",
    pills: [{ label: "Replied", variant: "green", icon: "check" }],
  },
  {
    id: "anon",
    name: "Anonymous",
    initials: "AN",
    avatarColor: "slate",
    platform: "x",
    sub: "@nothappy_99 · quote-posted you",
    preview: `"Worst support I've ever dealt with."`,
    time: "2 hours",
    sentiment: "negative",
    tags: [{ label: "escalate", variant: "amber" }],
    unread: true,
  },
];

export type PublishItem = {
  id: string;
  title: string;
  platform: Platform | "draft";
  preview: string;
  time: string;
  pill: { label: string; variant: "amber" | "default" | "outline"; icon?: "clock" };
  tags: string[];
};

export const SCHEDULED_ITEMS: PublishItem[] = [
  {
    id: "p1",
    title: "Product launch teaser",
    platform: "linkedin",
    preview: `"Something new is coming this Thursday. Any guesses? 👀"`,
    time: "Today 18:00",
    pill: { label: "In 3h", variant: "amber", icon: "clock" },
    tags: ["LinkedIn"],
  },
  {
    id: "p2",
    title: "Customer spotlight reel",
    platform: "instagram",
    preview: `"How @rania.makes styles our summer line 🌸"`,
    time: "Tue 12:00",
    pill: { label: "Scheduled", variant: "default" },
    tags: ["Instagram", "+ Reel"],
  },
];

export const DRAFT_ITEMS: PublishItem[] = [
  {
    id: "d1",
    title: "Weekend giveaway",
    platform: "draft",
    preview: `"Win a year of free shipping — here's how 🎁"`,
    time: "Draft",
    pill: { label: "Not scheduled", variant: "outline" },
    tags: ["X · Facebook"],
  },
];

export const PLATFORM_CHIPS = [
  { id: "all", label: "All", count: 23 },
  { id: "x", label: "X", count: 8 },
  { id: "facebook", label: "Facebook", count: 6 },
  { id: "instagram", label: "Instagram", count: 5 },
  { id: "linkedin", label: "LinkedIn", count: 4 },
] as const;

export const STATS = [
  { value: "1,204", label: "Impressions", icon: "eye", delta: "▲ 38% vs avg", up: true },
  { value: "82", label: "Engagements", icon: "heart", delta: "▲ 12", up: true },
  { value: "23", label: "Reposts", icon: "repeat", delta: "▲ high", up: true },
  { value: "6.8%", label: "Engmt rate", icon: "trending", delta: "▲ 2.1%", up: true },
] as const;

export const AUTHOR_ACTIVITY = [
  {
    id: "a1",
    title: "Public complaint",
    date: "4 min ago",
    desc: "Quote-mention about delivery delay on #80421",
    icon: "x" as const,
    iconColor: "#b91c1c",
    factSentiment: "negative" as Sentiment,
    factSentimentLabel: "negative",
    factSuffix: "1.2K views",
  },
  {
    id: "a2",
    title: "DM thread",
    date: "Jun 12",
    desc: "Asked about return policy — resolved by Yasmin",
    icon: "send" as const,
    iconColor: "var(--si-brand)",
    factText: "Status:",
    factBold: "resolved",
    factSentiment: "positive" as Sentiment,
    factSentimentLabel: "positive",
  },
  {
    id: "a3",
    title: "Liked a post",
    date: "Jun 2",
    desc: "Engaged positively with the spring launch reel",
    icon: "heart" as const,
    iconColor: "var(--si-a-amber)",
    factSentiment: "positive" as Sentiment,
    factSentimentLabel: "positive",
  },
];
