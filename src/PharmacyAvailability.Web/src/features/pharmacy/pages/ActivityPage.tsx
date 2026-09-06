import { useQuery } from '@tanstack/react-query';
import { History, ArrowUpRight, Clock, CheckCircle2 } from 'lucide-react';
import { useAuthStore } from '@/app/store/authStore';
import { useBranchStore } from '@/app/store/branchStore';
import { pharmacyOpsApi } from '../api/pharmacyOpsApi';
import { BranchActivityFeedItemDto } from '@/types/operations';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { LoadingSpinner } from '@/components/feedback/LoadingSpinner';
import { ErrorState } from '@/components/feedback/ErrorState';
import { EmptyState } from '@/components/feedback/EmptyState';
import { formatDateTime, formatRelativeTime } from '@/lib/formatters';
import { useTranslation } from '@/i18n/useTranslation';

export function ActivityPage() {
  const { t } = useTranslation();
  const { user } = useAuthStore();
  const { currentBranch } = useBranchStore();

  const pharmacyId = user?.pharmacyId;
  const branchId = currentBranch?.id;

  const { data, isLoading, isError, error, refetch } = useQuery({
    queryKey: ['branch-activity', pharmacyId, branchId],
    queryFn: () =>
      pharmacyId && branchId
        ? pharmacyOpsApi.getBranchActivity(pharmacyId, branchId, 1, 50)
        : Promise.reject('Missing branch'),
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

  const getActivityIcon = (type: string) => {
    switch (type) {
      case 'ReservationPlaced':
        return <Clock className="h-4 w-4 text-blue-600 dark:text-blue-400" />;
      case 'ReservationConfirmed':
        return <CheckCircle2 className="h-4 w-4 text-emerald-600 dark:text-emerald-400" />;
      case 'StockIncrease':
      case 'StockAdjusted':
        return <ArrowUpRight className="h-4 w-4 text-indigo-600 dark:text-indigo-400" />;
      default:
        return <History className="h-4 w-4 text-slate-500 dark:text-slate-400" />;
    }
  };

  return (
    <div className="space-y-5">
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
        <div>
          <h1 className="text-2xl font-bold text-slate-900 dark:text-slate-100">{t('pharmacy.activityTitle')}</h1>
          <p className="text-xs text-slate-500 dark:text-slate-400">
            {t('pharmacy.activityDesc', { branch: currentBranch.name })}
          </p>
        </div>
        <Button size="sm" variant="outline" onClick={() => refetch()} className="text-xs">
          {t('common.refresh')}
        </Button>
      </div>

      {isLoading ? (
        <div className="py-16">
          <LoadingSpinner size="lg" text={t('common.loading')} />
        </div>
      ) : isError ? (
        <ErrorState error={error} onRetry={() => refetch()} />
      ) : !data || data.items.length === 0 ? (
        <EmptyState
          icon={History}
          title={t('feedback.noDataTitle')}
          description={t('feedback.noDataDesc')}
        />
      ) : (
        <div className="space-y-3">
          {data.items.map((event: BranchActivityFeedItemDto) => (
            <div
              key={event.id}
              className="rounded-xl border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 p-4 shadow-xs flex items-center justify-between gap-4"
            >
              <div className="flex items-start gap-3">
                <div className="flex h-9 w-9 items-center justify-center rounded-lg bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 shrink-0 mt-0.5">
                  {getActivityIcon(event.activityType)}
                </div>
                <div>
                  <div className="flex items-center gap-2">
                    <span className="font-semibold text-slate-900 dark:text-slate-100 text-sm">{event.description}</span>
                    <Badge variant="secondary" className="text-[10px]">
                      {event.activityType}
                    </Badge>
                  </div>
                  {event.medicineBrandName && (
                    <p className="text-xs text-slate-500 dark:text-slate-400 mt-0.5">
                      {t('reservations.medicine')}: <span className="font-medium text-slate-700 dark:text-slate-300">{event.medicineBrandName}</span>
                      {event.deltaQuantity != null && (
                        <span> • {t('common.quantity')}: {event.deltaQuantity > 0 ? `+${event.deltaQuantity}` : event.deltaQuantity} {t('common.units')}</span>
                      )}
                    </p>
                  )}
                  <p className="text-[11px] text-slate-400 mt-1">
                    {event.actorRole || 'System'} • {formatRelativeTime(event.timestamp)} ({formatDateTime(event.timestamp)})
                  </p>
                </div>
              </div>

              {event.resultingQuantity != null && (
                <div className="text-end shrink-0">
                  <span className="text-xs text-slate-400 block">{t('pharmacy.resultingStock')}</span>
                  <span className="font-bold text-slate-800 dark:text-slate-200 text-sm">{event.resultingQuantity} {t('common.units')}</span>
                </div>
              )}
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
