import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
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

export function AdminReservationsPage() {
  const queryClient = useQueryClient();
  const [statusFilter, setStatusFilter] = useState('');

  // Release Modal
  const [releaseItem, setReleaseItem] = useState<ReservationDto | null>(null);
  const [releaseReason, setReleaseReason] = useState('');
  const [releaseError, setReleaseError] = useState<string | null>(null);

  const { data, isLoading, isError, error, refetch } = useQuery({
    queryKey: ['admin-all-reservations', statusFilter],
    queryFn: () => adminApi.getAllReservations({ status: statusFilter || undefined, pageSize: 50 }),
  });

  const releaseMutation = useMutation({
    mutationFn: () => {
      if (!releaseItem) return Promise.reject('No reservation selected');
      return adminApi.releaseReservation(releaseItem.id, { reason: releaseReason });
    },
    onSuccess: () => {
      setReleaseItem(null);
      setReleaseReason('');
      queryClient.invalidateQueries({ queryKey: ['admin-all-reservations'] });
    },
    onError: (err: any) => {
      setReleaseError(err?.detail || err?.message || 'Failed to release reservation.');
    },
  });

  return (
    <div className="space-y-5">
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
        <div>
          <h1 className="text-2xl font-bold text-slate-900">System Reservations Oversight</h1>
          <p className="text-xs text-slate-500">
            Platform-wide audit of medicine hold reservations and emergency administrative releases.
          </p>
        </div>

        <div className="flex items-center gap-3">
          <select
            value={statusFilter}
            onChange={(e) => setStatusFilter(e.target.value)}
            className="h-9 rounded-md border border-slate-300 bg-white px-3 text-xs text-slate-800"
          >
            <option value="">All Statuses</option>
            <option value="Active">Active Holds</option>
            <option value="Confirmed">Confirmed Pickups</option>
            <option value="Expired">Expired</option>
            <option value="Released">Admin Released</option>
            <option value="Cancelled">Cancelled</option>
          </select>

          <Button size="sm" variant="outline" onClick={() => refetch()} className="text-xs">
            Refresh
          </Button>
        </div>
      </div>

      {isLoading ? (
        <div className="py-16">
          <LoadingSpinner size="lg" text="Auditing system reservations..." />
        </div>
      ) : isError ? (
        <ErrorState error={error} onRetry={() => refetch()} />
      ) : !data || data.items.length === 0 ? (
        <EmptyState
          icon={Clock}
          title="No Reservations Recorded"
          description="There are currently no platform reservations matching your criteria."
        />
      ) : (
        <div className="rounded-xl border border-slate-200 bg-white shadow-xs overflow-hidden">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Reservation / Customer</TableHead>
                <TableHead>Medicine</TableHead>
                <TableHead>Branch</TableHead>
                <TableHead className="text-center">Units</TableHead>
                <TableHead className="text-center">Status</TableHead>
                <TableHead>Timing</TableHead>
                <TableHead className="text-right">Action</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {data.items.map((res: ReservationDto) => (
                <TableRow key={res.id}>
                  <TableCell>
                    <div className="font-mono text-xs text-slate-500">{res.id.slice(0, 8)}...</div>
                    <div className="font-semibold text-slate-800 text-xs">Customer: {res.customerId.slice(0, 8)}...</div>
                  </TableCell>

                  <TableCell>
                    <div className="font-bold text-slate-900">{res.medicineBrandName}</div>
                    <div className="text-xs text-slate-500">{res.medicineGenericName}</div>
                  </TableCell>

                  <TableCell className="text-xs text-slate-700">
                    <div className="font-medium">{res.pharmacyName}</div>
                    <div className="text-slate-400">{res.branchName}</div>
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
                      {res.status}
                    </Badge>
                  </TableCell>

                  <TableCell className="text-xs text-slate-500">
                    <div>Created: {formatDateTime(res.createdAt)}</div>
                    <div className="text-[11px] text-slate-400">Expires: {formatDateTime(res.expiresAt)}</div>
                  </TableCell>

                  <TableCell className="text-right">
                    {res.status === 'Active' && (
                      <Button
                        size="sm"
                        variant="destructive"
                        className="h-7 px-2 text-xs"
                        onClick={() => {
                          setReleaseItem(res);
                          setReleaseReason('');
                          setReleaseError(null);
                        }}
                      >
                        Force Release
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
      <Dialog open={!!releaseItem} onOpenChange={() => setReleaseItem(null)}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle className="text-lg font-bold text-slate-900">Administrative Reservation Release</DialogTitle>
            <DialogDescription className="text-xs text-slate-500">
              Forcibly release reserved stock back into available inventory with an audit log reason.
            </DialogDescription>
          </DialogHeader>

          {releaseItem && (
            <div className="space-y-4 py-2 text-xs">
              {releaseError && (
                <div className="rounded-md bg-red-50 p-2 text-red-700 border border-red-200">
                  {releaseError}
                </div>
              )}

              <div className="rounded-lg bg-slate-50 p-3 border border-slate-200 space-y-1">
                <p className="font-bold text-slate-900">{releaseItem.medicineBrandName}</p>
                <p className="text-slate-500">
                  Branch: {releaseItem.pharmacyName} — {releaseItem.branchName} • Units: {releaseItem.quantity}
                </p>
              </div>

              <div className="space-y-1.5">
                <label className="font-semibold text-slate-700 block">Release Audit Reason (Required)</label>
                <Input
                  value={releaseReason}
                  onChange={(e) => setReleaseReason(e.target.value)}
                  placeholder="e.g. Pharmacy branch closed unexpectedly / customer dispute"
                  required
                />
              </div>
            </div>
          )}

          <DialogFooter>
            <Button variant="outline" onClick={() => setReleaseItem(null)}>Cancel</Button>
            <Button
              variant="destructive"
              disabled={!releaseReason.trim() || releaseMutation.isPending}
              onClick={() => releaseMutation.mutate()}
            >
              {releaseMutation.isPending ? 'Releasing...' : 'Confirm Force Release'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
