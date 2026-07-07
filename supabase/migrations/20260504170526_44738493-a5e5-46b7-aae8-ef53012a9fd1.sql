DROP INDEX IF EXISTS public.calls_call_id_uniq;
ALTER TABLE public.calls ADD CONSTRAINT calls_call_id_key UNIQUE (call_id);