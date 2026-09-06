import { ReactNode } from 'react';
import { Navigate, useLocation } from 'react-router-dom';
import { useAuthStore } from '@/app/store/authStore';
import { UserRole } from '@/types/auth';
import { LoadingSpinner } from '@/components/feedback/LoadingSpinner';

interface ProtectedRouteProps {
  allowedRoles?: UserRole[];
  requireVerified?: boolean;
  children: ReactNode;
}

export function ProtectedRoute({ allowedRoles, requireVerified = true, children }: ProtectedRouteProps) {
  const { isAuthenticated, user, isInitialized } = useAuthStore();
  const location = useLocation();

  if (!isInitialized) {
    return (
      <div className="flex h-screen w-full items-center justify-center bg-slate-50">
        <LoadingSpinner size="lg" text="Initializing session..." />
      </div>
    );
  }

  if (!isAuthenticated || !user) {
    return <Navigate to="/login" state={{ from: location }} replace />;
  }

  if (requireVerified && (!user.isEmailVerified || !user.isPhoneVerified)) {
    return <Navigate to="/verify" replace />;
  }

  if (allowedRoles && !allowedRoles.includes(user.role)) {
    return <Navigate to="/unauthorized" replace />;
  }

  return <>{children}</>;
}
