import { useAgentContext, useFrontendTool } from "@/lib/copilotkit-compat";
import { useNavigate } from "react-router-dom";
import { z } from "zod";
import { useAuth } from "@/hooks/useAuth";
import { useLiveSnapshot } from "@/hooks/useLiveSnapshot";
import { useCopilotUiContext } from "./CopilotProvider";

/**
 * Wires app state and frontend tools into the Copilot (v2 client).
 * Mount once inside the authenticated AppShell.
 */
export function CopilotAppContext() {
  const navigate = useNavigate();
  const { user } = useAuth();
  const { isCopilotOpen } = useCopilotUiContext();
  const snapshot = useLiveSnapshot({ enabled: isCopilotOpen, refetchInterval: false }).data ?? null;

  // --- READABLE CONTEXT ---
  useAgentContext({
    description: "Currently signed-in user (id, email).",
    value: user ? { id: user.id, email: user.email ?? "" } : null,
  });

  useAgentContext({
    description: "Live contact-center snapshot: queue stats and currently active calls.",
    value: (snapshot as any) ?? { note: "Live snapshot not loaded yet." },
  });

  // --- FRONTEND TOOLS ---
  useFrontendTool({
    name: "navigateTo",
    description:
      "Navigate the user to an internal app route, e.g. /dashboard, /live/queue-monitor, /calls, /surveys.",
    parameters: z.object({
      path: z.string().describe("Absolute app path starting with '/'."),
    }),
    handler: async ({ path }) => {
      if (!path?.startsWith("/")) return { ok: false, error: "Path must start with /" };
      navigate(path);
      return { ok: true, navigatedTo: path };
    },
  });

  useFrontendTool({
    name: "filterQueueMonitor",
    description:
      "Open the live queue monitor filtered by a specific queue name or status.",
    parameters: z.object({
      queueName: z.string().optional().describe("Queue name to filter by."),
      status: z.string().optional().describe("Status filter (e.g. waiting, talking)."),
    }),
    handler: async ({ queueName, status }) => {
      const params = new URLSearchParams();
      if (queueName) params.set("queue", queueName);
      if (status) params.set("status", status);
      const qs = params.toString();
      navigate(`/live/queue-monitor${qs ? `?${qs}` : ""}`);
      return { ok: true };
    },
  });

  useFrontendTool({
    name: "createSurvey",
    description: "Open the new-survey editor, optionally prefilling a title.",
    parameters: z.object({
      title: z.string().optional().describe("Optional survey title."),
    }),
    handler: async ({ title }) => {
      const qs = title ? `?title=${encodeURIComponent(title)}` : "";
      navigate(`/surveys/new${qs}`);
      return { ok: true };
    },
  });

  return null;
}
