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

export function CatalogManagementPage() {
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
      // Direct call via customer/admin or fetch
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
          <h1 className="text-2xl font-bold text-slate-900 dark:text-slate-100">Central Medicine Catalog</h1>
          <p className="text-xs text-slate-500 dark:text-slate-400">
            Authoritative platform catalog definitions, barcode mappings, and search aliases.
          </p>
        </div>

        <Button
          size="sm"
          onClick={() => setIsCreateOpen(true)}
          className="flex items-center gap-1.5 text-xs bg-purple-700 hover:bg-purple-800 text-white"
        >
          <Plus className="h-4 w-4" />
          Add New Medicine
        </Button>
      </div>

      {/* Search Input */}
      <div className="relative w-full max-w-sm">
        <Search className="absolute left-3 top-2.5 h-4 w-4 text-slate-400" />
        <Input
          placeholder="Search catalog by brand or generic..."
          value={searchTerm}
          onChange={(e) => setSearchTerm(e.target.value)}
          className="pl-9 h-9 text-xs bg-white dark:bg-slate-800 border-slate-300 dark:border-slate-700 dark:text-slate-100"
        />
      </div>

      {isLoading ? (
        <div className="py-16">
          <LoadingSpinner size="lg" text="Loading central medicine catalog..." />
        </div>
      ) : isError ? (
        <ErrorState error={error} onRetry={() => refetch()} />
      ) : !medicines || medicines.items.length === 0 ? (
        <EmptyState
          icon={BookOpen}
          title="Catalog Empty"
          description="No medicines found matching your search filter."
          actionLabel="Add Medicine"
          onAction={() => setIsCreateOpen(true)}
        />
      ) : (
        <div className="rounded-xl border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 shadow-xs overflow-hidden">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Medicine Name</TableHead>
                <TableHead>Active Ingredient & Form</TableHead>
                <TableHead>Category / Manufacturer</TableHead>
                <TableHead className="text-center">Barcodes</TableHead>
                <TableHead className="text-center">Prescription</TableHead>
                <TableHead className="text-right">Actions</TableHead>
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
                      {med.barcodes?.length || 0} registered
                    </Badge>
                  </TableCell>

                  <TableCell className="text-center">
                    {med.requiresPrescription ? (
                      <Badge variant="destructive" className="text-[10px]">Rx Required</Badge>
                    ) : (
                      <Badge variant="success" className="text-[10px]">OTC</Badge>
                    )}
                  </TableCell>

                  <TableCell className="text-right space-x-1">
                    <Button
                      size="sm"
                      variant="outline"
                      className="h-7 px-2 text-xs"
                      onClick={() => setBarcodeMedicine(med)}
                    >
                      <Barcode className="h-3 w-3 mr-1" />
                      Barcodes & Aliases
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
            <DialogTitle className="text-lg font-bold text-slate-900">
              Barcodes & Aliases: {barcodeMedicine?.brandName}
            </DialogTitle>
          </DialogHeader>

          {barcodeMedicine && (
            <div className="space-y-4 py-2 text-xs">
              {/* Existing Barcodes */}
              <div>
                <span className="font-semibold text-slate-700 block mb-1.5">Registered Barcodes</span>
                <div className="space-y-1">
                  {barcodeMedicine.barcodes?.map((b) => (
                    <div key={b.id} className="flex justify-between items-center bg-slate-50 p-2 rounded border border-slate-200">
                      <span className="font-mono text-slate-800">{b.barcode}</span>
                      {b.isPrimary && <Badge variant="default" className="text-[9px]">Primary</Badge>}
                    </div>
                  ))}
                </div>

                <div className="flex items-center gap-2 mt-2">
                  <Input
                    placeholder="New barcode..."
                    value={newBarcode}
                    onChange={(e) => setNewBarcode(e.target.value)}
                    className="h-8 text-xs font-mono"
                  />
                  <label className="flex items-center gap-1 text-[11px] text-slate-600 whitespace-nowrap">
                    <input
                      type="checkbox"
                      checked={isPrimaryBarcode}
                      onChange={(e) => setIsPrimaryBarcode(e.target.checked)}
                      className="rounded text-blue-600"
                    />
                    Primary
                  </label>
                  <Button
                    size="sm"
                    className="h-8 text-xs"
                    disabled={!newBarcode || addBarcodeMutation.isPending}
                    onClick={() => addBarcodeMutation.mutate()}
                  >
                    Add
                  </Button>
                </div>
              </div>

              {/* Existing Aliases */}
              <div className="pt-3 border-t border-slate-200">
                <span className="font-semibold text-slate-700 block mb-1.5">Search Aliases</span>
                <div className="flex flex-wrap gap-1 mb-2">
                  {barcodeMedicine.aliases?.map((a) => (
                    <Badge key={a.id} variant="secondary" className="text-xs">
                      {a.alias}
                    </Badge>
                  ))}
                </div>

                <div className="flex gap-2">
                  <Input
                    placeholder="New alias name..."
                    value={newAlias}
                    onChange={(e) => setNewAlias(e.target.value)}
                    className="h-8 text-xs"
                  />
                  <Button
                    size="sm"
                    className="h-8 text-xs"
                    disabled={!newAlias || addAliasMutation.isPending}
                    onClick={() => addAliasMutation.mutate()}
                  >
                    Add
                  </Button>
                </div>
              </div>
            </div>
          )}

          <DialogFooter>
            <Button variant="outline" onClick={() => setBarcodeMedicine(null)}>Done</Button>
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
          <DialogTitle className="text-lg font-bold text-slate-900">Add Medicine to Central Catalog</DialogTitle>
        </DialogHeader>

        <form onSubmit={handleSubmit(onSubmit)} className="space-y-3 py-2 text-xs">
          {serverError && (
            <div className="rounded-md bg-red-50 p-2.5 text-xs text-red-700 border border-red-200">
              {serverError}
            </div>
          )}

          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="font-semibold text-slate-700 block mb-1">Brand Name</label>
              <Input {...register('brandName')} placeholder="Panadol Extra" />
              {errors.brandName && <p className="text-[11px] text-red-600 mt-0.5">{errors.brandName.message}</p>}
            </div>
            <div>
              <label className="font-semibold text-slate-700 block mb-1">Generic Name</label>
              <Input {...register('genericName')} placeholder="Paracetamol" />
              {errors.genericName && <p className="text-[11px] text-red-600 mt-0.5">{errors.genericName.message}</p>}
            </div>
          </div>

          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="font-semibold text-slate-700 block mb-1">Active Ingredient</label>
              <Input {...register('activeIngredient')} placeholder="Paracetamol 500mg" />
              {errors.activeIngredient && <p className="text-[11px] text-red-600 mt-0.5">{errors.activeIngredient.message}</p>}
            </div>
            <div>
              <label className="font-semibold text-slate-700 block mb-1">Strength</label>
              <Input {...register('strength')} placeholder="500mg" />
              {errors.strength && <p className="text-[11px] text-red-600 mt-0.5">{errors.strength.message}</p>}
            </div>
          </div>

          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="font-semibold text-slate-700 block mb-1">Dosage Form</label>
              <Input {...register('dosageForm')} placeholder="Tablet" />
              {errors.dosageForm && <p className="text-[11px] text-red-600 mt-0.5">{errors.dosageForm.message}</p>}
            </div>
            <div>
              <label className="font-semibold text-slate-700 block mb-1">Initial Barcode</label>
              <Input {...register('initialBarcode')} placeholder="622123456789" />
              {errors.initialBarcode && <p className="text-[11px] text-red-600 mt-0.5">{errors.initialBarcode.message}</p>}
            </div>
          </div>

          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="font-semibold text-slate-700 block mb-1">Category</label>
              <select
                {...register('categoryId')}
                className="h-10 w-full rounded-md border border-slate-300 bg-white px-3 text-xs"
              >
                <option value="">Select Category</option>
                {categories.map((c) => (
                  <option key={c.id} value={c.id}>{c.name}</option>
                ))}
              </select>
              {errors.categoryId && <p className="text-[11px] text-red-600 mt-0.5">{errors.categoryId.message}</p>}
            </div>
            <div>
              <label className="font-semibold text-slate-700 block mb-1">Manufacturer</label>
              <select
                {...register('manufacturerId')}
                className="h-10 w-full rounded-md border border-slate-300 bg-white px-3 text-xs"
              >
                <option value="">Select Manufacturer</option>
                {manufacturers.map((m) => (
                  <option key={m.id} value={m.id}>{m.name}</option>
                ))}
              </select>
              {errors.manufacturerId && <p className="text-[11px] text-red-600 mt-0.5">{errors.manufacturerId.message}</p>}
            </div>
          </div>

          <div className="flex items-center gap-2 pt-2">
            <input
              type="checkbox"
              id="prescription"
              {...register('requiresPrescription')}
              className="rounded text-blue-600"
            />
            <label htmlFor="prescription" className="text-slate-700 font-medium">
              Requires Prescription (Rx)
            </label>
          </div>

          <DialogFooter className="pt-2">
            <Button type="button" variant="outline" onClick={onClose} disabled={isSubmitting}>Cancel</Button>
            <Button type="submit" disabled={isSubmitting}>
              {isSubmitting ? 'Saving...' : 'Save to Catalog'}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
