CREATE TABLE public.calls (
  id uuid NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
  user_id uuid NOT NULL,
  direction text NOT NULL,
  status text NOT NULL,
  started_at timestamptz NOT NULL,
  ring_seconds integer NOT NULL DEFAULT 0,
  hold_seconds integer NOT NULL DEFAULT 0,
  active_seconds integer NOT NULL DEFAULT 0,
  total_seconds integer NOT NULL DEFAULT 0,
  total_hold_seconds integer NOT NULL DEFAULT 0,
  agent_id text,
  group_id text,
  caller text NOT NULL DEFAULT '',
  called text NOT NULL DEFAULT '',
  tag_ids text[] NOT NULL DEFAULT '{}',
  auto_tag_ids text[] NOT NULL DEFAULT '{}',
  sentiment text,
  inputs text NOT NULL DEFAULT '',
  has_recording boolean NOT NULL DEFAULT false,
  notes text NOT NULL DEFAULT '',
  created_at timestamptz NOT NULL DEFAULT now(),
  updated_at timestamptz NOT NULL DEFAULT now()
);

ALTER TABLE public.calls ENABLE ROW LEVEL SECURITY;

CREATE POLICY "Users can view own calls" ON public.calls
  FOR SELECT TO authenticated USING (auth.uid() = user_id);
CREATE POLICY "Users can create own calls" ON public.calls
  FOR INSERT TO authenticated WITH CHECK (auth.uid() = user_id);
CREATE POLICY "Users can update own calls" ON public.calls
  FOR UPDATE TO authenticated USING (auth.uid() = user_id);
CREATE POLICY "Users can delete own calls" ON public.calls
  FOR DELETE TO authenticated USING (auth.uid() = user_id);

CREATE INDEX idx_calls_user_started ON public.calls (user_id, started_at DESC);

CREATE TRIGGER update_calls_updated_at
  BEFORE UPDATE ON public.calls
  FOR EACH ROW EXECUTE FUNCTION public.update_updated_at_column();