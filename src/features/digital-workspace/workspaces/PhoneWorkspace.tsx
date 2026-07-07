import { useEffect, useState } from "react";
import { toast } from "sonner";
import { SoftphoneWorkspace } from "@/features/softphone/workspace/SoftphoneWorkspace";
import { useSipController } from "@/features/softphone/sip/useSipController";
import { useGlobalKeyboardShortcuts } from "@/features/softphone/hooks/useGlobalKeyboardShortcuts";
import { useSipAutoRegister } from "@/features/softphone/hooks/useSipAutoRegister";
import {
  PermissionsCheckModal,
  shouldShowPermissionsModal,
} from "@/features/softphone/views/PermissionsCheckModal";
import { enableDemoMode } from "@/features/softphone/sip/useSipState";
import { useSoftphone } from "@/features/softphone/store";

const INTERRUPTED_KEY = "softphone:interrupted-call";

export function PhoneWorkspace() {
  useSipController();
  useGlobalKeyboardShortcuts();

  const [permissionsOpen, setPermissionsOpen] = useState(false);
  const [verified, setVerified] = useState(false);
  const setDialed = useSoftphone((s) => s.setDialed);
  useSipAutoRegister(verified);

  useEffect(() => {
    const isDemo = localStorage.getItem("softphone:demo-mode") === "1";
    if (isDemo) enableDemoMode(true);
    try {
      localStorage.removeItem("softphone:sip-password");
    } catch {
      /* ignore */
    }

    // Recover any call that was interrupted by a reload / tab close.
    try {
      const raw = localStorage.getItem(INTERRUPTED_KEY);
      if (raw) {
        const rec = JSON.parse(raw) as { number?: string };
        localStorage.removeItem(INTERRUPTED_KEY);
        if (rec?.number) {
          import("i18next").then(({ default: i18n }) => {
            toast.message(i18n.t("softphone.previousCallEnded", { number: rec.number }));
          });
          setDialed(rec.number);
        }
      }
    } catch {
      /* ignore */
    }

    if (shouldShowPermissionsModal()) {
      setPermissionsOpen(true);
    } else {
      setVerified(true);
    }
  }, [setDialed]);

  return (
    <div className="h-full w-full flex flex-col overflow-hidden">
      <SoftphoneWorkspace />
      <PermissionsCheckModal
        open={permissionsOpen}
        onClose={(ok) => {
          setPermissionsOpen(false);
          if (ok) setVerified(true);
        }}
      />
    </div>
  );
}
