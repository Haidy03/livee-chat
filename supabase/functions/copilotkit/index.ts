// CopilotKit runtime (v2) hosted on a Supabase Edge Function (Deno).
// Uses @ai-sdk/azure for Azure OpenAI.
import {
  CopilotRuntime,
  createCopilotRuntimeHandler,
  BuiltInAgent,
} from "npm:@copilotkit/runtime/v2";
import { createAzure } from "npm:@ai-sdk/azure";

// Silence the AI SDK "System messages in the prompt or messages fields can be
// a security risk" warning. CopilotKit forwards readable-context as system
// messages by design, and the default warning is emitted on every run.
// @ts-ignore — global flag read by the AI SDK at runtime
globalThis.AI_SDK_LOG_WARNINGS = false;

// --- Normalize the Azure endpoint to the resource root + "/openai" ---
function normalizeAzureEndpoint(raw: string | undefined): string {
  if (!raw) throw new Error("AZURE_OPENAI_ENDPOINT is not set");
  let url = raw.trim().replace(/\/+$/, "");
  url = url.replace(/\/openai\/deployments\/.+$/i, "");
  url = url.replace(/\/deployments\/.+$/i, "");
  if (!/\/openai$/i.test(url)) url = `${url}/openai`;
  return url;
}

const azure = createAzure({
  baseURL: normalizeAzureEndpoint(Deno.env.get("AZURE_OPENAI_ENDPOINT")),
  apiKey: Deno.env.get("AZURE_OPENAI_API_KEY"),
  apiVersion: Deno.env.get("AZURE_OPENAI_API_VERSION"),
  useDeploymentBasedUrls: true,
});

const deployment = Deno.env.get("AZURE_OPENAI_DEPLOYMENT_NAME");
if (!deployment) throw new Error("AZURE_OPENAI_DEPLOYMENT_NAME is not set");

const agent = new BuiltInAgent({
  model: azure.chat(deployment),
  system:
    "You are the Dialplan Builder Copilot for a contact-center IVR editor. " +
    "Always reply with concise natural-language text. Use the available tools " +
    "(proposeDialplanChanges, applyProposal, validateDialplan, explainDialplan, " +
    "focusCanvasNode, navigateTo, etc.) when the user asks you to modify or " +
    "inspect the dialplan. Use the user's language (Arabic or English).",
});

const runtime = new CopilotRuntime({
  agents: {
    default: agent,
  },
});

const handler = createCopilotRuntimeHandler({
  runtime,
  basePath: "/copilotkit",
  mode: "multi-route",
  cors: true,
});

Deno.serve(async (req: Request) => {
  const url = new URL(req.url);
  console.log(`[copilotkit] ${req.method} ${url.pathname}`);
  try {
    const res = await handler(req);
    console.log(`[copilotkit] -> ${res.status} ${url.pathname}`);
    return res;
  } catch (err) {
    console.error("[copilotkit] handler error:", err, (err as Error)?.stack);
    return new Response(
      JSON.stringify({ error: (err as Error)?.message ?? String(err) }),
      { status: 500, headers: { "Content-Type": "application/json" } },
    );
  }
});

