import { getRoleFromToken } from "@/lib/apiClient";
import { useAuth } from "@/hooks/useAuth";

export type AppRole = "owner" | "admin" | "agent";

function normalizeRole(raw: string | undefined): AppRole {
  const r = (raw ?? "").toLowerCase();
  if (r === "owner") return "owner";
  if (r === "admin") return "admin";
  return "agent";
}

export function useUserRole() {
 
  const { user, loading: authLoading } = useAuth();
  const role = user ? normalizeRole(getRoleFromToken()) : null;

  return {
    role,
    loading: authLoading,
    isOwner: role === "owner",
    isAdmin: role === "admin",
    isAgent: role === "agent",
    canManageUsers: role === "owner" || role === "admin",
  };
}
