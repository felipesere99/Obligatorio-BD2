import type { UserSession } from "./types";

const SERVER_URL =
  (import.meta.env.VITE_SERVER_URL as string | undefined) ?? "http://localhost:5050";

/** Error de negocio devuelto por el server (cuerpo { error: "..." }). */
export class ApiError extends Error {
  readonly status: number;
  constructor(message: string, status: number) {
    super(message);
    this.status = status;
  }
}

let session: UserSession | null = null;

/** El App setea la sesión activa; se reenvía como headers en cada request. */
export function setSession(s: UserSession | null) {
  session = s;
}

async function request<T>(method: string, path: string, body?: unknown): Promise<T> {
  const headers: Record<string, string> = {};
  if (body !== undefined) headers["Content-Type"] = "application/json";
  if (session) {
    headers["X-Documento"] = session.documento;
    headers["X-Rol"] = session.rol;
  }

  const resp = await fetch(`${SERVER_URL}${path}`, {
    method,
    headers,
    body: body !== undefined ? JSON.stringify(body) : undefined,
  });

  if (!resp.ok) {
    let message = `Error HTTP ${resp.status}`;
    try {
      const err = await resp.json();
      if (err && typeof err.error === "string") message = err.error;
    } catch {
      /* cuerpo no-JSON */
    }
    throw new ApiError(message, resp.status);
  }

  // 201/200 sin cuerpo (p.ej. algunos POST) → undefined.
  const text = await resp.text();
  return (text ? JSON.parse(text) : undefined) as T;
}

export const api = {
  get: <T>(path: string) => request<T>("GET", path),
  post: <T>(path: string, body?: unknown) => request<T>("POST", path, body),
  put: <T>(path: string, body?: unknown) => request<T>("PUT", path, body),
  patch: <T>(path: string, body?: unknown) => request<T>("PATCH", path, body),
  delete: <T>(path: string, body?: unknown) => request<T>("DELETE", path, body),
};
