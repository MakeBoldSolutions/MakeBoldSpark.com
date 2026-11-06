import { useState, type FormEvent } from 'react';
import { BrandMark } from '../BrandMark';
import { useAuth } from './AuthContext';

export function LoginPage() {
  const { signIn } = useAuth();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');

  async function submit(event: FormEvent) {
    event.preventDefault();
    setError('');
    try {
      await signIn(email, password);
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Sign in failed.');
    }
  }

  return (
    <main className="login-page">
      <div className="login-card">
        <BrandMark />
        <div>
          <p className="eyebrow">Make Bold Spark</p>
          <h1>Recipe Maintenance</h1>
        </div>
        <p className="lede">Sign in with the same author account used by MakeBoldSpark CMS.</p>
        <form onSubmit={submit}>
          <label className="field">
            <span>Email</span>
            <input aria-label="Email" type="email" autoComplete="username" value={email} onChange={(e) => setEmail(e.target.value)} required />
          </label>
          <label className="field">
            <span>Password</span>
            <input aria-label="Password" type="password" autoComplete="current-password" value={password} onChange={(e) => setPassword(e.target.value)} required />
          </label>
          {error && <p role="alert">{error}</p>}
          <button type="submit" className="primary">Sign in</button>
        </form>
      </div>
    </main>
  );
}
