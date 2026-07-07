// Realistic mock seed for the Digital Workspace.
// Generates 25+ rooms across 10 channels, mixed EN/AR, with attachments,
// internal notes, SLA states, escalations, offers, bot handoffs.

import type {
  Agent,
  CaseItem,
  Channel,
  Room,
  Customer,
  CustomerJourneyItem,
  KnowledgeArticle,
  Message,
  Queue,
} from "../models";
import { ALL_CHANNELS } from "../models";

const now = Date.now();
const iso = (offsetSec: number) => new Date(now + offsetSec * 1000).toISOString();
const r = <T>(arr: T[]) => arr[Math.floor(Math.random() * arr.length)];
const pickN = <T>(arr: T[], n: number) =>
  [...arr].sort(() => Math.random() - 0.5).slice(0, Math.min(n, arr.length));

export const TENANT_ID = "tenant-demo";

// -------------------- Agents --------------------
export const MOCK_AGENTS: Agent[] = [
  { id: "agent-me", name: "Yasmin Al-Sayed", email: "yasmin@demo.co", presence: "available", capacity: { current: 3, max: 5 }, skills: ["billing", "technical"], languages: ["en", "ar"], team: "Customer Service", role: "agent", channelAvailability: { phone: true, chat: false, email: false, social: false } },
  { id: "agent-2", name: "Mark Henderson", email: "mark@demo.co", presence: "busy", capacity: { current: 4, max: 5 }, skills: ["sales"], languages: ["en"], team: "Sales", channelAvailability: { phone: true, chat: true, email: false, social: true } },
  { id: "agent-3", name: "Layla Ibrahim", email: "layla@demo.co", presence: "available", capacity: { current: 2, max: 5 }, skills: ["technical"], languages: ["ar"], team: "Technical Support", channelAvailability: { phone: true, chat: true, email: true, social: false } },
  { id: "agent-4", name: "Priya Sharma", email: "priya@demo.co", presence: "after_contact_work", capacity: { current: 5, max: 5 }, skills: ["complaints", "vip"], languages: ["en"], team: "VIP Support", channelAvailability: { phone: true, chat: true, email: true, social: true } },
  { id: "agent-5", name: "Omar Khalil", email: "omar@demo.co", presence: "away", capacity: { current: 0, max: 5 }, skills: ["technical"], languages: ["ar", "en"], team: "Technical Support", channelAvailability: { phone: false, chat: true, email: true, social: true } },
  { id: "agent-6", name: "Sofia Romano", email: "sofia@demo.co", presence: "available", capacity: { current: 1, max: 4 }, skills: ["billing"], languages: ["en"], team: "Customer Service", channelAvailability: { phone: true, chat: true, email: true, social: true } },
  { id: "agent-7", name: "Daniel Park", email: "daniel@demo.co", presence: "break", capacity: { current: 0, max: 5 }, skills: ["sales"], languages: ["en"], team: "Sales", channelAvailability: { phone: true, chat: true, email: true, social: true } },
  { id: "agent-8", name: "Hanna Mubarak", email: "hanna@demo.co", presence: "available", capacity: { current: 2, max: 5 }, skills: ["complaints"], languages: ["ar"], team: "Complaints", channelAvailability: { phone: true, chat: true, email: true, social: true } },
  { id: "agent-9", name: "Lucas Müller", email: "lucas@demo.co", presence: "offline", capacity: { current: 0, max: 5 }, skills: ["technical"], languages: ["en"], team: "Technical Support", channelAvailability: { phone: true, chat: true, email: true, social: true } },
  { id: "sup-1", name: "Rasha Tarek", email: "rasha@demo.co", presence: "available", capacity: { current: 0, max: 0 }, skills: ["supervisor"], languages: ["ar", "en"], team: "Customer Service", role: "supervisor", channelAvailability: { phone: true, chat: true, email: true, social: true } },
];

// -------------------- Queues --------------------
export const MOCK_QUEUES: Queue[] = [
  { id: "q-cs", name: "Customer Service", group: "Customer Service", channels: ALL_CHANNELS, requiredSkills: [], waiting: 12, activeAgents: 6, expectedWaitSec: 90 },
  { id: "q-tech", name: "Technical Support", group: "Technical Support", channels: ["web_chat", "mobile_app", "whatsapp", "messenger"], requiredSkills: ["technical"], waiting: 4, activeAgents: 3, expectedWaitSec: 180 },
  { id: "q-sales", name: "Sales", group: "Sales", channels: ["web_chat", "whatsapp", "instagram_dm"], requiredSkills: ["sales"], waiting: 8, activeAgents: 2 },
  { id: "q-complaints", name: "Complaints", group: "Complaints", channels: ["whatsapp", "twitter_dm", "twitter_mention", "facebook_comment", "instagram_comment"], requiredSkills: ["complaints"], waiting: 6, activeAgents: 2 },
  { id: "q-vip", name: "VIP Support", group: "VIP Support", channels: ALL_CHANNELS, requiredSkills: ["vip"], waiting: 1, activeAgents: 2 },
];

// -------------------- Customers --------------------
const customers: Customer[] = [
  { id: "cust-1", name: "Ahmed Al-Rashid", language: "ar", country: "AE", segment: "Gold", vip: true, authenticated: true, phone: "+971501234567", email: "ahmed@example.com", tags: ["repeat-customer", "subscription"], identities: [
    { id: "id-1", channel: "whatsapp", handle: "+971501234567", verified: true },
    { id: "id-2", channel: "web_chat", handle: "visitor-91a2", display: "Web visitor" },
  ] },
  { id: "cust-2", name: "Emily Carter", language: "en", country: "US", segment: "Silver", tags: ["new"], phone: "+14155550199", identities: [
    { id: "id-3", channel: "web_chat", handle: "visitor-7c4f" },
  ] },
  { id: "cust-3", name: "محمد عبدالله", language: "ar", country: "EG", segment: "Bronze", tags: [], phone: "+201001234567", identities: [
    { id: "id-4", channel: "whatsapp", handle: "+201001234567" },
  ] },
  { id: "cust-4", name: "Jamal Williams", language: "en", country: "GB", segment: "Platinum", vip: true, tags: ["vip", "high-value"], phone: "+447700900900", identities: [
    { id: "id-5", channel: "messenger", handle: "jamal.williams.fb" },
    { id: "id-6", channel: "instagram_dm", handle: "@jamal_w" },
  ] },
  { id: "cust-5", name: "Sara Mahmoud", language: "ar", country: "SA", segment: "Gold", authenticated: true, tags: ["refund"], identities: [
    { id: "id-7", channel: "mobile_app", handle: "app-user-882" },
  ] },
  { id: "cust-6", name: "Carlos Mendes", language: "en", country: "BR", tags: [], identities: [
    { id: "id-8", channel: "telegram", handle: "@cmendes" },
  ] },
  { id: "cust-7", name: "Aisha Noor", language: "ar", country: "AE", tags: ["complaint"], identities: [
    { id: "id-9", channel: "twitter_mention", handle: "@aisha_noor" },
  ] },
  { id: "cust-8", name: "Tom Becker", language: "en", country: "DE", tags: [], identities: [
    { id: "id-10", channel: "facebook_comment", handle: "tom.becker.fb" },
  ] },
  { id: "cust-9", name: "Nadia Hassan", language: "ar", country: "JO", tags: [], identities: [
    { id: "id-11", channel: "instagram_comment", handle: "@nadia.h" },
  ] },
  { id: "cust-10", name: "Robert Lee", language: "en", country: "CA", tags: [], identities: [
    { id: "id-12", channel: "twitter_dm", handle: "@rob_lee" },
  ] },
];
// extend to 14
for (let i = 11; i <= 14; i++) {
  customers.push({
    id: `cust-${i}`,
    name: `Customer ${i}`,
    language: i % 2 ? "en" : "ar",
    tags: [],
    identities: [{ id: `id-${100 + i}`, channel: r(ALL_CHANNELS), handle: `handle-${i}` }],
  });
}
export const MOCK_CUSTOMERS = customers;

// -------------------- Rooms --------------------
const seedRoom = (i: number): Room => {
  const customer = MOCK_CUSTOMERS[i % MOCK_CUSTOMERS.length];
  const channel: Channel = customer.identities[0].channel;
  const queue = r(MOCK_QUEUES);
  const minutesAgo = Math.floor(Math.random() * 240) + 1;
  const statusPool: Room["status"][] = [
    "active", "active", "active", "waiting_customer", "pending", "offered", "escalated", "assigned", "resolved",
  ];
  const status = statusPool[i % statusPool.length];
  const priorityPool: Room["priority"][] = ["normal", "normal", "high", "low", "urgent"];
  const sentimentPool: Room["sentiment"][] = ["neutral", "neutral", "positive", "negative"];
  const slaState: Room["sla"]["state"] =
    i % 9 === 0 ? "breached" : i % 5 === 0 ? "warning" : "ok";

  const assignedAgentId =
    status === "offered" || status === "new" ? undefined : i % 3 === 0 ? "agent-me" : r(MOCK_AGENTS).id;

  return {
    id: `conv-${i + 1}`,
    tenantId: TENANT_ID,
    customerId: customer.id,
    channel,
    channelAccountId: `acct-${channel}`,
    channelRoomId: `wire-${i}`,
    queueId: queue.id,
    assignedAgentId,
    status,
    priority: priorityPool[i % priorityPool.length],
    language: customer.language,
    sentiment: sentimentPool[i % sentimentPool.length],
    subject: customer.language === "ar" ? "استفسار عن الطلب" : "Question about order",
    tags: pickN(["billing", "refund", "tech", "vip", "renewal", "shipping"], (i % 3) + 1),
    createdAt: iso(-minutesAgo * 60 - 600),
    updatedAt: iso(-minutesAgo * 60),
    lastMessageAt: iso(-minutesAgo * 60),
    customerWaitingSince: status === "waiting_customer" || status === "offered" ? iso(-minutesAgo * 60) : undefined,
    sla: {
      state: slaState,
      firstResponseDeadline: iso(slaState === "breached" ? -120 : slaState === "warning" ? 90 : 600),
      resolutionDeadline: iso(slaState === "breached" ? 600 : 3600),
    },
    unreadCount: status === "active" || status === "assigned" ? (i % 4) : 0,
    lastMessagePreview:
      customer.language === "ar"
        ? "مرحباً، أحتاج إلى متابعة طلبي."
        : i % 3 === 0
          ? "Can you check the status of my refund?"
          : "Thanks, I'll wait for your reply.",
    botHandled: i % 6 === 0,
    humanHandled: true,
    version: 1,
    sequenceNumber: i,
    participants: assignedAgentId ? [assignedAgentId] : [],
    customerTyping: i === 0,
    offerExpiresAt: status === "offered" ? iso(45) : undefined,
  };
};
export const MOCK_ROOMS: Room[] = Array.from({ length: 28 }, (_, i) => seedRoom(i));

// Make sure several are assigned to me, and include one with each special state.
MOCK_ROOMS.slice(0, 5).forEach((c) => {
  c.assignedAgentId = "agent-me";
  c.status = "active";
  c.unreadCount = 2;
});

// -------------------- Messages --------------------
const messagesByConv: Record<string, Message[]> = {};
MOCK_ROOMS.forEach((c, ci) => {
  const baseTime = +new Date(c.createdAt);
  const lines = c.language === "ar"
    ? [
        { from: "customer" as const, text: "مرحباً، أحتاج إلى متابعة طلبي." },
        { from: "agent" as const, text: "أهلاً بك. سأتحقق من الطلب الآن، لحظة من فضلك." },
        { from: "customer" as const, text: "شكراً لك." },
        { from: "agent" as const, text: "تم تحديث الطلب وسيصل خلال يومين." },
      ]
    : [
        { from: "customer" as const, text: "Hi, I need help with my recent order." },
        { from: "agent" as const, text: "Hello! Happy to help — can you share the order number?" },
        { from: "customer" as const, text: "Sure, it's #A-7821." },
        { from: "agent" as const, text: "Thanks. I see it shipped this morning, ETA Wednesday." },
      ];

  const msgs: Message[] = lines.map((l, i) => ({
    id: `${c.id}-m${i}`,
    roomId: c.id,
    tenantId: c.tenantId,
    senderId: l.from === "customer" ? c.customerId : c.assignedAgentId ?? "agent-me",
    senderType: l.from,
    channel: c.channel,
    type: "text",
    text: l.text,
    status: l.from === "customer" ? "delivered" : "read",
    sentAt: new Date(baseTime + i * 30000).toISOString(),
    sequenceNumber: i,
    publicReply: l.from === "agent" ? true : undefined,
  }));

  // Internal note on a few rooms
  if (ci % 3 === 0) {
    msgs.push({
      id: `${c.id}-note`,
      roomId: c.id,
      tenantId: c.tenantId,
      senderId: "agent-me",
      senderType: "agent",
      channel: c.channel,
      type: "internal_note",
      internal: true,
      text: c.language === "ar"
        ? "ملاحظة داخلية — غير مرئية للعميل. تم التواصل مع قسم الشحن."
        : "Internal note — not visible to the customer. Reached out to shipping.",
      status: "sent",
      sentAt: new Date(baseTime + lines.length * 30000 + 10000).toISOString(),
      sequenceNumber: lines.length + 1,
    });
  }

  // SLA warning system event on warning/breached
  if (c.sla.state !== "ok") {
    msgs.push({
      id: `${c.id}-sys-sla`,
      roomId: c.id,
      tenantId: c.tenantId,
      senderId: "system",
      senderType: "system",
      channel: c.channel,
      type: "sla_warning",
      text: c.language === "ar"
        ? "اقتربت المحادثة من تجاوز اتفاقية مستوى الخدمة."
        : "Room is approaching SLA breach.",
      status: "sent",
      sentAt: new Date(baseTime + (lines.length + 2) * 30000).toISOString(),
      sequenceNumber: lines.length + 2,
    });
  }

  // Attachment on one
  if (ci === 2) {
    msgs.push({
      id: `${c.id}-attach`,
      roomId: c.id,
      tenantId: c.tenantId,
      senderId: c.customerId,
      senderType: "customer",
      channel: c.channel,
      type: "image",
      text: "Receipt photo",
      attachments: [{ id: "att-1", kind: "image", url: "https://images.unsplash.com/photo-1554224155-6726b3ff858f?w=400", name: "receipt.jpg", mime: "image/jpeg", sizeBytes: 184320 }],
      status: "delivered",
      sentAt: new Date(baseTime + (lines.length + 3) * 30000).toISOString(),
      sequenceNumber: lines.length + 3,
    });
  }

  messagesByConv[c.id] = msgs;
});
export const MOCK_MESSAGES = messagesByConv;

// -------------------- Journey --------------------
const journeyByCustomer: Record<string, CustomerJourneyItem[]> = {};
MOCK_CUSTOMERS.forEach((c) => {
  journeyByCustomer[c.id] = [
    { id: `${c.id}-j1`, customerId: c.id, channel: "web_chat", at: iso(-3 * 86400), queue: "Customer Service", agentName: "Bot", status: "resolved", summary: "Bot answered FAQ about returns", intent: "policy.returns", sentiment: "neutral" },
    { id: `${c.id}-j2`, customerId: c.id, channel: c.identities[0].channel, at: iso(-86400), queue: "Customer Service", agentName: "Mark Henderson", status: "resolved", summary: "Handled refund request", intent: "billing.refund", sentiment: "positive" },
    { id: `${c.id}-j3`, customerId: c.id, channel: c.identities[0].channel, at: iso(-1800), queue: "Customer Service", agentName: "Yasmin Al-Sayed", status: "active", summary: "Ongoing follow-up", intent: "support.order", sentiment: "neutral" },
  ];
});
export const MOCK_JOURNEY = journeyByCustomer;

// -------------------- Cases --------------------
const casesByCustomer: Record<string, CaseItem[]> = {};
MOCK_CUSTOMERS.slice(0, 6).forEach((c, i) => {
  casesByCustomer[c.id] = [
    {
      id: `case-${i + 1}`,
      subject: c.language === "ar" ? "متابعة شكوى الفاتورة" : "Billing complaint follow-up",
      status: i % 2 ? "open" : "pending",
      priority: i === 0 ? "urgent" : "normal",
      ownerId: "agent-me",
      createdAt: iso(-2 * 86400),
      updatedAt: iso(-3600),
      roomIds: [],
    },
  ];
});
export const MOCK_CASES = casesByCustomer;

// -------------------- Knowledge --------------------
export const MOCK_KNOWLEDGE: KnowledgeArticle[] = [
  { id: "kb-1", title: "Refund policy & timelines", excerpt: "Standard refunds process within 5–7 business days...", body: "Refunds are processed to the original payment method within 5–7 business days. For prepaid cards, allow up to 14 days.", source: "Help Center", updatedAt: iso(-7 * 86400), tags: ["billing", "refund"] },
  { id: "kb-2", title: "Shipping ETAs by region", excerpt: "Domestic 2–3 days, international 7–14 days...", body: "Domestic orders ship within 2–3 business days. International varies by destination.", source: "Help Center", updatedAt: iso(-2 * 86400), tags: ["shipping"] },
  { id: "kb-3", title: "Resetting account password", excerpt: "Walk customer through the self-serve reset flow...", body: "Direct customer to /forgot-password. The reset link expires after 1 hour.", source: "Internal Playbook", updatedAt: iso(-30 * 86400), tags: ["account", "technical"] },
  { id: "kb-4", title: "VIP escalation policy", excerpt: "Route Platinum customers to VIP queue immediately...", body: "Platinum-segment customers are routed to the VIP queue and answered within 60 seconds.", source: "Internal Playbook", updatedAt: iso(-14 * 86400), tags: ["vip"] },
];

// -------------------- AI suggestions --------------------
import type { AISuggestion } from "../models";
export function getMockAISuggestions(room: Room, customer: Customer): AISuggestion[] {
  const ar = room.language === "ar";
  return [
    {
      id: "ai-summary",
      kind: "summary",
      title: ar ? "ملخص المحادثة" : "Room summary",
      body: ar
        ? "العميل يستفسر عن حالة طلبه الأخير ويرغب في معرفة موعد الوصول."
        : "Customer is following up on a recent order and asking for the delivery ETA.",
      confidence: 0.92,
      source: "AI Assist",
    },
    {
      id: "ai-intent",
      kind: "intent",
      title: ar ? "النية المكتشفة" : "Detected intent",
      body: "support.order.status",
      confidence: 0.88,
    },
    {
      id: "ai-sentiment",
      kind: "sentiment",
      title: ar ? "المشاعر" : "Sentiment",
      body: room.sentiment,
      confidence: 0.81,
    },
    {
      id: "ai-reply-1",
      kind: "reply",
      title: ar ? "رد مقترح" : "Suggested reply",
      body: ar
        ? `أهلاً ${customer.name}، طلبك في الطريق وسيصل خلال 24 ساعة. هل أساعدك في شيء آخر؟`
        : `Hi ${customer.name}, your order is on the way and will arrive within 24 hours. Anything else I can help with?`,
      confidence: 0.79,
    },
    {
      id: "ai-reply-2",
      kind: "reply",
      title: ar ? "رد مقترح بديل" : "Alternate reply",
      body: ar
        ? "أعتذر عن التأخير. سأتواصل مع شركة الشحن فوراً وأرجع إليك بالنتيجة."
        : "Apologies for the delay — I'm reaching out to the shipping partner now and will get back to you shortly.",
      confidence: 0.71,
    },
    {
      id: "ai-kb",
      kind: "knowledge",
      title: ar ? "مقالات ذات صلة" : "Relevant articles",
      body: "Shipping ETAs by region",
      confidence: 0.86,
      meta: { articleId: "kb-2" },
    },
    {
      id: "ai-disposition",
      kind: "disposition",
      title: ar ? "تصنيف مقترح للإغلاق" : "Suggested wrap-up",
      body: ar ? "استفسار عن الشحن — تم الحل" : "Shipping inquiry — Resolved",
      confidence: 0.74,
    },
  ];
}
