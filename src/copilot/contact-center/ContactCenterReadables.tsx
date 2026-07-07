import { useMemo } from "react";
import { useLocation } from "react-router-dom";
import { useAgentContext } from "@/lib/copilotkit-compat";
import { useAuth } from "@/hooks/useAuth";
import { useUserRole } from "@/hooks/useUserRole";
import { useLiveSnapshot } from "@/hooks/useLiveSnapshot";
import { COPILOT_GUIDE_INSTRUCTIONS } from "@/copilot/shared/constants";

interface Props {
  isRtl: boolean;
  assistantOpen: boolean;
}

const SECTION_MAP: Record<string, { ar: string; en: string }> = {
  "": { ar: "الرئيسية", en: "Home" },
  dashboard: { ar: "لوحة التحكم", en: "Dashboard" },
  live: { ar: "المراقبة المباشرة", en: "Live monitor" },
  calls: { ar: "سجل المكالمات", en: "Calls" },
  campaigns: { ar: "الحملات", en: "Campaigns" },
  directory: { ar: "الدليل", en: "Directory" },
  ivr: { ar: "محرر IVR", en: "IVR editor" },
  softphone: { ar: "السوفت فون", en: "Softphone" },
  agent: { ar: "وحدة العميل الرقمي", en: "Digital agent workspace" },
  "account-settings": { ar: "إعدادات الحساب", en: "Account settings" },
  "ai-settings": { ar: "إعدادات الذكاء الاصطناعي", en: "AI settings" },
  "system-settings": { ar: "إعدادات النظام", en: "System settings" },
  surveys: { ar: "الاستبيانات", en: "Surveys" },
  "voice-agent": { ar: "الوكيل الصوتي", en: "Voice agent" },
  logs: { ar: "السجلات", en: "Logs" },
  groups: { ar: "المجموعات", en: "Groups" },
  queues: { ar: "الطوابير", en: "Queues" },
};

function resolveSection(pathname: string, isRtl: boolean) {
  const seg = pathname.replace(/^\/+/, "").split("/")[0] ?? "";
  const entry = SECTION_MAP[seg];
  return {
    key: seg || "home",
    label: entry ? (isRtl ? entry.ar : entry.en) : (isRtl ? "غير معروف" : "Unknown"),
    path: pathname,
  };
}

export function ContactCenterReadables({ isRtl, assistantOpen }: Props) {
  const { user } = useAuth();
  const { role } = useUserRole();
  const location = useLocation();
  const snapshot = useLiveSnapshot({
    enabled: assistantOpen,
    refetchInterval: assistantOpen ? 5000 : false,
  }).data;

  const section = useMemo(() => resolveSection(location.pathname, isRtl), [location.pathname, isRtl]);

  useAgentContext({
    description:
      "Combined contact-center context: signed-in user, current app section, and a live snapshot summary. " +
      "For detailed call/campaign/billing data, call the matching tool (this readable does not inline them).",
    value: {
      locale: isRtl ? "ar" : "en",
      dir: isRtl ? "rtl" : "ltr",
      user: user ? { id: user.id, email: user.email, role } : null,
      currentSection: section,
      liveSummary: snapshot
        ? {
            waiting: snapshot.queue?.waiting ?? 0,
            longestWaitSeconds: snapshot.queue?.longestWaitSeconds ?? 0,
            activeCalls: snapshot.activeCalls?.length ?? 0,
            agentsOnline: (snapshot.agents ?? []).filter((a) => a.status !== "offline").length,
          }
        : null,
    },
  });

  useAgentContext({
    description: "System persona and rules for the Admin Insight assistant. Follow these strictly.",
    value: COPILOT_GUIDE_INSTRUCTIONS,
  });

  return null;
}
