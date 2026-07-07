export function formatPhone(input: string): string {
  if (!input) return "";
  const hasPlus = input.startsWith("+");
  const digits = input.replace(/[^\d*#]/g, "");
  if (!digits) return hasPlus ? "+" : "";

  // North American style if starts with 1 and has 11 digits, or just digits-only
  if (hasPlus && digits.startsWith("1")) {
    const d = digits.slice(1);
    const a = d.slice(0, 3);
    const b = d.slice(3, 6);
    const c = d.slice(6, 10);
    let out = "+1";
    if (a) out += ` (${a}`;
    if (a.length === 3) out += ")";
    if (b) out += ` ${b}`;
    if (c) out += `-${c}`;
    return out;
  }
  if (hasPlus) {
    // Generic international: +CC XXX XXX XXXX
    const cc = digits.slice(0, Math.min(3, digits.length));
    const rest = digits.slice(cc.length);
    const groups = rest.match(/.{1,3}/g) ?? [];
    return `+${cc}${groups.length ? " " + groups.join(" ") : ""}`;
  }
  // No leading +. Group by 3
  const groups = digits.match(/.{1,3}/g) ?? [];
  return groups.join(" ");
}
