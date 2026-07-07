import { useEffect } from "react";
import { useChatContext } from "@copilotkit/react-ui";

/**
 * Renders as a child of <CopilotSidebar> and syncs an external `open` prop
 * into CopilotKit's internal ChatContext via setOpen(). This lets the parent
 * control open/close without remounting the sidebar (which would blow away
 * the chat state).
 */
export function CopilotSidebarOpenBridge({ open }: { open: boolean }) {
  const { setOpen } = useChatContext();
  useEffect(() => {
    setOpen(open);
  }, [open, setOpen]);
  return null;
}
