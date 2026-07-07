REVOKE EXECUTE ON FUNCTION public.handle_new_user() FROM PUBLIC, anon, authenticated;
REVOKE EXECUTE ON FUNCTION public.has_role(uuid, public.app_role) FROM PUBLIC, anon;
GRANT  EXECUTE ON FUNCTION public.has_role(uuid, public.app_role) TO authenticated, service_role;
REVOKE EXECUTE ON FUNCTION public.is_tenant_admin() FROM PUBLIC, anon;
GRANT  EXECUTE ON FUNCTION public.is_tenant_admin() TO authenticated, service_role;
REVOKE EXECUTE ON FUNCTION public.current_tenant_id() FROM PUBLIC, anon;
GRANT  EXECUTE ON FUNCTION public.current_tenant_id() TO authenticated, service_role;
ALTER PUBLICATION supabase_realtime DROP TABLE public.calls;