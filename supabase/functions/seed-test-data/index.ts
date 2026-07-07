// One-off seeding: creates 2 agent users and a batch of mock calls.
// Idempotent on agents (skips if email exists). Always inserts calls.
import { createClient } from "https://esm.sh/@supabase/supabase-js@2.45.0";

const corsHeaders = {
  "Access-Control-Allow-Origin": "*",
  "Access-Control-Allow-Headers":
    "authorization, x-client-info, apikey, content-type",
  "Access-Control-Allow-Methods": "POST, OPTIONS",
};

const AGENTS = [
  { email: "ahmed.agent@example.com", firstName: "Ahmed", lastName: "Hassan", extension: 1001 },
  { email: "sara.agent@example.com", firstName: "Sara", lastName: "Mahmoud", extension: 1002 },
];

const DIRECTIONS = ["incoming", "outgoing", "internal"] as const;
const STATUSES = ["completed", "missed", "no-answer", "rejected", "voicemail"] as const;
const SENTIMENTS = ["positive", "neutral", "negative", null] as const;

function randomPick<T>(arr: readonly T[]): T {
  return arr[Math.floor(Math.random() * arr.length)];
}
function randomPhone() {
  return "+201" + Math.floor(100000000 + Math.random() * 900000000);
}

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

    // 1) Find owner (to attach calls under their user_id, since RLS is per-user)
    const { data: ownerProfile } = await admin
      .from("profiles")
      .select("user_id")
      .eq("role", "owner")
      .limit(1)
      .maybeSingle();
    if (!ownerProfile) {
      return new Response(JSON.stringify({ error: "no owner found; create admin first" }), {
        status: 400,
        headers: { ...corsHeaders, "Content-Type": "application/json" },
      });
    }
    const ownerId = ownerProfile.user_id as string;

    // 2) Create agents (skip if email exists)
    const createdAgents: { id: string; email: string }[] = [];
    for (const a of AGENTS) {
      const { data: existing } = await admin
        .from("profiles")
        .select("user_id")
        .eq("email", a.email)
        .maybeSingle();
      if (existing) {
        createdAgents.push({ id: existing.user_id, email: a.email });
        continue;
      }
      const { data: created, error: createErr } =
        await admin.auth.admin.createUser({
          email: a.email,
          password: "Soft_1234",
          email_confirm: true,
          user_metadata: {
            first_name: a.firstName,
            last_name: a.lastName,
            display_name: `${a.firstName} ${a.lastName}`,
            role: "agent",
            extension_number: String(a.extension),
          },
        });
      if (createErr) throw createErr;
      createdAgents.push({ id: created.user!.id, email: a.email });
    }

    // 3) Generate calls
    const now = Date.now();
    const rows: any[] = [];
    const total = 60;
    for (let i = 0; i < total; i++) {
      const startedOffsetMs = Math.floor(Math.random() * 14 * 24 * 60 * 60 * 1000); // last 14 days
      const startedAt = new Date(now - startedOffsetMs).toISOString();
      const ring = Math.floor(Math.random() * 30);
      const active = Math.floor(Math.random() * 600);
      const hold = Math.floor(Math.random() * 60);
      const totalSec = ring + active + hold;
      const direction = randomPick(DIRECTIONS);
      const status = randomPick(STATUSES);
      const agent = randomPick(createdAgents);
      rows.push({
        user_id: ownerId,
        agent_id: agent.id,
        direction,
        status,
        started_at: startedAt,
        ring_seconds: ring,
        hold_seconds: hold,
        active_seconds: active,
        total_seconds: totalSec,
        total_hold_seconds: hold,
        caller: direction === "incoming" ? randomPhone() : "+20100" + agent.id.slice(0, 7),
        called: direction === "outgoing" ? randomPhone() : "+20100" + agent.id.slice(0, 7),
        sentiment: randomPick(SENTIMENTS),
        has_recording: Math.random() > 0.5,
        notes: "",
        inputs: "",
        tag_ids: [],
        auto_tag_ids: [],
      });
    }
    const { error: callsErr, count } = await admin
      .from("calls")
      .insert(rows, { count: "exact" });
    if (callsErr) throw callsErr;

    return new Response(
      JSON.stringify({
        agents: createdAgents,
        callsInserted: count ?? rows.length,
      }),
      { headers: { ...corsHeaders, "Content-Type": "application/json" } },
    );
  } catch (e) {
    return new Response(
      JSON.stringify({ error: (e as Error).message }),
      { status: 500, headers: { ...corsHeaders, "Content-Type": "application/json" } },
    );
  }
});
