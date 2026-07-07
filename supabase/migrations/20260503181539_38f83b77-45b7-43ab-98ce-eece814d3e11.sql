
CREATE TABLE IF NOT EXISTS public.rbac_roles (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  name text NOT NULL UNIQUE,
  description text NOT NULL DEFAULT '',
  status text NOT NULL DEFAULT 'active' CHECK (status IN ('active','inactive')),
  is_system boolean NOT NULL DEFAULT false,
  permissions jsonb NOT NULL DEFAULT '{}'::jsonb,
  created_at timestamptz NOT NULL DEFAULT now(),
  updated_at timestamptz NOT NULL DEFAULT now()
);

ALTER TABLE public.rbac_roles ENABLE ROW LEVEL SECURITY;

CREATE POLICY "Roles viewable by authenticated"
  ON public.rbac_roles FOR SELECT TO authenticated USING (true);

CREATE POLICY "Admins can insert roles"
  ON public.rbac_roles FOR INSERT TO authenticated
  WITH CHECK (public.has_role(auth.uid(), 'admin'::app_role));

CREATE POLICY "Admins can update roles"
  ON public.rbac_roles FOR UPDATE TO authenticated
  USING (public.has_role(auth.uid(), 'admin'::app_role))
  WITH CHECK (public.has_role(auth.uid(), 'admin'::app_role));

CREATE POLICY "Admins can delete non-system roles"
  ON public.rbac_roles FOR DELETE TO authenticated
  USING (public.has_role(auth.uid(), 'admin'::app_role) AND is_system = false);

CREATE TRIGGER trg_rbac_roles_updated
  BEFORE UPDATE ON public.rbac_roles
  FOR EACH ROW EXECUTE FUNCTION public.update_updated_at_column();

CREATE TABLE IF NOT EXISTS public.rbac_user_roles (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  user_id uuid NOT NULL,
  role_id uuid NOT NULL REFERENCES public.rbac_roles(id) ON DELETE CASCADE,
  created_at timestamptz NOT NULL DEFAULT now(),
  UNIQUE (user_id, role_id)
);

CREATE INDEX IF NOT EXISTS idx_rbac_user_roles_user ON public.rbac_user_roles(user_id);

ALTER TABLE public.rbac_user_roles ENABLE ROW LEVEL SECURITY;

CREATE POLICY "Users view own assignments or admins view all"
  ON public.rbac_user_roles FOR SELECT TO authenticated
  USING (auth.uid() = user_id OR public.has_role(auth.uid(), 'admin'::app_role));

CREATE POLICY "Admins manage assignments"
  ON public.rbac_user_roles FOR ALL TO authenticated
  USING (public.has_role(auth.uid(), 'admin'::app_role))
  WITH CHECK (public.has_role(auth.uid(), 'admin'::app_role));

-- Seed default roles
INSERT INTO public.rbac_roles (name, description, status, is_system, permissions) VALUES
('Owner', 'Full access to every module and action.', 'active', true,
  '{"dashboard":["view","create","edit","delete","export","approve"],"live":["view","create","edit","delete","export","approve"],"calls":["view","create","edit","delete","export","approve"],"missed_calls":["view","create","edit","delete","export","approve"],"users":["view","create","edit","delete","export","approve"],"groups":["view","create","edit","delete","export","approve"],"reports":["view","create","edit","delete","export","approve"],"directory":["view","create","edit","delete","export","approve"],"ivr":["view","create","edit","delete","export","approve"],"softphone":["view","create","edit","delete","export","approve"],"account_settings":["view","create","edit","delete","export","approve"]}'::jsonb),
('Admin', 'Administrative access without ownership controls.', 'active', true,
  '{"dashboard":["view","create","edit","delete","export","approve"],"live":["view","create","edit","delete","export","approve"],"calls":["view","create","edit","delete","export","approve"],"missed_calls":["view","create","edit","delete","export","approve"],"users":["view","create","edit","delete","export","approve"],"groups":["view","create","edit","delete","export","approve"],"reports":["view","create","edit","delete","export","approve"],"directory":["view","create","edit","delete","export","approve"],"ivr":["view","create","edit","delete","export","approve"],"softphone":["view","create","edit","delete","export","approve"],"account_settings":["view","edit"]}'::jsonb),
('Agent', 'Frontline agent handling calls and conversations.', 'active', true,
  '{"dashboard":["view"],"calls":["view","create","edit"],"missed_calls":["view"],"softphone":["view","create","edit"],"directory":["view"],"reports":["view"],"live":[],"users":[],"groups":[],"ivr":[],"account_settings":[]}'::jsonb)
ON CONFLICT (name) DO NOTHING;
