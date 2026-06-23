import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from 'react';
import { login as loginRequest, SESSION_EXPIRED_EVENT } from '../api/client';

interface Session {
  token: string;
  displayName: string;
  expiresAt: string;
}

interface AuthContextValue {
  session: Session | null;
  reauthRequired: boolean;
  signIn: (email: string, password: string) => Promise<void>;
  signOut: () => void;
  /** Marks the session expired (a 401 from any authenticated call) without clearing it,
   *  so callers holding an open edit can prompt re-authentication instead of losing work. */
  markSessionExpired: () => void;
  clearReauthRequired: () => void;
}

const SESSION_KEY = 'makeboldspark.cms.session';

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

function readStoredSession(): Session | null {
  const raw = sessionStorage.getItem(SESSION_KEY);
  if (!raw) return null;
  try {
    return JSON.parse(raw) as Session;
  } catch {
    return null;
  }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [session, setSession] = useState<Session | null>(() => readStoredSession());
  const [reauthRequired, setReauthRequired] = useState(false);

  const signIn = useCallback(async (email: string, password: string) => {
    const result = await loginRequest({ email, password });
    const next: Session = {
      token: result.accessToken,
      displayName: result.displayName,
      expiresAt: result.expiresAt,
    };
    sessionStorage.setItem(SESSION_KEY, JSON.stringify(next));
    setSession(next);
    setReauthRequired(false);
  }, []);

  const signOut = useCallback(() => {
    sessionStorage.removeItem(SESSION_KEY);
    setSession(null);
    setReauthRequired(false);
  }, []);

  const markSessionExpired = useCallback(() => {
    setReauthRequired(true);
  }, []);

  const clearReauthRequired = useCallback(() => {
    setReauthRequired(false);
  }, []);

  useEffect(() => {
    const handler = () => setReauthRequired(true);
    window.addEventListener(SESSION_EXPIRED_EVENT, handler);
    return () => window.removeEventListener(SESSION_EXPIRED_EVENT, handler);
  }, []);

  const value = useMemo<AuthContextValue>(
    () => ({ session, reauthRequired, signIn, signOut, markSessionExpired, clearReauthRequired }),
    [session, reauthRequired, signIn, signOut, markSessionExpired, clearReauthRequired],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used within an AuthProvider');
  return ctx;
}
