import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Building2, CheckCircle2, PauseCircle, PlayCircle, XCircle, MapPin } from 'lucide-react';
import { adminApi } from '../api/adminApi';
import { PharmacyDto, PharmacyStatus } from '@/types/pharmacies';
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
import { LoadingSpinner } from '@/components/feedback/LoadingSpinner';
import { ErrorState } from '@/components/feedback/ErrorState';
import { EmptyState } from '@/components/feedback/EmptyState';

export function PharmaciesApprovalPage() {
  const queryClient = useQueryClient();
  const [statusFilter, setStatusFilter] = useState<PharmacyStatus | ''>('');

  const { data: pharmacies, isLoading, isError, error, refetch } = useQuery({
    queryKey: ['admin-pharmacies-list', statusFilter],
    queryFn: () => adminApi.getAllPharmacies(statusFilter ? statusFilter : undefined),
  });

  const approveMutation = useMutation({
    mutationFn: (id: string) => adminApi.approvePharmacy(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['admin-pharmacies-list'] }),
  });

  const suspendMutation = useMutation({
    mutationFn: (id: string) => adminApi.suspendPharmacy(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['admin-pharmacies-list'] }),
  });

  const reactivateMutation = useMutation({
    mutationFn: (id: string) => adminApi.reactivatePharmacy(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['admin-pharmacies-list'] }),
  });

  const deactivateMutation = useMutation({
    mutationFn: (id: string) => adminApi.deactivatePharmacy(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['admin-pharmacies-list'] }),
  });

  const getStatusBadge = (status: PharmacyStatus) => {
    switch (status) {
      case 'Pending':
        return <Badge variant="warning">Pending Review</Badge>;
      case 'Approved':
        return <Badge variant="success">Approved</Badge>;
      case 'Suspended':
        return <Badge variant="destructive">Suspended</Badge>;
      case 'Deactivated':
        return <Badge variant="secondary">Deactivated</Badge>;
      default:
        return <Badge>{status}</Badge>;
    }
  };

  return (
    <div className="space-y-5">
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
        <div>
          <h1 className="text-2xl font-bold text-slate-900">Pharmacy Onboarding & Approvals</h1>
          <p className="text-xs text-slate-500">
            Review partner pharmacy registrations, verify regulatory licenses, and manage account statuses.
          </p>
        </div>

        <select
          value={statusFilter}
          onChange={(e) => setStatusFilter(e.target.value as PharmacyStatus | '')}
          className="h-9 rounded-md border border-slate-300 bg-white px-3 text-xs text-slate-800"
        >
          <option value="">All Statuses</option>
          <option value="Pending">Pending Review</option>
          <option value="Approved">Approved</option>
          <option value="Suspended">Suspended</option>
          <option value="Deactivated">Deactivated</option>
        </select>
      </div>

      {isLoading ? (
        <div className="py-16">
          <LoadingSpinner size="lg" text="Loading pharmacy applications..." />
        </div>
      ) : isError ? (
        <ErrorState error={error} onRetry={() => refetch()} />
      ) : !pharmacies || pharmacies.length === 0 ? (
        <EmptyState
          icon={Building2}
          title="No Pharmacies Found"
          description="There are currently no pharmacy records matching your status filter."
        />
      ) : (
        <div className="rounded-xl border border-slate-200 bg-white shadow-xs overflow-hidden">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Pharmacy</TableHead>
                <TableHead>License & City</TableHead>
                <TableHead>Contact</TableHead>
                <TableHead className="text-center">Branches</TableHead>
                <TableHead className="text-center">Status</TableHead>
                <TableHead className="text-right">Lifecycle Actions</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {pharmacies.map((pharmacy: PharmacyDto) => (
                <TableRow key={pharmacy.id}>
                  <TableCell>
                    <div className="font-bold text-slate-900">{pharmacy.name}</div>
                    <div className="flex items-center gap-1 text-xs text-slate-500">
                      <MapPin className="h-3 w-3" />
                      <span>{pharmacy.address}</span>
                    </div>
                  </TableCell>

                  <TableCell className="text-xs text-slate-600">
                    <div className="font-mono font-semibold text-slate-800">{pharmacy.licenseNumber}</div>
                    <div className="text-slate-500">{pharmacy.city}</div>
                  </TableCell>

                  <TableCell className="text-xs text-slate-600">
                    <div>{pharmacy.phone}</div>
                    {pharmacy.email && <div className="text-slate-400">{pharmacy.email}</div>}
                  </TableCell>

                  <TableCell className="text-center text-xs font-semibold text-slate-700">
                    {pharmacy.branchesCount}
                  </TableCell>

                  <TableCell className="text-center">
                    {getStatusBadge(pharmacy.status)}
                  </TableCell>

                  <TableCell className="text-right space-x-1">
                    {pharmacy.status === 'Pending' && (
                      <Button
                        size="sm"
                        disabled={approveMutation.isPending}
                        onClick={() => approveMutation.mutate(pharmacy.id)}
                        className="bg-emerald-600 hover:bg-emerald-700 text-white h-7 px-2.5 text-xs"
                      >
                        <CheckCircle2 className="h-3 w-3 mr-1" />
                        Approve
                      </Button>
                    )}

                    {pharmacy.status === 'Approved' && (
                      <Button
                        size="sm"
                        variant="outline"
                        disabled={suspendMutation.isPending}
                        onClick={() => suspendMutation.mutate(pharmacy.id)}
                        className="text-amber-700 border-amber-300 hover:bg-amber-50 h-7 px-2.5 text-xs"
                      >
                        <PauseCircle className="h-3 w-3 mr-1" />
                        Suspend
                      </Button>
                    )}

                    {pharmacy.status === 'Suspended' && (
                      <Button
                        size="sm"
                        variant="outline"
                        disabled={reactivateMutation.isPending}
                        onClick={() => reactivateMutation.mutate(pharmacy.id)}
                        className="text-blue-700 border-blue-300 hover:bg-blue-50 h-7 px-2.5 text-xs"
                      >
                        <PlayCircle className="h-3 w-3 mr-1" />
                        Reactivate
                      </Button>
                    )}

                    {pharmacy.status !== 'Deactivated' && (
                      <Button
                        size="sm"
                        variant="ghost"
                        disabled={deactivateMutation.isPending}
                        onClick={() => deactivateMutation.mutate(pharmacy.id)}
                        className="text-red-600 hover:bg-red-50 h-7 px-2 text-xs"
                        title="Deactivate"
                      >
                        <XCircle className="h-3.5 w-3.5" />
                      </Button>
                    )}
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </div>
      )}
    </div>
  );
}
