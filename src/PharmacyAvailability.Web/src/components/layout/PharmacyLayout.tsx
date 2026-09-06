import { useEffect } from 'react';
import { NavLink, Outlet } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  LayoutDashboard,
  ClipboardList,
  Boxes,
  AlertTriangle,
  History,
  Users,
  Building2,
  Power,
} from 'lucide-react';
import { Navbar } from './Navbar';
import { useAuthStore } from '@/app/store/authStore';
import { useBranchStore } from '@/app/store/branchStore';
import { pharmacyOpsApi } from '@/features/pharmacy/api/pharmacyOpsApi';
import { customerApi } from '@/features/customer/api/customerApi';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { LoadingSpinner } from '@/components/feedback/LoadingSpinner';
import { useTranslation } from '@/i18n/useTranslation';

export function PharmacyLayout() {
  const { user } = useAuthStore();
  const { currentBranch, setCurrentBranch } = useBranchStore();
  const queryClient = useQueryClient();
  const { t, language } = useTranslation();

  // If PlatformAdmin has no pharmacyId assigned, they can inspect all branches in the platform
  const userPharmacyId = user?.pharmacyId;

  const { data: branches, isLoading } = useQuery({
    queryKey: ['pharmacy-branches', userPharmacyId],
    queryFn: async () => {
      if (userPharmacyId) {
        return pharmacyOpsApi.getBranches(userPharmacyId);
      }
      return customerApi.getActiveBranches();
    },
  });

  // Auto-select first active branch if not yet selected or if selected one is not in current branches
  useEffect(() => {
    if (branches && branches.length > 0) {
      if (!currentBranch || !branches.some((b) => b.id === currentBranch.id)) {
        setCurrentBranch(branches[0]);
      }
    }
  }, [branches, currentBranch, setCurrentBranch]);

  // Toggle branch operational status mutation
  const effectivePharmacyId = userPharmacyId || currentBranch?.pharmacyId;

  const toggleStatusMutation = useMutation({
    mutationFn: async ({ branchId, isActive }: { branchId: string; isActive: boolean }) => {
      if (!effectivePharmacyId) return;
      return pharmacyOpsApi.toggleBranchStatus(effectivePharmacyId, branchId, isActive);
    },
    onSuccess: (updated) => {
      if (updated) {
        setCurrentBranch(updated);
        queryClient.invalidateQueries({ queryKey: ['pharmacy-branches'] });
      }
    },
  });

  const navItems = [
    { label: t('pharmacy.dashboard'), path: '/pharmacy/dashboard', icon: LayoutDashboard },
    { label: t('pharmacy.pickupQueue'), path: '/pharmacy/queue', icon: ClipboardList },
    { label: t('pharmacy.inventory'), path: '/pharmacy/inventory', icon: Boxes },
    { label: t('pharmacy.lowStock'), path: '/pharmacy/low-stock', icon: AlertTriangle },
    { label: language === 'ar' ? 'سجل النشاط' : 'Activity Feed', path: '/pharmacy/activity', icon: History },
  ];

  if (user?.role === 'PharmacyAdmin' || user?.role === 'PlatformAdmin') {
    navItems.push({ label: t('pharmacy.staff'), path: '/pharmacy/staff', icon: Users });
  }

  return (
    <div className="flex min-h-screen flex-col bg-slate-50 dark:bg-slate-950">
      <Navbar />

      {/* Pharmacy Operations Top Header */}
      <div className="border-b border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 shadow-xs">
        <div className="mx-auto flex max-w-7xl flex-wrap items-center justify-between gap-4 px-4 py-3 sm:px-6 lg:px-8">
          <div className="flex items-center gap-3">
            <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-blue-50 dark:bg-blue-950/50 text-blue-700 dark:text-blue-400">
              <Building2 className="h-6 w-6" />
            </div>
            <div>
              <div className="flex items-center gap-2">
                <span className="font-bold text-slate-900 dark:text-slate-100 text-base">{t('pharmacy.portalTitle')}</span>
                {currentBranch && (
                  <Badge variant={currentBranch.isActive ? 'success' : 'destructive'} className="text-xs">
                    {currentBranch.isActive ? t('pharmacy.branchOpen') : t('pharmacy.branchClosed')}
                  </Badge>
                )}
              </div>
              <p className="text-xs text-slate-500 dark:text-slate-400">
                {currentBranch ? `${currentBranch.pharmacyName} — ${currentBranch.name}` : (language === 'ar' ? 'يرجى اختيار فرع' : 'Select a branch')}
              </p>
            </div>
          </div>

          {/* Branch Selector and Status Toggle */}
          <div className="flex items-center gap-3">
            {branches && branches.length > 0 ? (
              <div className="flex items-center gap-2">
                <label className="text-xs font-medium text-slate-500 dark:text-slate-400 hidden sm:inline">
                  {t('pharmacy.switchBranch')}
                </label>
                <select
                  value={currentBranch?.id || ''}
                  onChange={(e) => {
                    const selected = branches.find((b) => b.id === e.target.value);
                    if (selected) setCurrentBranch(selected);
                  }}
                  className="h-9 rounded-md border border-slate-300 dark:border-slate-700 bg-white dark:bg-slate-800 px-3 py-1 text-xs font-semibold text-slate-800 dark:text-slate-200 focus:outline-none focus:ring-2 focus:ring-blue-500 shadow-xs"
                >
                  {branches.map((b) => (
                    <option key={b.id} value={b.id}>
                      {b.pharmacyName ? `${b.pharmacyName} — ${b.name}` : b.name} ({b.city})
                    </option>
                  ))}
                </select>
              </div>
            ) : isLoading ? (
              <div className="text-xs text-slate-400 flex items-center gap-1.5">
                <LoadingSpinner size="sm" />
                <span>{t('common.loading')}</span>
              </div>
            ) : null}

            {currentBranch && (
              <Button
                size="sm"
                variant={currentBranch.isActive ? 'outline' : 'default'}
                className="text-xs flex items-center gap-1.5"
                disabled={toggleStatusMutation.isPending}
                onClick={() =>
                  toggleStatusMutation.mutate({
                    branchId: currentBranch.id,
                    isActive: !currentBranch.isActive,
                  })
                }
              >
                <Power className="h-3.5 w-3.5" />
                {currentBranch.isActive
                  ? (language === 'ar' ? 'إغلاق الفرع' : 'Close Branch')
                  : (language === 'ar' ? 'فتح الفرع' : 'Open Branch')}
              </Button>
            )}
          </div>
        </div>
      </div>

      {/* Main Content Area with Navigation Tabs */}
      <div className="mx-auto w-full max-w-7xl flex-1 px-4 py-6 sm:px-6 lg:px-8">
        {/* Navigation Bar / Tabs */}
        <div className="mb-6 flex overflow-x-auto border-b border-slate-200 dark:border-slate-800 pb-px">
          {navItems.map((item) => {
            const Icon = item.icon;
            return (
              <NavLink
                key={item.path}
                to={item.path}
                className={({ isActive }) =>
                  `flex items-center gap-2 whitespace-nowrap border-b-2 px-4 py-2.5 text-sm font-medium transition-colors ${
                    isActive
                      ? 'border-blue-600 text-blue-600 dark:text-blue-400 font-semibold'
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

        {isLoading ? (
          <div className="py-12">
            <LoadingSpinner size="lg" text={t('common.loading')} />
          </div>
        ) : !userPharmacyId && user?.role !== 'PlatformAdmin' ? (
          <div className="rounded-xl border border-amber-200 bg-amber-50 p-6 text-center text-amber-800">
            {language === 'ar'
              ? 'حسابك غير مرتبط بأي صيدلية معتمدة حالياً. يرجى التواصل مع إدارة النظام.'
              : 'You are not currently linked to any active pharmacy. Please contact support.'}
          </div>
        ) : (
          <Outlet />
        )}
      </div>
    </div>
  );
}
