ALTER TABLE public.calls
  ADD COLUMN IF NOT EXISTS summary text,
  ADD COLUMN IF NOT EXISTS full_transcript text,
  ADD COLUMN IF NOT EXISTS summary_language varchar(10) DEFAULT 'ar',
  ADD COLUMN IF NOT EXISTS summary_accuracy_feedback varchar(20);