ALTER TABLE public.calls
  ADD COLUMN IF NOT EXISTS call_id text,
  ADD COLUMN IF NOT EXISTS from_uri text,
  ADD COLUMN IF NOT EXISTS from_display text,
  ADD COLUMN IF NOT EXISTS to_uri text,
  ADD COLUMN IF NOT EXISTS to_display text,
  ADD COLUMN IF NOT EXISTS answered_at timestamptz,
  ADD COLUMN IF NOT EXISTS ended_at timestamptz,
  ADD COLUMN IF NOT EXISTS hangup_cause text,
  ADD COLUMN IF NOT EXISTS recording_url text;

CREATE UNIQUE INDEX IF NOT EXISTS calls_call_id_uniq ON public.calls(call_id) WHERE call_id IS NOT NULL;
CREATE INDEX IF NOT EXISTS calls_user_started_idx ON public.calls(user_id, started_at DESC);

ALTER PUBLICATION supabase_realtime ADD TABLE public.calls;