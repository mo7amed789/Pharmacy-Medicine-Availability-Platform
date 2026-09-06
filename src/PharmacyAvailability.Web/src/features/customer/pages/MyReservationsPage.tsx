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

export function MyReservationsPage() {
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
        return <Badge variant="warning">Active Hold</Badge>;
      case 'Confirmed':
        return <Badge variant="success">Picked Up</Badge>;
      case 'Cancelled':
        return <Badge variant="secondary">Cancelled</Badge>;
      case 'Expired':
        return <Badge variant="destructive">Expired</Badge>;
      case 'Released':
        return <Badge variant="outline">Admin Released</Badge>;
      default:
        return <Badge variant="secondary">{status}</Badge>;
    }
  };

  return (
    <div className="mx-auto max-w-7xl px-4 py-8 sm:px-6 lg:px-8">
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 mb-6">
        <div>
          <h1 className="text-2xl font-bold tracking-tight text-slate-900">My Medicine Reservations</h1>
          <p className="text-sm text-slate-500">Track and manage your active pharmacy hold pickups.</p>
        </div>

        {/* Filter */}
        <select
          value={statusFilter}
          onChange={(e) => setStatusFilter(e.target.value)}
          className="rounded-md border border-slate-300 bg-white px-3 py-1.5 text-xs font-medium text-slate-700 shadow-xs focus:outline-none focus:ring-2 focus:ring-blue-500"
        >
          <option value="">All Statuses</option>
          <option value="Active">Active Holds</option>
          <option value="Confirmed">Confirmed Pickups</option>
          <option value="Cancelled">Cancelled</option>
          <option value="Expired">Expired</option>
        </select>
      </div>

      {isLoading ? (
        <div className="py-16">
          <LoadingSpinner size="lg" text="Loading your reservations..." />
        </div>
      ) : isError ? (
        <ErrorState error={error} onRetry={() => refetch()} />
      ) : !data || data.items.length === 0 ? (
        <EmptyState
          title="No Reservations Found"
          description="You don't have any medicine reservations matching your filter. Search for medicines to place a hold."
        />
      ) : (
        <div className="space-y-4">
          {data.items.map((res: ReservationDto) => (
            <Card key={res.id} className="border-slate-200">
              <CardContent className="p-5">
                <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
                  <div className="space-y-2">
                    <div className="flex items-center gap-2">
                      <h3 className="text-lg font-bold text-slate-900">{res.medicineBrandName}</h3>
                      {getStatusBadge(res.status)}
                      <span className="text-xs font-bold text-slate-500 bg-slate-100 px-2 py-0.5 rounded">
                        Qty: {res.quantity}
                      </span>
                    </div>

                    <p className="text-xs text-slate-500">{res.medicineGenericName}</p>

                    <div className="flex flex-wrap items-center gap-x-4 gap-y-1 text-xs text-slate-600">
                      <div className="flex items-center gap-1 font-semibold text-slate-800">
                        <Building2 className="h-3.5 w-3.5 text-slate-500" />
                        <span>{res.pharmacyName} — {res.branchName}</span>
                      </div>
                      <div className="flex items-center gap-1 text-slate-500">
                        <MapPin className="h-3.5 w-3.5 text-slate-400" />
                        <span>{res.branchAddress}</span>
                      </div>
                    </div>

                    <div className="flex flex-wrap items-center gap-x-4 gap-y-1 text-[11px] text-slate-400">
                      <span>Created: {formatDateTime(res.createdAt)}</span>
                      {res.status === 'Active' && (
                        <div className="flex items-center gap-1 font-semibold text-amber-600">
                          <Clock className="h-3 w-3" />
                          <span>Expires: {formatDateTime(res.expiresAt)} ({formatRelativeTime(res.expiresAt)})</span>
                        </div>
                      )}
                      {res.confirmedAt && (
                        <span className="text-emerald-600 font-medium">
                          Picked up: {formatDateTime(res.confirmedAt)}
                        </span>
                      )}
                    </div>
                  </div>

                  {res.status === 'Active' && (
                    <div className="flex md:flex-col items-end justify-center pt-3 md:pt-0 border-t md:border-t-0">
                      <Button
                        variant="outline"
                        size="sm"
                        disabled={cancelMutation.isPending}
                        onClick={() => cancelMutation.mutate(res.id)}
                        className="text-red-600 border-red-200 hover:bg-red-50 flex items-center gap-1.5"
                      >
                        <Ban className="h-3.5 w-3.5" />
                        Cancel Hold
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
