import { Link, useNavigate } from 'react-router-dom';
import { LogOut, User as UserIcon, ShieldCheck, Store } from 'lucide-react';
import { useAuthStore } from '@/app/store/authStore';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { authApi } from '@/features/auth/api/authApi';
import { tokenManager } from '@/api/tokenManager';
import { ThemeToggle } from '@/components/common/ThemeToggle';
import { LanguageToggle } from '@/components/common/LanguageToggle';
import { useTranslation } from '@/i18n/useTranslation';

export function Navbar() {
  const { user, isAuthenticated, logout } = useAuthStore();
  const navigate = useNavigate();
  const { t } = useTranslation();

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
    <header className="sticky top-0 z-40 w-full border-b border-slate-200 dark:border-slate-800 bg-white/95 dark:bg-slate-900/95 backdrop-blur-xs">
      <div className="mx-auto flex h-16 max-w-7xl items-center justify-between px-4 sm:px-6 lg:px-8">
        {/* Brand */}
        <div className="flex items-center gap-6">
          <Link to="/" className="flex items-center gap-2.5 font-bold text-slate-900 dark:text-slate-100 group">
            <img
              src="/logo.png"
              alt="PharmaFinder"
              className="h-10 w-10 object-contain rounded-lg transition-transform group-hover:scale-105 shadow-xs"
            />
            <div className="flex flex-col">
              <span className="text-lg tracking-tight font-black text-blue-950 dark:text-blue-400 font-heading leading-tight">
                {t('nav.brand')}
              </span>
              <span className="text-[10px] text-slate-400 dark:text-slate-500 font-medium leading-none">
                PharmaFinder
              </span>
            </div>
          </Link>

          {/* Navigation Links */}
          <nav className="hidden md:flex items-center gap-5 text-sm font-medium text-slate-600 dark:text-slate-300">
            <Link to="/customer/search" className="hover:text-blue-600 dark:hover:text-blue-400 transition-colors">
              {t('nav.findMedicine')}
            </Link>
            <Link to="/customer/branches" className="hover:text-blue-600 dark:hover:text-blue-400 transition-colors">
              {t('nav.pharmacies')}
            </Link>

            {isAuthenticated && (user?.role === 'Customer' || user?.role === 'PlatformAdmin') && (
              <Link to="/customer/reservations" className="hover:text-blue-600 dark:hover:text-blue-400 transition-colors">
                {t('nav.myHolds')}
              </Link>
            )}

            {isAuthenticated &&
              (user?.role === 'PharmacyStaff' ||
                user?.role === 'PharmacyAdmin' ||
                user?.role === 'PlatformAdmin') && (
                <Link
                  to="/pharmacy/dashboard"
                  className="flex items-center gap-1.5 text-blue-700 dark:text-blue-400 font-semibold hover:text-blue-900 dark:hover:text-blue-300"
                >
                  <Store className="h-4 w-4" />
                  {t('nav.staffOps')}
                </Link>
              )}

            {isAuthenticated && user?.role === 'PlatformAdmin' && (
              <Link
                to="/admin/dashboard"
                className="flex items-center gap-1.5 text-purple-700 dark:text-purple-400 font-semibold hover:text-purple-900 dark:hover:text-purple-300"
              >
                <ShieldCheck className="h-4 w-4" />
                {t('nav.platformAdmin')}
              </Link>
            )}
          </nav>
        </div>

        {/* Right Auth, Language & Theme Controls */}
        <div className="flex items-center gap-2 sm:gap-3">
          <LanguageToggle />
          <ThemeToggle />

          {isAuthenticated && user ? (
            <div className="flex items-center gap-2 sm:gap-3">
              <div className="hidden sm:flex flex-col text-start">
                <span className="text-sm font-semibold text-slate-800 dark:text-slate-100">{user.fullName}</span>
                <div className="flex justify-start">
                  <Badge variant={user.role === 'PlatformAdmin' ? 'default' : 'secondary'} className="text-[10px] px-1.5 py-0">
                    {t(`nav.roles.${user.role}`)}
                  </Badge>
                </div>
              </div>
              <Button
                variant="ghost"
                size="sm"
                onClick={handleLogout}
                className="text-slate-600 dark:text-slate-300 hover:text-red-600 dark:hover:text-red-400 flex items-center gap-1"
              >
                <LogOut className="h-4 w-4" />
                <span className="hidden sm:inline">{t('nav.signOut')}</span>
              </Button>
            </div>
          ) : (
            <div className="flex items-center gap-2">
              <Button variant="ghost" size="sm" asChild>
                <Link to="/login" className="flex items-center gap-1 text-slate-700 dark:text-slate-200">
                  <UserIcon className="h-4 w-4" />
                  {t('nav.signIn')}
                </Link>
              </Button>
              <Button size="sm" asChild>
                <Link to="/register">{t('nav.register')}</Link>
              </Button>
            </div>
          )}
        </div>
      </div>
    </header>
  );
}
