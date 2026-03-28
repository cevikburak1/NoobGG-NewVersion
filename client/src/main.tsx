import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import axios from 'axios';
import { useAuthStore } from '@/stores/authStore';
import './styles/globals.css';

const BASE_URL = import.meta.env.VITE_API_URL ?? '';
const REFRESH_TOKEN_KEY = 'noobgg_refresh_token';

async function bootstrap() {
  const refreshToken =
    sessionStorage.getItem(REFRESH_TOKEN_KEY) ??
    localStorage.getItem(REFRESH_TOKEN_KEY);

  if (refreshToken) {
    try {
      const { data } = await axios.post(`${BASE_URL}/api/auth/refresh`, {
        token: refreshToken,
      });
      useAuthStore.getState().login(data.user, data.accessToken, data.refreshToken, data.isDeactivated);
    } catch {
      sessionStorage.removeItem(REFRESH_TOKEN_KEY);
      localStorage.removeItem(REFRESH_TOKEN_KEY);
      useAuthStore.getState().logout();
    }
  }

  useAuthStore.getState().setInitialized();

  const { default: App } = await import('./app/App');

  createRoot(document.getElementById('root')!).render(
    <StrictMode>
      <App />
    </StrictMode>,
  );
}

bootstrap();
