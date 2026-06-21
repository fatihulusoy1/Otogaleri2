import {
  createContext,
  useContext,
  useEffect,
  useMemo,
  useState,
  type PropsWithChildren
} from "react";
import { api, unauthorizedEventName } from "../lib/api";
import type { AuthResponse, LoginRequest, RegisterRequest } from "../types";

const storageKey = "autogallery.session";

interface AuthContextValue {
  session: AuthResponse | null;
  isAuthenticated: boolean;
  login: (payload: LoginRequest) => Promise<void>;
  register: (payload: RegisterRequest) => Promise<void>;
  logout: () => void;
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

export function AuthProvider({ children }: PropsWithChildren) {
  const [session, setSession] = useState<AuthResponse | null>(null);

  const isExpired = (value: AuthResponse | null) => {
    if (!value?.expiry) {
      return true;
    }

    const expiryTime = Date.parse(value.expiry);
    return Number.isNaN(expiryTime) || expiryTime <= Date.now();
  };

  useEffect(() => {
    const savedSession = localStorage.getItem(storageKey);
    if (!savedSession) {
      return;
    }

    try {
      const parsedSession = JSON.parse(savedSession) as AuthResponse;
      if (isExpired(parsedSession)) {
        localStorage.removeItem(storageKey);
        return;
      }

      setSession(parsedSession);
    } catch {
      localStorage.removeItem(storageKey);
    }
  }, []);

  const persistSession = (value: AuthResponse | null) => {
    setSession(value);

    if (value) {
      localStorage.setItem(storageKey, JSON.stringify(value));
      return;
    }

    localStorage.removeItem(storageKey);
  };

  useEffect(() => {
    function handleUnauthorized() {
      persistSession(null);
    }

    window.addEventListener(unauthorizedEventName, handleUnauthorized);
    return () => window.removeEventListener(unauthorizedEventName, handleUnauthorized);
  }, []);

  const value = useMemo<AuthContextValue>(
    () => ({
      session,
      isAuthenticated: Boolean(session?.token),
      async login(payload) {
        const response = await api.login(payload);
        persistSession(response);
      },
      async register(payload) {
        const response = await api.register(payload);
        persistSession(response);
      },
      logout() {
        persistSession(null);
      }
    }),
    [session]
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const context = useContext(AuthContext);

  if (!context) {
    throw new Error("useAuth must be used within AuthProvider");
  }

  return context;
}
