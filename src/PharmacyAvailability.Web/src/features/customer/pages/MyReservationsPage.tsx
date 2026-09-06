import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Clock, MapPin, Building2, Ban } from 'lucide-react';
import { customerApi } from '../api/customerApi';
import { ReservationDto } from '@/types/reservations';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Card, CardContent } from '@/components/ui/card';
import { LoadingSpinner } from '@/components/feedback/LoadingSpinner';
import { ErrorState } from '@/components/feedback/ErrorState';
import { EmptyState } from '@/components/feedback/EmptyState';
import { formatDateTime, formatRelativeTime } from '@/lib/formatters';
import { useTranslation } from '@/i18n/useTranslation';

export function MyReservationsPage() {
  const { t, language } = useTranslation();
  const queryClient = useQueryClient();
  const [statusFilter, setStatusFilter] = useState<string>('');

  const { data, isLoading, isError, error, refetch } = useQuery({
    queryKey: ['my-reservations', statusFilter],
    queryFn: () => customerApi.getMyReservations({ status: statusFilter || undefined }),
    refetchInterval: 15000, // Poll every 15s for expiration/confirmation updates
  });

  const cancelMutation = useMutation({
    mutationFn: (id: string) => customerApi.cancelReservation(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['my-reservations'] });
    },
  });

  const getStatusBadge = (status: string) => {
    switch (status) {
      case 'Active':
        return <Badge variant="warning">{language === 'ar' ? 'حجز نشط' : 'Active Hold'}</Badge>;
      case 'Confirmed':
        return <Badge variant="success">{language === 'ar' ? 'تم الاستلام' : 'Picked Up'}</Badge>;
      case 'Cancelled':
        return <Badge variant="secondary">{language === 'ar' ? 'ملغي' : 'Cancelled'}</Badge>;
      case 'Expired':
        return <Badge variant="destructive">{language === 'ar' ? 'منتهي الصلاحية' : 'Expired'}</Badge>;
      case 'Released':
        return <Badge variant="outline">{language === 'ar' ? 'ملغي إدارياً' : 'Admin Released'}</Badge>;
      default:
        return <Badge variant="secondary">{status}</Badge>;
    }
  };

  return (
    <div className="mx-auto max-w-7xl px-4 py-8 sm:px-6 lg:px-8">
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 mb-6">
        <div>
          <h1 className="text-2xl font-bold tracking-tight text-slate-900 dark:text-slate-100">
            {t('reservations.title')}
          </h1>
          <p className="text-sm text-slate-500 dark:text-slate-400">
            {t('reservations.subtitle')}
          </p>
        </div>

        {/* Filter */}
        <select
          value={statusFilter}
          onChange={(e) => setStatusFilter(e.target.value)}
          className="rounded-md border border-slate-300 dark:border-slate-700 bg-white dark:bg-slate-800 px-3 py-1.5 text-xs font-medium text-slate-700 dark:text-slate-200 shadow-xs focus:outline-none focus:ring-2 focus:ring-blue-500"
        >
          <option value="">{t('common.all')}</option>
          <option value="Active">{t('reservations.activeHolds')}</option>
          <option value="Confirmed">{t('reservations.confirmedPickups')}</option>
          <option value="Cancelled">{t('reservations.cancelledHolds')}</option>
          <option value="Expired">{t('reservations.expiredHolds')}</option>
        </select>
      </div>

      {isLoading ? (
        <div className="py-16">
          <LoadingSpinner size="lg" text={language === 'ar' ? 'جارٍ تحميل حجوزاتك...' : 'Loading your reservations...'} />
        </div>
      ) : isError ? (
        <ErrorState error={error} onRetry={() => refetch()} />
      ) : !data || data.items.length === 0 ? (
        <EmptyState
          title={t('reservations.emptyTitle')}
          description={t('reservations.emptyDesc')}
        />
      ) : (
        <div className="space-y-4">
          {data.items.map((res: ReservationDto) => (
            <Card key={res.id} className="border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900">
              <CardContent className="p-5">
                <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
                  <div className="space-y-2">
                    <div className="flex items-center gap-2">
                      <h3 className="text-lg font-bold text-slate-900 dark:text-slate-100">{res.medicineBrandName}</h3>
                      {getStatusBadge(res.status)}
                      <span className="text-xs font-bold text-slate-500 dark:text-slate-400 bg-slate-100 dark:bg-slate-800 px-2 py-0.5 rounded">
                        {language === 'ar' ? `الكمية: ${res.quantity}` : `Qty: ${res.quantity}`}
                      </span>
                    </div>

                    <p className="text-xs text-slate-500 dark:text-slate-400">{res.medicineGenericName}</p>

                    <div className="flex flex-wrap items-center gap-x-4 gap-y-1 text-xs text-slate-600 dark:text-slate-400">
                      <div className="flex items-center gap-1 font-semibold text-slate-800 dark:text-slate-200">
                        <Building2 className="h-3.5 w-3.5 text-slate-500" />
                        <span>{res.pharmacyName} — {res.branchName}</span>
                      </div>
                      <div className="flex items-center gap-1 text-slate-500 dark:text-slate-400">
                        <MapPin className="h-3.5 w-3.5 text-slate-400" />
                        <span>{res.branchAddress}</span>
                      </div>
                    </div>

                    <div className="flex flex-wrap items-center gap-x-4 gap-y-1 text-[11px] text-slate-400 dark:text-slate-500">
                      <span>{language === 'ar' ? 'تاريخ الحجز:' : 'Created:'} {formatDateTime(res.createdAt)}</span>
                      {res.status === 'Active' && (
                        <div className="flex items-center gap-1 font-semibold text-amber-600 dark:text-amber-400">
                          <Clock className="h-3 w-3" />
                          <span>{language === 'ar' ? 'ينتهي في:' : 'Expires:'} {formatDateTime(res.expiresAt)} ({formatRelativeTime(res.expiresAt)})</span>
                        </div>
                      )}
                      {res.confirmedAt && (
                        <span className="text-emerald-600 dark:text-emerald-400 font-medium">
                          {language === 'ar' ? 'تم الاستلام في:' : 'Picked up:'} {formatDateTime(res.confirmedAt)}
                        </span>
                      )}
                    </div>
                  </div>

                  {res.status === 'Active' && (
                    <div className="flex md:flex-col items-end justify-center pt-3 md:pt-0 border-t md:border-t-0 dark:border-slate-800">
                      <Button
                        variant="outline"
                        size="sm"
                        disabled={cancelMutation.isPending}
                        onClick={() => cancelMutation.mutate(res.id)}
                        className="text-red-600 dark:text-red-400 border-red-200 dark:border-red-900/60 hover:bg-red-50 dark:hover:bg-red-950/40 flex items-center gap-1.5"
                      >
                        <Ban className="h-3.5 w-3.5" />
                        {t('reservations.releaseHold')}
                      </Button>
                    </div>
                  )}
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
      )}
    </div>
  );
}
