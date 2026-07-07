import { getCurrentTenantId } from "@/lib/tenant";

/**
 * Append `-<tenantId>` to a plain internal extension so the SIP INVITE
 * routes to the correct tenant context (e.g. `2004` -> `2004-<tenantId>`).
 *
 * Leaves targets alone when they already look like a full URI, contain `@`,
 * carry a tenant suffix, or include non-extension chars like `+ * #`.
 */
export function withTenantExtension(target: string): string {
  const t = (target ?? "").trim();
  if (!t) return t;
  // Already a URI or fully-qualified target — don't touch.
  if (t.includes("@") || t.toLowerCase().startsWith("sip:")) return t;
  // Looks like a PSTN number or carries DTMF/control chars — don't touch.
  if (/[+*#]/.test(t)) return t;
  // Plain internal extension: digits only, no existing tenant suffix.
  if (!/^\d{2,8}$/.test(t)) return t;
  try {
    const tid = getCurrentTenantId();
    if (!tid) return t;
    return `${t}-${tid}`;
  } catch {
    return t;
  }
}
