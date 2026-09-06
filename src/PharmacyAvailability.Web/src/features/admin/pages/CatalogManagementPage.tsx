import { useState, useEffect } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { BookOpen, Plus, Barcode, Search } from 'lucide-react';
import { adminApi } from '../api/adminApi';
import { customerApi } from '@/features/customer/api/customerApi';
import { MedicineDto, ManufacturerDto, CategoryDto } from '@/types/catalog';
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
import { ApiError } from '@/api/apiClient';
import { medicineSchema, MedicineFormData } from '@/lib/validations';
import { useTranslation } from '@/i18n/useTranslation';

export function CatalogManagementPage() {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const [searchTerm, setSearchTerm] = useState('');

  // Create Medicine State
  const [isCreateOpen, setIsCreateOpen] = useState(false);

  // Add Barcode State
  const [barcodeMedicine, setBarcodeMedicine] = useState<MedicineDto | null>(null);
  const [newBarcode, setNewBarcode] = useState('');
  const [isPrimaryBarcode, setIsPrimaryBarcode] = useState(false);

  // Add Alias State
  const [newAlias, setNewAlias] = useState('');

  // Data queries
  const { data: categories } = useQuery({
    queryKey: ['categories-all'],
    queryFn: () => customerApi.getCategories(),
  });

  const { data: manufacturers } = useQuery({
    queryKey: ['manufacturers-all'],
    queryFn: async () => {
      const res = await fetch('/api/v1/manufacturers');
      return res.json() as Promise<ManufacturerDto[]>;
    },
  });

  const { data: medicines, isLoading, isError, error, refetch } = useQuery({
    queryKey: ['admin-medicines-search', searchTerm],
    queryFn: async () => {
      const res = await fetch(`/api/v1/medicines?searchTerm=${encodeURIComponent(searchTerm)}&pageSize=50`);
      return res.json() as Promise<{ items: MedicineDto[]; totalCount: number }>;
    },
  });

  const addBarcodeMutation = useMutation({
    mutationFn: () => {
      if (!barcodeMedicine) return Promise.reject('No medicine');
      return adminApi.addBarcode(barcodeMedicine.id, {
        barcode: newBarcode,
        isPrimary: isPrimaryBarcode,
      });
    },
    onSuccess: () => {
      setNewBarcode('');
      queryClient.invalidateQueries({ queryKey: ['admin-medicines-search'] });
    },
  });

  const addAliasMutation = useMutation({
    mutationFn: () => {
      if (!barcodeMedicine) return Promise.reject('No medicine');
      return adminApi.addAlias(barcodeMedicine.id, {
        alias: newAlias,
      });
    },
    onSuccess: () => {
      setNewAlias('');
      queryClient.invalidateQueries({ queryKey: ['admin-medicines-search'] });
    },
  });

  return (
    <div className="space-y-5">
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
        <div>
          <h1 className="text-2xl font-bold text-slate-900 dark:text-slate-100">{t('admin.centralCatalogTitle')}</h1>
          <p className="text-xs text-slate-500 dark:text-slate-400">
            {t('admin.centralCatalogDesc')}
          </p>
        </div>

        <Button
          size="sm"
          onClick={() => setIsCreateOpen(true)}
          className="flex items-center gap-1.5 text-xs bg-purple-700 hover:bg-purple-800 text-white"
        >
          <Plus className="h-4 w-4" />
          {t('admin.addNewMedicine')}
        </Button>
      </div>

      {/* Search Input */}
      <div className="relative w-full max-w-sm">
        <Search className="absolute start-3 top-2.5 h-4 w-4 text-slate-400" />
        <Input
          placeholder={t('admin.searchCatalogPlaceholder')}
          value={searchTerm}
          onChange={(e) => setSearchTerm(e.target.value)}
          className="ps-9 h-9 text-xs bg-white dark:bg-slate-800 border-slate-300 dark:border-slate-700 dark:text-slate-100"
        />
      </div>

      {isLoading ? (
        <div className="py-16">
          <LoadingSpinner size="lg" text={t('common.loading')} />
        </div>
      ) : isError ? (
        <ErrorState error={error} onRetry={() => refetch()} />
      ) : !medicines || medicines.items.length === 0 ? (
        <EmptyState
          icon={BookOpen}
          title={t('feedback.noDataTitle')}
          description={t('feedback.noDataDesc')}
          actionLabel={t('admin.addNewMedicine')}
          onAction={() => setIsCreateOpen(true)}
        />
      ) : (
        <div className="rounded-xl border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 shadow-xs overflow-hidden">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>{t('reservations.medicine')}</TableHead>
                <TableHead>{t('admin.dosageAndStrength')}</TableHead>
                <TableHead>{t('admin.categoryAndMfg')}</TableHead>
                <TableHead className="text-center">{t('common.details')}</TableHead>
                <TableHead className="text-center">{t('admin.requiresRxLabel')}</TableHead>
                <TableHead className="text-end">{t('common.actions')}</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {medicines.items.map((med: MedicineDto) => (
                <TableRow key={med.id}>
                  <TableCell>
                    <div className="font-bold text-slate-900 dark:text-slate-100">{med.brandName}</div>
                    <div className="text-xs text-slate-500 dark:text-slate-400">{med.genericName}</div>
                  </TableCell>

                  <TableCell className="text-xs text-slate-600 dark:text-slate-300">
                    <div>{med.activeIngredient}</div>
                    <div className="text-[11px] text-slate-400 dark:text-slate-500">
                      {med.dosageForm} • {med.strength}
                    </div>
                  </TableCell>

                  <TableCell className="text-xs text-slate-600 dark:text-slate-300">
                    <div className="font-medium">{med.categoryName}</div>
                    <div className="text-[11px] text-slate-400 dark:text-slate-500">{med.manufacturerName}</div>
                  </TableCell>

                  <TableCell className="text-center text-xs">
                    <Badge variant="secondary" className="font-mono text-[10px]">
                      {t('admin.registeredBarcodes', { count: med.barcodes?.length || 0 })}
                    </Badge>
                  </TableCell>

                  <TableCell className="text-center">
                    {med.requiresPrescription ? (
                      <Badge variant="destructive" className="text-[10px]">{t('admin.prescriptionRequired')}</Badge>
                    ) : (
                      <Badge variant="success" className="text-[10px]">{t('admin.otc')}</Badge>
                    )}
                  </TableCell>

                  <TableCell className="text-end space-x-1">
                    <Button
                      size="sm"
                      variant="outline"
                      className="h-7 px-2 text-xs"
                      onClick={() => setBarcodeMedicine(med)}
                    >
                      <Barcode className="h-3 w-3 me-1" />
                      {t('admin.barcodesAndAliases')}
                    </Button>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </div>
      )}

      {/* Create Medicine Modal */}
      <CreateMedicineDialog
        isOpen={isCreateOpen}
        categories={categories || []}
        manufacturers={manufacturers || []}
        onClose={() => setIsCreateOpen(false)}
        onSuccess={() => queryClient.invalidateQueries({ queryKey: ['admin-medicines-search'] })}
      />

      {/* Barcodes and Aliases Modal */}
      <Dialog open={!!barcodeMedicine} onOpenChange={() => setBarcodeMedicine(null)}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle className="text-lg font-bold text-slate-900 dark:text-slate-100">
              {t('admin.barcodesTitle', { name: barcodeMedicine?.brandName || '' })}
            </DialogTitle>
          </DialogHeader>

          {barcodeMedicine && (
            <div className="space-y-4 py-2 text-xs">
              {/* Existing Barcodes */}
              <div>
                <span className="font-semibold text-slate-700 dark:text-slate-300 block mb-1.5">{t('admin.registeredBarcodesTitle')}</span>
                <div className="space-y-1">
                  {barcodeMedicine.barcodes?.map((b) => (
                    <div key={b.id} className="flex justify-between items-center bg-slate-50 dark:bg-slate-800 p-2 rounded border border-slate-200 dark:border-slate-700">
                      <span className="font-mono text-slate-800 dark:text-slate-200">{b.barcode}</span>
                      {b.isPrimary && <Badge variant="default" className="text-[9px]">{t('admin.isPrimary')}</Badge>}
                    </div>
                  ))}
                </div>

                <div className="flex items-center gap-2 mt-2">
                  <Input
                    placeholder={t('admin.addBarcode')}
                    value={newBarcode}
                    onChange={(e) => setNewBarcode(e.target.value)}
                    className="h-8 text-xs font-mono dark:bg-slate-800 dark:border-slate-700"
                  />
                  <label className="flex items-center gap-1 text-[11px] text-slate-600 dark:text-slate-400 whitespace-nowrap">
                    <input
                      type="checkbox"
                      checked={isPrimaryBarcode}
                      onChange={(e) => setIsPrimaryBarcode(e.target.checked)}
                      className="rounded text-blue-600"
                    />
                    {t('admin.isPrimary')}
                  </label>
                  <Button
                    size="sm"
                    className="h-8 text-xs"
                    disabled={!newBarcode || addBarcodeMutation.isPending}
                    onClick={() => addBarcodeMutation.mutate()}
                  >
                    {t('admin.addBtn')}
                  </Button>
                </div>
              </div>

              {/* Existing Aliases */}
              <div className="pt-3 border-t border-slate-200 dark:border-slate-700">
                <span className="font-semibold text-slate-700 dark:text-slate-300 block mb-1.5">{t('admin.searchAliasesTitle')}</span>
                <div className="flex flex-wrap gap-1 mb-2">
                  {barcodeMedicine.aliases?.map((a) => (
                    <Badge key={a.id} variant="secondary" className="text-xs">
                      {a.alias}
                    </Badge>
                  ))}
                </div>

                <div className="flex gap-2">
                  <Input
                    placeholder={t('admin.addAlias')}
                    value={newAlias}
                    onChange={(e) => setNewAlias(e.target.value)}
                    className="h-8 text-xs dark:bg-slate-800 dark:border-slate-700"
                  />
                  <Button
                    size="sm"
                    className="h-8 text-xs"
                    disabled={!newAlias || addAliasMutation.isPending}
                    onClick={() => addAliasMutation.mutate()}
                  >
                    {t('admin.addBtn')}
                  </Button>
                </div>
              </div>
            </div>
          )}

          <DialogFooter>
            <Button variant="outline" onClick={() => setBarcodeMedicine(null)}>{t('admin.doneBtn')}</Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}

interface CreateMedicineDialogProps {
  isOpen: boolean;
  categories: CategoryDto[];
  manufacturers: ManufacturerDto[];
  onClose: () => void;
  onSuccess: () => void;
}

function CreateMedicineDialog({ isOpen, categories, manufacturers, onClose, onSuccess }: CreateMedicineDialogProps) {
  const { t } = useTranslation();
  const [serverError, setServerError] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    reset,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<MedicineFormData>({
    resolver: zodResolver(medicineSchema),
    defaultValues: {
      brandName: '',
      genericName: '',
      activeIngredient: '',
      strength: '',
      dosageForm: '',
      categoryId: '',
      manufacturerId: '',
      requiresPrescription: false,
      initialBarcode: '',
    },
  });

  useEffect(() => {
    if (isOpen) {
      reset({
        brandName: '',
        genericName: '',
        activeIngredient: '',
        strength: '',
        dosageForm: '',
        categoryId: '',
        manufacturerId: '',
        requiresPrescription: false,
        initialBarcode: '',
      });
      setServerError(null);
    }
  }, [isOpen, reset]);

  const onSubmit = async (values: MedicineFormData) => {
    setServerError(null);

    try {
      await adminApi.createMedicine({
        ...values,
        initialBarcode: values.initialBarcode || null,
      });
      onSuccess();
      onClose();
    } catch (err: unknown) {
      if (err instanceof ApiError) {
        if (err.isValidationError) {
          const fieldErrors = err.getAllFieldErrors();
          Object.entries(fieldErrors).forEach(([field, message]) => {
            const camelKey = (field.charAt(0).toLowerCase() + field.slice(1)) as keyof MedicineFormData;
            setError(camelKey, { message });
          });
        }
        setServerError(err.detail || err.title);
      } else if (err instanceof Error) {
        setServerError(err.message);
      } else {
        setServerError('Failed to create medicine.');
      }
    }
  };

  return (
    <Dialog open={isOpen} onOpenChange={(open) => !open && onClose()}>
      <DialogContent className="sm:max-w-lg">
        <DialogHeader>
          <DialogTitle className="text-lg font-bold text-slate-900 dark:text-slate-100">{t('admin.createMedTitle')}</DialogTitle>
        </DialogHeader>

        <form onSubmit={handleSubmit(onSubmit)} className="space-y-3 py-2 text-xs">
          {serverError && (
            <div className="rounded-md bg-red-50 dark:bg-red-950/40 p-2.5 text-xs text-red-700 dark:text-red-300 border border-red-200 dark:border-red-900/50">
              {serverError}
            </div>
          )}

          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="font-semibold text-slate-700 dark:text-slate-300 block mb-1">{t('admin.brandNameLabel')}</label>
              <Input {...register('brandName')} placeholder="Panadol Extra" className="dark:bg-slate-800 dark:border-slate-700" />
              {errors.brandName && <p className="text-[11px] text-red-600 dark:text-red-400 mt-0.5">{errors.brandName.message}</p>}
            </div>
            <div>
              <label className="font-semibold text-slate-700 dark:text-slate-300 block mb-1">{t('admin.genericNameLabel')}</label>
              <Input {...register('genericName')} placeholder="Paracetamol" className="dark:bg-slate-800 dark:border-slate-700" />
              {errors.genericName && <p className="text-[11px] text-red-600 dark:text-red-400 mt-0.5">{errors.genericName.message}</p>}
            </div>
          </div>

          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="font-semibold text-slate-700 dark:text-slate-300 block mb-1">{t('admin.activeIngredientLabel')}</label>
              <Input {...register('activeIngredient')} placeholder="Paracetamol 500mg" className="dark:bg-slate-800 dark:border-slate-700" />
              {errors.activeIngredient && <p className="text-[11px] text-red-600 dark:text-red-400 mt-0.5">{errors.activeIngredient.message}</p>}
            </div>
            <div>
              <label className="font-semibold text-slate-700 dark:text-slate-300 block mb-1">{t('admin.strengthLabel')}</label>
              <Input {...register('strength')} placeholder="500mg" className="dark:bg-slate-800 dark:border-slate-700" />
              {errors.strength && <p className="text-[11px] text-red-600 dark:text-red-400 mt-0.5">{errors.strength.message}</p>}
            </div>
          </div>

          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="font-semibold text-slate-700 dark:text-slate-300 block mb-1">{t('admin.dosageFormLabel')}</label>
              <Input {...register('dosageForm')} placeholder="Tablet" className="dark:bg-slate-800 dark:border-slate-700" />
              {errors.dosageForm && <p className="text-[11px] text-red-600 dark:text-red-400 mt-0.5">{errors.dosageForm.message}</p>}
            </div>
            <div>
              <label className="font-semibold text-slate-700 dark:text-slate-300 block mb-1">{t('admin.barcodeLabel')}</label>
              <Input {...register('initialBarcode')} placeholder="622123456789" className="dark:bg-slate-800 dark:border-slate-700" />
              {errors.initialBarcode && <p className="text-[11px] text-red-600 dark:text-red-400 mt-0.5">{errors.initialBarcode.message}</p>}
            </div>
          </div>

          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="font-semibold text-slate-700 dark:text-slate-300 block mb-1">{t('admin.categoryLabel')}</label>
              <select
                {...register('categoryId')}
                className="h-10 w-full rounded-md border border-slate-300 dark:border-slate-700 bg-white dark:bg-slate-800 px-3 text-xs dark:text-slate-200"
              >
                <option value="">{t('admin.categoryLabel')}</option>
                {categories.map((c) => (
                  <option key={c.id} value={c.id}>{c.name}</option>
                ))}
              </select>
              {errors.categoryId && <p className="text-[11px] text-red-600 dark:text-red-400 mt-0.5">{errors.categoryId.message}</p>}
            </div>
            <div>
              <label className="font-semibold text-slate-700 dark:text-slate-300 block mb-1">{t('admin.mfgLabel')}</label>
              <select
                {...register('manufacturerId')}
                className="h-10 w-full rounded-md border border-slate-300 dark:border-slate-700 bg-white dark:bg-slate-800 px-3 text-xs dark:text-slate-200"
              >
                <option value="">{t('admin.mfgLabel')}</option>
                {manufacturers.map((m) => (
                  <option key={m.id} value={m.id}>{m.name}</option>
                ))}
              </select>
              {errors.manufacturerId && <p className="text-[11px] text-red-600 dark:text-red-400 mt-0.5">{errors.manufacturerId.message}</p>}
            </div>
          </div>

          <div className="flex items-center gap-2 pt-2">
            <input
              type="checkbox"
              id="prescription"
              {...register('requiresPrescription')}
              className="rounded text-blue-600"
            />
            <label htmlFor="prescription" className="text-slate-700 dark:text-slate-300 font-medium">
              {t('admin.requiresRxLabel')}
            </label>
          </div>

          <DialogFooter className="pt-2">
            <Button type="button" variant="outline" onClick={onClose} disabled={isSubmitting}>{t('common.cancel')}</Button>
            <Button type="submit" disabled={isSubmitting}>
              {isSubmitting ? t('admin.savingMed') : t('admin.saveMedBtn')}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
