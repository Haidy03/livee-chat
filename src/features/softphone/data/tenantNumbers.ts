/**
 * Tenant outbound account numbers (DIDs) available as caller-ID for outbound calls.
 * Sourced from GET /api/v1/accounts -> { numbers: [...] }.
 */
import { useEffect, useState } from "react";
import { api } from "@/lib/apiClient";

export interface TenantNumber {
  id: string;
  number: string;
  label: string;
  country?: string;
}

interface RawNumber {
  id?: string;
  number?: string;
  did?: string;
  e164?: string;
  label?: string;
  name?: string;
  country?: string;
}

interface AccountResponse {
  phoneNumbers?: RawNumber[];
}

function normalize(raw: RawNumber, idx: number): TenantNumber | null {
  const number = raw.number ?? raw.did ?? raw.e164 ?? "";
  if (!number) return null;
  return {
    id: raw.id ?? `n${idx}`,
    number,
    label: raw.label ?? raw.name ?? number,
    country: raw.country,
  };
}

let cache: TenantNumber[] | null = null;
let inflight: Promise<TenantNumber[]> | null = null;

async function fetchTenantNumbers(): Promise<TenantNumber[]> {
  if (cache) return cache;
  if (inflight) return inflight;
  inflight = (async () => {
    try {
      const acc = await api.get<AccountResponse>("/accounts");
      const list = (acc?.phoneNumbers ?? [])
        .map((r, i) => normalize(r, i))
        .filter((x): x is TenantNumber => x !== null);
      cache = list;
      return list;
    } catch {
      cache = [];
      return cache;
    } finally {
      inflight = null;
    }
  })();
  return inflight;
}

export interface UseTenantNumbersResult {
  numbers: TenantNumber[];
  loading: boolean;
}

export function useTenantNumbers(): UseTenantNumbersResult {
  const [numbers, setNumbers] = useState<TenantNumber[]>(() => cache ?? []);
  const [loading, setLoading] = useState<boolean>(() => cache === null);

  useEffect(() => {
    let mounted = true;
    if (cache) {
      setNumbers(cache);
      setLoading(false);
      return () => {
        mounted = false;
      };
    }
    fetchTenantNumbers().then((list) => {
      if (!mounted) return;
      setNumbers(list);
      setLoading(false);
    });
    return () => {
      mounted = false;
    };
  }, []);

  return { numbers, loading };
}
