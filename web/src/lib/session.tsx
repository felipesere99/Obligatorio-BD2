import { createContext, useContext, useEffect, useState, type ReactNode } from "react";
import { api, setSession as setApiSession } from "./api";
import type { UserSession } from "./types";

const STORAGE_KEY = "ticketing.session";

interface SessionContextValue {
  session: UserSession | null;
  login: (documento: string, contrasenia: string) => Promise<UserSession>;
  logout: () => void;
}

const SessionContext = createContext<SessionContextValue | null>(null);

function load(): UserSession | null {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    return raw ? (JSON.parse(raw) as UserSession) : null;
  } catch {
    return null;
  }
}

export function SessionProvider({ children }: { children: ReactNode }) {
  const [session, setSessionState] = useState<UserSession | null>(load);

  // Mantiene sincronizada la sesión que usa el cliente API.
  useEffect(() => {
    setApiSession(session);
  }, [session]);

  async function login(documento: string, contrasenia: string) {
    const user = await api.post<UserSession>("/login", { documento, contrasenia });
    localStorage.setItem(STORAGE_KEY, JSON.stringify(user));
    setApiSession(user);
    setSessionState(user);
    return user;
  }

  function logout() {
    localStorage.removeItem(STORAGE_KEY);
    setApiSession(null);
    setSessionState(null);
  }

  return (
    <SessionContext.Provider value={{ session, login, logout }}>
      {children}
    </SessionContext.Provider>
  );
}

export function useSession() {
  const ctx = useContext(SessionContext);
  if (!ctx) throw new Error("useSession debe usarse dentro de <SessionProvider>");
  return ctx;
}
