import * as React from "react";
import { cn } from "@/lib/utils";
import { SiX, SiFacebook, SiInstagram } from "react-icons/si";
import { FaLinkedinIn } from "react-icons/fa6";
import { ChevronDown } from "lucide-react";

/* ---------- Avatar ---------- */
export type AvatarColor =
  | "red" | "blue" | "sky" | "teal" | "purple" | "violet"
  | "amber" | "rose" | "green" | "slate";
const AVATAR_VAR: Record<AvatarColor, string> = {
  red: "--si-a-red", blue: "--si-a-blue", sky: "--si-a-sky", teal: "--si-a-teal",
  purple: "--si-a-purple", violet: "--si-a-violet", amber: "--si-a-amber",
  rose: "--si-a-rose", green: "--si-a-green", slate: "--si-a-slate",
};
export function Avatar({
  initials, color = "slate", size = "md", className,
}: {
  initials: string;
  color?: AvatarColor;
  size?: "sm" | "md" | "lg" | number;
  className?: string;
}) {
  const px = typeof size === "number" ? size : size === "sm" ? 32 : size === "lg" ? 48 : 40;
  const fontSize = px <= 24 ? 9 : px <= 32 ? 11 : px <= 40 ? 13 : 15;
  return (
    <span
      className={cn("inline-flex items-center justify-center font-bold text-white rounded-full shrink-0", className)}
      style={{
        width: px, height: px, fontSize,
        background: `var(${AVATAR_VAR[color]})`,
        letterSpacing: "-0.01em",
      }}
    >
      {initials}
    </span>
  );
}

/* ---------- PlatformBadge ---------- */
export type Platform = "x" | "facebook" | "instagram" | "linkedin";
const PLATFORM_VAR: Record<Platform, string> = {
  x: "--si-x", facebook: "--si-fb", instagram: "--si-ig", linkedin: "--si-li",
};
const PLATFORM_ICON: Record<Platform, React.ComponentType<any>> = {
  x: SiX, facebook: SiFacebook, instagram: SiInstagram, linkedin: FaLinkedinIn,
};
export function PlatformBadge({
  platform, size = "md", className,
}: {
  platform: Platform;
  size?: "md" | "header" | "overlay" | number;
  className?: string;
}) {
  const px = typeof size === "number" ? size : size === "header" ? 26 : size === "overlay" ? 18 : 30;
  const radius = size === "overlay" ? 6 : size === "header" ? 7 : 9;
  const icon = px <= 18 ? 10 : px <= 26 ? 14 : 16;
  const Icon = PLATFORM_ICON[platform];
  return (
    <span
      className={cn("inline-flex items-center justify-center shrink-0", className)}
      style={{
        width: px, height: px, borderRadius: radius,
        background: `var(${PLATFORM_VAR[platform]})`,
        border: size === "overlay" ? "2px solid #fff" : undefined,
      }}
      aria-label={platform}
    >
      <Icon size={icon} color="#fff" />
    </span>
  );
}

/* ---------- Pill ---------- */
type PillVariant = "default" | "green" | "red" | "outline" | "purple" | "amber";
const PILL_STYLE: Record<PillVariant, React.CSSProperties> = {
  default: { background: "#f1f2f4", color: "var(--si-text-2)" },
  green: { background: "var(--si-brand-soft)", color: "var(--si-brand-soft-tx)" },
  red: { background: "var(--si-danger-soft)", color: "#b91c1c" },
  outline: { background: "transparent", color: "var(--si-text-2)", border: "1px solid var(--si-border-2)" },
  purple: { background: "#ede9fe", color: "#7c3aed" },
  amber: { background: "#fef3c7", color: "#b45309" },
};
export function Pill({
  variant = "default", icon: Icon, children, className, style,
}: {
  variant?: PillVariant;
  icon?: React.ComponentType<any>;
  children: React.ReactNode;
  className?: string;
  style?: React.CSSProperties;
}) {
  return (
    <span
      className={cn("inline-flex items-center gap-1 rounded-full font-semibold", className)}
      style={{ height: 22, padding: "0 9px", fontSize: 11, ...PILL_STYLE[variant], ...style }}
    >
      {Icon ? <Icon size={11} /> : null}
      {children}
    </span>
  );
}

/* ---------- Tag ---------- */
export function Tag({
  children, variant = "default", className,
}: {
  children: React.ReactNode;
  variant?: "default" | "red" | "amber";
  className?: string;
}) {
  const style: React.CSSProperties =
    variant === "red"
      ? { background: "var(--si-danger-soft)", color: "#b91c1c" }
      : variant === "amber"
      ? { background: "#fef3c7", color: "#b45309" }
      : { background: "#f1f2f4", color: "var(--si-text-2)" };
  return (
    <span
      className={cn("inline-flex items-center font-medium", className)}
      style={{ height: 20, padding: "0 8px", borderRadius: 6, fontSize: 11, ...style }}
    >
      {children}
    </span>
  );
}

/* ---------- SentimentDot ---------- */
export type Sentiment = "positive" | "neutral" | "negative";
export function SentimentDot({ type, children }: { type: Sentiment; children?: React.ReactNode }) {
  const dot = type === "positive" ? "var(--si-brand)" : type === "negative" ? "var(--si-danger)" : "#94a3b8";
  const tx = type === "positive" ? "var(--si-brand-soft-tx)" : type === "negative" ? "#b91c1c" : "var(--si-text-2)";
  return (
    <span className="inline-flex items-center gap-1.5 font-semibold" style={{ fontSize: 11, color: tx }}>
      <span style={{ width: 7, height: 7, borderRadius: 999, background: dot }} />
      {children ?? (type === "positive" ? "Positive" : type === "negative" ? "Negative" : "Neutral")}
    </span>
  );
}

/* ---------- IconButton ---------- */
export const IconButton = React.forwardRef<
  HTMLButtonElement,
  React.ButtonHTMLAttributes<HTMLButtonElement> & { active?: boolean }
>(({ className, active, children, ...props }, ref) => (
  <button
    ref={ref}
    type="button"
    className={cn(
      "inline-flex items-center justify-center transition-colors",
      active ? "bg-[#eef7f0]" : "hover:bg-[#f1f2f4]",
      className,
    )}
    style={{ width: 34, height: 34, borderRadius: 9, color: active ? "var(--si-brand-soft-tx)" : "var(--si-text-2)" }}
    {...props}
  >
    {children}
  </button>
));
IconButton.displayName = "IconButton";

/* ---------- Button (primary/ghost + split) ---------- */
type BtnVariant = "primary" | "ghost";
type BtnSize = "md" | "sm";
export function Button({
  variant = "primary", size = "md", icon: Icon, children, className, danger, split, onCaretClick, ...rest
}: {
  variant?: BtnVariant;
  size?: BtnSize;
  icon?: React.ComponentType<any>;
  children?: React.ReactNode;
  className?: string;
  danger?: boolean;
  split?: boolean;
  onCaretClick?: () => void;
} & React.ButtonHTMLAttributes<HTMLButtonElement>) {
  const h = size === "sm" ? 30 : 36;
  const padX = size === "sm" ? 11 : 14;
  const radius = size === "sm" ? 8 : 9;
  const fs = size === "sm" ? 12.5 : 13;
  const iconSize = 15;

  const base: React.CSSProperties = {
    height: h, padding: `0 ${padX}px`, fontSize: fs, gap: 7,
    borderRadius: split ? `${radius}px 0 0 ${radius}px` : radius,
  };
  const styles: React.CSSProperties =
    variant === "primary"
      ? { ...base, background: "var(--si-brand)", color: "#fff" }
      : danger
      ? { ...base, background: "var(--si-panel)", color: "#b91c1c", border: "1px solid #fecaca" }
      : { ...base, background: "var(--si-panel)", color: "var(--si-text)", border: "1px solid var(--si-border-2)" };

  const hoverCls =
    variant === "primary"
      ? "hover:bg-[var(--si-brand-hover)]"
      : "hover:bg-[var(--si-panel-2)]";

  const btn = (
    <button
      type="button"
      className={cn("inline-flex items-center justify-center font-semibold transition-colors", hoverCls, className)}
      style={styles}
      {...rest}
    >
      {Icon ? <Icon size={iconSize} /> : null}
      {children}
    </button>
  );

  if (!split) return btn;
  return (
    <span className="inline-flex">
      {btn}
      <button
        type="button"
        onClick={onCaretClick}
        aria-label="More options"
        className={cn("inline-flex items-center justify-center transition-colors", hoverCls)}
        style={{
          height: h, width: 34,
          background: "var(--si-brand)", color: "#fff",
          borderRadius: `0 ${radius}px ${radius}px 0`,
          borderLeft: "1px solid rgba(255,255,255,.25)",
        }}
      >
        <ChevronDown size={15} />
      </button>
    </span>
  );
}

/* ---------- CountBadge / NumberBadge ---------- */
export function CountBadge({ children }: { children: React.ReactNode }) {
  return (
    <span
      className="inline-flex items-center justify-center font-bold tnum"
      style={{ minWidth: 22, height: 20, borderRadius: 6, background: "#eef0f2", color: "var(--si-text-2)", fontSize: 11.5, padding: "0 5px" }}
    >
      {children}
    </span>
  );
}
export function NumberBadge({ children }: { children: React.ReactNode }) {
  return (
    <span
      className="inline-flex items-center justify-center font-bold tnum"
      style={{ minWidth: 20, height: 20, borderRadius: 7, background: "var(--si-brand)", color: "#fff", fontSize: 11, padding: "0 5px" }}
    >
      {children}
    </span>
  );
}

/* ---------- SectionLabel ---------- */
export function SectionLabel({ children, className }: { children: React.ReactNode; className?: string }) {
  return (
    <div
      className={cn("uppercase font-bold", className)}
      style={{ fontSize: 11, letterSpacing: "0.04em", color: "var(--si-text-3)", margin: "8px 10px 6px" }}
    >
      {children}
    </div>
  );
}
