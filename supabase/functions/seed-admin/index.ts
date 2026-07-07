// Seeds the very first admin/owner user. Only works while there are zero users.
import { createClient } from "https://esm.sh/@supabase/supabase-js@2.45.0";

const corsHeaders = {
  "Access-Control-Allow-Origin": "*",
  "Access-Control-Allow-Headers":
    "authorization, x-client-info, apikey, content-type",
  "Access-Control-Allow-Methods": "POST, OPTIONS",
};

Deno.serve(async (req) => {
  if (req.method === "OPTIONS") {
    return new Response(null, { headers: corsHeaders });
  }
  try {
    const SUPABASE_URL = Deno.env.get("SUPABASE_URL")!;
    const SERVICE_ROLE = Deno.env.get("SUPABASE_SERVICE_ROLE_KEY")!;
    const admin = createClient(SUPABASE_URL, SERVICE_ROLE, {
      auth: { autoRefreshToken: false, persistSession: false },
    });

    // Refuse if any user already exists.
    const { count, error: countErr } = await admin
      .from("profiles")
      .select("user_id", { count: "exact", head: true });
    if (countErr) throw countErr;
    if ((count ?? 0) > 0) {
      return new Response(
        JSON.stringify({ error: "users already exist; seeding disabled" }),
        { status: 403, headers: { ...corsHeaders, "Content-Type": "application/json" } },
      );
    }

    const body = await req.json().catch(() => ({}));
    const email = body?.email ?? "admin@example.com";
    const password = body?.password ?? "Soft_1234";
    const role = body?.role ?? "owner";
    const firstName = body?.firstName ?? "Admin";
    const lastName = body?.lastName ?? "User";

    const { data: created, error: createErr } =
      await admin.auth.admin.createUser({
        email,
        password,
        email_confirm: true,
        user_metadata: {
          first_name: firstName,
          last_name: lastName,
          display_name: `${firstName} ${lastName}`,
          role,
        },
      });
    if (createErr) throw createErr;

    return new Response(
      JSON.stringify({ userId: created.user?.id, email }),
      { headers: { ...corsHeaders, "Content-Type": "application/json" } },
    );
  } catch (e) {
    return new Response(
      JSON.stringify({ error: (e as Error).message }),
      { status: 500, headers: { ...corsHeaders, "Content-Type": "application/json" } },
    );
  }
});
