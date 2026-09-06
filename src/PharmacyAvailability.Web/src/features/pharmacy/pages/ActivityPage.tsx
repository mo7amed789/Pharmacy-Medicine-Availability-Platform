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

export function ActivityPage() {
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
      <div className="rounded-xl border border-slate-200 bg-white p-8 text-center text-slate-500">
        Please select a branch to view the operational audit feed.
      </div>
    );
  }

  const getActivityIcon = (type: string) => {
    switch (type) {
      case 'ReservationPlaced':
        return <Clock className="h-4 w-4 text-blue-600" />;
      case 'ReservationConfirmed':
        return <CheckCircle2 className="h-4 w-4 text-emerald-600" />;
      case 'StockIncrease':
      case 'StockAdjusted':
        return <ArrowUpRight className="h-4 w-4 text-indigo-600" />;
      default:
        return <History className="h-4 w-4 text-slate-500" />;
    }
  };

  return (
    <div className="space-y-5">
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
        <div>
          <h1 className="text-2xl font-bold text-slate-900">Branch Activity Audit Feed</h1>
          <p className="text-xs text-slate-500">
            Chronological log of inventory movements, reservations, and staff fulfillment events for {currentBranch.name}.
          </p>
        </div>
        <Button size="sm" variant="outline" onClick={() => refetch()} className="text-xs">
          Refresh Feed
        </Button>
      </div>

      {isLoading ? (
        <div className="py-16">
          <LoadingSpinner size="lg" text="Loading branch activity..." />
        </div>
      ) : isError ? (
        <ErrorState error={error} onRetry={() => refetch()} />
      ) : !data || data.items.length === 0 ? (
        <EmptyState
          icon={History}
          title="No Recent Activity"
          description="There are no logged operational events for this branch yet."
        />
      ) : (
        <div className="space-y-3">
          {data.items.map((event: BranchActivityFeedItemDto) => (
            <div
              key={event.id}
              className="rounded-xl border border-slate-200 bg-white p-4 shadow-xs flex items-center justify-between gap-4"
            >
              <div className="flex items-start gap-3">
                <div className="flex h-9 w-9 items-center justify-center rounded-lg bg-slate-50 border border-slate-200 shrink-0 mt-0.5">
                  {getActivityIcon(event.activityType)}
                </div>
                <div>
                  <div className="flex items-center gap-2">
                    <span className="font-semibold text-slate-900 text-sm">{event.description}</span>
                    <Badge variant="secondary" className="text-[10px]">
                      {event.activityType}
                    </Badge>
                  </div>
                  {event.medicineBrandName && (
                    <p className="text-xs text-slate-500 mt-0.5">
                      Medicine: <span className="font-medium text-slate-700">{event.medicineBrandName}</span>
                      {event.deltaQuantity != null && (
                        <span> • Change: {event.deltaQuantity > 0 ? `+${event.deltaQuantity}` : event.deltaQuantity} units</span>
                      )}
                    </p>
                  )}
                  <p className="text-[11px] text-slate-400 mt-1">
                    Actor: {event.actorRole || 'System'} • {formatRelativeTime(event.timestamp)} ({formatDateTime(event.timestamp)})
                  </p>
                </div>
              </div>

              {event.resultingQuantity != null && (
                <div className="text-right shrink-0">
                  <span className="text-xs text-slate-400 block">Stock Result</span>
                  <span className="font-bold text-slate-800 text-sm">{event.resultingQuantity} units</span>
                </div>
              )}
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
