import { createClient } from "https://esm.sh/@supabase/supabase-js@2.45.0";

const corsHeaders = {
  "Access-Control-Allow-Origin": "*",
  "Access-Control-Allow-Headers": "authorization, x-client-info, apikey, content-type",
};

Deno.serve(async (req) => {
  if (req.method === "OPTIONS") return new Response(null, { headers: corsHeaders });

  try {
    const authHeader = req.headers.get("Authorization");
    if (!authHeader) {
      return new Response(JSON.stringify({ error: "Missing authorization" }), {
        status: 401, headers: { ...corsHeaders, "Content-Type": "application/json" },
      });
    }

    const { callId } = await req.json();
    if (!callId) {
      return new Response(JSON.stringify({ error: "callId is required" }), {
        status: 400, headers: { ...corsHeaders, "Content-Type": "application/json" },
      });
    }

    const SUPABASE_URL = Deno.env.get("SUPABASE_URL")!;
    const SERVICE_KEY = Deno.env.get("SUPABASE_SERVICE_ROLE_KEY")!;
    const LOVABLE_API_KEY = Deno.env.get("LOVABLE_API_KEY");
    if (!LOVABLE_API_KEY) throw new Error("LOVABLE_API_KEY is not configured");

    // RLS-respecting client to verify the user can access this call
    const userClient = createClient(SUPABASE_URL, Deno.env.get("SUPABASE_ANON_KEY")!, {
      global: { headers: { Authorization: authHeader } },
    });
    const { data: call, error: callErr } = await userClient
      .from("calls")
      .select("id, summary, full_transcript, direction, sentiment, from_display, to_display, total_seconds")
      .eq("id", callId)
      .maybeSingle();
    if (callErr || !call) {
      return new Response(JSON.stringify({ error: "Call not found" }), {
        status: 404, headers: { ...corsHeaders, "Content-Type": "application/json" },
      });
    }

    if (call.full_transcript && call.summary) {
      return new Response(JSON.stringify({ skipped: true, summary: call.summary, full_transcript: call.full_transcript }), {
        headers: { ...corsHeaders, "Content-Type": "application/json" },
      });
    }

    const ctx = `اتجاه المكالمة: ${call.direction || "غير معروف"}. المدة: ${call.total_seconds || 60} ثانية. المتصل: ${call.from_display || "عميل"}. المستقبل: ${call.to_display || "ممثل خدمة"}. العاطفة: ${call.sentiment || "محايد"}.`;

    const aiResp = await fetch("https://ai.gateway.lovable.dev/v1/chat/completions", {
      method: "POST",
      headers: {
        Authorization: `Bearer ${LOVABLE_API_KEY}`,
        "Content-Type": "application/json",
      },
      body: JSON.stringify({
        model: "google/gemini-2.5-flash",
        messages: [
          {
            role: "system",
            content: "أنت مساعد يولّد نصوص مكالمات مركز اتصال واقعية باللغة العربية. يجب أن تستخدم استدعاء الأداة فقط.",
          },
          {
            role: "user",
            content: `ولّد محادثة واقعية بين ممثل خدمة عملاء وعميل بناءً على السياق التالي، ثم لخّصها في 2-3 جمل.\n\n${ctx}\n\nقواعد المحادثة:\n- 6 إلى 12 سطراً متبادلاً.\n- كل سطر بالصيغة: [MM:SS] الوكيل: ...  أو  [MM:SS] العميل: ...\n- اجعل الطوابع الزمنية متصاعدة ومنطقية ضمن مدة المكالمة.\n- لغة عربية فصحى مبسطة وطبيعية.`,
          },
        ],
        tools: [{
          type: "function",
          function: {
            name: "save_call_content",
            description: "حفظ نص المكالمة والملخص المولّدين",
            parameters: {
              type: "object",
              properties: {
                transcript: { type: "string", description: "نص المحادثة بصيغة الأسطر [MM:SS] الدور: النص" },
                summary: { type: "string", description: "ملخص قصير للمكالمة بالعربية" },
              },
              required: ["transcript", "summary"],
              additionalProperties: false,
            },
          },
        }],
        tool_choice: { type: "function", function: { name: "save_call_content" } },
      }),
    });

    if (!aiResp.ok) {
      const t = await aiResp.text();
      console.error("AI error", aiResp.status, t);
      if (aiResp.status === 429) {
        return new Response(JSON.stringify({ error: "تم تجاوز الحد المسموح، حاول لاحقاً." }), {
          status: 429, headers: { ...corsHeaders, "Content-Type": "application/json" },
        });
      }
      if (aiResp.status === 402) {
        return new Response(JSON.stringify({ error: "الرصيد غير كافٍ، يرجى إضافة رصيد." }), {
          status: 402, headers: { ...corsHeaders, "Content-Type": "application/json" },
        });
      }
      throw new Error("AI gateway failed");
    }

    const aiJson = await aiResp.json();
    const toolCall = aiJson.choices?.[0]?.message?.tool_calls?.[0];
    const args = toolCall ? JSON.parse(toolCall.function.arguments) : null;
    if (!args?.transcript || !args?.summary) throw new Error("AI returned no content");

    // Update with service role (RLS already verified above)
    const admin = createClient(SUPABASE_URL, SERVICE_KEY);
    const { error: upErr } = await admin
      .from("calls")
      .update({
        full_transcript: args.transcript,
        summary: args.summary,
        summary_language: "ar",
      })
      .eq("id", callId);
    if (upErr) throw upErr;

    return new Response(JSON.stringify({ summary: args.summary, full_transcript: args.transcript }), {
      headers: { ...corsHeaders, "Content-Type": "application/json" },
    });
  } catch (e) {
    console.error("generate-call-content error", e);
    return new Response(JSON.stringify({ error: e instanceof Error ? e.message : "Unknown error" }), {
      status: 500, headers: { ...corsHeaders, "Content-Type": "application/json" },
    });
  }
});
