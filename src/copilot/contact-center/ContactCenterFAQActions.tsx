import { useFrontendTool, useAgentContext } from "@/lib/copilotkit-compat";
import { useNavigate } from "react-router-dom";
import { z } from "zod";

/**
 * Arabic-only FAQ answers. The LLM translates on the fly when the user writes in English.
 * Answers must be returned VERBATIM to the user (rule injected via useAgentContext below).
 */
const faqs = {
  // -------- Campaigns --------
  create_campaign: {
    question: "كيف أنشئ حملة جديدة؟",
    answer:
      "لإنشاء حملة جديدة:\n١. افتح صفحة الحملات من القائمة الجانبية.\n٢. اضغط زر «إضافة حملة».\n٣. اتبع المعالج: معلومات الحملة ← الوكلاء المسندون ← مراجعة وإطلاق.\n٤. بعد الإنشاء، أضف قائمة الأهداف من شاشة تعديل الحملة في تبويب «الأهداف».",
  },
  add_campaign_targets: {
    question: "كيف أضيف أهدافاً للحملة؟",
    answer:
      "افتح الحملة من صفحة الحملات، ثم اذهب لتبويب «الأهداف». يمكنك:\n• إضافة يدوياً عبر زر «إضافة هدف».\n• استيراد ملف CSV.\n• الإضافة من الدليل عبر «إضافة من الدليل» مع إمكانية التصفية بالوسوم.",
  },
  assign_agents_to_campaign: {
    question: "كيف أُسند وكلاء للحملة؟",
    answer:
      "في صفحة تعديل الحملة، افتح تبويب «الوكلاء المسندون»، ثم استخدم لوحة النقل (Shuttle) لاختيار الوكلاء من القائمة المتاحة ونقلهم للقائمة المسندة.",
  },

  // -------- IVR --------
  create_ivr_flow: {
    question: "كيف أنشئ تدفق IVR؟",
    answer:
      "من القائمة الجانبية افتح «محرر IVR»، ثم:\n١. اضغط «إنشاء تدفق».\n٢. اسحب العقد من اللوحة اليسرى إلى الكنفا.\n٣. وصّل العقد ببعضها.\n٤. اضغط «حفظ».",
  },
  publish_ivr_flow: {
    question: "كيف أنشر تدفق IVR؟",
    answer:
      "افتح التدفق في محرر IVR، اضغط زر «نشر» أعلى يمين الشاشة. تأكد أولاً من حفظ آخر التغييرات وعدم وجود تحذيرات في فاحص التدفق.",
  },

  // -------- Users / RBAC --------
  add_user: {
    question: "كيف أضيف مستخدماً جديداً؟",
    answer:
      "افتح إعدادات النظام ← المستخدمون، اضغط «إضافة مستخدم»، أدخل البريد الإلكتروني والاسم والدور، ثم احفظ. سيستلم المستخدم رسالة لتعيين كلمة المرور.",
  },
  assign_role: {
    question: "كيف أعيّن دوراً لمستخدم؟",
    answer:
      "افتح إعدادات النظام ← الأدوار، اختر المستخدم ثم اضغط «تعيين دور»، اختر الدور المناسب (مالك / مدير / وكيل) واحفظ.",
  },
  create_group: {
    question: "كيف أنشئ مجموعة؟",
    answer:
      "افتح صفحة المجموعات، اضغط «إنشاء مجموعة»، أدخل الاسم والوصف وحدد الأعضاء، ثم احفظ.",
  },

  // -------- Settings catalogs --------
  create_skill: {
    question: "كيف أضيف مهارة جديدة؟",
    answer:
      "افتح إعدادات النظام ← المهارات، اضغط «إضافة مهارة»، أدخل الاسم والمستوى الافتراضي ثم احفظ.",
  },
  create_wrapup_code: {
    question: "كيف أنشئ كود إنهاء مكالمة؟",
    answer:
      "افتح إعدادات النظام ← أكواد الإنهاء، اضغط «إضافة كود»، اكتب الاسم والوصف واختر اللون ثم احفظ.",
  },
  create_auto_tag: {
    question: "كيف أنشئ وسماً تلقائياً؟",
    answer:
      "افتح إعدادات الذكاء الاصطناعي ← الوسوم التلقائية، اضغط «إضافة وسم»، حدد الاسم وقواعد التطبيق (الكلمات المفتاحية أو شرط الذكاء الاصطناعي) ثم احفظ.",
  },

  // -------- Live operations --------
  view_live_queues: {
    question: "كيف أتابع الطوابير المباشرة؟",
    answer:
      "افتح القائمة الجانبية ← «المراقبة المباشرة» ← «مراقبة الطوابير». ستجد عدد المنتظرين، أطول مدة انتظار، والوكلاء المتاحين في الوقت الفعلي.",
  },
  view_missed_calls: {
    question: "كيف أرى المكالمات الفائتة؟",
    answer:
      "من «سجل المكالمات»، استخدم مرشّح الحالة واختر «فائتة»، أو من لوحة التحكم اضغط على بطاقة «المكالمات الفائتة».",
  },
  export_calls_report: {
    question: "كيف أُصدّر تقرير المكالمات؟",
    answer:
      "في صفحة المكالمات، حدّد الفترة الزمنية والمرشّحات، ثم اضغط أيقونة التصدير أعلى الجدول لتنزيل ملف CSV.",
  },

  // -------- Surveys --------
  create_survey: {
    question: "كيف أنشئ استبياناً؟",
    answer:
      "افتح صفحة الاستبيانات، اضغط «استبيان جديد»، أضف الأسئلة (تقييم / متعدد / نصي) ثم احفظ. يمكنك ربطه بـ IVR لاحقاً.",
  },
  view_survey_results: {
    question: "كيف أرى نتائج الاستبيانات؟",
    answer:
      "افتح صفحة الاستبيانات، اضغط على اسم الاستبيان ثم تبويب «النتائج». ستجد متوسط التقييم وعدد الردود والتفاصيل لكل مشارك.",
  },

  // -------- Billing --------
  top_up_balance: {
    question: "كيف أشحن الرصيد؟",
    answer:
      "افتح إعدادات الحساب ← الفوترة ← «تفاصيل الرصيد»، أدخل المبلغ في حقل الشحن واضغط «تحديث».",
  },
  update_billing_info: {
    question: "كيف أحدّث بيانات الفوترة؟",
    answer:
      "افتح إعدادات الحساب ← الفوترة ← تبويب «عام»، عدّل الاسم التجاري وعنوان الفوترة والدولة والرقم الضريبي، ثم اضغط «حفظ».",
  },
  view_account_statement: {
    question: "كيف أرى كشف الحساب؟",
    answer:
      "افتح إعدادات الحساب ← الفوترة ← «كشف الحساب». ستجد الفواتير والمدفوعات والرصيد المتراكم مرتبة زمنياً.",
  },

  // -------- Agent workspaces --------
  open_softphone: {
    question: "كيف أفتح السوفت فون؟",
    answer:
      "اضغط أيقونة السوفت فون أعلى الشاشة، أو افتح صفحة السوفت فون من القائمة الجانبية. أول مرة يطلب الإذن للميكروفون ثم يقوم بتسجيل SIP تلقائياً.",
  },
  use_digital_agent_console: {
    question: "كيف أستخدم وحدة العميل الرقمي؟",
    answer:
      "افتح القائمة الجانبية ← «وحدة العميل الرقمي». ستجد صندوقاً موحداً يشمل المكالمات، الرسائل، البريد، والتواصل الاجتماعي. اختر القناة من الشريط الأيسر.",
  },
  set_focus_mode: {
    question: "كيف أفعّل وضع التركيز؟",
    answer:
      "اضغط أيقونة وضع التركيز في الشريط العلوي. سيختفي الشريط الجانبي وتظهر المهمة الحالية فقط. اضغط ✕ في الأعلى للخروج.",
  },
} as const;

export type FaqTopic = keyof typeof faqs;

const faqNavigation: Partial<Record<FaqTopic, string>> = {
  create_campaign: "/campaigns",
  add_campaign_targets: "/campaigns",
  assign_agents_to_campaign: "/campaigns",
  create_ivr_flow: "/ivr",
  publish_ivr_flow: "/ivr",
  add_user: "/system-settings",
  assign_role: "/system-settings",
  create_group: "/groups",
  create_skill: "/system-settings",
  create_wrapup_code: "/system-settings",
  create_auto_tag: "/ai-settings",
  view_live_queues: "/live/queue-monitor",
  view_missed_calls: "/calls",
  export_calls_report: "/calls",
  create_survey: "/surveys",
  view_survey_results: "/surveys",
  top_up_balance: "/account-settings",
  update_billing_info: "/account-settings",
  view_account_statement: "/account-settings",
  open_softphone: "/softphone",
  use_digital_agent_console: "/agent/digital",
};

interface Props {
  isRtl: boolean;
}

export function ContactCenterFAQActions(_props: Props) {
  const navigate = useNavigate();
  const topicKeys = Object.keys(faqs) as FaqTopic[];

  useFrontendTool({
    name: "answerContactCenterFAQ",
    description:
      "ALWAYS call this for 'how do I…' / 'كيف…' questions about using the platform. " +
      "Returns a pre-written Arabic how-to answer and navigates the app to the relevant page. " +
      "Valid topics: " +
      topicKeys.join(", "),
    parameters: z.object({
      topic: z.enum(topicKeys as [FaqTopic, ...FaqTopic[]]),
    }),
    handler: async ({ topic }) => {
      const entry = faqs[topic];
      if (!entry) return { success: false, error: "Unknown FAQ topic" };
      const nav = faqNavigation[topic];
      if (nav) {
        try { navigate(nav); } catch { /* ignore */ }
      }
      return {
        success: true,
        topic,
        question: entry.question,
        answer: entry.answer,
        navigatedTo: nav ?? null,
      };
    },
  });

  useAgentContext({
    description:
      "FAQ presentation rule: when answerContactCenterFAQ returns a result, present the `answer` " +
      "field VERBATIM. Do not rephrase, reorder, or rewrite. Translate only the language if the " +
      "user wrote in another language; preserve the steps and numbering. Always reply in the user's language.",
    value: { rule: "verbatim-faq" },
  });

  return null;
}
