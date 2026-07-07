
-- Extend profiles with full user info
ALTER TABLE public.profiles
  ADD COLUMN IF NOT EXISTS first_name text,
  ADD COLUMN IF NOT EXISTS last_name text,
  ADD COLUMN IF NOT EXISTS email text,
  ADD COLUMN IF NOT EXISTS timezone text NOT NULL DEFAULT 'UTC+00:00',
  ADD COLUMN IF NOT EXISTS language text NOT NULL DEFAULT 'English',
  ADD COLUMN IF NOT EXISTS browser_notifications boolean NOT NULL DEFAULT false,
  ADD COLUMN IF NOT EXISTS role text NOT NULL DEFAULT 'agent',
  ADD COLUMN IF NOT EXISTS groups text[] NOT NULL DEFAULT '{}'::text[],
  ADD COLUMN IF NOT EXISTS extension_number integer,
  ADD COLUMN IF NOT EXISTS status text NOT NULL DEFAULT 'offline',
  ADD COLUMN IF NOT EXISTS disabled boolean NOT NULL DEFAULT false;

CREATE INDEX IF NOT EXISTS idx_profiles_user_id ON public.profiles(user_id);

-- Replace handle_new_user to populate the new fields from sign-up metadata
CREATE OR REPLACE FUNCTION public.handle_new_user()
RETURNS trigger
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path TO 'public'
AS $function$
DECLARE
  meta jsonb := COALESCE(NEW.raw_user_meta_data, '{}'::jsonb);
  v_first text := COALESCE(meta->>'first_name', '');
  v_last  text := COALESCE(meta->>'last_name', '');
  v_display text := NULLIF(trim(COALESCE(meta->>'display_name', concat_ws(' ', v_first, v_last))), '');
  v_role text := COALESCE(meta->>'role', 'agent');
  v_groups text[] := CASE
    WHEN meta ? 'groups' AND jsonb_typeof(meta->'groups') = 'array'
    THEN ARRAY(SELECT jsonb_array_elements_text(meta->'groups'))
    ELSE '{}'::text[]
  END;
BEGIN
  INSERT INTO public.profiles (
    user_id, display_name,
    first_name, last_name, email,
    timezone, language, browser_notifications,
    role, groups, extension_number
  ) VALUES (
    NEW.id,
    COALESCE(v_display, NEW.email),
    v_first,
    v_last,
    NEW.email,
    COALESCE(meta->>'timezone', 'UTC+00:00'),
    COALESCE(meta->>'language', 'English'),
    COALESCE((meta->>'browser_notifications')::boolean, false),
    v_role,
    v_groups,
    NULLIF(meta->>'extension_number','')::integer
  );

  INSERT INTO public.user_roles (user_id, role)
  VALUES (
    NEW.id,
    CASE WHEN v_role IN ('admin','owner') THEN 'admin'::app_role ELSE 'user'::app_role END
  )
  ON CONFLICT DO NOTHING;

  RETURN NEW;
END;
$function$;

-- Ensure trigger exists on auth.users
DROP TRIGGER IF EXISTS on_auth_user_created ON auth.users;
CREATE TRIGGER on_auth_user_created
AFTER INSERT ON auth.users
FOR EACH ROW EXECUTE FUNCTION public.handle_new_user();

-- Allow admins to update any profile (in addition to existing self-update policy)
DROP POLICY IF EXISTS "Admins can update any profile" ON public.profiles;
CREATE POLICY "Admins can update any profile"
ON public.profiles
FOR UPDATE
TO authenticated
USING (public.has_role(auth.uid(), 'admin'::app_role))
WITH CHECK (public.has_role(auth.uid(), 'admin'::app_role));
