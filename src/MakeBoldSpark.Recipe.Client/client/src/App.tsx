import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom';
import { AuthProvider, useAuth } from './auth/AuthContext';
import { LoginPage } from './auth/LoginPage';
import { CategoryMaintenancePage } from './recipes/CategoryMaintenancePage';
import { RecipeLayout } from './recipes/RecipeLayout';
import { RecipeMaintenancePage } from './recipes/RecipeMaintenancePage';

function RoutesForSession() {
  const { token } = useAuth();
  if (!token) return <LoginPage />;

  return (
    <RecipeLayout>
      <Routes>
        <Route path="/" element={<RecipeMaintenancePage />} />
        <Route path="/categories" element={<CategoryMaintenancePage />} />
        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </RecipeLayout>
  );
}

export function App() {
  return (
    <BrowserRouter basename="/recipes">
      <AuthProvider>
        <RoutesForSession />
      </AuthProvider>
    </BrowserRouter>
  );
}
