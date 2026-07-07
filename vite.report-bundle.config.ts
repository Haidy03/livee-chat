import { defineConfig } from "vite";
import react from "@vitejs/plugin-react-swc";
import path from "path";

// Builds the report export bundle (src/features/resonance/reportExportBundle.tsx)
// into a single self-contained IIFE — React, ReactDOM and Recharts all inlined —
// that the C# HtmlReportRenderer embeds into the exported HTML / headless PDF page.
//
//   npm run build:report-bundle
//
// Output lands directly in the backend project's assets folder so the renderer can
// read it. Re-run whenever ReportResultView (the chart code) changes.
export default defineConfig({
  plugins: [react()],
  define: {
    "process.env.NODE_ENV": JSON.stringify("production"),
  },
  resolve: {
    alias: { "@": path.resolve(__dirname, "src") },
  },
  build: {
    outDir: "backend/csharp/Contact-Center.Api/src/VoiceFlow.Infrastructure/Reports/Rendering/assets",
    emptyOutDir: false,
    // Don't copy the app's public/ dir (favicon, robots.txt, widget/…) into the backend assets.
    copyPublicDir: false,
    cssCodeSplit: false,
    minify: true,
    lib: {
      entry: path.resolve(__dirname, "src/features/resonance/reportExportBundle.tsx"),
      formats: ["iife"],
      name: "ReportExportBundle",
      fileName: () => "report-charts.js",
    },
    rollupOptions: {
      output: { inlineDynamicImports: true },
    },
  },
});
