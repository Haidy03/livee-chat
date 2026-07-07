import { useEffect, useRef } from "react";
import { toast } from "sonner";
import { useAuth } from "@/hooks/useAuth";
import { useAgents, agentLabel } from "@/hooks/useAgents";
import { decodeToken } from "@/lib/apiClient";
import { getSipAdapter, getSipSnapshot } from "@/features/softphone/sip/useSipState";

/**
 * Auto-register the SIP adapter once when mounted, using the signed-in
 * agent's extension + tenant. Mirrors the behavior in /softphone so other
 * surfaces (e.g. Digital Workspace phone channel) connect on load.
 */
export function useSipAutoRegister(enabled: boolean = true) {
  const { user } = useAuth();
  const agents = useAgents();
  const myAgent = user ? agents.find((a) => a.user_id === user.id) : undefined;
  const ext = myAgent?.extension_number ?? null;
  const tenantId = decodeToken()?.tenant_id ?? null;
  const registeredRef = useRef(false);

  useEffect(() => {
    if (!enabled || registeredRef.current) return;
    if (localStorage.getItem("softphone:demo-mode") === "1") return;
    if (!user || !ext || !tenantId) return;

    const snap = getSipSnapshot();
    if (snap.registration === "registered" || snap.registration === "connecting" || snap.call) {
      registeredRef.current = true;
      return;
    }

    registeredRef.current = true;

    const domain = (import.meta.env.VITE_SIP_DOMAIN as string | undefined) ?? "alkhwarizmi.alkhwarizmi.ai";
    const wsUrl = (import.meta.env.VITE_SIP_WS_URL as string | undefined) ?? "wss://pbx.alkhwarizmi.cloud:8089/ws";
    const password = (import.meta.env.VITE_SIP_PASSWORD as string | undefined) ?? "soft_123";
    const stunUrls = ((import.meta.env.VITE_SIP_STUN_URLS as string | undefined) ?? "stun:stun.l.google.com:19302")
      .split(",").map((s) => s.trim()).filter(Boolean);

    const authId = `${ext}-${tenantId}`;
    const sipUri = `sip:${authId}@${domain}`;
    const displayName = agentLabel(myAgent);

    void (async () => {
      try {
        if (!wsUrl.startsWith("wss://")) {
          toast.error("SIP WebSocket URL is invalid");
          registeredRef.current = false;
          return;
        }
        await getSipAdapter().register({
          displayName,
          sipUri,
          authId,
          password,
          wsUrl,
          stunUrls,
          turnUrl: "",
          turnUsername: "",
        });
      } catch (err) {
        registeredRef.current = false;
        toast.error(`SIP registration failed: ${(err as Error).message}`);
      }
    })();
  }, [enabled, user, ext, tenantId, myAgent]);
}
