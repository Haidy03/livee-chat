CREATE TABLE public.account_settings (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  user_id uuid NOT NULL UNIQUE,
  org_name text NOT NULL DEFAULT '',
  wait_time int NOT NULL DEFAULT 30,
  dialer_method text NOT NULL DEFAULT 'GET',
  dialer_url text NOT NULL DEFAULT '',
  param_name text NOT NULL DEFAULT 'caller',
  number_format text NOT NULL DEFAULT 'intl-no-prefix',
  away_status text NOT NULL DEFAULT 'Away',
  domains text NOT NULL DEFAULT '',
  call_tags text NOT NULL DEFAULT '',
  auto_tagging boolean NOT NULL DEFAULT true,
  default_country text NOT NULL DEFAULT 'SA',
  show_inbound boolean NOT NULL DEFAULT false,
  allow_transfer_away boolean NOT NULL DEFAULT true,
  allow_reject boolean NOT NULL DEFAULT true,
  auto_assign boolean NOT NULL DEFAULT false,
  acw_in boolean NOT NULL DEFAULT false,
  acw_out boolean NOT NULL DEFAULT false,
  auto_answer boolean NOT NULL DEFAULT true,
  auto_answer_secs int NOT NULL DEFAULT 30,
  internal_timeout boolean NOT NULL DEFAULT false,
  outbound_ring_limit boolean NOT NULL DEFAULT false,
  limit_ivr boolean NOT NULL DEFAULT true,
  ivr_timeout int NOT NULL DEFAULT 30,
  created_at timestamptz NOT NULL DEFAULT now(),
  updated_at timestamptz NOT NULL DEFAULT now()
);

ALTER TABLE public.account_settings ENABLE ROW LEVEL SECURITY;

CREATE POLICY "Users can view own account settings" ON public.account_settings
  FOR SELECT TO authenticated USING (auth.uid() = user_id);

CREATE POLICY "Users can insert own account settings" ON public.account_settings
  FOR INSERT TO authenticated WITH CHECK (auth.uid() = user_id);

CREATE POLICY "Users can update own account settings" ON public.account_settings
  FOR UPDATE TO authenticated USING (auth.uid() = user_id) WITH CHECK (auth.uid() = user_id);

CREATE TRIGGER update_account_settings_updated_at
  BEFORE UPDATE ON public.account_settings
  FOR EACH ROW EXECUTE FUNCTION public.update_updated_at_column();