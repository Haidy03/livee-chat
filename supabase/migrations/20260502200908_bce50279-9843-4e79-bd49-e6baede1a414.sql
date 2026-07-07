
-- Remove existing users (cascades to profiles/user_roles via app logic)
DELETE FROM public.user_roles;
DELETE FROM public.profiles;
DELETE FROM auth.users;
