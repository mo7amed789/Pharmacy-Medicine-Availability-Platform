import { useQuery } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import {
  Clock,
  CheckCircle,
  AlertTriangle,
  Timer,
  ArrowRight,
} from 'lucide-react';
import { useAuthStore } from '@/app/store/authStore';
import { useBranchStore } from '@/app/store/branchStore';
import { pharmacyOpsApi } from '../api/pharmacyOpsApi';
import { Card, CardHeader, CardTitle, CardContent } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { LoadingSpinner } from '@/components/feedback/LoadingSpinner';
import { ErrorState } from '@/components/feedback/ErrorState';
import { formatDateTime } from '@/lib/formatters';
import { useTranslation } from '@/i18n/useTranslation';

export function DashboardPage() {
  const { t } = useTranslation();
  const { user } = useAuthStore();
  const { currentBranch } = useBranchStore();

  const pharmacyId = user?.pharmacyId;
  const branchId = currentBranch?.id;

  const { data: dashboard, isLoading, isError, error, refetch } = useQuery({
    queryKey: ['branch-dashboard', pharmacyId, branchId],
    queryFn: () =>
      pharmacyId && branchId
        ? pharmacyOpsApi.getDashboard(pharmacyId, branchId)
        : Promise.reject('Missing pharmacy or branch'),
    enabled: !!pharmacyId && !!branchId,
    refetchInterval: 15000,
  });

  if (!currentBranch) {
    return (
      <div className="rounded-xl border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 p-8 text-center text-slate-500 dark:text-slate-400">
        {t('pharmacy.selectBranchPrompt')}
      </div>
    );
  }

  if (isLoading) {
    return (
      <div className="py-20">
        <LoadingSpinner size="lg" text={t('common.loading')} />
      </div>
    );
  }

  if (isError) {
    return <ErrorState error={error} onRetry={() => refetch()} />;
  }

  const kpis = [
    {
      title: t('reservations.activeHolds'),
      value: dashboard?.activeReservationsCount ?? 0,
      icon: Clock,
      color: 'text-blue-600 bg-blue-50 dark:bg-blue-950/40 border-blue-100 dark:border-blue-900/40',
      link: '/pharmacy/queue',
    },
    {
      title: t('pharmacy.expiringSoon'),
      value: dashboard?.expiringSoonReservationsCount ?? 0,
      icon: Timer,
      color: 'text-amber-600 bg-amber-50 dark:bg-amber-950/40 border-amber-100 dark:border-amber-900/40',
      link: '/pharmacy/queue?expiringSoonOnly=true',
    },
    {
      title: t('pharmacy.todayPickups'),
      value: dashboard?.todayConfirmedPickupsCount ?? 0,
      icon: CheckCircle,
      color: 'text-emerald-600 bg-emerald-50 dark:bg-emerald-950/40 border-emerald-100 dark:border-emerald-900/40',
      link: '/pharmacy/queue?status=Confirmed',
    },
    {
      title: t('pharmacy.lowStock'),
      value: dashboard?.lowStockItemsCount ?? 0,
      icon: AlertTriangle,
      color: 'text-red-600 bg-red-50 dark:bg-red-950/40 border-red-100 dark:border-red-900/40',
      link: '/pharmacy/low-stock',
    },
  ];

  return (
    <div className="space-y-6">
      {/* Top Welcome Bar */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-2">
        <div>
          <h1 className="text-2xl font-bold text-slate-900 dark:text-slate-100">
            {currentBranch.name} - {t('pharmacy.dashboard')}
          </h1>
          <p className="text-xs text-slate-500 dark:text-slate-400">
            {t('pharmacy.realtimeMetrics', { time: formatDateTime(dashboard?.generatedAtUtc) })}
          </p>
        </div>
        <Button size="sm" variant="outline" onClick={() => refetch()} className="text-xs">
          {t('pharmacy.refreshMetrics')}
        </Button>
      </div>

      {/* KPI Cards Grid */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        {kpis.map((kpi) => {
          const Icon = kpi.icon;
          return (
            <Link key={kpi.title} to={kpi.link} className="block transition-transform hover:-translate-y-0.5">
              <Card className="border-slate-200 dark:border-slate-800 shadow-xs hover:border-blue-300 dark:hover:border-blue-700">
                <CardContent className="p-5 flex items-center justify-between">
                  <div>
                    <p className="text-xs font-semibold text-slate-500 dark:text-slate-400">{kpi.title}</p>
                    <h3 className="text-3xl font-extrabold text-slate-900 dark:text-slate-100 mt-1">{kpi.value}</h3>
                  </div>
                  <div className={`flex h-12 w-12 items-center justify-center rounded-xl border ${kpi.color}`}>
                    <Icon className="h-6 w-6" />
                  </div>
                </CardContent>
              </Card>
            </Link>
          );
        })}
      </div>

      {/* Secondary Operational Stats */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        <Card className="border-slate-200 dark:border-slate-800">
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-semibold text-slate-600 dark:text-slate-300">{t('pharmacy.activeScope')}</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="flex items-baseline gap-2">
              <span className="text-2xl font-bold text-slate-900 dark:text-slate-100">{dashboard?.totalActiveMedicinesCount ?? 0}</span>
              <span className="text-xs text-slate-500 dark:text-slate-400">{t('pharmacy.distinctStocked')}</span>
            </div>
            <Button variant="link" size="sm" asChild className="mt-2 text-xs p-0">
              <Link to="/pharmacy/inventory" className="flex items-center gap-1">
                {t('pharmacy.manageStockLink')} <ArrowRight className="h-3.5 w-3.5 rtl:rotate-180" />
              </Link>
            </Button>
          </CardContent>
        </Card>

        <Card className="border-slate-200 dark:border-slate-800">
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-semibold text-slate-600 dark:text-slate-300">{t('pharmacy.todayCancelled')}</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="flex items-baseline gap-2">
              <span className="text-2xl font-bold text-slate-700 dark:text-slate-300">{dashboard?.todayCancelledReservationsCount ?? 0}</span>
              <span className="text-xs text-slate-500 dark:text-slate-400">{t('pharmacy.cancelledByCustomers')}</span>
            </div>
          </CardContent>
        </Card>

        <Card className="border-slate-200 dark:border-slate-800">
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-semibold text-slate-600 dark:text-slate-300">{t('pharmacy.todayExpired')}</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="flex items-baseline gap-2">
              <span className="text-2xl font-bold text-amber-700 dark:text-amber-400">{dashboard?.todayExpiredReservationsCount ?? 0}</span>
              <span className="text-xs text-slate-500 dark:text-slate-400">{t('pharmacy.sweptBack')}</span>
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
