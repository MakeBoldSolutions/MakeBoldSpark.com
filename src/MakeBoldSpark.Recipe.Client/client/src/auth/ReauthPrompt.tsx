import { useState, type FormEvent } from 'react';
import { useAuth } from './AuthContext';

export function ReauthPrompt() {
  const { signIn, clearReauthRequired } = useAuth();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');

  async function submit(event: FormEvent) {
    event.preventDefault();
    setError('');
    try {
      await signIn(email, password);
      clearReauthRequired();
    } catch {
      setError('Invalid email or password.');
    }
  }

  return (
    <section className="reauth" aria-label="Reauthentication required">
      <h2>Session expired</h2>
      <form onSubmit={submit}>
        <label className="field">
          <span>Email</span>
          <input type="email" value={email} onChange={(event) => setEmail(event.target.value)} />
        </label>
        <label className="field">
          <span>Password</span>
          <input type="password" value={password} onChange={(event) => setPassword(event.target.value)} />
        </label>
        {error && <p role="alert">{error}</p>}
        <button type="submit">Sign in</button>
      </form>
    </section>
  );
}
