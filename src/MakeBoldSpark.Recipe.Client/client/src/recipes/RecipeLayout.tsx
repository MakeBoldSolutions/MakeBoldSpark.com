import { type ReactNode } from 'react';
import { NavLink } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';
import { ReauthPrompt } from '../auth/ReauthPrompt';

export function RecipeLayout({ children }: { children: ReactNode }) {
  const { session, signOut, reauthRequired } = useAuth();

  return (
    <div className="recipe-shell">
      <aside className="recipe-sidebar">
        <div className="recipe-brand">MakeBoldSpark Recipes</div>
        <nav>
          <NavLink to="/" end>
            Recipes
          </NavLink>
          <NavLink to="/categories">Categories</NavLink>
        </nav>
        <div className="recipe-session">
          <span>{session?.displayName}</span>
          <button type="button" onClick={signOut}>
            Sign out
          </button>
        </div>
      </aside>
      <main className="recipe-content">
        {reauthRequired && <ReauthPrompt />}
        {children}
      </main>
    </div>
  );
}
