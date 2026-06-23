import { type ReactNode } from 'react';
import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom';
import { AuthProvider, useAuth } from './auth/AuthContext';
import { LoginPage } from './auth/LoginPage';
import { CmsLayout } from './cms/CmsLayout';
import { DashboardPage } from './cms/DashboardPage';
import { EntityCrudPage } from './cms/EntityCrudPage';
import {
  siteConfig,
  blogConfig,
  authorConfig,
  categoryConfig,
  keywordConfig,
  contentPartConfig,
  subscriberConfig,
  newsletterConfig,
  mailSettingConfig,
} from './cms/entityConfigs';
import { BlogSelector } from './cms/overrides/BlogSelector';
import { SiteSelector } from './cms/overrides/SiteSelector';

function RequireAuth({ children }: { children: ReactNode }) {
  const { session } = useAuth();
  if (!session) return <Navigate to="/login" replace />;
  return <>{children}</>;
}

function AppRoutes() {
  const { session } = useAuth();

  return (
    <Routes>
      <Route path="/login" element={session ? <Navigate to="/" replace /> : <LoginPage />} />
      <Route
        path="/*"
        element={
          <RequireAuth>
            <CmsLayout>
              <Routes>
                <Route path="/" element={<DashboardPage />} />
                <Route path="/sites" element={<EntityCrudPage config={siteConfig} />} />
                <Route path="/blogs" element={<EntityCrudPage config={blogConfig} />} />
                <Route path="/authors" element={<EntityCrudPage config={authorConfig} />} />
                <Route path="/categories" element={<EntityCrudPage config={categoryConfig} />} />
                <Route path="/posts" element={<BlogSelector />} />
                <Route path="/menus" element={<SiteSelector />} />
                <Route path="/keywords" element={<EntityCrudPage config={keywordConfig} />} />
                <Route path="/content-parts" element={<EntityCrudPage config={contentPartConfig} />} />
                <Route path="/subscribers" element={<EntityCrudPage config={subscriberConfig} />} />
                <Route path="/newsletters" element={<EntityCrudPage config={newsletterConfig} />} />
                <Route path="/mail-settings" element={<EntityCrudPage config={mailSettingConfig} />} />
              </Routes>
            </CmsLayout>
          </RequireAuth>
        }
      />
    </Routes>
  );
}

export function App() {
  return (
    <BrowserRouter basename="/cms">
      <AuthProvider>
        <AppRoutes />
      </AuthProvider>
    </BrowserRouter>
  );
}
