import { type ReactNode } from 'react';
import { NavLink } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';
import { ReauthPrompt } from './ReauthPrompt';

export const navSections: Array<{ to: string; label: string }> = [
  { to: '/sites', label: 'Sites' },
  { to: '/blogs', label: 'Blogs' },
  { to: '/authors', label: 'Authors' },
  { to: '/categories', label: 'Categories' },
  { to: '/posts', label: 'Posts' },
  { to: '/menus', label: 'Menus' },
  { to: '/keywords', label: 'Keywords' },
  { to: '/content-parts', label: 'Content Parts' },
  { to: '/subscribers', label: 'Subscribers' },
  { to: '/newsletters', label: 'Newsletters' },
  { to: '/mail-settings', label: 'Mail Configuration' },
];

export function CmsLayout({ children }: { children: ReactNode }) {
  const { session, signOut, reauthRequired } = useAuth();

  return (
    <div className="cms-shell">
      <aside className="cms-sidebar">
        <img src="/assets/makebold/logos/make-bold-solutions-logo.svg" alt="MakeBold Solutions" className="cms-sidebar-logo" />
        <nav>
          <NavLink to="/" end>
            Dashboard
          </NavLink>
          {navSections.map((item) => (
            <NavLink key={item.to} to={item.to}>
              {item.label}
            </NavLink>
          ))}
        </nav>
        <div className="cms-sidebar-footer">
          <span>{session?.displayName}</span>
          <button type="button" onClick={signOut}>
            Sign out
          </button>
        </div>
      </aside>
      <main className="cms-content">
        {reauthRequired && <ReauthPrompt />}
        {children}
      </main>
    </div>
  );
}
