import {
  createContext,
  useContext,
  useEffect,
  useMemo,
  useRef,
  useState,
  type PropsWithChildren
} from "react";
import { api, unauthorizedEventName } from "../lib/api";
import type { AuthResponse, LoginRequest, RegisterRequest } from "../types";

const storageKey = "autogallery.session";
// Token suresinin dolmasindan bu kadar once proaktif olarak yenileriz.
const refreshLeadTimeMs = 60_000;

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
  // En guncel oturuma senkron erisim icin ref (kapanis/closure tuzaklarini onler).
  const sessionRef = useRef<AuthResponse | null>(null);
  // Ayni anda birden fazla yenileme istegini engelleyen kilit.
  const refreshInFlight = useRef<Promise<boolean> | null>(null);

  const isExpired = (value: AuthResponse | null) => {
    if (!value?.expiry) {
      return true;
    }

    const expiryTime = Date.parse(value.expiry);
    return Number.isNaN(expiryTime) || expiryTime <= Date.now();
  };

  const persistSession = (value: AuthResponse | null) => {
    sessionRef.current = value;
    setSession(value);

    if (value) {
      localStorage.setItem(storageKey, JSON.stringify(value));
      return;
    }

    localStorage.removeItem(storageKey);
  };

  // Mevcut refresh token ile yeni bir oturum almayi dener.
  // Basarisizsa oturumu kapatir. Es zamanli cagrilarda tek istek paylasilir.
  const tryRefresh = (): Promise<boolean> => {
    if (refreshInFlight.current) {
      return refreshInFlight.current;
    }

    const current = sessionRef.current;
    if (!current?.refreshToken) {
      persistSession(null);
      return Promise.resolve(false);
    }

    const promise = api
      .refreshToken({ token: current.token, refreshToken: current.refreshToken })
      .then((response) => {
        persistSession(response);
        return true;
      })
      .catch(() => {
        persistSession(null);
        return false;
      })
      .finally(() => {
        refreshInFlight.current = null;
      });

    refreshInFlight.current = promise;
    return promise;
  };

  // Sayfa yuklenince kayitli oturumu geri yukle (gerekirse token'i yenile).
  useEffect(() => {
    const savedSession = localStorage.getItem(storageKey);
    if (!savedSession) {
      return;
    }

    try {
      const parsedSession = JSON.parse(savedSession) as AuthResponse;
      sessionRef.current = parsedSession;

      if (isExpired(parsedSession)) {
        // Erisim token'i dolmus; refresh token varsa yenilemeyi dene, yoksa cikis yap.
        if (parsedSession.refreshToken) {
          void tryRefresh();
        } else {
          persistSession(null);
        }
        return;
      }

      setSession(parsedSession);
    } catch {
      localStorage.removeItem(storageKey);
    }
  }, []);

  useEffect(() => {
    function handleUnauthorized() {
      // 401 alindiginda dogrudan cikis yapmak yerine once token yenilemeyi dene.
      void tryRefresh();
    }

    window.addEventListener(unauthorizedEventName, handleUnauthorized);
    return () => window.removeEventListener(unauthorizedEventName, handleUnauthorized);
  }, []);

  // Token suresi dolmadan kisa bir sure once proaktif olarak yenile.
  useEffect(() => {
    if (!session?.expiry) {
      return;
    }

    const expiryTime = Date.parse(session.expiry);
    if (Number.isNaN(expiryTime)) {
      return;
    }

    const delay = Math.max(expiryTime - Date.now() - refreshLeadTimeMs, 0);
    const timer = window.setTimeout(() => {
      void tryRefresh();
    }, delay);

    return () => window.clearTimeout(timer);
  }, [session?.expiry]);

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
