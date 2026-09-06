import { create } from 'zustand';
import { UserDto } from '@/types/auth';
import { tokenManager } from '@/api/tokenManager';

interface AuthState {
  user: UserDto | null;
  isAuthenticated: boolean;
  isInitialized: boolean;
  setSession: (user: UserDto, accessToken: string, refreshToken?: string) => void;
  setUser: (user: UserDto | null) => void;
  logout: () => void;
  setInitialized: (initialized: boolean) => void;
}

export const useAuthStore = create<AuthState>((set) => ({
  user: null,
  isAuthenticated: false,
  isInitialized: false,

  setSession: (user: UserDto, accessToken: string, refreshToken?: string) => {
    tokenManager.setAccessToken(accessToken);
    if (refreshToken) {
      tokenManager.setRefreshToken(refreshToken);
    }
    set({
      user,
      isAuthenticated: true,
      isInitialized: true,
    });
  },

  setUser: (user: UserDto | null) => {
    set({
      user,
      isAuthenticated: !!user,
    });
  },

  logout: () => {
    tokenManager.clearTokens();
    set({
      user: null,
      isAuthenticated: false,
      isInitialized: true,
    });
  },

  setInitialized: (initialized: boolean) => {
    set({ isInitialized: initialized });
  },
}));
