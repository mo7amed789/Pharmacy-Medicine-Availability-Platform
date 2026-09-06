import { Link, useNavigate } from 'react-router-dom';
import { Pill, LogOut, User as UserIcon, ShieldCheck, Store } from 'lucide-react';
import { useAuthStore } from '@/app/store/authStore';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { authApi } from '@/features/auth/api/authApi';
import { tokenManager } from '@/api/tokenManager';

export function Navbar() {
  const { user, isAuthenticated, logout } = useAuthStore();
  const navigate = useNavigate();

  const handleLogout = async () => {
    try {
      const refreshToken = tokenManager.getRefreshToken();
      if (refreshToken) {
        await authApi.revokeToken({ refreshToken });
      }
    } catch {
      // Ignore network errors on logout
    } finally {
      logout();
      navigate('/login');
    }
  };

  return (
    <header className="sticky top-0 z-40 w-full border-b border-slate-200 bg-white/95 backdrop-blur-xs">
      <div className="mx-auto flex h-16 max-w-7xl items-center justify-between px-4 sm:px-6 lg:px-8">
        {/* Brand */}
        <div className="flex items-center gap-6">
          <Link to="/" className="flex items-center gap-2.5 font-bold text-slate-900">
            <div className="flex h-9 w-9 items-center justify-center rounded-lg bg-blue-600 text-white shadow-xs">
              <Pill className="h-5 w-5" />
            </div>
            <span className="text-lg tracking-tight font-extrabold text-blue-900">
              PharmaFinder
            </span>
          </Link>

          {/* Navigation Links */}
          <nav className="hidden md:flex items-center gap-5 text-sm font-medium text-slate-600">
            <Link to="/customer/search" className="hover:text-blue-600 transition-colors">
              Find Medicine
            </Link>
            <Link to="/customer/branches" className="hover:text-blue-600 transition-colors">
              Pharmacies
            </Link>

            {isAuthenticated && (user?.role === 'Customer' || user?.role === 'PlatformAdmin') && (
              <Link to="/customer/reservations" className="hover:text-blue-600 transition-colors">
                My Holds
              </Link>
            )}

            {isAuthenticated &&
              (user?.role === 'PharmacyStaff' ||
                user?.role === 'PharmacyAdmin' ||
                user?.role === 'PlatformAdmin') && (
                <Link
                  to="/pharmacy/dashboard"
                  className="flex items-center gap-1.5 text-blue-700 font-semibold hover:text-blue-900"
                >
                  <Store className="h-4 w-4" />
                  Staff Operations
                </Link>
              )}

            {isAuthenticated && user?.role === 'PlatformAdmin' && (
              <Link
                to="/admin/dashboard"
                className="flex items-center gap-1.5 text-purple-700 font-semibold hover:text-purple-900"
              >
                <ShieldCheck className="h-4 w-4" />
                Platform Admin
              </Link>
            )}
          </nav>
        </div>

        {/* Right Auth Controls */}
        <div className="flex items-center gap-3">
          {isAuthenticated && user ? (
            <div className="flex items-center gap-3">
              <div className="hidden sm:flex flex-col text-right">
                <span className="text-sm font-semibold text-slate-800">{user.fullName}</span>
                <div className="flex justify-end">
                  <Badge variant={user.role === 'PlatformAdmin' ? 'default' : 'secondary'} className="text-[10px] px-1.5 py-0">
                    {user.role}
                  </Badge>
                </div>
              </div>
              <Button
                variant="ghost"
                size="sm"
                onClick={handleLogout}
                className="text-slate-600 hover:text-red-600 flex items-center gap-1"
              >
                <LogOut className="h-4 w-4" />
                <span className="hidden sm:inline">Sign Out</span>
              </Button>
            </div>
          ) : (
            <div className="flex items-center gap-2">
              <Button variant="ghost" size="sm" asChild>
                <Link to="/login" className="flex items-center gap-1">
                  <UserIcon className="h-4 w-4" />
                  Sign In
                </Link>
              </Button>
              <Button size="sm" asChild>
                <Link to="/register">Register</Link>
              </Button>
            </div>
          )}
        </div>
      </div>
    </header>
  );
}
