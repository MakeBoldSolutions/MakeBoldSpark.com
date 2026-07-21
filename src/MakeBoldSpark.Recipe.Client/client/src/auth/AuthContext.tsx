import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from 'react';
import { SESSION_EXPIRED_EVENT } from '../api/recipeClient';

type Session = { token: string; displayName: string; expiresAt: string };
type LoginResponse = { accessToken: string; displayName: string; expiresAt: string };

type AuthValue = {
  token: string;
  session: Session | null;
  reauthRequired: boolean;
  signIn: (email: string, password: string) => Promise<void>;
  signOut: () => void;
  clearReauthRequired: () => void;
};

const SessionKey = 'mbs.recipe.session';
const Auth = createContext<AuthValue | undefined>(undefined);

function readSession() {
  try {
    const raw = sessionStorage.getItem(SessionKey);
    return raw ? (JSON.parse(raw) as Session) : null;
  } catch {
    return null;
  }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [session, setSession] = useState<Session | null>(() => readSession());
  const [reauthRequired, setReauthRequired] = useState(false);

  const signIn = useCallback(async (email: string, password: string) => {
    const response = await fetch('/api/public/auth/login', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ email, password }),
    });
    if (!response.ok) throw new Error('Invalid email or password.');
    const result = (await response.json()) as LoginResponse;
    const next = { token: result.accessToken, displayName: result.displayName, expiresAt: result.expiresAt };
    sessionStorage.setItem(SessionKey, JSON.stringify(next));
    setSession(next);
    setReauthRequired(false);
  }, []);

  const signOut = useCallback(() => {
    sessionStorage.removeItem(SessionKey);
    setSession(null);
    setReauthRequired(false);
  }, []);

  const clearReauthRequired = useCallback(() => setReauthRequired(false), []);

  useEffect(() => {
    const handler = () => setReauthRequired(true);
    window.addEventListener(SESSION_EXPIRED_EVENT, handler);
    return () => window.removeEventListener(SESSION_EXPIRED_EVENT, handler);
  }, []);

  const value = useMemo(
    () => ({
      token: session?.token ?? '',
      session,
      reauthRequired,
      signIn,
      signOut,
      clearReauthRequired,
    }),
    [session, reauthRequired, signIn, signOut, clearReauthRequired],
  );

  return <Auth.Provider value={value}>{children}</Auth.Provider>;
}

export function useAuth() {
  const value = useContext(Auth);
  if (!value) throw new Error('useAuth must be used within AuthProvider');
  return value;
}
