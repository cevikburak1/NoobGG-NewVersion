import { create } from 'zustand';
import type { UserResponse } from '@/features/auth/types';
import { registerAuthAccessor } from '@/lib/api';

const REFRESH_TOKEN_KEY = 'noobgg_refresh_token';

function getPersistedToken(): string | null {
  return sessionStorage.getItem(REFRESH_TOKEN_KEY)
    ?? localStorage.getItem(REFRESH_TOKEN_KEY);
}

function persistToken(token: string) {
  sessionStorage.setItem(REFRESH_TOKEN_KEY, token);
  localStorage.setItem(REFRESH_TOKEN_KEY, token);
}

function clearToken() {
  sessionStorage.removeItem(REFRESH_TOKEN_KEY);
  localStorage.removeItem(REFRESH_TOKEN_KEY);
}

interface AuthState {
  user: UserResponse | null;
  accessToken: string | null;
  refreshToken: string | null;
  isInitialized: boolean;

  isAuthenticated: () => boolean;
  login: (user: UserResponse, accessToken: string, refreshToken: string) => void;
  logout: () => void;
  setTokens: (accessToken: string, refreshToken: string) => void;
  setUser: (user: UserResponse) => void;
  setInitialized: () => void;
}

export const useAuthStore = create<AuthState>((set, get) => ({
  user: null,
  accessToken: null,
  refreshToken: getPersistedToken(),
  isInitialized: false,

  isAuthenticated: () => get().accessToken !== null && get().user !== null,

  login: (user, accessToken, refreshToken) => {
    persistToken(refreshToken);
    set({ user, accessToken, refreshToken });
  },

  logout: () => {
    clearToken();
    set({ user: null, accessToken: null, refreshToken: null });
  },

  setTokens: (accessToken, refreshToken) => {
    persistToken(refreshToken);
    set({ accessToken, refreshToken });
  },

  setUser: (user) => set({ user }),

  setInitialized: () => set({ isInitialized: true }),
}));

registerAuthAccessor({
  getAccessToken: () => useAuthStore.getState().accessToken,
  getRefreshToken: () => useAuthStore.getState().refreshToken,
  setTokens: (access, refresh) => useAuthStore.getState().setTokens(access, refresh),
  logout: () => useAuthStore.getState().logout(),
});
