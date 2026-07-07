
-- Agents see calls assigned to them; admins/owners see all
DROP POLICY IF EXISTS "Users can view own calls" ON public.calls;
CREATE POLICY "View calls (own, assigned, or admin)"
  ON public.calls FOR SELECT TO authenticated
  USING (
    auth.uid() = user_id
    OR auth.uid()::text = agent_id
    OR public.has_role(auth.uid(), 'admin'::public.app_role)
  );

DROP POLICY IF EXISTS "Users can update own calls" ON public.calls;
CREATE POLICY "Update calls (own, assigned, or admin)"
  ON public.calls FOR UPDATE TO authenticated
  USING (
    auth.uid() = user_id
    OR auth.uid()::text = agent_id
    OR public.has_role(auth.uid(), 'admin'::public.app_role)
  )
  WITH CHECK (
    auth.uid() = user_id
    OR auth.uid()::text = agent_id
    OR public.has_role(auth.uid(), 'admin'::public.app_role)
  );

DROP POLICY IF EXISTS "Users can delete own calls" ON public.calls;
CREATE POLICY "Delete calls (own or admin)"
  ON public.calls FOR DELETE TO authenticated
  USING (
    auth.uid() = user_id
    OR public.has_role(auth.uid(), 'admin'::public.app_role)
  );
