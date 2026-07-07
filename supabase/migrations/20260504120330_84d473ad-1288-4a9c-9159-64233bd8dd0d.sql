-- Drop existing user-only policies on sip_accounts
DROP POLICY IF EXISTS "Users view own sip account" ON public.sip_accounts;
DROP POLICY IF EXISTS "Users insert own sip account" ON public.sip_accounts;
DROP POLICY IF EXISTS "Users update own sip account" ON public.sip_accounts;
DROP POLICY IF EXISTS "Users delete own sip account" ON public.sip_accounts;

-- Tenant-scoped view: members of the tenant or the owning user
CREATE POLICY "Tenant view sip accounts"
ON public.sip_accounts
FOR SELECT
TO authenticated
USING (
  tenant_id = public.current_tenant_id()
  OR auth.uid() = user_id
);

-- Insert: must be own row AND belong to caller's tenant
CREATE POLICY "Users insert own sip account"
ON public.sip_accounts
FOR INSERT
TO authenticated
WITH CHECK (
  auth.uid() = user_id
  AND tenant_id = public.current_tenant_id()
);

-- Update: own row, OR tenant admin within same tenant
CREATE POLICY "Update own or tenant admin sip account"
ON public.sip_accounts
FOR UPDATE
TO authenticated
USING (
  (auth.uid() = user_id AND tenant_id = public.current_tenant_id())
  OR (tenant_id = public.current_tenant_id() AND public.is_tenant_admin())
)
WITH CHECK (
  (auth.uid() = user_id AND tenant_id = public.current_tenant_id())
  OR (tenant_id = public.current_tenant_id() AND public.is_tenant_admin())
);

-- Delete: own row, OR tenant admin within same tenant
CREATE POLICY "Delete own or tenant admin sip account"
ON public.sip_accounts
FOR DELETE
TO authenticated
USING (
  (auth.uid() = user_id AND tenant_id = public.current_tenant_id())
  OR (tenant_id = public.current_tenant_id() AND public.is_tenant_admin())
);

-- Index for tenant lookups
CREATE INDEX IF NOT EXISTS idx_sip_accounts_tenant_id ON public.sip_accounts(tenant_id);