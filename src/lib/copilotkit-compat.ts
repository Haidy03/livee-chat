/**
 * Compat shim: exposes the v2-style hook names (`useFrontendTool`,
 * `useHumanInTheLoop`, `useAgentContext`) on top of the v1 API so we can keep
 * our existing call sites (which pass zod schemas) working while the runtime
 * stays on v1 classic mode.
 */
import {
  useCopilotReadable,
  useFrontendTool as useFrontendToolV1,
  useHumanInTheLoop as useHumanInTheLoopV1,
} from "@copilotkit/react-core";
import type { Parameter } from "@copilotkit/shared";
import type { ZodTypeAny, ZodObject } from "zod";

function unwrap(schema: ZodTypeAny): { inner: ZodTypeAny; required: boolean } {
  let s: any = schema;
  let required = true;
  while (s?._def?.typeName === "ZodOptional" || s?._def?.typeName === "ZodDefault" || s?._def?.typeName === "ZodNullable") {
    required = false;
    s = s._def.innerType;
  }
  return { inner: s, required };
}

function zodToParameter(name: string, schema: ZodTypeAny): Parameter {
  const { inner, required } = unwrap(schema);
  const def: any = (inner as any)._def ?? {};
  const description: string | undefined = def.description ?? (schema as any)._def?.description;
  const tn: string | undefined = def.typeName;

  switch (tn) {
    case "ZodString":
      return { name, type: "string", description, required } as Parameter;
    case "ZodNumber":
      return { name, type: "number", description, required } as Parameter;
    case "ZodBoolean":
      return { name, type: "boolean", description, required } as Parameter;
    case "ZodEnum":
      return {
        name,
        type: "string",
        description,
        required,
        enum: def.values as string[],
      } as Parameter;
    case "ZodArray": {
      const item = def.type as ZodTypeAny;
      const itemDef: any = (item as any)?._def;
      const itemTn = itemDef?.typeName;
      if (itemTn === "ZodObject") {
        return {
          name,
          type: "object[]",
          description,
          required,
          attributes: zodObjectToParameters(item as ZodObject<any>),
        } as Parameter;
      }
      const scalar =
        itemTn === "ZodNumber" ? "number[]" : itemTn === "ZodBoolean" ? "boolean[]" : "string[]";
      return { name, type: scalar as any, description, required } as Parameter;
    }
    case "ZodObject":
      return {
        name,
        type: "object",
        description,
        required,
        attributes: zodObjectToParameters(inner as ZodObject<any>),
      } as Parameter;
    case "ZodRecord":
    case "ZodAny":
    case "ZodUnknown":
      return { name, type: "object", description, required } as Parameter;
    default:
      // Fallback — treat unknown types as string, keeps runtime happy.
      return { name, type: "string", description, required } as Parameter;
  }
}

function zodObjectToParameters(schema: ZodObject<any>): Parameter[] {
  const shape = typeof (schema as any)._def?.shape === "function"
    ? (schema as any)._def.shape()
    : (schema as any).shape;
  if (!shape) return [];
  return Object.entries(shape).map(([name, sch]) => zodToParameter(name, sch as ZodTypeAny));
}

function toParameters(parameters: unknown): Parameter[] {
  if (!parameters) return [];
  if (Array.isArray(parameters)) return parameters as Parameter[];
  const anySchema = parameters as any;
  if (anySchema?._def?.typeName === "ZodObject") {
    return zodObjectToParameters(anySchema as ZodObject<any>);
  }
  return [];
}

// ---- v2-style hooks -------------------------------------------------------

export function useFrontendTool(tool: {
  name: string;
  description?: string;
  parameters?: any;
  handler?: (args: any) => any;
  available?: "disabled" | "enabled";
  followUp?: boolean;
}) {
  useFrontendToolV1({
    name: tool.name,
    description: tool.description,
    parameters: toParameters(tool.parameters) as any,
    handler: tool.handler as any,
    available: tool.available,
    followUp: tool.followUp as any,
  } as any);
}

export function useHumanInTheLoop(tool: {
  name: string;
  description?: string;
  parameters?: any;
  render: (props: any) => any;
  available?: "disabled" | "enabled";
  followUp?: boolean;
}) {
  useHumanInTheLoopV1({
    name: tool.name,
    description: tool.description,
    parameters: toParameters(tool.parameters) as any,
    render: tool.render as any,
    available: tool.available,
    followUp: tool.followUp as any,
  } as any);
}

export function useAgentContext(input: { description: string; value: unknown }) {
  useCopilotReadable({
    description: input.description,
    value: input.value,
  });
}
