import { useEffect } from 'react';
import { RouterProvider } from 'react-router-dom';
import { router } from './router';
import { Providers } from './providers';
import { useAuthStore } from '@/stores/authStore';
import { api } from '@/lib/api';

export default function App() {
  const setUser = useAuthStore((s) => s.setUser);
  const setTokens = useAuthStore((s) => s.setTokens);
  const setInitialized = useAuthStore((s) => s.setInitialized);
  const logout = useAuthStore((s) => s.logout);
  const refreshToken = useAuthStore((s) => s.refreshToken);

  useEffect(() => {
    async function restoreSession() {
      if (!refreshToken) {
        setInitialized();
        return;
      }

      try {
        const { data: authData } = await api.post('/api/auth/refresh', { token: refreshToken });
        setTokens(authData.accessToken, authData.refreshToken);

        const { data: user } = await api.get('/api/auth/me');
        setUser(user);
      } catch {
        logout();
      } finally {
        setInitialized();
      }
    }

    restoreSession();
  }, []); // eslint-disable-line react-hooks/exhaustive-deps

  return (
    <Providers>
      <RouterProvider router={router} />
    </Providers>
  );
}
