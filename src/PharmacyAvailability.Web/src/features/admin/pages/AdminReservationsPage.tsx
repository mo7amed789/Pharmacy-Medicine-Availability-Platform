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

export function AdminReservationsPage() {
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
                        onClick={() => setReleaseItem(res)}
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
          <DialogTitle className="text-lg font-bold text-slate-900">Administrative Reservation Release</DialogTitle>
          <DialogDescription className="text-xs text-slate-500">
            Forcibly release reserved stock back into available inventory with an audit log reason.
          </DialogDescription>
        </DialogHeader>

        {item && (
          <form onSubmit={handleSubmit(onSubmit)} className="space-y-4 py-2 text-xs">
            {serverError && (
              <div className="rounded-md bg-red-50 p-2 text-red-700 border border-red-200">
                {serverError}
              </div>
            )}

            <div className="rounded-lg bg-slate-50 p-3 border border-slate-200 space-y-1">
              <p className="font-bold text-slate-900">{item.medicineBrandName}</p>
              <p className="text-slate-500">
                Branch: {item.pharmacyName} — {item.branchName} • Units: {item.quantity}
              </p>
            </div>

            <div className="space-y-1">
              <label className="font-semibold text-slate-700 block">Release Audit Reason (Required)</label>
              <Input
                {...register('reason')}
                placeholder="e.g. Pharmacy branch closed unexpectedly / customer dispute"
              />
              {errors.reason && (
                <p className="text-xs text-red-600 font-medium">{errors.reason.message}</p>
              )}
            </div>

            <DialogFooter className="pt-2">
              <Button type="button" variant="outline" onClick={onClose} disabled={isSubmitting}>Cancel</Button>
              <Button
                type="submit"
                variant="destructive"
                disabled={isSubmitting}
              >
                {isSubmitting ? 'Releasing...' : 'Confirm Force Release'}
              </Button>
            </DialogFooter>
          </form>
        )}
      </DialogContent>
    </Dialog>
  );
}
