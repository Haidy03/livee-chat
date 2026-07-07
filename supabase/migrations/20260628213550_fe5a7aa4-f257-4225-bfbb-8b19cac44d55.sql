
-- 1) Restrict SECURITY DEFINER function is_tenant_owner to authenticated only
REVOKE EXECUTE ON FUNCTION public.is_tenant_owner() FROM PUBLIC, anon;
GRANT EXECUTE ON FUNCTION public.is_tenant_owner() TO authenticated, service_role;

-- 2) Prevent profile self-update role escalation
DROP POLICY IF EXISTS "Users update own profile" ON public.profiles;
CREATE POLICY "Users update own profile"
ON public.profiles
FOR UPDATE
TO authenticated
USING (auth.uid() = user_id)
WITH CHECK (
  auth.uid() = user_id
  AND role = (SELECT p.role FROM public.profiles p WHERE p.user_id = auth.uid())
  AND tenant_id = (SELECT p.tenant_id FROM public.profiles p WHERE p.user_id = auth.uid())
);

-- 3) Tighten sip_accounts SELECT policy to require tenant scope
DROP POLICY IF EXISTS "Owner or admin view sip accounts" ON public.sip_accounts;
CREATE POLICY "Owner or admin view sip accounts"
ON public.sip_accounts
FOR SELECT
TO authenticated
USING (
  tenant_id = current_tenant_id()
  AND (auth.uid() = user_id OR is_tenant_admin())
);
