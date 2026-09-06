import { NavLink, Outlet } from 'react-router-dom';
import { ShieldAlert, Building2, BookOpen, Clock } from 'lucide-react';
import { Navbar } from './Navbar';
import { Badge } from '@/components/ui/badge';
import { useTranslation } from '@/i18n/useTranslation';

export function AdminLayout() {
  const { t, language } = useTranslation();

  const navItems = [
    { label: language === 'ar' ? 'نظرة عامة' : 'Platform Overview', path: '/admin/dashboard', icon: ShieldAlert },
    { label: t('admin.pharmacyApprovals'), path: '/admin/pharmacies', icon: Building2 },
    { label: t('admin.centralCatalog'), path: '/admin/catalog', icon: BookOpen },
    { label: t('admin.reservationsOversight'), path: '/admin/reservations', icon: Clock },
  ];

  return (
    <div className="flex min-h-screen flex-col bg-slate-50 dark:bg-slate-950">
      <Navbar />

      {/* Admin Header */}
      <div className="border-b border-purple-200 dark:border-purple-900/60 bg-purple-950 text-white">
        <div className="mx-auto flex max-w-7xl items-center justify-between px-4 py-4 sm:px-6 lg:px-8">
          <div className="flex items-center gap-3">
            <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-purple-800 text-purple-200">
              <ShieldAlert className="h-6 w-6" />
            </div>
            <div>
              <div className="flex items-center gap-2">
                <span className="font-bold text-white text-base">
                  {language === 'ar' ? 'الإدارة المركزية للمنصة' : 'Platform Administration'}
                </span>
                <Badge variant="default" className="bg-purple-600 text-white text-xs">
                  {language === 'ar' ? 'مسؤول النظام' : 'Superuser'}
                </Badge>
              </div>
              <p className="text-xs text-purple-200">
                {t('admin.superuserSubtitle')}
              </p>
            </div>
          </div>
        </div>
      </div>

      {/* Admin Subnav */}
      <div className="border-b border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900">
        <div className="mx-auto flex max-w-7xl overflow-x-auto px-4 sm:px-6 lg:px-8">
          {navItems.map((item) => {
            const Icon = item.icon;
            return (
              <NavLink
                key={item.path}
                to={item.path}
                className={({ isActive }) =>
                  `flex items-center gap-2 whitespace-nowrap border-b-2 px-4 py-3 text-sm font-medium transition-colors ${
                    isActive
                      ? 'border-purple-600 text-purple-700 dark:text-purple-400 font-semibold'
                      : 'border-transparent text-slate-600 dark:text-slate-400 hover:border-slate-300 dark:hover:border-slate-700 hover:text-slate-900 dark:hover:text-slate-100'
                  }`
                }
              >
                <Icon className="h-4 w-4" />
                {item.label}
              </NavLink>
            );
          })}
        </div>
      </div>

      {/* Main Outlet */}
      <main className="mx-auto w-full max-w-7xl flex-1 px-4 py-8 sm:px-6 lg:px-8">
        <Outlet />
      </main>
    </div>
  );
}
