export type WrapUpCategory = "positive" | "negative" | "neutral" | "follow_up" | "general";

export interface WrapUpCode {
  id: string;
  tenantId: string;
  label: string;
  labelAr: string | null;
  category: WrapUpCategory | string;
  color: string;
  isActive: boolean;
  sortOrder: number;
  createdAt: string;
  updatedAt: string;
}

export interface WrapUpCodeInput {
  label: string;
  labelAr?: string | null;
  category?: string;
  color?: string;
  isActive?: boolean;
  sortOrder?: number;
}

export const WRAPUP_CATEGORIES: { value: string; label: string; color: string }[] = [
  { value: "positive", label: "Positive", color: "#16A34A" },
  { value: "negative", label: "Negative", color: "#DC2626" },
  { value: "neutral", label: "Neutral", color: "#64748b" },
  { value: "follow_up", label: "Follow-up", color: "#2563EB" },
  { value: "general", label: "General", color: "#B45309" },
];
