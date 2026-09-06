import { useState, useEffect } from 'react';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Boxes, History, Search } from 'lucide-react';
import { useAuthStore } from '@/app/store/authStore';
import { useBranchStore } from '@/app/store/branchStore';
import { pharmacyOpsApi } from '../api/pharmacyOpsApi';
import { InventoryItemDto, InventoryTransactionDto } from '@/types/inventory';
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
  DialogFooter,
} from '@/components/ui/dialog';
import { LoadingSpinner } from '@/components/feedback/LoadingSpinner';
import { ErrorState } from '@/components/feedback/ErrorState';
import { EmptyState } from '@/components/feedback/EmptyState';
import { formatCurrency, formatDateTime } from '@/lib/formatters';
import { ApiError } from '@/api/apiClient';
import {
  adjustStockSchema,
  AdjustStockFormData,
  updatePriceSchema,
  UpdatePriceFormData,
} from '@/lib/validations';
import { useTranslation } from '@/i18n/useTranslation';

export function InventoryPage() {
  const { t } = useTranslation();
  const { user } = useAuthStore();
  const { currentBranch } = useBranchStore();
  const queryClient = useQueryClient();

  const [searchTerm, setSearchTerm] = useState('');

  // Modals state
  const [adjustItem, setAdjustItem] = useState<InventoryItemDto | null>(null);
  const [priceItem, setPriceItem] = useState<InventoryItemDto | null>(null);
  const [historyItem, setHistoryItem] = useState<InventoryItemDto | null>(null);

  const pharmacyId = user?.pharmacyId || currentBranch?.pharmacyId;
  const branchId = currentBranch?.id;

  const { data: inventory, isLoading, isError, error, refetch } = useQuery({
    queryKey: ['branch-inventory', pharmacyId, branchId],
    queryFn: () =>
      pharmacyId && branchId
        ? pharmacyOpsApi.getBranchInventory(pharmacyId, branchId)
        : Promise.reject('Missing branch'),
    enabled: !!pharmacyId && !!branchId,
  });

  const { data: transactions, isLoading: isTxLoading } = useQuery({
    queryKey: ['item-transactions', pharmacyId, branchId, historyItem?.medicineId],
    queryFn: () =>
      pharmacyId && branchId && historyItem
        ? pharmacyOpsApi.getTransactions(pharmacyId, branchId, historyItem.medicineId)
        : Promise.resolve([]),
    enabled: !!pharmacyId && !!branchId && !!historyItem,
  });

  if (!currentBranch) {
    return (
      <div className="rounded-xl border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 p-8 text-center text-slate-500 dark:text-slate-400">
        {t('pharmacy.selectBranchPrompt')}
      </div>
    );
  }

  const filtered = (inventory || []).filter((item: InventoryItemDto) => {
    if (!searchTerm) return true;
    const term = searchTerm.toLowerCase();
    return (
      item.medicineBrandName.toLowerCase().includes(term) ||
      item.medicineGenericName.toLowerCase().includes(term)
    );
  });

  return (
    <div className="space-y-5">
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
        <div>
          <h1 className="text-2xl font-bold text-slate-900 dark:text-slate-100">{t('pharmacy.branchInventoryTitle')}</h1>
          <p className="text-xs text-slate-500 dark:text-slate-400">
            {t('pharmacy.branchInventoryDesc', { branch: currentBranch.name })}
          </p>
        </div>
        <Button size="sm" variant="outline" onClick={() => refetch()} className="text-xs">
          {t('common.refresh')}
        </Button>
      </div>

      {/* Search Bar */}
      <div className="relative w-full max-w-sm">
        <Search className="absolute start-3 top-2.5 h-4 w-4 text-slate-400" />
        <Input
          placeholder={t('pharmacy.searchInventoryPlaceholder')}
          value={searchTerm}
          onChange={(e) => setSearchTerm(e.target.value)}
          className="ps-9 h-9 text-xs bg-white dark:bg-slate-800 dark:border-slate-700"
        />
      </div>

      {isLoading ? (
        <div className="py-16">
          <LoadingSpinner size="lg" text={t('common.loading')} />
        </div>
      ) : isError ? (
        <ErrorState error={error} onRetry={() => refetch()} />
      ) : filtered.length === 0 ? (
        <EmptyState
          icon={Boxes}
          title={t('feedback.noDataTitle')}
          description={t('feedback.noDataDesc')}
        />
      ) : (
        <div className="rounded-xl border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 shadow-xs overflow-hidden">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>{t('reservations.medicine')}</TableHead>
                <TableHead className="text-center">{t('pharmacy.availableUnits')}</TableHead>
                <TableHead className="text-center">{t('pharmacy.reservedQuantity')}</TableHead>
                <TableHead className="text-center">{t('pharmacy.physicalStock')}</TableHead>
                <TableHead className="text-end">{t('common.price')}</TableHead>
                <TableHead className="text-center">{t('pharmacy.freshness')}</TableHead>
                <TableHead className="text-end">{t('common.actions')}</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {filtered.map((item: InventoryItemDto) => (
                <TableRow key={item.id}>
                  <TableCell>
                    <div className="font-bold text-slate-900 dark:text-slate-100">{item.medicineBrandName}</div>
                    <div className="text-xs text-slate-500 dark:text-slate-400">{item.medicineGenericName}</div>
                  </TableCell>

                  <TableCell className="text-center">
                    <Badge variant={item.availableQuantity > 5 ? 'success' : item.availableQuantity > 0 ? 'warning' : 'destructive'}>
                      {item.availableQuantity} {t('common.units')}
                    </Badge>
                  </TableCell>

                  <TableCell className="text-center font-semibold text-slate-700 dark:text-slate-300 text-xs">
                    {item.reservedQuantity}
                  </TableCell>

                  <TableCell className="text-center font-bold text-slate-900 dark:text-slate-100 text-sm">
                    {item.totalStock}
                  </TableCell>

                  <TableCell className="text-end font-bold text-slate-900 dark:text-slate-100">
                    {formatCurrency(item.price, item.currency)}
                  </TableCell>

                  <TableCell className="text-center">
                    <span className="text-[11px] font-mono text-slate-500 dark:text-slate-400 bg-slate-100 dark:bg-slate-800 px-2 py-0.5 rounded">
                      {item.freshness}
                    </span>
                  </TableCell>

                  <TableCell className="text-end space-x-1">
                    <Button
                      size="sm"
                      variant="outline"
                      className="h-8 px-2 text-xs"
                      onClick={() => setAdjustItem(item)}
                      title={t('pharmacy.adjustStock')}
                    >
                      {t('pharmacy.adjustStock')}
                    </Button>
                    <Button
                      size="sm"
                      variant="outline"
                      className="h-8 px-2 text-xs"
                      onClick={() => setPriceItem(item)}
                      title={t('pharmacy.updatePrice')}
                    >
                      {t('pharmacy.updatePrice')}
                    </Button>
                    <Button
                      size="sm"
                      variant="ghost"
                      className="h-8 px-2 text-xs text-slate-500"
                      onClick={() => setHistoryItem(item)}
                      title={t('pharmacy.history')}
                    >
                      <History className="h-3.5 w-3.5" />
                    </Button>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </div>
      )}

      {/* Stock Adjustment Modal */}
      {pharmacyId && branchId && (
        <AdjustStockDialog
          item={adjustItem}
          pharmacyId={pharmacyId}
          branchId={branchId}
          onClose={() => setAdjustItem(null)}
          onSuccess={() => {
            queryClient.invalidateQueries({ queryKey: ['branch-inventory'] });
            queryClient.invalidateQueries({ queryKey: ['branch-dashboard'] });
          }}
        />
      )}

      {/* Price Update Modal */}
      {pharmacyId && branchId && (
        <UpdatePriceDialog
          item={priceItem}
          pharmacyId={pharmacyId}
          branchId={branchId}
          onClose={() => setPriceItem(null)}
          onSuccess={() => {
            queryClient.invalidateQueries({ queryKey: ['branch-inventory'] });
          }}
        />
      )}

      {/* Transaction History Modal */}
      <Dialog open={!!historyItem} onOpenChange={() => setHistoryItem(null)}>
        <DialogContent className="sm:max-w-lg">
          <DialogHeader>
            <DialogTitle className="text-lg font-bold text-slate-900 dark:text-slate-100">
              {t('pharmacy.auditHistoryTitle', { medicine: historyItem?.medicineBrandName || '' })}
            </DialogTitle>
          </DialogHeader>

          <div className="max-h-80 overflow-y-auto space-y-2 py-2">
            {isTxLoading ? (
              <LoadingSpinner size="sm" text={t('common.loading')} />
            ) : !transactions || transactions.length === 0 ? (
              <p className="text-xs text-slate-500 dark:text-slate-400 text-center py-6">{t('pharmacy.noAuditLogs')}</p>
            ) : (
              transactions.map((tx: InventoryTransactionDto) => (
                <div key={tx.id} className="rounded-md border border-slate-100 dark:border-slate-800 bg-slate-50 dark:bg-slate-850 p-3 text-xs flex justify-between items-center">
                  <div>
                    <span className="font-semibold text-slate-800 dark:text-slate-200">{tx.reason}</span>
                    <p className="text-[11px] text-slate-400">{formatDateTime(tx.createdAt)}</p>
                    {tx.notes && <p className="text-[11px] text-slate-600 dark:text-slate-400 italic">"{tx.notes}"</p>}
                  </div>
                  <div className="text-end">
                    <span className={`font-bold ${tx.deltaQuantity >= 0 ? 'text-emerald-600 dark:text-emerald-400' : 'text-red-600 dark:text-red-400'}`}>
                      {tx.deltaQuantity >= 0 ? `+${tx.deltaQuantity}` : tx.deltaQuantity}
                    </span>
                    <p className="text-[10px] text-slate-400">{t('pharmacy.resultingStock')} {tx.resultingQuantity}</p>
                  </div>
                </div>
              ))
            )}
          </div>

          <DialogFooter>
            <Button variant="outline" onClick={() => setHistoryItem(null)}>{t('common.close')}</Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}

interface AdjustStockDialogProps {
  item: InventoryItemDto | null;
  pharmacyId: string;
  branchId: string;
  onClose: () => void;
  onSuccess: () => void;
}

function AdjustStockDialog({ item, pharmacyId, branchId, onClose, onSuccess }: AdjustStockDialogProps) {
  const { t } = useTranslation();
  const [serverError, setServerError] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    watch,
    reset,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<AdjustStockFormData>({
    resolver: zodResolver(adjustStockSchema),
    defaultValues: {
      deltaQuantity: 0,
      reason: 'ManualAdjustment',
      notes: '',
    },
  });

  useEffect(() => {
    if (item) {
      reset({
        deltaQuantity: 0,
        reason: 'ManualAdjustment',
        notes: '',
      });
      setServerError(null);
    }
  }, [item, reset]);

  const deltaQuantity = watch('deltaQuantity') || 0;
  const resultingStock = (item?.totalStock ?? 0) + deltaQuantity;

  const onSubmit = async (values: AdjustStockFormData) => {
    if (!item) return;
    setServerError(null);

    try {
      await pharmacyOpsApi.adjustStock(pharmacyId, branchId, item.medicineId, {
        deltaQuantity: values.deltaQuantity,
        reason: values.reason,
        notes: values.notes || null,
      });
      onSuccess();
      onClose();
    } catch (err: unknown) {
      if (err instanceof ApiError) {
        if (err.isValidationError) {
          const fieldErrors = err.getAllFieldErrors();
          Object.entries(fieldErrors).forEach(([field, message]) => {
            const camelKey = (field.charAt(0).toLowerCase() + field.slice(1)) as keyof AdjustStockFormData;
            setError(camelKey, { message });
          });
        }
        setServerError(err.detail || err.title);
      } else if (err instanceof Error) {
        setServerError(err.message);
      } else {
        setServerError('Failed to apply stock adjustment.');
      }
    }
  };

  return (
    <Dialog open={!!item} onOpenChange={(open) => !open && onClose()}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle className="text-lg font-bold text-slate-900 dark:text-slate-100">{t('pharmacy.adjustStockTitle')}</DialogTitle>
        </DialogHeader>

        {item && (
          <form onSubmit={handleSubmit(onSubmit)} className="space-y-4 py-2">
            {serverError && (
              <div className="rounded-md bg-red-50 dark:bg-red-950/40 p-2.5 text-xs text-red-700 dark:text-red-300 border border-red-200 dark:border-red-900/50">
                {serverError}
              </div>
            )}

            <div className="rounded-lg bg-slate-50 dark:bg-slate-800/60 p-3 text-xs space-y-1">
              <p className="font-bold text-slate-900 dark:text-slate-100">{item.medicineBrandName}</p>
              <div className="flex justify-between text-slate-500 dark:text-slate-400">
                <span>{t('pharmacy.physicalStock')}: {item.totalStock}</span>
                <span>{t('pharmacy.availableUnits')}: {item.availableQuantity}</span>
              </div>
            </div>

            <div className="space-y-1">
              <label className="text-xs font-semibold text-slate-700 dark:text-slate-300">{t('pharmacy.deltaQuantity')}</label>
              <div className="flex items-center gap-2">
                <Input
                  type="number"
                  {...register('deltaQuantity', { valueAsNumber: true })}
                  placeholder="+10 / -5"
                  className="dark:bg-slate-800 dark:border-slate-700"
                />
                <span className="text-xs font-medium text-slate-600 dark:text-slate-400 whitespace-nowrap">
                  {t('pharmacy.resultingStock')} <strong className={resultingStock < 0 ? 'text-red-600' : 'text-slate-900 dark:text-slate-100'}>{resultingStock}</strong>
                </span>
              </div>
              {errors.deltaQuantity && (
                <p className="text-xs text-red-600 dark:text-red-400 font-medium">{errors.deltaQuantity.message}</p>
              )}
            </div>

            <div className="space-y-1">
              <label className="text-xs font-semibold text-slate-700 dark:text-slate-300">{t('pharmacy.reason')}</label>
              <select
                {...register('reason')}
                className="h-10 w-full rounded-md border border-slate-300 dark:border-slate-700 bg-white dark:bg-slate-800 px-3 text-xs dark:text-slate-200"
              >
                <option value="ReceivedShipment">{t('pharmacy.reasonShipment')}</option>
                <option value="ManualAdjustment">{t('pharmacy.reasonManual')}</option>
                <option value="DispensedDamaged">{t('pharmacy.reasonDamaged')}</option>
                <option value="ReturnFromCustomer">{t('pharmacy.reasonReturn')}</option>
              </select>
              {errors.reason && (
                <p className="text-xs text-red-600 dark:text-red-400 font-medium">{errors.reason.message}</p>
              )}
            </div>

            <div className="space-y-1">
              <label className="text-xs font-semibold text-slate-700 dark:text-slate-300">{t('pharmacy.notes')}</label>
              <Input
                {...register('notes')}
                placeholder={t('pharmacy.notes')}
                className="dark:bg-slate-800 dark:border-slate-700"
              />
              {errors.notes && (
                <p className="text-xs text-red-600 dark:text-red-400 font-medium">{errors.notes.message}</p>
              )}
            </div>

            <DialogFooter>
              <Button type="button" variant="outline" onClick={onClose} disabled={isSubmitting}>
                {t('common.cancel')}
              </Button>
              <Button type="submit" disabled={isSubmitting}>
                {isSubmitting ? t('common.loading') : t('pharmacy.applyAdjustment')}
              </Button>
            </DialogFooter>
          </form>
        )}
      </DialogContent>
    </Dialog>
  );
}

interface UpdatePriceDialogProps {
  item: InventoryItemDto | null;
  pharmacyId: string;
  branchId: string;
  onClose: () => void;
  onSuccess: () => void;
}

function UpdatePriceDialog({ item, pharmacyId, branchId, onClose, onSuccess }: UpdatePriceDialogProps) {
  const { t } = useTranslation();
  const [serverError, setServerError] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    reset,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<UpdatePriceFormData>({
    resolver: zodResolver(updatePriceSchema),
    defaultValues: {
      newPrice: item?.price || 0,
    },
  });

  useEffect(() => {
    if (item) {
      reset({
        newPrice: item.price,
      });
      setServerError(null);
    }
  }, [item, reset]);

  const onSubmit = async (values: UpdatePriceFormData) => {
    if (!item) return;
    setServerError(null);

    try {
      await pharmacyOpsApi.updatePrice(pharmacyId, branchId, item.medicineId, {
        newPrice: values.newPrice,
      });
      onSuccess();
      onClose();
    } catch (err: unknown) {
      if (err instanceof ApiError) {
        if (err.isValidationError) {
          const fieldErrors = err.getAllFieldErrors();
          Object.entries(fieldErrors).forEach(([field, message]) => {
            const camelKey = (field.charAt(0).toLowerCase() + field.slice(1)) as keyof UpdatePriceFormData;
            setError(camelKey, { message });
          });
        }
        setServerError(err.detail || err.title);
      } else if (err instanceof Error) {
        setServerError(err.message);
      } else {
        setServerError('Failed to update price.');
      }
    }
  };

  return (
    <Dialog open={!!item} onOpenChange={(open) => !open && onClose()}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle className="text-lg font-bold text-slate-900 dark:text-slate-100">{t('pharmacy.updatePriceTitle')}</DialogTitle>
        </DialogHeader>

        {item && (
          <form onSubmit={handleSubmit(onSubmit)} className="space-y-4 py-2">
            {serverError && (
              <div className="rounded-md bg-red-50 dark:bg-red-950/40 p-2.5 text-xs text-red-700 dark:text-red-300 border border-red-200 dark:border-red-900/50">
                {serverError}
              </div>
            )}

            <div className="rounded-lg bg-slate-50 dark:bg-slate-800/60 p-3 text-xs">
              <p className="font-bold text-slate-900 dark:text-slate-100">{item.medicineBrandName}</p>
              <p className="text-slate-500 dark:text-slate-400">{t('common.price')}: {formatCurrency(item.price, item.currency)}</p>
            </div>

            <div className="space-y-1">
              <label className="text-xs font-semibold text-slate-700 dark:text-slate-300">{t('pharmacy.newPrice', { currency: item.currency })}</label>
              <Input
                type="number"
                step="0.01"
                {...register('newPrice', { valueAsNumber: true })}
                className="dark:bg-slate-800 dark:border-slate-700"
              />
              {errors.newPrice && (
                <p className="text-xs text-red-600 dark:text-red-400 font-medium">{errors.newPrice.message}</p>
              )}
            </div>

            <DialogFooter>
              <Button type="button" variant="outline" onClick={onClose} disabled={isSubmitting}>
                {t('common.cancel')}
              </Button>
              <Button type="submit" disabled={isSubmitting}>
                {isSubmitting ? t('common.loading') : t('pharmacy.savePrice')}
              </Button>
            </DialogFooter>
          </form>
        )}
      </DialogContent>
    </Dialog>
  );
}
