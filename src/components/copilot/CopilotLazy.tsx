import { useMemo } from "react";
import type { CopilotKitCSSProperties } from "@copilotkit/react-ui";
import { CopilotSidebar } from "@copilotkit/react-ui";
import "@copilotkit/react-ui/styles.css";
import "@/copilot-sidebar-overrides.css";
import { ContactCenterCopilotProvider } from "@/copilot/contact-center/ContactCenterCopilotProvider";
import { CopilotSidebarOpenBridge } from "@/copilot/shared/CopilotSidebarOpenBridge";
import { getCopilotLabels } from "@/copilot/shared/constants";
import { MessagePersistenceBridge } from "./MessagePersistenceBridge";
import { CopilotAppContext } from "./CopilotAppContext";

interface Props {
  isOpen: boolean;
  isRtl: boolean;
  onClose: () => void;
  threadOverride: string | null;
}

function readOrCreateThreadId(): string {
  const key = "alk:copilot:thread:default";
  try {
    const existing = localStorage.getItem(key);
    if (existing) return existing;
  } catch {
    /* ignore */
  }
  const fresh = crypto.randomUUID();
  try {
    localStorage.setItem(key, fresh);
  } catch {
    /* ignore */
  }
  return fresh;
}

/**
 * CopilotKit + CopilotSidebar mount.
 *
 * Layout: rendered inside `.alk-copilot-overlay` (a sibling of the app shell,
 * animated via width 0 ↔ 380px by the `data-copilot-open` attribute on the
 * outer `.alk-copilot-shell`). The default floating launcher is suppressed
 * (`Button={() => null}`); open/close is driven by the parent through the
 * `CopilotSidebarOpenBridge` child.
 *
 * We use the v1 classic API (top-level `@copilotkit/react-core` /
 * `@copilotkit/react-ui`) — NOT `/v2` — because our backend runs
 * `@copilotkit/runtime@1.56.x` in classic single-endpoint mode.
 */
export function CopilotLazy({ isOpen, isRtl, onClose, threadOverride }: Props) {
  const labels = useMemo(() => getCopilotLabels(isRtl), [isRtl]);
  const threadId = threadOverride ?? readOrCreateThreadId();

  const themeVars: CopilotKitCSSProperties = {
    "--copilot-kit-primary-color": "hsl(var(--primary))",
    "--copilot-kit-contrast-color": "hsl(var(--primary-foreground))",
  };

  return (
    <div className="alk-copilot-overlay" style={themeVars} aria-hidden={!isOpen}>
      <ContactCenterCopilotProvider isRtl={isRtl} assistantOpen={isOpen} />
      <CopilotAppContext />
      <MessagePersistenceBridge key={threadId} threadId={threadId} />
      <CopilotSidebar
        Button={() => null}
        clickOutsideToClose={false}
        hitEscapeToClose
        defaultOpen={isOpen}
        onSetOpen={(next) => {
          if (!next) onClose();
        }}
        labels={{
          title: labels.title,
          initial: labels.initial,
          placeholder: labels.placeholder,
          error: labels.error,
          stopGenerating: labels.stopGenerating,
          regenerateResponse: labels.regenerateResponse,
          copyToClipboard: labels.copyToClipboard,
          thumbsUp: labels.thumbsUp,
          thumbsDown: labels.thumbsDown,
          copied: labels.copied,
        }}
      >
        <CopilotSidebarOpenBridge open={isOpen} />
      </CopilotSidebar>
    </div>
  );
}
