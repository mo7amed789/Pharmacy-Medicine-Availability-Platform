import { useEffect } from 'react';
import { Outlet } from 'react-router-dom';
import { useAuthStore } from '@/app/store/authStore';
import { authApi } from '@/features/auth/api/authApi';
import { tokenManager } from '@/api/tokenManager';
import { ErrorBoundary } from '@/components/feedback/ErrorBoundary';

export function RootLayout() {
  const { setSession, setInitialized, logout } = useAuthStore();

  useEffect(() => {
    const initializeAuth = async () => {
      const refreshToken = tokenManager.getRefreshToken();
      if (!refreshToken) {
        setInitialized(true);
        return;
      }

      try {
        const authResponse = await authApi.refreshToken({ refreshToken });
        setSession(authResponse.user, authResponse.accessToken, authResponse.refreshToken);
      } catch {
        // Failed session hydration: clear tokens
        logout();
      } finally {
        setInitialized(true);
      }
    };

    initializeAuth();
  }, [setSession, setInitialized, logout]);

  return (
    <ErrorBoundary>
      <Outlet />
    </ErrorBoundary>
  );
}
