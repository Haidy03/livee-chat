
CREATE TABLE public.voice_library (
  id UUID NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
  user_id UUID NOT NULL,
  name TEXT NOT NULL,
  source TEXT NOT NULL CHECK (source IN ('upload','tts','url')),
  text TEXT,
  url TEXT,
  file_path TEXT,
  language TEXT NOT NULL DEFAULT 'ar',
  voice TEXT NOT NULL DEFAULT 'female',
  interruptible BOOLEAN NOT NULL DEFAULT true,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

ALTER TABLE public.voice_library ENABLE ROW LEVEL SECURITY;

CREATE POLICY "Users can view own voice library"
  ON public.voice_library FOR SELECT TO authenticated
  USING (auth.uid() = user_id);

CREATE POLICY "Users can create own voice library"
  ON public.voice_library FOR INSERT TO authenticated
  WITH CHECK (auth.uid() = user_id);

CREATE POLICY "Users can update own voice library"
  ON public.voice_library FOR UPDATE TO authenticated
  USING (auth.uid() = user_id) WITH CHECK (auth.uid() = user_id);

CREATE POLICY "Users can delete own voice library"
  ON public.voice_library FOR DELETE TO authenticated
  USING (auth.uid() = user_id);

CREATE TRIGGER update_voice_library_updated_at
  BEFORE UPDATE ON public.voice_library
  FOR EACH ROW EXECUTE FUNCTION public.update_updated_at_column();

INSERT INTO storage.buckets (id, name, public)
VALUES ('voice-library', 'voice-library', false)
ON CONFLICT (id) DO NOTHING;

CREATE POLICY "Users read own voice files"
  ON storage.objects FOR SELECT TO authenticated
  USING (bucket_id = 'voice-library' AND auth.uid()::text = (storage.foldername(name))[1]);

CREATE POLICY "Users upload own voice files"
  ON storage.objects FOR INSERT TO authenticated
  WITH CHECK (bucket_id = 'voice-library' AND auth.uid()::text = (storage.foldername(name))[1]);

CREATE POLICY "Users update own voice files"
  ON storage.objects FOR UPDATE TO authenticated
  USING (bucket_id = 'voice-library' AND auth.uid()::text = (storage.foldername(name))[1]);

CREATE POLICY "Users delete own voice files"
  ON storage.objects FOR DELETE TO authenticated
  USING (bucket_id = 'voice-library' AND auth.uid()::text = (storage.foldername(name))[1]);
