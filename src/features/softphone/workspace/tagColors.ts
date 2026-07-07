// Deterministic tag color mapping. Presets get fixed accents; custom tags hash to a hue.

const PRESET_COLORS: Record<string, { bg: string; fg: string; border: string }> = {
  VIP:         { bg: "33 90% 92%",  fg: "33 90% 30%",  border: "33 90% 70%" },
  Lead:        { bg: "210 90% 92%", fg: "210 80% 32%", border: "210 80% 70%" },
  Customer:    { bg: "152 70% 90%", fg: "152 80% 25%", border: "152 60% 60%" },
  Support:     { bg: "262 80% 93%", fg: "262 60% 38%", border: "262 60% 72%" },
  Billing:     { bg: "0 80% 94%",   fg: "0 70% 38%",   border: "0 60% 72%" },
  "Follow-up": { bg: "188 80% 90%", fg: "188 80% 26%", border: "188 60% 60%" },
  Vendor:      { bg: "28 90% 92%",  fg: "28 80% 32%",  border: "28 80% 65%" },
  Cold:        { bg: "220 30% 92%", fg: "220 20% 35%", border: "220 20% 72%" },
};

export const PRESET_TAGS = Object.keys(PRESET_COLORS);

function hashHue(s: string): number {
  let h = 0;
  for (let i = 0; i < s.length; i++) h = (h * 31 + s.charCodeAt(i)) | 0;
  return Math.abs(h) % 360;
}

export function tagColor(tag: string) {
  const preset = PRESET_COLORS[tag];
  if (preset) return preset;
  const hue = hashHue(tag);
  return {
    bg: `${hue} 70% 92%`,
    fg: `${hue} 60% 32%`,
    border: `${hue} 50% 70%`,
  };
}
