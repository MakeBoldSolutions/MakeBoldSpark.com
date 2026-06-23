import { useState, type FormEvent } from 'react';
import { useAuth } from '../auth/AuthContext';

/**
 * Rendered as a banner over the current page (not a navigation) so a 401 received while an
 * edit is open re-authenticates in place without discarding the open form's unsaved state
 * (gate finding analyze-E2 / tasks.md T031c).
 */
export function ReauthPrompt() {
  const { signIn, clearReauthRequired } = useAuth();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setSubmitting(true);
    setError(null);
    try {
      await signIn(email, password);
      clearReauthRequired();
    } catch {
      setError('Invalid email or password.');
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <div role="alertdialog" className="cms-reauth-banner">
      <p>Your session expired. Sign in again to continue — your unsaved changes are kept.</p>
      <form onSubmit={handleSubmit}>
        <input
          type="email"
          placeholder="Email"
          autoComplete="username"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          required
        />
        <input
          type="password"
          placeholder="Password"
          autoComplete="current-password"
          maxLength={256}
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          required
        />
        <button type="submit" disabled={submitting}>
          {submitting ? 'Signing in…' : 'Sign in'}
        </button>
        {error && <span role="alert">{error}</span>}
      </form>
    </div>
  );
}
