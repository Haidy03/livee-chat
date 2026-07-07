import { decodeToken } from "./apiClient";

/** Resolved from JWT tenant_id claim (server-side isolation). */
export function getCurrentTenantId(): string {
  const tid = decodeToken()?.tenant_id;
  if (!tid) throw new Error("No tenant in token");
  return tid;
}

/** Legacy hook — tenant now lives on the JWT. */
export function clearTenantCache(): void {}
