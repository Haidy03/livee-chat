import { defineConfig, type Plugin } from "vite";
import react from "@vitejs/plugin-react-swc";
import path from "path";
import fs from "fs";
import { createRequire } from "module";
import { componentTagger } from "lovable-tagger";

// Intercept @copilotkit v2 CSS so Tailwind/PostCSS doesn't try to process it
// (their CSS uses `@layer base` without a matching `@tailwind base`).
function copilotkitCssBypass(): Plugin {
  const PREFIX = "\0copilotkit-css:";
  const SUFFIX = ".js";
  return {
    name: "copilotkit-css-bypass",
    enforce: "pre" as const,
    async resolveId(source, importer) {
      if (!source.endsWith(".css")) return null;
      const fromCopilot = importer && importer.includes("@copilotkit");
      const toCopilot = source.includes("@copilotkit");
      if (!fromCopilot && !toCopilot) return null;
      const resolved = await this.resolve(source, importer, { skipSelf: true });
      if (!resolved || !resolved.id.includes("@copilotkit")) return null;
      return PREFIX + resolved.id + SUFFIX;
    },
    load(id) {
      if (!id.startsWith(PREFIX)) return null;
      const filePath = id.slice(PREFIX.length, id.length - SUFFIX.length);
      // Recursively inline @import statements so the browser doesn't try to
      // fetch them as separate URLs (which 404 in production builds).
      const inlineImports = (file: string, seen = new Set<string>()): string => {
        if (seen.has(file)) return "";
        seen.add(file);
        let css = fs.readFileSync(file, "utf-8");
        css = css.replace(
          /@import\s+(?:url\()?["']([^"')]+)["']\)?\s*;?/g,
          (_m, spec: string) => {
            try {
              let target: string;
              if (spec.startsWith(".") || spec.startsWith("/")) {
                target = path.resolve(path.dirname(file), spec);
              } else {
                const req = createRequire(file);
                target = req.resolve(spec);
              }
              return inlineImports(target, seen);
            } catch {
              return "";
            }
          },
        );
        return css;
      };
      const css = inlineImports(filePath);
      return `const css = ${JSON.stringify(css)};
if (typeof document !== "undefined") {
  const sid = "copilotkit-css-" + ${JSON.stringify(filePath.replace(/[^a-z0-9]/gi, "_"))};
  if (!document.getElementById(sid)) {
    const s = document.createElement("style");
    s.id = sid;
    s.textContent = css;
    document.head.appendChild(s);
  }
}
export default css;
`;
    },
  };
}

// https://vitejs.dev/config/
export default defineConfig(({ mode }) => ({
  server: {
    host: "::",
    port: 8080,
    hmr: {
      overlay: false,
    },
  },
  plugins: [
    copilotkitCssBypass(),
    react(),
    mode === "development" && componentTagger(),
  ].filter(Boolean),
  resolve: {
    alias: {
      "@": path.resolve(__dirname, "./src"),
    },
    dedupe: ["react", "react-dom", "react/jsx-runtime", "react/jsx-dev-runtime", "@tanstack/react-query", "@tanstack/query-core"],
  },
}));
