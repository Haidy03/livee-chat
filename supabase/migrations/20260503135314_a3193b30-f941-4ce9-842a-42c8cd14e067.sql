ALTER TABLE public.account_settings
  ADD COLUMN IF NOT EXISTS invoice_name text NOT NULL DEFAULT 'شركة اساس القرار للاتصالات وتقنية المعلومات',
  ADD COLUMN IF NOT EXISTS billing_address text NOT NULL DEFAULT '',
  ADD COLUMN IF NOT EXISTS billing_country text NOT NULL DEFAULT '',
  ADD COLUMN IF NOT EXISTS vat_number text NOT NULL DEFAULT '',
  ADD COLUMN IF NOT EXISTS registration_number text NOT NULL DEFAULT '',
  ADD COLUMN IF NOT EXISTS billing_emails text NOT NULL DEFAULT '',
  ADD COLUMN IF NOT EXISTS send_invoices_to_admins boolean NOT NULL DEFAULT true,
  ADD COLUMN IF NOT EXISTS notify_on_agent_changes boolean NOT NULL DEFAULT true,
  ADD COLUMN IF NOT EXISTS payment_methods jsonb NOT NULL DEFAULT '[]'::jsonb;