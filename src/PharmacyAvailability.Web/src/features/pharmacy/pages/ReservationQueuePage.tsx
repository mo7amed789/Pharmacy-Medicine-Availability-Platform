import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  Search,
  CheckCircle2,
  Clock,
  AlertTriangle,
  Phone,
  Filter,
} from 'lucide-react';
import { useAuthStore } from '@/app/store/authStore';
import { useBranchStore } from '@/app/store/branchStore';
import { pharmacyOpsApi } from '../api/pharmacyOpsApi';
import { StaffReservationQueueItemDto } from '@/types/operations';
import { Input } from '@/components/ui/input';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
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
import { formatCurrency, formatDateTime } from '@/lib/formatters';
import { useTranslation } from '@/i18n/useTranslation';

export function ReservationQueuePage() {
  const { t } = useTranslation();
  const { user } = useAuthStore();
  const { currentBranch } = useBranchStore();
  const queryClient = useQueryClient();

  const [search, setSearch] = useState('');
  const [status, setStatus] = useState<string>('Active');
  const [expiringSoonOnly, setExpiringSoonOnly] = useState(false);

  // Pickup Verification Modal state
  const [selectedQueueItem, setSelectedQueueItem] = useState<StaffReservationQueueItemDto | null>(null);
  const [isPickupDialogOpen, setIsPickupDialogOpen] = useState(false);
  const [pickupError, setPickupError] = useState<string | null>(null);

  const pharmacyId = user?.pharmacyId;
  const branchId = currentBranch?.id;

  const { data, isLoading, isError, error, refetch } = useQuery({
    queryKey: ['staff-queue', pharmacyId, branchId, search, status, expiringSoonOnly],
    queryFn: () =>
      pharmacyId && branchId
        ? pharmacyOpsApi.getReservationQueue(pharmacyId, branchId, {
            search: search || undefined,
            status: status || undefined,
            expiringSoonOnly: expiringSoonOnly || undefined,
            page: 1,
            pageSize: 50,
          })
        : Promise.reject('Missing pharmacy or branch'),
    enabled: !!pharmacyId && !!branchId,
    refetchInterval: 10000,
  });

  const confirmMutation = useMutation({
    mutationFn: (reservationId: string) => pharmacyOpsApi.confirmPickup(reservationId),
    onSuccess: () => {
      setIsPickupDialogOpen(false);
      setSelectedQueueItem(null);
      queryClient.invalidateQueries({ queryKey: ['staff-queue'] });
      queryClient.invalidateQueries({ queryKey: ['branch-dashboard'] });
    },
    onError: (err: any) => {
      setPickupError(err?.detail || err?.message || 'Failed to confirm pickup.');
    },
  });

  const handleOpenConfirm = (item: StaffReservationQueueItemDto) => {
    setSelectedQueueItem(item);
    setPickupError(null);
    setIsPickupDialogOpen(true);
  };

  const handleConfirmPickup = () => {
    if (selectedQueueItem) {
      confirmMutation.mutate(selectedQueueItem.reservationId);
    }
  };

  if (!currentBranch) {
    return (
      <div className="rounded-xl border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 p-8 text-center text-slate-500 dark:text-slate-400">
        {t('pharmacy.selectBranchPrompt')}
      </div>
    );
  }

  return (
    <div className="space-y-5">
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
        <div>
          <h1 className="text-2xl font-bold text-slate-900 dark:text-slate-100">{t('pharmacy.queueTitle')}</h1>
          <p className="text-xs text-slate-500 dark:text-slate-400">
            {t('pharmacy.queueDesc')}
          </p>
        </div>
        <Button size="sm" variant="outline" onClick={() => refetch()} className="text-xs">
          {t('common.refresh')}
        </Button>
      </div>

      {/* Filter and Search Bar */}
      <div className="flex flex-col md:flex-row items-center justify-between gap-3 rounded-xl border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 p-3 shadow-xs">
        <div className="relative w-full md:w-80">
          <Search className="absolute start-3 top-2.5 h-4 w-4 text-slate-400" />
          <Input
            placeholder={t('customer.searchPlaceholder')}
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            className="ps-9 h-9 text-xs dark:bg-slate-800 dark:border-slate-700"
          />
        </div>

        <div className="flex w-full md:w-auto items-center justify-between md:justify-end gap-3 text-xs">
          <div className="flex items-center gap-1.5">
            <Filter className="h-3.5 w-3.5 text-slate-400" />
            <select
              value={status}
              onChange={(e) => setStatus(e.target.value)}
              className="h-9 rounded-md border border-slate-300 dark:border-slate-700 bg-white dark:bg-slate-800 px-2.5 text-xs text-slate-800 dark:text-slate-200 focus:outline-none"
            >
              <option value="">{t('admin.allStatuses')}</option>
              <option value="Active">{t('reservations.activeHolds')}</option>
              <option value="Confirmed">{t('reservations.confirmedPickups')}</option>
              <option value="Expired">{t('reservations.expiredHolds')}</option>
              <option value="Cancelled">{t('reservations.cancelledHolds')}</option>
            </select>
          </div>

          <label className="flex items-center gap-1.5 text-slate-700 dark:text-slate-300 cursor-pointer font-medium">
            <input
              type="checkbox"
              checked={expiringSoonOnly}
              onChange={(e) => setExpiringSoonOnly(e.target.checked)}
              className="rounded text-amber-600 focus:ring-amber-500"
            />
            <span className="text-amber-700 dark:text-amber-400 font-semibold">{t('pharmacy.expiringSoonFilter')}</span>
          </label>
        </div>
      </div>

      {/* Queue Table */}
      {isLoading ? (
        <div className="py-16">
          <LoadingSpinner size="lg" text={t('common.loading')} />
        </div>
      ) : isError ? (
        <ErrorState error={error} onRetry={() => refetch()} />
      ) : !data || data.items.length === 0 ? (
        <EmptyState
          title={t('reservations.emptyTitle')}
          description={t('reservations.emptyDesc')}
        />
      ) : (
        <div className="rounded-xl border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 shadow-xs overflow-hidden">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>{t('pharmacy.customerName')}</TableHead>
                <TableHead>{t('reservations.medicine')}</TableHead>
                <TableHead className="text-center">{t('common.quantity')}</TableHead>
                <TableHead className="text-end">{t('pharmacy.amountDue')}</TableHead>
                <TableHead>{t('reservations.timeRemaining')}</TableHead>
                <TableHead className="text-end">{t('common.actions')}</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {data.items.map((item: StaffReservationQueueItemDto) => (
                <TableRow key={item.reservationId} className={item.isExpiringSoon ? 'bg-amber-50/40 dark:bg-amber-950/20' : undefined}>
                  <TableCell>
                    <div className="font-semibold text-slate-900 dark:text-slate-100">{item.customerName}</div>
                    <div className="flex items-center gap-1 text-xs text-slate-500 dark:text-slate-400">
                      <Phone className="h-3 w-3" />
                      <span>{item.customerPhone || '-'}</span>
                    </div>
                  </TableCell>

                  <TableCell>
                    <div className="font-semibold text-slate-900 dark:text-slate-100">{item.medicineBrandName}</div>
                    <div className="text-xs text-slate-500 dark:text-slate-400">
                      {item.medicineGenericName} • {item.strength} • {item.dosageForm}
                    </div>
                  </TableCell>

                  <TableCell className="text-center">
                    <Badge variant="secondary" className="font-bold text-xs">
                      {item.quantity}
                    </Badge>
                  </TableCell>

                  <TableCell className="text-end">
                    <div className="font-bold text-slate-900 dark:text-slate-100">
                      {formatCurrency(item.totalAmountDue)}
                    </div>
                    <div className="text-[11px] text-slate-400">
                      @{formatCurrency(item.unitPrice)}
                    </div>
                  </TableCell>

                  <TableCell>
                    {item.status === 'Active' ? (
                      <div className="space-y-1">
                        {item.isExpiringSoon ? (
                          <Badge variant="warning" className="text-[11px] flex items-center gap-1 w-fit">
                            <AlertTriangle className="h-3 w-3" />
                            {item.remainingMinutes}m
                          </Badge>
                        ) : (
                          <Badge variant="outline" className="text-[11px] text-slate-600 dark:text-slate-300 w-fit">
                            <Clock className="h-3 w-3 me-1" />
                            {item.remainingMinutes}m
                          </Badge>
                        )}
                        <div className="text-[10px] text-slate-400">
                          {t('reservations.expiresAt')} {formatDateTime(item.expiresAt)}
                        </div>
                      </div>
                    ) : (
                      <Badge variant="secondary">{item.status}</Badge>
                    )}
                  </TableCell>

                  <TableCell className="text-end">
                    {item.status === 'Active' ? (
                      <Button
                        size="sm"
                        onClick={() => handleOpenConfirm(item)}
                        className="bg-emerald-600 hover:bg-emerald-700 text-white flex items-center gap-1 text-xs font-semibold shadow-xs"
                      >
                        <CheckCircle2 className="h-3.5 w-3.5" />
                        {t('pharmacy.confirmPickup')}
                      </Button>
                    ) : (
                      <span className="text-xs text-slate-400 font-medium">{t('common.completed')}</span>
                    )}
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </div>
      )}

      {/* Pickup Verification & Handover Modal */}
      <Dialog open={isPickupDialogOpen} onOpenChange={setIsPickupDialogOpen}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle className="text-lg font-bold text-slate-900 dark:text-slate-100">{t('pharmacy.pickupModalTitle')}</DialogTitle>
            <DialogDescription className="text-slate-500 dark:text-slate-400 text-xs">
              {t('pharmacy.pickupModalDesc')}
            </DialogDescription>
          </DialogHeader>

          {selectedQueueItem && (
            <div className="space-y-4 py-2">
              {pickupError && (
                <div className="rounded-md bg-red-50 dark:bg-red-950/40 p-3 text-xs text-red-700 dark:text-red-300 border border-red-200 dark:border-red-900/50">
                  {pickupError}
                </div>
              )}

              {/* Customer and Order Summary */}
              <div className="rounded-lg bg-slate-50 dark:bg-slate-800/60 p-4 border border-slate-200 dark:border-slate-700 space-y-2 text-sm">
                <div className="flex justify-between">
                  <span className="text-slate-500 dark:text-slate-400">{t('pharmacy.customerName')}:</span>
                  <span className="font-semibold text-slate-900 dark:text-slate-100">{selectedQueueItem.customerName}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-slate-500 dark:text-slate-400">{t('common.phone')}:</span>
                  <span className="font-semibold text-slate-900 dark:text-slate-100">{selectedQueueItem.customerPhone || '-'}</span>
                </div>
                <div className="border-t border-slate-200 dark:border-slate-700 pt-2 flex justify-between">
                  <span className="text-slate-500 dark:text-slate-400">{t('reservations.medicine')}:</span>
                  <span className="font-semibold text-slate-900 dark:text-slate-100">{selectedQueueItem.medicineBrandName} ({selectedQueueItem.strength})</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-slate-500 dark:text-slate-400">{t('pharmacy.handoverQty')}:</span>
                  <span className="font-bold text-slate-900 dark:text-slate-100">{selectedQueueItem.quantity} {t('common.units')}</span>
                </div>
              </div>

              {/* Total Amount Due */}
              <div className="rounded-lg bg-blue-50/70 dark:bg-blue-950/40 p-4 border border-blue-200 dark:border-blue-800/50 flex items-center justify-between">
                <div>
                  <span className="text-xs font-semibold uppercase tracking-wider text-blue-900 dark:text-blue-300">{t('pharmacy.totalDue')}</span>
                  <p className="text-[11px] text-blue-600 dark:text-blue-400">{t('pharmacy.collectPayment')}</p>
                </div>
                <div className="text-2xl font-extrabold text-blue-800 dark:text-blue-200">
                  {formatCurrency(selectedQueueItem.totalAmountDue)}
                </div>
              </div>
            </div>
          )}

          <DialogFooter className="gap-2 sm:gap-0">
            <Button variant="outline" onClick={() => setIsPickupDialogOpen(false)} disabled={confirmMutation.isPending}>
              {t('common.cancel')}
            </Button>
            <Button
              onClick={handleConfirmPickup}
              disabled={confirmMutation.isPending}
              className="bg-emerald-600 hover:bg-emerald-700 text-white"
            >
              {confirmMutation.isPending ? t('common.loading') : t('pharmacy.handoverFinalize')}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
