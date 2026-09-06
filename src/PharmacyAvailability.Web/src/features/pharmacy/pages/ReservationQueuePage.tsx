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

export function ReservationQueuePage() {
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
    refetchInterval: 10000, // Poll queue every 10 seconds
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
      <div className="rounded-xl border border-slate-200 bg-white p-8 text-center text-slate-500">
        Please select a branch to view the pickup fulfillment queue.
      </div>
    );
  }

  return (
    <div className="space-y-5">
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
        <div>
          <h1 className="text-2xl font-bold text-slate-900">Pickup Fulfillment Queue</h1>
          <p className="text-xs text-slate-500">
            Verify customer identity, collect payment, and confirm medicine handover.
          </p>
        </div>
        <Button size="sm" variant="outline" onClick={() => refetch()} className="text-xs">
          Refresh Queue
        </Button>
      </div>

      {/* Filter and Search Bar */}
      <div className="flex flex-col md:flex-row items-center justify-between gap-3 rounded-xl border border-slate-200 bg-white p-3 shadow-xs">
        <div className="relative w-full md:w-80">
          <Search className="absolute left-3 top-2.5 h-4 w-4 text-slate-400" />
          <Input
            placeholder="Search customer name, phone, medicine..."
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            className="pl-9 h-9 text-xs"
          />
        </div>

        <div className="flex w-full md:w-auto items-center justify-between md:justify-end gap-3 text-xs">
          <div className="flex items-center gap-1.5">
            <Filter className="h-3.5 w-3.5 text-slate-400" />
            <select
              value={status}
              onChange={(e) => setStatus(e.target.value)}
              className="h-9 rounded-md border border-slate-300 bg-white px-2.5 text-xs text-slate-800 focus:outline-none"
            >
              <option value="">All Statuses</option>
              <option value="Active">Active Holds</option>
              <option value="Confirmed">Confirmed Pickups</option>
              <option value="Expired">Expired</option>
              <option value="Cancelled">Cancelled</option>
            </select>
          </div>

          <label className="flex items-center gap-1.5 text-slate-700 cursor-pointer font-medium">
            <input
              type="checkbox"
              checked={expiringSoonOnly}
              onChange={(e) => setExpiringSoonOnly(e.target.checked)}
              className="rounded text-amber-600 focus:ring-amber-500"
            />
            <span className="text-amber-700 font-semibold">Expiring Soon Only (&lt;30m)</span>
          </label>
        </div>
      </div>

      {/* Queue Table */}
      {isLoading ? (
        <div className="py-16">
          <LoadingSpinner size="lg" text="Loading fulfillment queue..." />
        </div>
      ) : isError ? (
        <ErrorState error={error} onRetry={() => refetch()} />
      ) : !data || data.items.length === 0 ? (
        <EmptyState
          title="Fulfillment Queue is Empty"
          description="There are currently no customer pickup reservations matching your filter."
        />
      ) : (
        <div className="rounded-xl border border-slate-200 bg-white shadow-xs overflow-hidden">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Customer</TableHead>
                <TableHead>Medicine Details</TableHead>
                <TableHead className="text-center">Qty</TableHead>
                <TableHead className="text-right">Amount Due</TableHead>
                <TableHead>Urgency / Time Remaining</TableHead>
                <TableHead className="text-right">Action</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {data.items.map((item: StaffReservationQueueItemDto) => (
                <TableRow key={item.reservationId} className={item.isExpiringSoon ? 'bg-amber-50/40' : undefined}>
                  <TableCell>
                    <div className="font-semibold text-slate-900">{item.customerName}</div>
                    <div className="flex items-center gap-1 text-xs text-slate-500">
                      <Phone className="h-3 w-3" />
                      <span>{item.customerPhone || 'No phone'}</span>
                    </div>
                  </TableCell>

                  <TableCell>
                    <div className="font-semibold text-slate-900">{item.medicineBrandName}</div>
                    <div className="text-xs text-slate-500">
                      {item.medicineGenericName} • {item.strength} • {item.dosageForm}
                    </div>
                  </TableCell>

                  <TableCell className="text-center">
                    <Badge variant="secondary" className="font-bold text-xs">
                      {item.quantity}
                    </Badge>
                  </TableCell>

                  <TableCell className="text-right">
                    <div className="font-bold text-slate-900">
                      {formatCurrency(item.totalAmountDue)}
                    </div>
                    <div className="text-[11px] text-slate-400">
                      @{formatCurrency(item.unitPrice)}/ea
                    </div>
                  </TableCell>

                  <TableCell>
                    {item.status === 'Active' ? (
                      <div className="space-y-1">
                        {item.isExpiringSoon ? (
                          <Badge variant="warning" className="text-[11px] flex items-center gap-1 w-fit">
                            <AlertTriangle className="h-3 w-3" />
                            Expiring in {item.remainingMinutes}m
                          </Badge>
                        ) : (
                          <Badge variant="outline" className="text-[11px] text-slate-600 w-fit">
                            <Clock className="h-3 w-3 mr-1" />
                            {item.remainingMinutes}m remaining
                          </Badge>
                        )}
                        <div className="text-[10px] text-slate-400">
                          Expires: {formatDateTime(item.expiresAt)}
                        </div>
                      </div>
                    ) : (
                      <Badge variant="secondary">{item.status}</Badge>
                    )}
                  </TableCell>

                  <TableCell className="text-right">
                    {item.status === 'Active' ? (
                      <Button
                        size="sm"
                        onClick={() => handleOpenConfirm(item)}
                        className="bg-emerald-600 hover:bg-emerald-700 text-white flex items-center gap-1 text-xs font-semibold shadow-xs"
                      >
                        <CheckCircle2 className="h-3.5 w-3.5" />
                        Confirm Pickup
                      </Button>
                    ) : (
                      <span className="text-xs text-slate-400 font-medium">Completed</span>
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
            <DialogTitle className="text-lg font-bold text-slate-900">Confirm Customer Pickup</DialogTitle>
            <DialogDescription className="text-slate-500 text-xs">
              Collect payment and finalize the inventory consumption for this hold.
            </DialogDescription>
          </DialogHeader>

          {selectedQueueItem && (
            <div className="space-y-4 py-2">
              {pickupError && (
                <div className="rounded-md bg-red-50 p-3 text-xs text-red-700 border border-red-200">
                  {pickupError}
                </div>
              )}

              {/* Customer and Order Summary */}
              <div className="rounded-lg bg-slate-50 p-4 border border-slate-200 space-y-2 text-sm">
                <div className="flex justify-between">
                  <span className="text-slate-500">Customer Name:</span>
                  <span className="font-semibold text-slate-900">{selectedQueueItem.customerName}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-slate-500">Phone:</span>
                  <span className="font-semibold text-slate-900">{selectedQueueItem.customerPhone || 'N/A'}</span>
                </div>
                <div className="border-t border-slate-200 pt-2 flex justify-between">
                  <span className="text-slate-500">Medicine:</span>
                  <span className="font-semibold text-slate-900">{selectedQueueItem.medicineBrandName} ({selectedQueueItem.strength})</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-slate-500">Handover Quantity:</span>
                  <span className="font-bold text-slate-900">{selectedQueueItem.quantity} unit(s)</span>
                </div>
              </div>

              {/* Total Amount Due */}
              <div className="rounded-lg bg-blue-50/70 p-4 border border-blue-200 flex items-center justify-between">
                <div>
                  <span className="text-xs font-semibold uppercase tracking-wider text-blue-900">Total Amount Due</span>
                  <p className="text-[11px] text-blue-600">Collect payment at POS</p>
                </div>
                <div className="text-2xl font-extrabold text-blue-800">
                  {formatCurrency(selectedQueueItem.totalAmountDue)}
                </div>
              </div>
            </div>
          )}

          <DialogFooter className="gap-2 sm:gap-0">
            <Button variant="outline" onClick={() => setIsPickupDialogOpen(false)} disabled={confirmMutation.isPending}>
              Cancel
            </Button>
            <Button
              onClick={handleConfirmPickup}
              disabled={confirmMutation.isPending}
              className="bg-emerald-600 hover:bg-emerald-700 text-white"
            >
              {confirmMutation.isPending ? 'Confirming...' : 'Handover & Finalize'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
