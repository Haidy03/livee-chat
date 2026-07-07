import type { AppRole } from "@/hooks/useUserRole";

export interface CopilotQuickSuggestion {
  title: string;
  message: string;
}

export function getCopilotSidebarTitle(isRtl: boolean): string {
  return isRtl ? "مساعد مركز الاتصال" : "Contact Center Assistant";
}

export function getCopilotLabels(isRtl: boolean) {
  return {
    title: getCopilotSidebarTitle(isRtl),
    placeholder: isRtl
      ? "اكتب سؤالك أو اطلب جولة إرشادية…"
      : "Ask a question or request a guided tour…",
    initial: isRtl
      ? "مرحباً — يمكنني الإجابة على أسئلتك حول المكالمات، الحملات، الوكلاء، والفوترة."
      : "Hi — I can answer questions about calls, campaigns, agents, and billing.",
    error: isRtl ? "حدث خطأ. حاول مرة أخرى." : "Something went wrong. Try again.",
    stopGenerating: isRtl ? "إيقاف" : "Stop generating",
    regenerateResponse: isRtl ? "إعادة المحاولة" : "Regenerate response",
    copyToClipboard: isRtl ? "نسخ" : "Copy",
    thumbsUp: isRtl ? "مفيد" : "Thumbs up",
    thumbsDown: isRtl ? "غير مفيد" : "Thumbs down",
    copied: isRtl ? "تم النسخ" : "Copied",
  };
}

export const COPILOT_GUIDE_INSTRUCTIONS = `
You are the Admin Insight assistant for a contact-center platform. Use the available read-only tools
to answer questions; never claim data is unavailable if a matching tool exists.

CAPABILITIES → TOOL MAPPING:
- Call volume / missed / wait time → getCallStatistics
- Agent performance → getCallsByAgent, getActiveAgents, getAgentStatus
- Queues live state → getLiveQueueSnapshot, getCallsByQueue
- Wrap-up / disposition breakdown → getCallsByDisposition
- Recent missed calls / recordings → getMissedCalls, getRecentRecordings
- Campaigns → getCampaignSummary, getCampaignTargetsProgress
- Directory → getDirectoryContactCount
- Surveys → getSurveyResults
- Billing / balance → getBillingBalance, getBillingSummary
- Tags / skills / wrap-up codes → getAutoTagsList, getSkillsAndWrapUpCodes
- Users / groups / roles → getUserAndGroupCount, getRoleAssignments
- Audit / edit logs → getEditLogs
- How-to questions ("how do I…") → answerContactCenterFAQ
- Navigation requests → navigateTo

RULES:
- READ-ONLY. Never modify data; only describe what you find.
- Always state the time period covered (echo startDate/endDate from the tool result).
- Lead with the number / the headline, then add one short sentence of context.
- Mirror the user's language exactly (Arabic dialect or English). Translate FAQ answers to the user's language but keep their meaning verbatim.
- Relate answers to the current section when possible (see currentSection in context).
- Be concise. No filler.
`.trim();

const ADMIN_ONLY = new Set([
  "campaigns_summary",
  "billing_balance",
  "users_count",
  "edit_logs",
]);

export function getCopilotQuickSuggestions(
  isRtl: boolean,
  opts: { role?: AppRole | null } = {},
): CopilotQuickSuggestion[] {
  const role = opts.role ?? "agent";
  const all: Array<CopilotQuickSuggestion & { id: string }> = [
    {
      id: "call_stats_today",
      title: isRtl ? "إحصائيات اليوم" : "Today's call stats",
      message: isRtl
        ? "ما هي إحصائيات المكالمات لليوم؟"
        : "What are today's call statistics?",
    },
    {
      id: "live_queue",
      title: isRtl ? "الطابور المباشر" : "Live queue",
      message: isRtl
        ? "كم عدد المكالمات المنتظرة الآن؟"
        : "How many calls are waiting right now?",
    },
    {
      id: "missed_calls",
      title: isRtl ? "المكالمات الفائتة" : "Missed calls",
      message: isRtl
        ? "أعرض المكالمات الفائتة لآخر 7 أيام"
        : "Show me missed calls in the last 7 days",
    },
    {
      id: "top_agents",
      title: isRtl ? "أفضل الوكلاء" : "Top agents",
      message: isRtl
        ? "من هم أفضل الوكلاء أداءً اليوم؟"
        : "Who are the top performing agents today?",
    },
    {
      id: "campaigns_summary",
      title: isRtl ? "ملخص الحملات" : "Campaigns summary",
      message: isRtl ? "اعطني ملخص الحملات" : "Give me a campaign summary",
    },
    {
      id: "billing_balance",
      title: isRtl ? "الرصيد المتاح" : "Account balance",
      message: isRtl ? "ما هو الرصيد المتاح؟" : "What's my available balance?",
    },
    {
      id: "how_create_campaign",
      title: isRtl ? "كيف أنشئ حملة؟" : "How to create a campaign?",
      message: isRtl
        ? "كيف أنشئ حملة جديدة؟"
        : "How do I create a new campaign?",
    },
    {
      id: "how_open_softphone",
      title: isRtl ? "فتح السوفت فون" : "Open softphone",
      message: isRtl ? "كيف أفتح السوفت فون؟" : "How do I open the softphone?",
    },
  ];

  return all
    .filter((s) => role !== "agent" || !ADMIN_ONLY.has(s.id))
    .map(({ id: _id, ...rest }) => rest);
}
