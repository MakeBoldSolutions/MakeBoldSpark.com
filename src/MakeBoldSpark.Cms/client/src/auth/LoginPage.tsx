import { useState, type FormEvent } from 'react';
import { BrandMark } from '../BrandMark';
import { useAuth } from './AuthContext';

export function LoginPage() {
  const { signIn } = useAuth();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setSubmitting(true);
    setError(null);
    try {
      await signIn(email, password);
    } catch {
      // The API returns the same generic message for every rejection reason
      // (unknown email, wrong password, non-admin account, throttled) — FR-001a.
      setError('Invalid email or password.');
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <div className="cms-login">
      <div className="cms-login-card">
      <BrandMark />
      <div>
        <p className="eyebrow">Make Bold Spark</p>
        <h1>MakeBoldSpark CMS</h1>
      </div>
      <form onSubmit={handleSubmit}>
        <label htmlFor="email">Email</label>
        <input
          id="email"
          type="email"
          autoComplete="username"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          required
        />

        <label htmlFor="password">Password</label>
        <input
          id="password"
          type="password"
          autoComplete="current-password"
          maxLength={256}
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          required
        />

        {error && <p role="alert" className="cms-login-error">{error}</p>}

        <button type="submit" className="primary" disabled={submitting}>
          {submitting ? 'Signing in…' : 'Sign in'}
        </button>
      </form>
      </div>
    </div>
  );
}
