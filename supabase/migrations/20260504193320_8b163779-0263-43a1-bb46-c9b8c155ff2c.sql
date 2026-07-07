
CREATE TABLE public.edit_logs (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  tenant_id uuid NOT NULL,
  user_id uuid NOT NULL,
  entity_type text NOT NULL,
  entity_id text NOT NULL,
  action text NOT NULL,
  field text,
  old_value jsonb,
  new_value jsonb,
  summary text,
  metadata jsonb NOT NULL DEFAULT '{}'::jsonb,
  created_at timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX idx_edit_logs_tenant_created ON public.edit_logs (tenant_id, created_at DESC);
CREATE INDEX idx_edit_logs_entity ON public.edit_logs (entity_type, entity_id);
CREATE INDEX idx_edit_logs_user ON public.edit_logs (user_id);

ALTER TABLE public.edit_logs ENABLE ROW LEVEL SECURITY;

CREATE POLICY "Tenant view edit logs"
ON public.edit_logs FOR SELECT TO authenticated
USING (tenant_id = current_tenant_id());

CREATE POLICY "Users insert own edit logs"
ON public.edit_logs FOR INSERT TO authenticated
WITH CHECK (tenant_id = current_tenant_id() AND auth.uid() = user_id);

CREATE POLICY "Admins update edit logs"
ON public.edit_logs FOR UPDATE TO authenticated
USING (tenant_id = current_tenant_id() AND is_tenant_admin())
WITH CHECK (tenant_id = current_tenant_id() AND is_tenant_admin());

CREATE POLICY "Admins delete edit logs"
ON public.edit_logs FOR DELETE TO authenticated
USING (tenant_id = current_tenant_id() AND is_tenant_admin());
