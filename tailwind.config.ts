import type { Config } from "tailwindcss";

export default {
  darkMode: ["class"],
  content: ["./pages/**/*.{ts,tsx}", "./components/**/*.{ts,tsx}", "./app/**/*.{ts,tsx}", "./src/**/*.{ts,tsx}"],
  prefix: "",
  theme: {
    container: {
      center: true,
      padding: "2rem",
      screens: {
        "2xl": "1400px",
      },
    },
    extend: {
      colors: {
        border: "hsl(var(--border))",
        input: "hsl(var(--input))",
        ring: "hsl(var(--ring))",
        background: "hsl(var(--background))",
        foreground: "hsl(var(--foreground))",
        primary: {
          DEFAULT: "hsl(var(--primary))",
          foreground: "hsl(var(--primary-foreground))",
        },
        secondary: {
          DEFAULT: "hsl(var(--secondary))",
          foreground: "hsl(var(--secondary-foreground))",
        },
        destructive: {
          DEFAULT: "hsl(var(--destructive))",
          foreground: "hsl(var(--destructive-foreground))",
        },
        muted: {
          DEFAULT: "hsl(var(--muted))",
          foreground: "hsl(var(--muted-foreground))",
        },
        accent: {
          DEFAULT: "hsl(var(--accent))",
          foreground: "hsl(var(--accent-foreground))",
        },
        popover: {
          DEFAULT: "hsl(var(--popover))",
          foreground: "hsl(var(--popover-foreground))",
        },
        card: {
          DEFAULT: "hsl(var(--card))",
          foreground: "hsl(var(--card-foreground))",
        },
        canvas: {
          DEFAULT: "hsl(var(--canvas-bg))",
          dot: "hsl(var(--canvas-dot))",
        },
        panel: {
          DEFAULT: "hsl(var(--panel-bg))",
          border: "hsl(var(--panel-border))",
        },
        edge: {
          DEFAULT: "hsl(var(--edge-default))",
          active: "hsl(var(--edge-active))",
          success: "hsl(var(--edge-success))",
          danger: "hsl(var(--edge-danger))",
        },
        deploy: {
          DEFAULT: "hsl(var(--deploy))",
          foreground: "hsl(var(--deploy-foreground))",
        },
        app: {
          bg: "hsl(var(--app-bg))",
          surface: "hsl(var(--app-surface))",
          border: "hsl(var(--app-border))",
          "border-strong": "hsl(var(--app-border-strong))",
        },
        status: {
          answered: "hsl(var(--status-answered))",
          missed: "hsl(var(--status-missed))",
          noanswer: "hsl(var(--status-noanswer))",
          busy: "hsl(var(--status-busy))",
        },
        agent: {
          available: "hsl(var(--agent-available))",
          busy: "hsl(var(--agent-busy))",
          break: "hsl(var(--agent-break))",
          offline: "hsl(var(--agent-offline))",
        },
        brand: {
          indigo: "hsl(var(--brand-indigo))",
          accent: "hsl(var(--brand-accent))",
        },
        landing: {
          bg: "hsl(var(--landing-bg))",
          fg: "hsl(var(--landing-fg))",
          muted: "hsl(var(--landing-muted))",
          border: "hsl(var(--landing-border))",
          primary: "hsl(var(--landing-primary))",
          accent: "hsl(var(--landing-accent))",
          cta: "hsl(var(--landing-cta))",
          "hero-bg": "hsl(var(--landing-hero-bg))",
        },
        node: {
          "call-start": "hsl(var(--node-call-start))",
          "working-hours": "hsl(var(--node-working-hours))",
          "ivr-menu": "hsl(var(--node-ivr-menu))",
          "sound-file": "hsl(var(--node-sound-file))",
          "call-fwd": "hsl(var(--node-call-fwd))",
          "ai-agent": "hsl(var(--node-ai-agent))",
          messages: "hsl(var(--node-messages))",
          rule: "hsl(var(--node-rule))",
          flags: "hsl(var(--node-flags))",
          voicemail: "hsl(var(--node-voicemail))",
          webhook: "hsl(var(--node-webhook))",
          "end-call": "hsl(var(--node-end-call))",
          "user-input": "hsl(var(--node-user-input))",
          survey: "hsl(var(--node-survey))",
        },
        sidebar: {
          DEFAULT: "hsl(var(--sidebar-background))",
          foreground: "hsl(var(--sidebar-foreground))",
          primary: "hsl(var(--sidebar-primary))",
          "primary-foreground": "hsl(var(--sidebar-primary-foreground))",
          accent: "hsl(var(--sidebar-accent))",
          "accent-foreground": "hsl(var(--sidebar-accent-foreground))",
          border: "hsl(var(--sidebar-border))",
          ring: "hsl(var(--sidebar-ring))",
        },
      },
      borderRadius: {
        lg: "var(--radius)",
        md: "calc(var(--radius) - 2px)",
        sm: "calc(var(--radius) - 4px)",
      },
      keyframes: {
        "accordion-down": {
          from: {
            height: "0",
          },
          to: {
            height: "var(--radix-accordion-content-height)",
          },
        },
        "accordion-up": {
          from: {
            height: "var(--radix-accordion-content-height)",
          },
          to: {
            height: "0",
          },
        },
      },
      fontFamily: {
        sans: ['IBM Plex Sans Arabic', 'Cairo', 'Inter', 'system-ui', 'sans-serif'],
        mono: ['IBM Plex Mono', 'ui-monospace', 'monospace'],
        tajawal: ['Tajawal', 'IBM Plex Sans Arabic', 'system-ui', 'sans-serif'],
        inter: ['Inter', 'system-ui', 'sans-serif'],
      },
      animation: {
        "accordion-down": "accordion-down 0.2s ease-out",
        "accordion-up": "accordion-up 0.2s ease-out",
      },
    },
  },
  plugins: [require("tailwindcss-animate")],
} satisfies Config;
