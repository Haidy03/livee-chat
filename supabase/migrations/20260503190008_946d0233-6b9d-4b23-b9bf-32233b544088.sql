
ALTER TABLE public.rbac_roles DROP CONSTRAINT IF EXISTS rbac_roles_name_key;

-- Now continue from where the previous migration failed.
-- 1. SCHEMA (idempotent)
ALTER TABLE public.profiles ADD COLUMN IF NOT EXISTS tenant_id uuid;
ALTER TABLE public.calls ADD COLUMN IF NOT EXISTS tenant_id uuid;
ALTER TABLE public.contacts ADD COLUMN IF NOT EXISTS tenant_id uuid;
ALTER TABLE public.flows ADD COLUMN IF NOT EXISTS tenant_id uuid;
ALTER TABLE public.tags ADD COLUMN IF NOT EXISTS tenant_id uuid;
ALTER TABLE public.invoices ADD COLUMN IF NOT EXISTS tenant_id uuid;
ALTER TABLE public.voice_library ADD COLUMN IF NOT EXISTS tenant_id uuid;
ALTER TABLE public.rbac_roles ADD COLUMN IF NOT EXISTS tenant_id uuid;
ALTER TABLE public.rbac_user_roles ADD COLUMN IF NOT EXISTS tenant_id uuid;

-- Backfill profiles
DO $$
DECLARE r record; v_tenant uuid;
BEGIN
  UPDATE public.profiles p SET tenant_id = a.id
    FROM public.account_settings a
   WHERE a.user_id = p.user_id AND p.tenant_id IS NULL;
  FOR r IN SELECT user_id FROM public.profiles WHERE tenant_id IS NULL LOOP
    SELECT id INTO v_tenant FROM public.account_settings ORDER BY created_at LIMIT 1;
    IF v_tenant IS NULL THEN
      INSERT INTO public.account_settings (user_id) VALUES (r.user_id) RETURNING id INTO v_tenant;
    END IF;
    UPDATE public.profiles SET tenant_id = v_tenant WHERE user_id = r.user_id;
  END LOOP;
END $$;

UPDATE public.calls c SET tenant_id = p.tenant_id FROM public.profiles p WHERE p.user_id = c.user_id AND c.tenant_id IS NULL;
UPDATE public.contacts c SET tenant_id = p.tenant_id FROM public.profiles p WHERE p.user_id = c.user_id AND c.tenant_id IS NULL;
UPDATE public.flows c SET tenant_id = p.tenant_id FROM public.profiles p WHERE p.user_id = c.user_id AND c.tenant_id IS NULL;
UPDATE public.tags c SET tenant_id = p.tenant_id FROM public.profiles p WHERE p.user_id = c.user_id AND c.tenant_id IS NULL;
UPDATE public.invoices c SET tenant_id = p.tenant_id FROM public.profiles p WHERE p.user_id = c.user_id AND c.tenant_id IS NULL;
UPDATE public.voice_library c SET tenant_id = p.tenant_id FROM public.profiles p WHERE p.user_id = c.user_id AND c.tenant_id IS NULL;

DO $$ DECLARE v_first uuid; BEGIN
  SELECT id INTO v_first FROM public.account_settings ORDER BY created_at LIMIT 1;
  IF v_first IS NOT NULL THEN
    UPDATE public.calls SET tenant_id = v_first WHERE tenant_id IS NULL;
    UPDATE public.contacts SET tenant_id = v_first WHERE tenant_id IS NULL;
    UPDATE public.flows SET tenant_id = v_first WHERE tenant_id IS NULL;
    UPDATE public.tags SET tenant_id = v_first WHERE tenant_id IS NULL;
    UPDATE public.invoices SET tenant_id = v_first WHERE tenant_id IS NULL;
    UPDATE public.voice_library SET tenant_id = v_first WHERE tenant_id IS NULL;
  END IF;
END $$;

UPDATE public.rbac_roles SET tenant_id = NULL, is_system = true WHERE tenant_id IS NULL;

INSERT INTO public.rbac_roles (name, description, status, is_system, permissions, tenant_id)
SELECT t.name, t.description, t.status, false, t.permissions, a.id
FROM public.rbac_roles t
CROSS JOIN public.account_settings a
WHERE t.tenant_id IS NULL
  AND NOT EXISTS (
    SELECT 1 FROM public.rbac_roles r2
    WHERE r2.tenant_id = a.id AND lower(r2.name) = lower(t.name)
  );

UPDATE public.rbac_user_roles ur
SET role_id = sub.new_role_id,
    tenant_id = sub.tenant_id
FROM (
  SELECT ur2.id AS ur_id, p.tenant_id AS tenant_id, new_r.id AS new_role_id
  FROM public.rbac_user_roles ur2
  JOIN public.rbac_roles old_r ON old_r.id = ur2.role_id AND old_r.tenant_id IS NULL
  JOIN public.profiles p ON p.user_id = ur2.user_id
  JOIN public.rbac_roles new_r ON lower(new_r.name) = lower(old_r.name) AND new_r.tenant_id = p.tenant_id
) sub
WHERE ur.id = sub.ur_id;

UPDATE public.rbac_user_roles ur SET tenant_id = p.tenant_id
FROM public.profiles p WHERE p.user_id = ur.user_id AND ur.tenant_id IS NULL;

ALTER TABLE public.profiles ALTER COLUMN tenant_id SET NOT NULL;
ALTER TABLE public.calls ALTER COLUMN tenant_id SET NOT NULL;
ALTER TABLE public.contacts ALTER COLUMN tenant_id SET NOT NULL;
ALTER TABLE public.flows ALTER COLUMN tenant_id SET NOT NULL;
ALTER TABLE public.tags ALTER COLUMN tenant_id SET NOT NULL;
ALTER TABLE public.invoices ALTER COLUMN tenant_id SET NOT NULL;
ALTER TABLE public.voice_library ALTER COLUMN tenant_id SET NOT NULL;
ALTER TABLE public.rbac_user_roles ALTER COLUMN tenant_id SET NOT NULL;

DO $$ BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'account_settings_user_id_key') THEN
    ALTER TABLE public.account_settings ADD CONSTRAINT account_settings_user_id_key UNIQUE (user_id);
  END IF;
END $$;

CREATE INDEX IF NOT EXISTS idx_profiles_tenant ON public.profiles(tenant_id);
CREATE INDEX IF NOT EXISTS idx_calls_tenant ON public.calls(tenant_id);
CREATE INDEX IF NOT EXISTS idx_contacts_tenant ON public.contacts(tenant_id);
CREATE INDEX IF NOT EXISTS idx_flows_tenant ON public.flows(tenant_id);
CREATE INDEX IF NOT EXISTS idx_tags_tenant ON public.tags(tenant_id);
CREATE INDEX IF NOT EXISTS idx_invoices_tenant ON public.invoices(tenant_id);
CREATE INDEX IF NOT EXISTS idx_voice_lib_tenant ON public.voice_library(tenant_id);
CREATE INDEX IF NOT EXISTS idx_rbac_roles_tenant ON public.rbac_roles(tenant_id);
CREATE INDEX IF NOT EXISTS idx_rbac_user_roles_tenant ON public.rbac_user_roles(tenant_id);

CREATE UNIQUE INDEX IF NOT EXISTS rbac_roles_tenant_name_key
  ON public.rbac_roles (tenant_id, lower(name)) WHERE tenant_id IS NOT NULL;

CREATE OR REPLACE FUNCTION public.current_tenant_id()
RETURNS uuid LANGUAGE sql STABLE SECURITY DEFINER SET search_path = public AS $$
  SELECT tenant_id FROM public.profiles WHERE user_id = auth.uid() LIMIT 1
$$;

CREATE OR REPLACE FUNCTION public.is_tenant_admin()
RETURNS boolean LANGUAGE sql STABLE SECURITY DEFINER SET search_path = public AS $$
  SELECT EXISTS (
    SELECT 1 FROM public.rbac_user_roles ur
    JOIN public.rbac_roles r ON r.id = ur.role_id
    WHERE ur.user_id = auth.uid()
      AND ur.tenant_id = public.current_tenant_id()
      AND lower(r.name) IN ('owner','admin')
  )
$$;

CREATE OR REPLACE FUNCTION public.has_role(_user_id uuid, _role app_role)
RETURNS boolean LANGUAGE sql STABLE SECURITY DEFINER SET search_path = public AS $$
  SELECT EXISTS (
    SELECT 1 FROM public.rbac_user_roles ur
    JOIN public.rbac_roles r ON r.id = ur.role_id
    JOIN public.profiles p ON p.user_id = ur.user_id
    WHERE ur.user_id = _user_id
      AND ur.tenant_id = p.tenant_id
      AND ((_role::text = 'admin' AND lower(r.name) IN ('owner','admin'))
           OR lower(r.name) = lower(_role::text))
  )
$$;

DROP POLICY IF EXISTS "Admins can update any profile" ON public.profiles;
DROP POLICY IF EXISTS "Profiles viewable by authenticated users" ON public.profiles;
DROP POLICY IF EXISTS "Users can insert own profile" ON public.profiles;
DROP POLICY IF EXISTS "Users can update own profile" ON public.profiles;
CREATE POLICY "Tenant members view profiles" ON public.profiles FOR SELECT TO authenticated USING (tenant_id = public.current_tenant_id());
CREATE POLICY "Insert own profile" ON public.profiles FOR INSERT TO authenticated WITH CHECK (auth.uid() = user_id);
CREATE POLICY "Users update own profile" ON public.profiles FOR UPDATE TO authenticated USING (auth.uid() = user_id) WITH CHECK (auth.uid() = user_id);
CREATE POLICY "Admins update tenant profiles" ON public.profiles FOR UPDATE TO authenticated USING (tenant_id = public.current_tenant_id() AND public.is_tenant_admin()) WITH CHECK (tenant_id = public.current_tenant_id() AND public.is_tenant_admin());

DROP POLICY IF EXISTS "Users can insert own account settings" ON public.account_settings;
DROP POLICY IF EXISTS "Users can update own account settings" ON public.account_settings;
DROP POLICY IF EXISTS "Users can view own account settings" ON public.account_settings;
CREATE POLICY "Tenant members view settings" ON public.account_settings FOR SELECT TO authenticated USING (id = public.current_tenant_id());
CREATE POLICY "Tenant admins update settings" ON public.account_settings FOR UPDATE TO authenticated USING (id = public.current_tenant_id() AND public.is_tenant_admin()) WITH CHECK (id = public.current_tenant_id() AND public.is_tenant_admin());
CREATE POLICY "Provision tenant on signup" ON public.account_settings FOR INSERT TO authenticated WITH CHECK (auth.uid() = user_id);

DROP POLICY IF EXISTS "Delete calls (own or admin)" ON public.calls;
DROP POLICY IF EXISTS "Update calls (own, assigned, or admin)" ON public.calls;
DROP POLICY IF EXISTS "Users can create own calls" ON public.calls;
DROP POLICY IF EXISTS "View calls (own, assigned, or admin)" ON public.calls;
CREATE POLICY "Tenant view calls" ON public.calls FOR SELECT TO authenticated USING (tenant_id = public.current_tenant_id());
CREATE POLICY "Tenant insert calls" ON public.calls FOR INSERT TO authenticated WITH CHECK (tenant_id = public.current_tenant_id());
CREATE POLICY "Tenant update calls" ON public.calls FOR UPDATE TO authenticated USING (tenant_id = public.current_tenant_id()) WITH CHECK (tenant_id = public.current_tenant_id());
CREATE POLICY "Tenant delete calls" ON public.calls FOR DELETE TO authenticated USING (tenant_id = public.current_tenant_id() AND (auth.uid() = user_id OR public.is_tenant_admin()));

DROP POLICY IF EXISTS "Users can create own contacts" ON public.contacts;
DROP POLICY IF EXISTS "Users can delete own contacts" ON public.contacts;
DROP POLICY IF EXISTS "Users can update own contacts" ON public.contacts;
DROP POLICY IF EXISTS "Users can view own contacts" ON public.contacts;
CREATE POLICY "Tenant view contacts" ON public.contacts FOR SELECT TO authenticated USING (tenant_id = public.current_tenant_id());
CREATE POLICY "Tenant insert contacts" ON public.contacts FOR INSERT TO authenticated WITH CHECK (tenant_id = public.current_tenant_id());
CREATE POLICY "Tenant update contacts" ON public.contacts FOR UPDATE TO authenticated USING (tenant_id = public.current_tenant_id()) WITH CHECK (tenant_id = public.current_tenant_id());
CREATE POLICY "Tenant delete contacts" ON public.contacts FOR DELETE TO authenticated USING (tenant_id = public.current_tenant_id());

DROP POLICY IF EXISTS "Users can create own flows" ON public.flows;
DROP POLICY IF EXISTS "Users can delete own flows" ON public.flows;
DROP POLICY IF EXISTS "Users can update own flows" ON public.flows;
DROP POLICY IF EXISTS "Users can view own flows" ON public.flows;
CREATE POLICY "Tenant view flows" ON public.flows FOR SELECT TO authenticated USING (tenant_id = public.current_tenant_id());
CREATE POLICY "Tenant insert flows" ON public.flows FOR INSERT TO authenticated WITH CHECK (tenant_id = public.current_tenant_id());
CREATE POLICY "Tenant update flows" ON public.flows FOR UPDATE TO authenticated USING (tenant_id = public.current_tenant_id()) WITH CHECK (tenant_id = public.current_tenant_id());
CREATE POLICY "Tenant delete flows" ON public.flows FOR DELETE TO authenticated USING (tenant_id = public.current_tenant_id());

DROP POLICY IF EXISTS "Users can create own tags" ON public.tags;
DROP POLICY IF EXISTS "Users can delete own tags" ON public.tags;
DROP POLICY IF EXISTS "Users can update own tags" ON public.tags;
DROP POLICY IF EXISTS "Users can view own tags" ON public.tags;
CREATE POLICY "Tenant view tags" ON public.tags FOR SELECT TO authenticated USING (tenant_id = public.current_tenant_id());
CREATE POLICY "Tenant insert tags" ON public.tags FOR INSERT TO authenticated WITH CHECK (tenant_id = public.current_tenant_id());
CREATE POLICY "Tenant update tags" ON public.tags FOR UPDATE TO authenticated USING (tenant_id = public.current_tenant_id()) WITH CHECK (tenant_id = public.current_tenant_id());
CREATE POLICY "Tenant delete tags" ON public.tags FOR DELETE TO authenticated USING (tenant_id = public.current_tenant_id());

DROP POLICY IF EXISTS "Users can delete own invoices" ON public.invoices;
DROP POLICY IF EXISTS "Users can insert own invoices" ON public.invoices;
DROP POLICY IF EXISTS "Users can update own invoices" ON public.invoices;
DROP POLICY IF EXISTS "Users can view own invoices" ON public.invoices;
CREATE POLICY "Tenant view invoices" ON public.invoices FOR SELECT TO authenticated USING (tenant_id = public.current_tenant_id());
CREATE POLICY "Tenant insert invoices" ON public.invoices FOR INSERT TO authenticated WITH CHECK (tenant_id = public.current_tenant_id());
CREATE POLICY "Tenant update invoices" ON public.invoices FOR UPDATE TO authenticated USING (tenant_id = public.current_tenant_id() AND public.is_tenant_admin()) WITH CHECK (tenant_id = public.current_tenant_id() AND public.is_tenant_admin());
CREATE POLICY "Tenant delete invoices" ON public.invoices FOR DELETE TO authenticated USING (tenant_id = public.current_tenant_id() AND public.is_tenant_admin());

DROP POLICY IF EXISTS "Users can create own voice library" ON public.voice_library;
DROP POLICY IF EXISTS "Users can delete own voice library" ON public.voice_library;
DROP POLICY IF EXISTS "Users can update own voice library" ON public.voice_library;
DROP POLICY IF EXISTS "Users can view own voice library" ON public.voice_library;
CREATE POLICY "Tenant view voice" ON public.voice_library FOR SELECT TO authenticated USING (tenant_id = public.current_tenant_id());
CREATE POLICY "Tenant insert voice" ON public.voice_library FOR INSERT TO authenticated WITH CHECK (tenant_id = public.current_tenant_id());
CREATE POLICY "Tenant update voice" ON public.voice_library FOR UPDATE TO authenticated USING (tenant_id = public.current_tenant_id()) WITH CHECK (tenant_id = public.current_tenant_id());
CREATE POLICY "Tenant delete voice" ON public.voice_library FOR DELETE TO authenticated USING (tenant_id = public.current_tenant_id());

DROP POLICY IF EXISTS "Admins can delete non-system roles" ON public.rbac_roles;
DROP POLICY IF EXISTS "Admins can insert roles" ON public.rbac_roles;
DROP POLICY IF EXISTS "Admins can update roles" ON public.rbac_roles;
DROP POLICY IF EXISTS "Roles viewable by authenticated" ON public.rbac_roles;
CREATE POLICY "Tenant view roles" ON public.rbac_roles FOR SELECT TO authenticated USING (tenant_id IS NULL OR tenant_id = public.current_tenant_id());
CREATE POLICY "Tenant admins insert roles" ON public.rbac_roles FOR INSERT TO authenticated WITH CHECK (tenant_id = public.current_tenant_id() AND public.is_tenant_admin());
CREATE POLICY "Tenant admins update roles" ON public.rbac_roles FOR UPDATE TO authenticated USING (tenant_id = public.current_tenant_id() AND public.is_tenant_admin()) WITH CHECK (tenant_id = public.current_tenant_id() AND public.is_tenant_admin());
CREATE POLICY "Tenant admins delete roles" ON public.rbac_roles FOR DELETE TO authenticated USING (tenant_id = public.current_tenant_id() AND public.is_tenant_admin() AND is_system = false);

DROP POLICY IF EXISTS "Admins manage assignments" ON public.rbac_user_roles;
DROP POLICY IF EXISTS "Users view own assignments or admins view all" ON public.rbac_user_roles;
CREATE POLICY "View assignments" ON public.rbac_user_roles FOR SELECT TO authenticated USING (auth.uid() = user_id OR (tenant_id = public.current_tenant_id() AND public.is_tenant_admin()));
CREATE POLICY "Admins manage assignments" ON public.rbac_user_roles FOR ALL TO authenticated USING (tenant_id = public.current_tenant_id() AND public.is_tenant_admin()) WITH CHECK (tenant_id = public.current_tenant_id() AND public.is_tenant_admin());

CREATE OR REPLACE FUNCTION public.handle_new_user()
RETURNS trigger LANGUAGE plpgsql SECURITY DEFINER SET search_path = public AS $function$
DECLARE
  meta jsonb := COALESCE(NEW.raw_user_meta_data, '{}'::jsonb);
  v_first text := COALESCE(meta->>'first_name', '');
  v_last  text := COALESCE(meta->>'last_name', '');
  v_display text := NULLIF(trim(COALESCE(meta->>'display_name', concat_ws(' ', v_first, v_last))), '');
  v_role text := COALESCE(meta->>'role', 'agent');
  v_groups text[] := CASE WHEN meta ? 'groups' AND jsonb_typeof(meta->'groups') = 'array'
    THEN ARRAY(SELECT jsonb_array_elements_text(meta->'groups')) ELSE '{}'::text[] END;
  v_role_name text;
  v_role_id uuid;
  v_tenant uuid;
  v_provided_tenant uuid := NULLIF(meta->>'tenant_id','')::uuid;
BEGIN
  IF v_provided_tenant IS NOT NULL THEN
    v_tenant := v_provided_tenant;
  ELSE
    INSERT INTO public.account_settings (user_id) VALUES (NEW.id) RETURNING id INTO v_tenant;
    INSERT INTO public.rbac_roles (name, description, status, is_system, permissions, tenant_id)
    SELECT name, description, status, false, permissions, v_tenant
    FROM public.rbac_roles WHERE tenant_id IS NULL;
    v_role := 'owner';
  END IF;

  v_role_name := CASE WHEN v_role = 'owner' THEN 'Owner' WHEN v_role = 'admin' THEN 'Admin' ELSE 'Agent' END;

  INSERT INTO public.profiles (
    user_id, display_name, first_name, last_name, email,
    timezone, language, browser_notifications, role, groups, extension_number, tenant_id
  ) VALUES (
    NEW.id, COALESCE(v_display, NEW.email), v_first, v_last, NEW.email,
    COALESCE(meta->>'timezone', 'UTC+00:00'),
    COALESCE(meta->>'language', 'English'),
    COALESCE((meta->>'browser_notifications')::boolean, false),
    v_role, v_groups, NULLIF(meta->>'extension_number','')::integer, v_tenant
  );

  SELECT id INTO v_role_id FROM public.rbac_roles WHERE lower(name) = lower(v_role_name) AND tenant_id = v_tenant LIMIT 1;
  IF v_role_id IS NOT NULL THEN
    INSERT INTO public.rbac_user_roles (user_id, role_id, tenant_id) VALUES (NEW.id, v_role_id, v_tenant) ON CONFLICT DO NOTHING;
  END IF;
  RETURN NEW;
END;
$function$;

DROP TRIGGER IF EXISTS on_auth_user_created ON auth.users;
CREATE TRIGGER on_auth_user_created AFTER INSERT ON auth.users FOR EACH ROW EXECUTE FUNCTION public.handle_new_user();
