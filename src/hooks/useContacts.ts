import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { api } from "@/lib/apiClient";

export interface Contact {
  id: string;
  name: string;
  phone: string;
  email: string;
  company: string;
  tagIds: string[];
  notes: string;
  lastCallAt: string | null;
  totalCalls: number;
}

export interface ContactInput {
  name: string;
  phone?: string;
  email?: string;
  company?: string;
  notes?: string;
  tagIds?: string[];
}

interface ContactApi {
  id: string;
  name: string;
  phone: string;
  email: string;
  company: string;
  tagIds: string[];
  notes: string;
  lastCallAt?: string | null;
  totalCalls: number;
}

interface PagedContacts {
  items: ContactApi[];
}

function mapContact(r: ContactApi): Contact {
  return {
    id: r.id,
    name: r.name,
    phone: r.phone ?? "",
    email: r.email ?? "",
    company: r.company ?? "",
    tagIds: r.tagIds ?? [],
    notes: r.notes ?? "",
    lastCallAt: r.lastCallAt ?? null,
    totalCalls: r.totalCalls ?? 0,
  };
}

export function useContacts() {
  return useQuery({
    queryKey: ["contacts"],
    queryFn: async (): Promise<Contact[]> => {
      const params = new URLSearchParams();
      params.set("page", "1");
      params.set("pageSize", "500");
      const page = await api.get<PagedContacts>(`/contacts?${params.toString()}`);
      return (page.items ?? []).map(mapContact);
    },
    staleTime: 5 * 60 * 1000,
    refetchOnWindowFocus: false,
    refetchOnMount: false,
  });
}

export function useCreateContact() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (input: ContactInput) => {
      const row = await api.post<ContactApi>("/contacts", {
        name: input.name,
        phone: input.phone ?? "",
        email: input.email ?? "",
        company: input.company ?? "",
        notes: input.notes ?? "",
        tagIds: input.tagIds ?? [],
      });
      return mapContact(row);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: ["contacts"] }),
  });
}

export function useUpdateContact() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async ({ id, ...input }: ContactInput & { id: string }) => {
      const row = await api.patch<ContactApi>(`/contacts/${id}`, {
        name: input.name,
        phone: input.phone ?? "",
        email: input.email ?? "",
        company: input.company ?? "",
        notes: input.notes ?? "",
        tagIds: input.tagIds ?? [],
      });
      return mapContact(row);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: ["contacts"] }),
  });
}

export function useBulkCreateContacts() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (inputs: ContactInput[]) => {
      if (inputs.length === 0) return [] as Contact[];
      const inserted: Contact[] = [];
      const CHUNK = 25;
      for (let i = 0; i < inputs.length; i += CHUNK) {
        const slice = inputs.slice(i, i + CHUNK);
        const chunkResults = await Promise.all(
          slice.map((input) =>
            api.post<ContactApi>("/contacts", {
              name: input.name,
              phone: input.phone ?? "",
              email: input.email ?? "",
              company: input.company ?? "",
              notes: input.notes ?? "",
              tagIds: input.tagIds ?? [],
            }),
          ),
        );
        inserted.push(...chunkResults.map(mapContact));
      }
      return inserted;
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: ["contacts"] }),
  });
}

export function useDeleteContact() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (id: string) => {
      await api.deleteRaw(`/contacts/${id}`);
      return id;
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: ["contacts"] }),
  });
}
