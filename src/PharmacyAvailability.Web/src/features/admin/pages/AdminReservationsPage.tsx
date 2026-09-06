import { useState, useEffect } from 'react';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Clock } from 'lucide-react';
import { adminApi } from '../api/adminApi';
import { ReservationDto } from '@/types/reservations';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Input } from '@/components/ui/input';
import {
  Table,
  TableHeader,
  TableBody,
  TableRow,
  TableHead,
  TableCell,
} from '@/components/ui/table';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
  DialogFooter,
} from '@/components/ui/dialog';
import { LoadingSpinner } from '@/components/feedback/LoadingSpinner';
import { ErrorState } from '@/components/feedback/ErrorState';
import { EmptyState } from '@/components/feedback/EmptyState';
import { formatDateTime } from '@/lib/formatters';
import { ApiError } from '@/api/apiClient';
import {
  adminReleaseReservationSchema,
  AdminReleaseReservationFormData,
} from '@/lib/validations';
import { useTranslation } from '@/i18n/useTranslation';

export function AdminReservationsPage() {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const [statusFilter, setStatusFilter] = useState('');

  // Release Modal
  const [releaseItem, setReleaseItem] = useState<ReservationDto | null>(null);

  const { data, isLoading, isError, error, refetch } = useQuery({
    queryKey: ['admin-all-reservations', statusFilter],
    queryFn: () => adminApi.getAllReservations({ status: statusFilter || undefined, pageSize: 50 }),
  });

  return (
    <div className="space-y-5">
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
        <div>
          <h1 className="text-2xl font-bold text-slate-900 dark:text-slate-100">{t('admin.oversightTitle')}</h1>
          <p className="text-xs text-slate-500 dark:text-slate-400">
            {t('admin.oversightDesc')}
          </p>
        </div>

        <div className="flex items-center gap-3">
          <select
            value={statusFilter}
            onChange={(e) => setStatusFilter(e.target.value)}
            className="h-9 rounded-md border border-slate-300 dark:border-slate-700 bg-white dark:bg-slate-800 px-3 text-xs text-slate-800 dark:text-slate-200 focus:outline-none focus:ring-2 focus:ring-purple-500"
          >
            <option value="">{t('admin.allStatuses')}</option>
            <option value="Active">{t('reservations.activeHolds')}</option>
            <option value="Confirmed">{t('reservations.confirmedPickups')}</option>
            <option value="Expired">{t('reservations.expiredHolds')}</option>
            <option value="Released">{t('reservations.releasedHolds')}</option>
            <option value="Cancelled">{t('reservations.cancelledHolds')}</option>
          </select>

          <Button size="sm" variant="outline" onClick={() => refetch()} className="text-xs">
            {t('common.refresh')}
          </Button>
        </div>
      </div>

      {isLoading ? (
        <div className="py-16">
          <LoadingSpinner size="lg" text={t('common.loading')} />
        </div>
      ) : isError ? (
        <ErrorState error={error} onRetry={() => refetch()} />
      ) : !data || data.items.length === 0 ? (
        <EmptyState
          icon={Clock}
          title={t('reservations.emptyTitle')}
          description={t('reservations.emptyDesc')}
        />
      ) : (
        <div className="rounded-xl border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 shadow-xs overflow-hidden">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>{t('reservations.pickupCode')} / {t('pharmacy.customerName')}</TableHead>
                <TableHead>{t('reservations.medicine')}</TableHead>
                <TableHead>{t('reservations.pharmacy')}</TableHead>
                <TableHead className="text-center">{t('common.quantity')}</TableHead>
                <TableHead className="text-center">{t('common.status')}</TableHead>
                <TableHead>{t('common.time')}</TableHead>
                <TableHead className="text-end">{t('common.actions')}</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {data.items.map((res: ReservationDto) => (
                <TableRow key={res.id}>
                  <TableCell>
                    <div className="font-mono text-xs text-slate-500 dark:text-slate-400">{res.id.slice(0, 8)}...</div>
                    <div className="font-semibold text-slate-800 dark:text-slate-200 text-xs">{t('nav.roles.Customer')}: {res.customerId.slice(0, 8)}...</div>
                  </TableCell>

                  <TableCell>
                    <div className="font-bold text-slate-900 dark:text-slate-100">{res.medicineBrandName}</div>
                    <div className="text-xs text-slate-500 dark:text-slate-400">{res.medicineGenericName}</div>
                  </TableCell>

                  <TableCell className="text-xs text-slate-700 dark:text-slate-300">
                    <div className="font-medium">{res.pharmacyName}</div>
                    <div className="text-slate-400 dark:text-slate-500">{res.branchName}</div>
                  </TableCell>

                  <TableCell className="text-center">
                    <Badge variant="secondary" className="font-bold">
                      {res.quantity}
                    </Badge>
                  </TableCell>

                  <TableCell className="text-center">
                    <Badge
                      variant={
                        res.status === 'Active'
                          ? 'warning'
                          : res.status === 'Confirmed'
                          ? 'success'
                          : res.status === 'Released'
                          ? 'outline'
                          : 'secondary'
                      }
                    >
                      {res.status === 'Active'
                        ? t('reservations.activeHolds')
                        : res.status === 'Confirmed'
                        ? t('reservations.confirmedPickups')
                        : res.status === 'Released'
                        ? t('reservations.releasedHolds')
                        : res.status === 'Expired'
                        ? t('reservations.expiredHolds')
                        : t('reservations.cancelledHolds')}
                    </Badge>
                  </TableCell>

                  <TableCell className="text-xs text-slate-500 dark:text-slate-400">
                    <div>{t('reservations.createdAt')} {formatDateTime(res.createdAt)}</div>
                    <div className="text-[11px] text-slate-400 dark:text-slate-500">{t('reservations.expiresAt')} {formatDateTime(res.expiresAt)}</div>
                  </TableCell>

                  <TableCell className="text-end">
                    {res.status === 'Active' && (
                      <Button
                        size="sm"
                        variant="destructive"
                        className="h-7 px-2 text-xs"
                        onClick={() => setReleaseItem(res)}
                      >
                        {t('admin.forceReleaseBtn')}
                      </Button>
                    )}
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </div>
      )}

      {/* Administrative Release Modal */}
      <ReleaseReservationDialog
        item={releaseItem}
        onClose={() => setReleaseItem(null)}
        onSuccess={() => {
          queryClient.invalidateQueries({ queryKey: ['admin-all-reservations'] });
        }}
      />
    </div>
  );
}

interface ReleaseReservationDialogProps {
  item: ReservationDto | null;
  onClose: () => void;
  onSuccess: () => void;
}

function ReleaseReservationDialog({ item, onClose, onSuccess }: ReleaseReservationDialogProps) {
  const { t } = useTranslation();
  const [serverError, setServerError] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    reset,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<AdminReleaseReservationFormData>({
    resolver: zodResolver(adminReleaseReservationSchema),
    defaultValues: {
      reason: '',
    },
  });

  useEffect(() => {
    if (item) {
      reset({ reason: '' });
      setServerError(null);
    }
  }, [item, reset]);

  const onSubmit = async (values: AdminReleaseReservationFormData) => {
    if (!item) return;
    setServerError(null);

    try {
      await adminApi.releaseReservation(item.id, { reason: values.reason });
      onSuccess();
      onClose();
    } catch (err: unknown) {
      if (err instanceof ApiError) {
        if (err.isValidationError) {
          const fieldErrors = err.getAllFieldErrors();
          Object.entries(fieldErrors).forEach(([field, message]) => {
            const camelKey = (field.charAt(0).toLowerCase() + field.slice(1)) as keyof AdminReleaseReservationFormData;
            setError(camelKey, { message });
          });
        }
        setServerError(err.detail || err.title);
      } else if (err instanceof Error) {
        setServerError(err.message);
      } else {
        setServerError('Failed to release reservation.');
      }
    }
  };

  return (
    <Dialog open={!!item} onOpenChange={(open) => !open && onClose()}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle className="text-lg font-bold text-slate-900 dark:text-slate-100">{t('admin.releaseDialogTitle')}</DialogTitle>
          <DialogDescription className="text-xs text-slate-500 dark:text-slate-400">
            {t('admin.releaseDialogDesc')}
          </DialogDescription>
        </DialogHeader>

        {item && (
          <form onSubmit={handleSubmit(onSubmit)} className="space-y-4 py-2 text-xs">
            {serverError && (
              <div className="rounded-md bg-red-50 dark:bg-red-950/40 p-2 text-red-700 dark:text-red-300 border border-red-200 dark:border-red-900/50">
                {serverError}
              </div>
            )}

            <div className="rounded-lg bg-slate-50 dark:bg-slate-800/60 p-3 border border-slate-200 dark:border-slate-700 space-y-1">
              <p className="font-bold text-slate-900 dark:text-slate-100">{item.medicineBrandName}</p>
              <p className="text-slate-500 dark:text-slate-400">
                {t('common.address')}: {item.pharmacyName} — {item.branchName} • {t('common.quantity')}: {item.quantity}
              </p>
            </div>

            <div className="space-y-1">
              <label className="font-semibold text-slate-700 dark:text-slate-300 block">{t('admin.releaseReasonLabel')}</label>
              <Input
                {...register('reason')}
                placeholder={t('admin.releaseReasonPlaceholder')}
                className="dark:bg-slate-800 dark:border-slate-700 dark:text-slate-100"
              />
              {errors.reason && (
                <p className="text-xs text-red-600 dark:text-red-400 font-medium">{errors.reason.message}</p>
              )}
            </div>

            <DialogFooter className="pt-2">
              <Button type="button" variant="outline" onClick={onClose} disabled={isSubmitting}>{t('common.cancel')}</Button>
              <Button
                type="submit"
                variant="destructive"
                disabled={isSubmitting}
              >
                {isSubmitting ? t('admin.releasingText') : t('admin.confirmForceRelease')}
              </Button>
            </DialogFooter>
          </form>
        )}
      </DialogContent>
    </Dialog>
  );
}
