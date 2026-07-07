import { createContext, ReactNode, useCallback, useContext, useMemo, useState, useEffect } from "react";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import { CopilotKit } from "@copilotkit/react-core";

/**
 * Lazy Copilot mount.
 *
 * The heavy CopilotKit runtime (which calls `/api/copilotkit/info` and other
 * runtime endpoints on mount) is NOT rendered until the user actually opens
 * the assistant panel. Before that, this provider just exposes a lightweight
 * context that lets the topbar button toggle Copilot.
 *
 * When closed, no CopilotKit code runs and no Copilot network requests fire.
 */

const RUNTIME_URL =
  (import.meta.env.VITE_COPILOT_RUNTIME_URL as string | undefined) ??
  "https://copilotkit.alkhwarizmi.pro/api/copilotkit";

interface CopilotUiContextValue {
  isCopilotOpen: boolean;
  setIsCopilotOpen: (open: boolean) => void;
  toggleCopilot: () => void;
  copilotAvailable: boolean;
  threadId: string | null;
  setThreadId: (id: string | null) => void;
  resetThread: () => void;
}

const CopilotUiContext = createContext<CopilotUiContextValue | null>(null);

export function useCopilotUiContext() {
  const ctx = useContext(CopilotUiContext);
  if (!ctx) throw new Error("useCopilotUiContext must be used inside CopilotProvider");
  return ctx;
}

const GLOBAL_THREAD_STORAGE_KEY = "alk:copilot:thread:default";
const OPEN_STATE_STORAGE_KEY = "alk:copilot:open";

function clearPersistedOpenState() {
  try {
    localStorage.removeItem(OPEN_STATE_STORAGE_KEY);
  } catch {
    /* ignore */
  }
}

export function CopilotProvider({ children }: { children: ReactNode }) {
  const { i18n } = useTranslation();
  const isRtl = i18n.language?.toLowerCase().startsWith("ar") ?? false;

  // Always start closed. Ignore any persisted open flag so Copilot never
  // auto-mounts (and never fires runtime requests) on page load.
  const [isCopilotOpen, setIsCopilotOpen] = useState(false);
  const [threadId, setThreadId] = useState<string | null>(null);
  // Once the user opens the assistant for the first time we keep the
  // CopilotKit runtime mounted so subsequent toggles just animate width
  // (preserving chat state, avoiding re-fetches).
  const [providerMounted, setProviderMounted] = useState(false);

  useEffect(() => {
    clearPersistedOpenState();
  }, []);

  const copilotAvailable = Boolean(RUNTIME_URL);

  const setIsCopilotOpenAndPersist = useCallback((open: boolean) => {
    setIsCopilotOpen(open);
    if (open) setProviderMounted(true);
  }, []);

  const toggleCopilot = useCallback(() => {
    if (!copilotAvailable) {
      toast.error(isRtl ? "المساعد غير متاح حالياً." : "Assistant is not available right now.");
      return;
    }
    setIsCopilotOpenAndPersist(!isCopilotOpen);
  }, [copilotAvailable, isRtl, isCopilotOpen, setIsCopilotOpenAndPersist]);

  const resetThread = useCallback(() => {
    try {
      localStorage.removeItem(GLOBAL_THREAD_STORAGE_KEY);
    } catch {
      /* ignore */
    }
    setThreadId(null);
    setIsCopilotOpen(false);
  }, []);

  const value = useMemo<CopilotUiContextValue>(
    () => ({
      isCopilotOpen,
      setIsCopilotOpen: setIsCopilotOpenAndPersist,
      toggleCopilot,
      copilotAvailable,
      threadId,
      setThreadId,
      resetThread,
    }),
    [isCopilotOpen, setIsCopilotOpenAndPersist, toggleCopilot, copilotAvailable, threadId, resetThread],
  );

  return (
    <CopilotUiContext.Provider value={value}>
      <CopilotKit runtimeUrl={RUNTIME_URL}>
        <div
          className="alk-copilot-shell"
          dir={isRtl ? "rtl" : "ltr"}
          data-copilot-open={isCopilotOpen ? "" : undefined}
        >
          <div className="alk-shell-content">{children}</div>
          {providerMounted && (
            <CopilotLazyMount
              isOpen={isCopilotOpen}
              isRtl={isRtl}
              onClose={() => setIsCopilotOpenAndPersist(false)}
              threadOverride={threadId}
            />
          )}
        </div>
      </CopilotKit>
    </CopilotUiContext.Provider>
  );
}

// ---------------------------------------------------------------------------
// Everything below is lazy — only imported and rendered after first open.
// ---------------------------------------------------------------------------

function CopilotLazyMount(props: {
  isOpen: boolean;
  isRtl: boolean;
  onClose: () => void;
  threadOverride: string | null;
}) {
  // Dynamically import at first render. Using require-style dynamic import
  // via React.lazy would need Suspense; a simple state-based dynamic import
  // keeps things predictable and avoids Suspense boundaries in the shell.
  const [Loaded, setLoaded] = useState<null | React.ComponentType<typeof props>>(null);

  useEffect(() => {
    let cancelled = false;
    import("./CopilotLazy").then((mod) => {
      if (!cancelled) setLoaded(() => mod.CopilotLazy);
    });
    return () => {
      cancelled = true;
    };
  }, []);

  if (!Loaded) return null;
  return <Loaded {...props} />;
}
