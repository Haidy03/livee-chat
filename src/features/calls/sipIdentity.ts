import { getCurrentTenantId } from "@/lib/tenant";

export type SipIdentityKind = "ext" | "ai" | "phone";

export interface SipIdentity {
  kind: SipIdentityKind;
  extension?: string;
  raw: string;
}

const UUID_RE = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

function stripTenantSuffix(user: string, tenantId?: string): string {
  if (tenantId && user.toLowerCase().endsWith(`-${tenantId.toLowerCase()}`)) {
    return user.slice(0, user.length - tenantId.length - 1);
  }
  // Fallback: strip a trailing -<uuid> shape
  const idx = user.lastIndexOf("-");
  if (idx > 0) {
    const tail = user.slice(idx + 1);
    if (UUID_RE.test(tail)) return user.slice(0, idx);
  }
  return user;
}

export function parseSipIdentity(raw: string, tenantIdArg?: string): SipIdentity {
  const value = (raw ?? "").trim();
  if (!value) return { kind: "phone", raw: value };

  let tenantId = tenantIdArg;
  if (!tenantId) {
    try { tenantId = getCurrentTenantId(); } catch { tenantId = undefined; }
  }

  // Strip sip: prefix and @host
  let user = value;
  if (/^sips?:/i.test(user)) user = user.replace(/^sips?:/i, "");
  const atIdx = user.indexOf("@");
  if (atIdx >= 0) user = user.slice(0, atIdx);

  // AI pipeline
  if (/^AI-/i.test(user)) {
    const rest = user.slice(3);
    const ext = stripTenantSuffix(rest, tenantId);
    if (/^\d{2,8}$/.test(ext)) return { kind: "ai", extension: ext, raw: value };
    return { kind: "ai", extension: ext || undefined, raw: value };
  }

  const ext = stripTenantSuffix(user, tenantId);
  if (/^\d{2,8}$/.test(ext)) return { kind: "ext", extension: ext, raw: value };

  return { kind: "phone", raw: value };
}
