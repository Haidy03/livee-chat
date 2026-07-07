/**
 * Softphone contact source — adapts the backend Directory contacts (`/contacts`)
 * to the shape the softphone views expect (numbers[], avatarColor, etc.).
 */
import { useCallback, useMemo } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { useContacts, type Contact } from "@/hooks/useContacts";

export interface SoftphoneContact {
  id: string;
  name: string;
  numbers: { label: string; value: string }[];
  email?: string;
  notes?: string;
  avatarColor: string; // hsl token (h s% l%)
}

const palette = [
  "240 89% 66%",
  "158 84% 39%",
  "28 100% 47%",
  "340 82% 52%",
  "200 89% 47%",
  "262 60% 56%",
  "6 78% 57%",
  "188 80% 40%",
];

function hashIndex(id: string, len: number): number {
  let h = 0;
  for (let i = 0; i < id.length; i++) h = (h * 31 + id.charCodeAt(i)) | 0;
  return Math.abs(h) % len;
}

function normalizeNumber(n: string): string {
  return (n || "").replace(/[^\d+]/g, "");
}

export function toSoftphoneContact(c: Contact): SoftphoneContact | null {
  const phone = normalizeNumber(c.phone);
  if (!phone) return null;
  return {
    id: c.id,
    name: c.name,
    numbers: [{ label: "mobile", value: phone }],
    email: c.email || undefined,
    notes: c.notes || undefined,
    avatarColor: palette[hashIndex(c.id, palette.length)],
  };
}

export function useSoftphoneContacts() {
  const { data, isLoading } = useContacts();
  const contacts = useMemo<SoftphoneContact[]>(
    () => (data ?? []).map(toSoftphoneContact).filter((x): x is SoftphoneContact => !!x),
    [data],
  );
  return { contacts, isLoading };
}

export function useFindContactByNumber() {
  const { contacts } = useSoftphoneContacts();
  return useCallback(
    (num: string): SoftphoneContact | undefined => {
      const norm = normalizeNumber(num);
      if (!norm) return undefined;
      return contacts.find((c) => c.numbers.some((n) => n.value === norm));
    },
    [contacts],
  );
}

/**
 * Non-React lookup using whatever React Query has cached for `["contacts"]`.
 * Used by the SIP controller (which lives outside the React tree).
 */
export function findContactByNumberFromCache(
  qc: ReturnType<typeof useQueryClient>,
  num: string,
): SoftphoneContact | undefined {
  const norm = normalizeNumber(num);
  if (!norm) return undefined;
  const cached = qc.getQueryData<Contact[]>(["contacts"]) ?? [];
  for (const c of cached) {
    const sp = toSoftphoneContact(c);
    if (sp && sp.numbers.some((n) => n.value === norm)) return sp;
  }
  return undefined;
}
