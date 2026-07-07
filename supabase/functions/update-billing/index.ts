import { createClient } from "https://esm.sh/@supabase/supabase-js@2.57.4";

const corsHeaders = {
  "Access-Control-Allow-Origin": "*",
  "Access-Control-Allow-Headers":
    "authorization, x-client-info, apikey, content-type",
  "Access-Control-Allow-Methods": "POST, OPTIONS",
};

interface BillingPayload {
  invoice_name?: string;
  billing_address?: string;
  billing_country?: string;
  vat_number?: string;
  registration_number?: string;
  billing_emails?: string;
  send_invoices_to_admins?: boolean;
  notify_on_agent_changes?: boolean;
}

function bad(msg: string, status = 400) {
  return new Response(JSON.stringify({ error: msg }), {
    status,
    headers: { ...corsHeaders, "Content-Type": "application/json" },
  });
}

Deno.serve(async (req) => {
  if (req.method === "OPTIONS") {
    return new Response(null, { headers: corsHeaders });
  }
  if (req.method !== "POST") return bad("Method not allowed", 405);

  const authHeader = req.headers.get("Authorization") ?? "";
  if (!authHeader.startsWith("Bearer ")) return bad("Unauthorized", 401);

  const supabase = createClient(
    Deno.env.get("SUPABASE_URL")!,
    Deno.env.get("SUPABASE_ANON_KEY")!,
    { global: { headers: { Authorization: authHeader } } },
  );

  const { data: userData, error: userErr } = await supabase.auth.getUser();
  if (userErr || !userData.user) return bad("Unauthorized", 401);
  const userId = userData.user.id;

  let body: BillingPayload;
  try {
    body = await req.json();
  } catch {
    return bad("Invalid JSON");
  }

  const trim = (v: unknown) => (typeof v === "string" ? v.trim() : "");
  const billing_address = trim(body.billing_address);
  const billing_country = trim(body.billing_country);
  const vat_number = trim(body.vat_number);
  if (!billing_address || !billing_country || !vat_number) {
    return bad("Missing required fields: billing_address, billing_country, vat_number");
  }

  const emails = trim(body.billing_emails);
  if (emails) {
    const list = emails.split(/[,;\s]+/).filter(Boolean);
    const re = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    if (!list.every((e) => re.test(e))) return bad("Invalid email in billing_emails");
  }

  const payload = {
    user_id: userId,
    invoice_name: trim(body.invoice_name) || undefined,
    billing_address,
    billing_country,
    vat_number,
    registration_number: trim(body.registration_number),
    billing_emails: emails,
    send_invoices_to_admins: !!body.send_invoices_to_admins,
    notify_on_agent_changes: !!body.notify_on_agent_changes,
  };

  const { data, error } = await supabase
    .from("account_settings")
    .upsert(payload, { onConflict: "user_id" })
    .select()
    .maybeSingle();

  if (error) return bad(error.message, 500);

  return new Response(JSON.stringify({ ok: true, data }), {
    headers: { ...corsHeaders, "Content-Type": "application/json" },
  });
});
