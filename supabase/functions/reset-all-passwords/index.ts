import { createClient } from "https://esm.sh/@supabase/supabase-js@2.45.0";

const corsHeaders = {
  "Access-Control-Allow-Origin": "*",
  "Access-Control-Allow-Headers": "authorization, x-client-info, apikey, content-type",
};

Deno.serve(async (req) => {
  if (req.method === "OPTIONS") return new Response(null, { headers: corsHeaders });
  const admin = createClient(
    Deno.env.get("SUPABASE_URL")!,
    Deno.env.get("SUPABASE_SERVICE_ROLE_KEY")!,
    { auth: { autoRefreshToken: false, persistSession: false } },
  );
  const password = "Tamer@3hya";
  const results: { id: string; email?: string; ok: boolean; error?: string }[] = [];
  let page = 1;
  while (true) {
    const { data, error } = await admin.auth.admin.listUsers({ page, perPage: 200 });
    if (error) return new Response(JSON.stringify({ error: error.message }), { status: 500, headers: corsHeaders });
    for (const u of data.users) {
      const { error: e } = await admin.auth.admin.updateUserById(u.id, { password });
      results.push({ id: u.id, email: u.email, ok: !e, error: e?.message });
    }
    if (data.users.length < 200) break;
    page++;
  }
  return new Response(JSON.stringify({ updated: results.length, results }), {
    headers: { ...corsHeaders, "Content-Type": "application/json" },
  });
});
