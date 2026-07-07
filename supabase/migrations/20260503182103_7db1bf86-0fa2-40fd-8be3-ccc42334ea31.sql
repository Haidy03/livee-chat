
INSERT INTO public.rbac_user_roles (user_id, role_id)
SELECT p.user_id, r.id
FROM public.profiles p
JOIN public.rbac_roles r
  ON lower(r.name) = lower(CASE
    WHEN p.role = 'owner' THEN 'Owner'
    WHEN p.role = 'admin' THEN 'Admin'
    ELSE 'Agent'
  END)
ON CONFLICT (user_id, role_id) DO NOTHING;

CREATE OR REPLACE FUNCTION public.has_role(_user_id uuid, _role app_role)
RETURNS boolean
LANGUAGE sql
STABLE
SECURITY DEFINER
SET search_path TO 'public'
AS $$
  SELECT EXISTS (
    SELECT 1
    FROM public.rbac_user_roles ur
    JOIN public.rbac_roles r ON r.id = ur.role_id
    WHERE ur.user_id = _user_id
      AND (
        (_role::text = 'admin' AND lower(r.name) IN ('owner','admin'))
        OR lower(r.name) = lower(_role::text)
      )
  )
$$;

CREATE OR REPLACE FUNCTION public.handle_new_user()
RETURNS trigger
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path TO 'public'
AS $$
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
  v_role_name text := CASE
    WHEN v_role = 'owner' THEN 'Owner'
    WHEN v_role = 'admin' THEN 'Admin'
    ELSE 'Agent'
  END;
  v_role_id uuid;
BEGIN
  INSERT INTO public.profiles (
    user_id, display_name,
    first_name, last_name, email,
    timezone, language, browser_notifications,
    role, groups, extension_number
  ) VALUES (
    NEW.id,
    COALESCE(v_display, NEW.email),
    v_first, v_last, NEW.email,
    COALESCE(meta->>'timezone', 'UTC+00:00'),
    COALESCE(meta->>'language', 'English'),
    COALESCE((meta->>'browser_notifications')::boolean, false),
    v_role, v_groups,
    NULLIF(meta->>'extension_number','')::integer
  );

  SELECT id INTO v_role_id FROM public.rbac_roles WHERE lower(name) = lower(v_role_name) LIMIT 1;
  IF v_role_id IS NOT NULL THEN
    INSERT INTO public.rbac_user_roles (user_id, role_id)
    VALUES (NEW.id, v_role_id)
    ON CONFLICT DO NOTHING;
  END IF;

  RETURN NEW;
END;
$$;

DROP TABLE IF EXISTS public.user_roles CASCADE;
DROP FUNCTION IF EXISTS app_private.has_role(uuid, app_role) CASCADE;
