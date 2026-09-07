import { Link, useNavigate } from 'react-router-dom';
import { ShieldX, Home, LogOut, ArrowRight, ArrowLeft } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card, CardHeader, CardTitle, CardDescription, CardContent, CardFooter } from '@/components/ui/card';
import { useAuthStore } from '@/app/store/authStore';
import { useTranslation } from '@/i18n/useTranslation';
import { ThemeToggle } from '@/components/common/ThemeToggle';
import { LanguageToggle } from '@/components/common/LanguageToggle';

export function UnauthorizedPage() {
  const { t, language } = useTranslation();
  const { user, logout } = useAuthStore();
  const navigate = useNavigate();

  const handleSwitchAccount = () => {
    logout();
    navigate('/login', { replace: true });
  };

  const dashboardPath =
    user?.role === 'PlatformAdmin'
      ? '/admin/dashboard'
      : user?.role === 'PharmacyStaff' || user?.role === 'PharmacyAdmin'
      ? '/pharmacy/dashboard'
      : '/customer/search';

  const dashboardLabel =
    user?.role === 'PlatformAdmin'
      ? t('nav.platformAdmin')
      : user?.role === 'PharmacyStaff' || user?.role === 'PharmacyAdmin'
      ? t('nav.staffOps')
      : t('nav.findMedicine');

  const ArrowIcon = language === 'ar' ? ArrowLeft : ArrowRight;

  return (
    <div className="relative flex min-h-screen items-center justify-center bg-slate-50 dark:bg-slate-950 px-4 py-12">
      <div className="absolute top-4 end-4 flex items-center gap-2">
        <LanguageToggle />
        <ThemeToggle />
      </div>

      <Card className="w-full max-w-md text-center shadow-lg border-slate-200 dark:border-slate-800">
        <CardHeader className="space-y-2">
          <div className="mx-auto flex h-14 w-14 items-center justify-center rounded-full bg-red-100 dark:bg-red-950/50 text-red-600 dark:text-red-400">
            <ShieldX className="h-8 w-8" />
          </div>
          <CardTitle className="text-2xl font-bold text-slate-900 dark:text-slate-100">{t('auth.unauthorizedTitle')}</CardTitle>
          <CardDescription className="text-slate-500 dark:text-slate-400">
            {t('auth.unauthorizedDesc')}
          </CardDescription>
        </CardHeader>

        <CardContent className="space-y-3">
          <div className="rounded-xl border border-slate-200 dark:border-slate-800 bg-slate-100/70 dark:bg-slate-900/60 p-3 text-xs text-slate-700 dark:text-slate-300">
            <span className="font-semibold text-slate-900 dark:text-slate-100">{user?.fullName || 'Anonymous'}</span>
            <span className="mx-1.5">•</span>
            <span className="inline-flex items-center px-2 py-0.5 rounded-full text-[11px] font-medium bg-blue-100 text-blue-800 dark:bg-blue-950/50 dark:text-blue-300">
              {user?.role ? t(`nav.roles.${user.role}` as any) || user.role : 'Guest'}
            </span>
          </div>
        </CardContent>

        <CardFooter className="flex flex-col gap-2">
          <Button asChild className="w-full gap-2">
            <Link to={dashboardPath}>
              <span>{dashboardLabel}</span>
              <ArrowIcon className="h-4 w-4" />
            </Link>
          </Button>

          <div className="flex w-full gap-2">
            <Button variant="outline" asChild size="sm" className="flex-1">
              <Link to="/" className="flex items-center justify-center gap-1.5">
                <Home className="h-4 w-4" />
                {t('auth.goToHome')}
              </Link>
            </Button>

            <Button
              variant="outline"
              size="sm"
              onClick={handleSwitchAccount}
              className="flex-1 flex items-center justify-center gap-1.5 text-amber-600 dark:text-amber-400 border-amber-200 dark:border-amber-900/50 hover:bg-amber-50 dark:hover:bg-amber-950/30"
            >
              <LogOut className="h-4 w-4" />
              {t('auth.switchAccount')}
            </Button>
          </div>
        </CardFooter>
      </Card>
    </div>
  );
}
