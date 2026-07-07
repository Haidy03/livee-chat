-- SIP softphone tables: sip_accounts (per-user, non-secret config) and call_logs.
-- Note: a `contacts` table already exists at the tenant level, so we don't recreate it.

CREATE TABLE public.sip_accounts (
  id UUID NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
  user_id UUID NOT NULL,
  tenant_id UUID NOT NULL,
  display_name TEXT NOT NULL DEFAULT '',
  sip_uri TEXT NOT NULL DEFAULT '',
  auth_id TEXT NOT NULL DEFAULT '',
  ws_url TEXT NOT NULL DEFAULT '',
  stun_urls TEXT[] NOT NULL DEFAULT ARRAY['stun:stun.l.google.com:19302']::text[],
  turn_url TEXT NOT NULL DEFAULT '',
  turn_username TEXT NOT NULL DEFAULT '',
  -- We deliberately do NOT store the SIP password in the database.
  -- It lives in the browser (localStorage, optionally encrypted) and is supplied at register-time.
  is_active BOOLEAN NOT NULL DEFAULT true,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  UNIQUE (user_id)
);

ALTER TABLE public.sip_accounts ENABLE ROW LEVEL SECURITY;

CREATE POLICY "Users view own sip account"
  ON public.sip_accounts FOR SELECT TO authenticated
  USING (auth.uid() = user_id);

CREATE POLICY "Users insert own sip account"
  ON public.sip_accounts FOR INSERT TO authenticated
  WITH CHECK (auth.uid() = user_id AND tenant_id = current_tenant_id());

CREATE POLICY "Users update own sip account"
  ON public.sip_accounts FOR UPDATE TO authenticated
  USING (auth.uid() = user_id)
  WITH CHECK (auth.uid() = user_id);

CREATE POLICY "Users delete own sip account"
  ON public.sip_accounts FOR DELETE TO authenticated
  USING (auth.uid() = user_id);

CREATE TRIGGER trg_sip_accounts_updated_at
  BEFORE UPDATE ON public.sip_accounts
  FOR EACH ROW EXECUTE FUNCTION public.update_updated_at_column();

-- Softphone call logs (separate from the broader `calls` table which is shared analytics data).
CREATE TABLE public.softphone_call_logs (
  id UUID NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
  user_id UUID NOT NULL,
  tenant_id UUID NOT NULL,
  direction TEXT NOT NULL CHECK (direction IN ('in','out')),
  status TEXT NOT NULL CHECK (status IN ('answered','missed','failed','rejected')),
  number TEXT NOT NULL,
  display_name TEXT NOT NULL DEFAULT '',
  contact_id UUID,
  started_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  duration_sec INTEGER NOT NULL DEFAULT 0,
  failure_reason TEXT NOT NULL DEFAULT '',
  created_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE INDEX idx_softphone_call_logs_user_started ON public.softphone_call_logs (user_id, started_at DESC);

ALTER TABLE public.softphone_call_logs ENABLE ROW LEVEL SECURITY;

CREATE POLICY "Users view own softphone logs"
  ON public.softphone_call_logs FOR SELECT TO authenticated
  USING (auth.uid() = user_id);

CREATE POLICY "Users insert own softphone logs"
  ON public.softphone_call_logs FOR INSERT TO authenticated
  WITH CHECK (auth.uid() = user_id AND tenant_id = current_tenant_id());

CREATE POLICY "Users delete own softphone logs"
  ON public.softphone_call_logs FOR DELETE TO authenticated
  USING (auth.uid() = user_id);
