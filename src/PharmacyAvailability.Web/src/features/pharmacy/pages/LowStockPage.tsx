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
import { useTranslation } from '@/i18n/useTranslation';

export function LowStockPage() {
  const { t } = useTranslation();
  const { user } = useAuthStore();
  const { currentBranch } = useBranchStore();
  const [threshold, setThreshold] = useState<number>(5);

  const pharmacyId = user?.pharmacyId || currentBranch?.pharmacyId;
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
      <div className="rounded-xl border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 p-8 text-center text-slate-500 dark:text-slate-400">
        {t('pharmacy.selectBranchPrompt')}
      </div>
    );
  }

  return (
    <div className="space-y-5">
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
        <div>
          <h1 className="text-2xl font-bold text-slate-900 dark:text-slate-100">{t('pharmacy.lowStockTitle')}</h1>
          <p className="text-xs text-slate-500 dark:text-slate-400">
            {t('pharmacy.lowStockDesc')}
          </p>
        </div>

        {/* Threshold filter */}
        <div className="flex items-center gap-2 text-xs">
          <Filter className="h-3.5 w-3.5 text-slate-400" />
          <span className="font-semibold text-slate-600 dark:text-slate-300">{t('pharmacy.thresholdStandard')}</span>
          <select
            value={threshold}
            onChange={(e) => setThreshold(Number(e.target.value))}
            className="h-8 rounded-md border border-slate-300 dark:border-slate-700 bg-white dark:bg-slate-800 px-2.5 text-xs text-slate-800 dark:text-slate-200"
          >
            <option value="3">&le; 3 {t('common.units')}</option>
            <option value="5">&le; 5 {t('common.units')}</option>
            <option value="10">&le; 10 {t('common.units')}</option>
            <option value="20">&le; 20 {t('common.units')}</option>
          </select>
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
          icon={Boxes}
          title={t('pharmacy.healthyTitle')}
          description={t('pharmacy.healthyDesc', { threshold })}
        />
      ) : (
        <div className="rounded-xl border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 shadow-xs overflow-hidden">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>{t('reservations.medicine')}</TableHead>
                <TableHead>{t('admin.categoryAndMfg')}</TableHead>
                <TableHead className="text-center">{t('pharmacy.availableUnits')}</TableHead>
                <TableHead className="text-center">{t('pharmacy.reservedQuantity')}</TableHead>
                <TableHead className="text-center">{t('pharmacy.physicalStock')}</TableHead>
                <TableHead className="text-end">{t('common.price')}</TableHead>
                <TableHead className="text-end">{t('common.date')}</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {data.items.map((item: LowStockAlertDto) => (
                <TableRow key={item.inventoryId} className="bg-red-50/25 dark:bg-red-950/20">
                  <TableCell>
                    <div className="flex items-center gap-2">
                      <AlertTriangle className="h-4 w-4 text-red-500 shrink-0" />
                      <div>
                        <span className="font-bold text-slate-900 dark:text-slate-100">{item.medicineBrandName}</span>
                        <p className="text-xs text-slate-500 dark:text-slate-400">{item.medicineGenericName}</p>
                      </div>
                    </div>
                  </TableCell>

                  <TableCell className="text-xs text-slate-600 dark:text-slate-300">
                    <div>{item.categoryName}</div>
                    <div className="text-[11px] text-slate-400 dark:text-slate-500">{item.manufacturerName}</div>
                  </TableCell>

                  <TableCell className="text-center">
                    <Badge variant={item.availableQuantity === 0 ? 'destructive' : 'warning'}>
                      {item.availableQuantity === 0 ? t('pharmacy.depleted') : t('pharmacy.remainingUnits', { count: item.availableQuantity })}
                    </Badge>
                  </TableCell>

                  <TableCell className="text-center text-xs font-semibold text-slate-700 dark:text-slate-300">
                    {item.reservedQuantity}
                  </TableCell>

                  <TableCell className="text-center text-xs font-bold text-slate-900 dark:text-slate-100">
                    {item.totalPhysicalStock}
                  </TableCell>

                  <TableCell className="text-end font-bold text-slate-900 dark:text-slate-100">
                    {formatCurrency(item.price)}
                  </TableCell>

                  <TableCell className="text-end text-xs text-slate-400">
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
