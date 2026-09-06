import { Link } from 'react-router-dom';
import { ShieldX, Home } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card, CardHeader, CardTitle, CardDescription, CardContent, CardFooter } from '@/components/ui/card';
import { useAuthStore } from '@/app/store/authStore';
import { useTranslation } from '@/i18n/useTranslation';

export function UnauthorizedPage() {
  const { t } = useTranslation();
  const { user } = useAuthStore();

  return (
    <div className="flex min-h-screen items-center justify-center bg-slate-50 dark:bg-slate-950 px-4 py-12">
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

        <CardContent>
          <p className="text-xs text-slate-600 dark:text-slate-400">
            {user?.fullName || 'Anonymous'} ({user?.role ? t(`nav.roles.${user.role}` as any) || user.role : 'Guest'})
          </p>
        </CardContent>

        <CardFooter className="flex justify-center gap-3">
          <Button variant="outline" asChild size="sm">
            <Link to="/" className="flex items-center gap-1.5">
              <Home className="h-4 w-4" />
              {t('auth.goToHome')}
            </Link>
          </Button>
          {user?.role === 'PharmacyStaff' || user?.role === 'PharmacyAdmin' ? (
            <Button asChild size="sm">
              <Link to="/pharmacy/dashboard">{t('nav.staffOps')}</Link>
            </Button>
          ) : user?.role === 'PlatformAdmin' ? (
            <Button asChild size="sm">
              <Link to="/admin/dashboard">{t('nav.platformAdmin')}</Link>
            </Button>
          ) : (
            <Button asChild size="sm">
              <Link to="/customer/search">{t('nav.findMedicine')}</Link>
            </Button>
          )}
        </CardFooter>
      </Card>
    </div>
  );
}
