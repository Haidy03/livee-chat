
CREATE TABLE public.wrapup_codes (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  tenant_id uuid NOT NULL,
  code text NOT NULL,
  label text NOT NULL,
  label_ar text,
  category text NOT NULL DEFAULT 'general',
  color text NOT NULL DEFAULT '#64748b',
  is_active boolean NOT NULL DEFAULT true,
  sort_order integer NOT NULL DEFAULT 0,
  created_at timestamptz NOT NULL DEFAULT now(),
  updated_at timestamptz NOT NULL DEFAULT now(),
  UNIQUE (tenant_id, code)
);

GRANT SELECT, INSERT, UPDATE, DELETE ON public.wrapup_codes TO authenticated;
GRANT ALL ON public.wrapup_codes TO service_role;

ALTER TABLE public.wrapup_codes ENABLE ROW LEVEL SECURITY;

CREATE POLICY "Tenant members can view wrapup codes"
  ON public.wrapup_codes FOR SELECT TO authenticated
  USING (tenant_id = public.current_tenant_id());

CREATE POLICY "Tenant admins can insert wrapup codes"
  ON public.wrapup_codes FOR INSERT TO authenticated
  WITH CHECK (tenant_id = public.current_tenant_id() AND public.is_tenant_admin());

CREATE POLICY "Tenant admins can update wrapup codes"
  ON public.wrapup_codes FOR UPDATE TO authenticated
  USING (tenant_id = public.current_tenant_id() AND public.is_tenant_admin())
  WITH CHECK (tenant_id = public.current_tenant_id() AND public.is_tenant_admin());

CREATE POLICY "Tenant admins can delete wrapup codes"
  ON public.wrapup_codes FOR DELETE TO authenticated
  USING (tenant_id = public.current_tenant_id() AND public.is_tenant_admin());

CREATE TRIGGER trg_wrapup_codes_updated_at
  BEFORE UPDATE ON public.wrapup_codes
  FOR EACH ROW EXECUTE FUNCTION public.update_updated_at_column();

CREATE TABLE public.queue_wrapup_codes (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  tenant_id uuid NOT NULL,
  queue_id text NOT NULL,
  wrapup_code_id uuid NOT NULL REFERENCES public.wrapup_codes(id) ON DELETE CASCADE,
  created_at timestamptz NOT NULL DEFAULT now(),
  UNIQUE (queue_id, wrapup_code_id)
);

GRANT SELECT, INSERT, UPDATE, DELETE ON public.queue_wrapup_codes TO authenticated;
GRANT ALL ON public.queue_wrapup_codes TO service_role;

ALTER TABLE public.queue_wrapup_codes ENABLE ROW LEVEL SECURITY;

CREATE POLICY "Tenant members can view queue wrapup mappings"
  ON public.queue_wrapup_codes FOR SELECT TO authenticated
  USING (tenant_id = public.current_tenant_id());

CREATE POLICY "Tenant admins manage queue wrapup mappings - insert"
  ON public.queue_wrapup_codes FOR INSERT TO authenticated
  WITH CHECK (tenant_id = public.current_tenant_id() AND public.is_tenant_admin());

CREATE POLICY "Tenant admins manage queue wrapup mappings - delete"
  ON public.queue_wrapup_codes FOR DELETE TO authenticated
  USING (tenant_id = public.current_tenant_id() AND public.is_tenant_admin());

CREATE INDEX idx_queue_wrapup_codes_queue ON public.queue_wrapup_codes(queue_id);
CREATE INDEX idx_wrapup_codes_tenant ON public.wrapup_codes(tenant_id);

-- Seed defaults per tenant
INSERT INTO public.wrapup_codes (tenant_id, code, label, category, color, sort_order)
SELECT a.id, v.code, v.label, v.category, v.color, v.ord
FROM public.account_settings a
CROSS JOIN (VALUES
  ('resolved', 'Resolved', 'positive', '#16A34A', 1),
  ('question_answered', 'Question answered', 'positive', '#16A34A', 2),
  ('callback_scheduled', 'Callback scheduled', 'follow_up', '#2563EB', 3),
  ('escalated', 'Escalated', 'negative', '#DC2626', 4),
  ('transferred', 'Transferred', 'neutral', '#B45309', 5),
  ('no_resolution', 'No resolution', 'negative', '#DC2626', 6)
) AS v(code, label, category, color, ord)
ON CONFLICT (tenant_id, code) DO NOTHING;
