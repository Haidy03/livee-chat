ALTER TABLE public.profiles
  ADD COLUMN record_inbound_external boolean NOT NULL DEFAULT false,
  ADD COLUMN record_inbound_internal boolean NOT NULL DEFAULT false,
  ADD COLUMN record_outbound_external boolean NOT NULL DEFAULT false,
  ADD COLUMN record_outbound_internal boolean NOT NULL DEFAULT false,
  ADD COLUMN record_on_demand boolean NOT NULL DEFAULT false;