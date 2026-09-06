import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { AlertTriangle, Boxes, Filter } from 'lucide-react';
import { useAuthStore } from '@/app/store/authStore';
import { useBranchStore } from '@/app/store/branchStore';
import { pharmacyOpsApi } from '../api/pharmacyOpsApi';
import { LowStockAlertDto } from '@/types/operations';
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
import { formatCurrency, formatDateTime } from '@/lib/formatters';

export function LowStockPage() {
  const { user } = useAuthStore();
  const { currentBranch } = useBranchStore();
  const [threshold, setThreshold] = useState<number>(5);

  const pharmacyId = user?.pharmacyId;
  const branchId = currentBranch?.id;

  const { data, isLoading, isError, error, refetch } = useQuery({
    queryKey: ['low-stock-alerts', pharmacyId, branchId, threshold],
    queryFn: () =>
      pharmacyId && branchId
        ? pharmacyOpsApi.getLowStockAlerts(pharmacyId, branchId, threshold, 1, 50)
        : Promise.reject('Missing branch'),
    enabled: !!pharmacyId && !!branchId,
  });

  if (!currentBranch) {
    return (
      <div className="rounded-xl border border-slate-200 bg-white p-8 text-center text-slate-500">
        Please select a branch to view low stock alerts.
      </div>
    );
  }

  return (
    <div className="space-y-5">
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
        <div>
          <h1 className="text-2xl font-bold text-slate-900">Low Stock Inventory Alerts</h1>
          <p className="text-xs text-slate-500">
            Early warning detection for medicines reaching stock depletion thresholds.
          </p>
        </div>

        {/* Threshold filter */}
        <div className="flex items-center gap-2 text-xs">
          <Filter className="h-3.5 w-3.5 text-slate-400" />
          <span className="font-semibold text-slate-600">Threshold:</span>
          <select
            value={threshold}
            onChange={(e) => setThreshold(Number(e.target.value))}
            className="h-8 rounded-md border border-slate-300 bg-white px-2.5 text-xs text-slate-800"
          >
            <option value="3">&le; 3 units</option>
            <option value="5">&le; 5 units (Standard)</option>
            <option value="10">&le; 10 units</option>
            <option value="20">&le; 20 units</option>
          </select>
        </div>
      </div>

      {isLoading ? (
        <div className="py-16">
          <LoadingSpinner size="lg" text="Analyzing inventory levels..." />
        </div>
      ) : isError ? (
        <ErrorState error={error} onRetry={() => refetch()} />
      ) : !data || data.items.length === 0 ? (
        <EmptyState
          icon={Boxes}
          title="Stock Levels Healthy"
          description={`No medicines at or below ${threshold} units. All monitored inventory items are safely stocked.`}
        />
      ) : (
        <div className="rounded-xl border border-slate-200 bg-white shadow-xs overflow-hidden">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Medicine</TableHead>
                <TableHead>Category / Manufacturer</TableHead>
                <TableHead className="text-center">Available Stock</TableHead>
                <TableHead className="text-center">Reserved Units</TableHead>
                <TableHead className="text-center">Physical Total</TableHead>
                <TableHead className="text-right">Unit Price</TableHead>
                <TableHead className="text-right">Last Updated</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {data.items.map((item: LowStockAlertDto) => (
                <TableRow key={item.inventoryId} className="bg-red-50/25">
                  <TableCell>
                    <div className="flex items-center gap-2">
                      <AlertTriangle className="h-4 w-4 text-red-500 shrink-0" />
                      <div>
                        <span className="font-bold text-slate-900">{item.medicineBrandName}</span>
                        <p className="text-xs text-slate-500">{item.medicineGenericName}</p>
                      </div>
                    </div>
                  </TableCell>

                  <TableCell className="text-xs text-slate-600">
                    <div>{item.categoryName}</div>
                    <div className="text-[11px] text-slate-400">{item.manufacturerName}</div>
                  </TableCell>

                  <TableCell className="text-center">
                    <Badge variant={item.availableQuantity === 0 ? 'destructive' : 'warning'}>
                      {item.availableQuantity === 0 ? 'Depleted (0)' : `${item.availableQuantity} remaining`}
                    </Badge>
                  </TableCell>

                  <TableCell className="text-center text-xs font-semibold text-slate-700">
                    {item.reservedQuantity}
                  </TableCell>

                  <TableCell className="text-center text-xs font-bold text-slate-900">
                    {item.totalPhysicalStock}
                  </TableCell>

                  <TableCell className="text-right font-bold text-slate-900">
                    {formatCurrency(item.price)}
                  </TableCell>

                  <TableCell className="text-right text-xs text-slate-400">
                    {formatDateTime(item.lastUpdated)}
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
