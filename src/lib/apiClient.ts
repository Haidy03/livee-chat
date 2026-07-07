import { jwtDecode } from "jwt-decode";

const ACCESS_KEY = "vf_access_token";
const REFRESH_KEY = "vf_refresh_token";

function apiOrigin(): string {
  const u = import.meta.env.VITE_API_URL as string | undefined;
  // In production the frontend is served by the API on the same origin,
  // so an empty string makes all requests use relative URLs (/api/v1/...).
  return u?.trim() ? u.replace(/\/$/, "") : "";
}

/** Base URL without trailing slash (no /api/v1). */
export function getApiOrigin(): string {
  return apiOrigin();
}

export interface VoiceFlowJwtPayload {
  sub: string;
  email?: string;
  tenant_id?: string;
  role?: string | string[];
}

export interface ApiEnvelope<T> {
  success: boolean;
  data?: T;
  message?: string | null;
  errors?: string[] | null;
}

export function getAccessToken(): string | null {
  return localStorage.getItem(ACCESS_KEY);
}

export function setTokens(access: string, refresh: string): void {
  localStorage.setItem(ACCESS_KEY, access);
  localStorage.setItem(REFRESH_KEY, refresh);
}

export function clearTokens(): void {
  localStorage.removeItem(ACCESS_KEY);
  localStorage.removeItem(REFRESH_KEY);
}

export function decodeToken(): VoiceFlowJwtPayload | null {
  const t = getAccessToken();
  if (!t) return null;
  try {

    const decoded = jwtDecode(t);
    const decoded1 = jwtDecode<VoiceFlowJwtPayload>(t);

    return jwtDecode<VoiceFlowJwtPayload>(t);
  } catch {
    return null;
  }
}

export function getRoleFromToken(): string | undefined {
  const r = decodeToken()?.role;
  if (!r) return undefined;
  return Array.isArray(r) ? r[0] : r;
}

function joinUrl(path: string): string {
  const p = path.startsWith("/") ? path : `/${path}`;
  return `${apiOrigin()}/api/v1${p}`;
}

async function tryRefresh(): Promise<boolean> {
  const refresh = localStorage.getItem(REFRESH_KEY);
  if (!refresh) return false;

  const res = await fetch(joinUrl("/auth/refresh"), {
    method: "POST",
    headers: { Accept: "application/json", "Content-Type": "application/json" },
    body: JSON.stringify({ refreshToken: refresh }),
  });

  const text = await res.text();
  let json: ApiEnvelope<{ accessToken: string; refreshToken?: string }> | null = null;
  try {
    json = text ? JSON.parse(text) : null;
  } catch {
    json = null;
  }

  if (!res.ok || !json?.success || !json.data?.accessToken) {
    clearTokens();
    return false;
  }

  setTokens(json.data.accessToken, json.data.refreshToken ?? refresh);
  return true;
}

async function parseResponse<T>(res: Response): Promise<T | undefined> {
  if (res.status === 204) return undefined;

  const text = await res.text();
  if (!text) return undefined;

  const json = JSON.parse(text) as ApiEnvelope<T> | T;

  if (json !== null && typeof json === "object" && "success" in (json as ApiEnvelope<T>)) {
    const env = json as ApiEnvelope<T>;
    if (!env.success) {
      throw buildEnvelopeError(env);
    }
    return env.data as T;
  }

  return json as T;
}

const FRIENDLY_CODE_FALLBACK: Record<string, string> = {
  create: "Failed to create",
  update: "Failed to update",
  delete: "Failed to delete",
  forbidden: "You don't have permission",
  notfound: "Resource not found",
  unauthorized: "You are not authorized",
  validation: "Validation failed",
};

function buildEnvelopeError(
  env: { message?: string | null; errors?: string[] | null },
  status?: number,
): Error {
  const errors = (env.errors ?? []).filter(Boolean);
  const code = errors[0];
  const message =
    env.message?.trim() ||
    (code && FRIENDLY_CODE_FALLBACK[code.toLowerCase()]) ||
    (errors.length > 1 ? errors.join(", ") : code) ||
    "Request failed";
  const err = new Error(message) as Error & { code?: string; errors?: string[]; status?: number };
  if (code) err.code = code;
  if (errors.length) err.errors = errors;
  if (status !== undefined) err.status = status;
  return err;
}

async function runRequest<T>(method: string, url: string, body?: unknown, init?: RequestInit): Promise<T | undefined> {
  const headers: Record<string, string> = {
    Accept: "application/json",
    ...((init?.headers as Record<string, string>) ?? {}),
  };

  const token = getAccessToken();
  if (token) headers.Authorization = `Bearer ${token}`;

  let reqBody: BodyInit | undefined;
  if (body instanceof FormData) {
    reqBody = body;
  } else if (body !== undefined && body !== null) {
    headers["Content-Type"] = "application/json";
    reqBody = JSON.stringify(body);
  }

  const exec = async () =>
    fetch(url, {
      ...init,
      method,
      headers,
      body: reqBody,
    });

  let res = await exec();

  if (res.status === 401 && token && !(body instanceof FormData)) {
    const refreshed = await tryRefresh();
    if (refreshed) {
      headers.Authorization = `Bearer ${getAccessToken() ?? ""}`;
      res = await fetch(url, { ...init, method, headers, body: reqBody });
    }
  }

  if (!res.ok) {
    const text = await res.text();
    let parsed: any = null;
    try {
      parsed = text ? JSON.parse(text) : null;
    } catch {
      /* not JSON */
    }
    if (parsed && typeof parsed === "object" && ("errors" in parsed || "message" in parsed || "success" in parsed)) {
      throw buildEnvelopeError(parsed, res.status);
    }
    throw new Error(text || `HTTP ${res.status}`);
  }

  return parseResponse<T>(res);
}

async function request<T>(method: string, path: string, body?: unknown, init?: RequestInit): Promise<T | undefined> {
  return runRequest<T>(method, joinUrl(path), body, init);
}

/**
 * Calls the Contact-Center CTI monitoring endpoints (mounted at `/api/cti/...`,
 * NOT under `/api/v1`). Uses the same JWT + refresh logic as the rest of the app.
 */
export async function ctiGet<T>(path: string, init?: RequestInit): Promise<T> {
  const p = path.startsWith("/") ? path : `/${path}`;
  const url = `${apiOrigin()}/api/cti${p}`;
  const v = await runRequest<T>("GET", url, undefined, init);
  if (v === undefined) throw new Error("Empty API response");
  return v;
}

/**
 * Calls LiveChat backend endpoints mounted at `/api/{projectId}/...`
 * (not under `/api/v1`). Reuses JWT + refresh flow.
 */
export async function livechatRequest<T>(
  method: string,
  path: string,
  body?: unknown,
  init?: RequestInit,
): Promise<T | undefined> {
  const p = path.startsWith("/") ? path : `/${path}`;
  const url = `${apiOrigin()}/api${p}`;
  return runRequest<T>(method, url, body, init);
}

async function unwrap<T>(promise: Promise<T | undefined>): Promise<T> {
  const v = await promise;
  if (v === undefined) throw new Error("Empty API response");
  return v;
}

/** Server-side logout (revokes refresh tokens). Ignores network failures. */
export async function logoutRequest(): Promise<void> {
  const token = getAccessToken();
  if (!token) return;
  try {
    const res = await fetch(joinUrl("/auth/logout"), {
      method: "POST",
      headers: { Accept: "application/json", Authorization: `Bearer ${token}` },
    });
    await res.text();
  } catch {
    /* ignore */
  }
}

export const api = {
  get: <T>(path: string, init?: RequestInit) => unwrap(request<T>("GET", path, undefined, init)),
  post: <T>(path: string, body?: unknown, init?: RequestInit) => unwrap(request<T>("POST", path, body, init)),
  patch: <T>(path: string, body?: unknown, init?: RequestInit) => unwrap(request<T>("PATCH", path, body, init)),
  put: <T>(path: string, body?: unknown, init?: RequestInit) => unwrap(request<T>("PUT", path, body, init)),
  deleteRaw: async (path: string, init?: RequestInit): Promise<void> => {
    await request("DELETE", path, undefined, init);
  },
  upload: <T>(path: string, form: FormData, init?: RequestInit) => unwrap(request<T>("POST", path, form, init)),
};
