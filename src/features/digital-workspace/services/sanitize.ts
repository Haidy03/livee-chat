/**
 * Placeholder for DOMPurify-backed HTML sanitization. Mock messages are plain
 * text today, but ANY future code that renders message.html MUST route through
 * this function first. Replace with a real DOMPurify import when wiring real
 * channels (WhatsApp templates, social rich content, etc.).
 */
const STRIP_RE = /<\/?(script|iframe|object|embed|style)\b[^>]*>/gi;
const ON_ATTR_RE = /\son\w+="[^"]*"/gi;

export function sanitizeHtml(input: string | undefined): string {
  if (!input) return "";
  return input.replace(STRIP_RE, "").replace(ON_ATTR_RE, "");
}
