import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { BookOpen, Plus, Barcode, Search } from 'lucide-react';
import { adminApi } from '../api/adminApi';
import { customerApi } from '@/features/customer/api/customerApi';
import { MedicineDto, ManufacturerDto } from '@/types/catalog';
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

export function CatalogManagementPage() {
  const queryClient = useQueryClient();
  const [searchTerm, setSearchTerm] = useState('');

  // Create Medicine State
  const [isCreateOpen, setIsCreateOpen] = useState(false);
  const [brandName, setBrandName] = useState('');
  const [genericName, setGenericName] = useState('');
  const [activeIngredient, setActiveIngredient] = useState('');
  const [strength, setStrength] = useState('');
  const [dosageForm, setDosageForm] = useState('');
  const [categoryId, setCategoryId] = useState('');
  const [manufacturerId, setManufacturerId] = useState('');
  const [requiresPrescription, setRequiresPrescription] = useState(false);
  const [initialBarcode, setInitialBarcode] = useState('');

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

  const createMutation = useMutation({
    mutationFn: () =>
      adminApi.createMedicine({
        brandName,
        genericName,
        activeIngredient,
        strength,
        dosageForm,
        categoryId,
        manufacturerId,
        requiresPrescription,
        initialBarcode: initialBarcode || null,
      }),
    onSuccess: () => {
      setIsCreateOpen(false);
      setBrandName('');
      setGenericName('');
      setActiveIngredient('');
      setStrength('');
      setDosageForm('');
      setInitialBarcode('');
      queryClient.invalidateQueries({ queryKey: ['admin-medicines-search'] });
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
          <h1 className="text-2xl font-bold text-slate-900">Central Medicine Catalog</h1>
          <p className="text-xs text-slate-500">
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
          className="pl-9 h-9 text-xs bg-white"
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
        <div className="rounded-xl border border-slate-200 bg-white shadow-xs overflow-hidden">
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
                    <div className="font-bold text-slate-900">{med.brandName}</div>
                    <div className="text-xs text-slate-500">{med.genericName}</div>
                  </TableCell>

                  <TableCell className="text-xs text-slate-600">
                    <div>{med.activeIngredient}</div>
                    <div className="text-[11px] text-slate-400">
                      {med.dosageForm} • {med.strength}
                    </div>
                  </TableCell>

                  <TableCell className="text-xs text-slate-600">
                    <div className="font-medium">{med.categoryName}</div>
                    <div className="text-[11px] text-slate-400">{med.manufacturerName}</div>
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
      <Dialog open={isCreateOpen} onOpenChange={setIsCreateOpen}>
        <DialogContent className="sm:max-w-lg">
          <DialogHeader>
            <DialogTitle className="text-lg font-bold text-slate-900">Add Medicine to Central Catalog</DialogTitle>
          </DialogHeader>

          <form
            onSubmit={(e) => {
              e.preventDefault();
              createMutation.mutate();
            }}
            className="space-y-3 py-2 text-xs"
          >
            <div className="grid grid-cols-2 gap-3">
              <div>
                <label className="font-semibold text-slate-700 block mb-1">Brand Name</label>
                <Input value={brandName} onChange={(e) => setBrandName(e.target.value)} placeholder="Panadol Extra" required />
              </div>
              <div>
                <label className="font-semibold text-slate-700 block mb-1">Generic Name</label>
                <Input value={genericName} onChange={(e) => setGenericName(e.target.value)} placeholder="Paracetamol" required />
              </div>
            </div>

            <div className="grid grid-cols-2 gap-3">
              <div>
                <label className="font-semibold text-slate-700 block mb-1">Active Ingredient</label>
                <Input value={activeIngredient} onChange={(e) => setActiveIngredient(e.target.value)} placeholder="Paracetamol 500mg" required />
              </div>
              <div>
                <label className="font-semibold text-slate-700 block mb-1">Strength</label>
                <Input value={strength} onChange={(e) => setStrength(e.target.value)} placeholder="500mg" required />
              </div>
            </div>

            <div className="grid grid-cols-2 gap-3">
              <div>
                <label className="font-semibold text-slate-700 block mb-1">Dosage Form</label>
                <Input value={dosageForm} onChange={(e) => setDosageForm(e.target.value)} placeholder="Tablet" required />
              </div>
              <div>
                <label className="font-semibold text-slate-700 block mb-1">Initial Barcode</label>
                <Input value={initialBarcode} onChange={(e) => setInitialBarcode(e.target.value)} placeholder="622123456789" />
              </div>
            </div>

            <div className="grid grid-cols-2 gap-3">
              <div>
                <label className="font-semibold text-slate-700 block mb-1">Category</label>
                <select
                  value={categoryId}
                  onChange={(e) => setCategoryId(e.target.value)}
                  className="h-10 w-full rounded-md border border-slate-300 bg-white px-3 text-xs"
                  required
                >
                  <option value="">Select Category</option>
                  {(categories || []).map((c) => (
                    <option key={c.id} value={c.id}>{c.name}</option>
                  ))}
                </select>
              </div>
              <div>
                <label className="font-semibold text-slate-700 block mb-1">Manufacturer</label>
                <select
                  value={manufacturerId}
                  onChange={(e) => setManufacturerId(e.target.value)}
                  className="h-10 w-full rounded-md border border-slate-300 bg-white px-3 text-xs"
                  required
                >
                  <option value="">Select Manufacturer</option>
                  {(manufacturers || []).map((m) => (
                    <option key={m.id} value={m.id}>{m.name}</option>
                  ))}
                </select>
              </div>
            </div>

            <div className="flex items-center gap-2 pt-2">
              <input
                type="checkbox"
                id="prescription"
                checked={requiresPrescription}
                onChange={(e) => setRequiresPrescription(e.target.checked)}
                className="rounded text-blue-600"
              />
              <label htmlFor="prescription" className="text-slate-700 font-medium">
                Requires Prescription (Rx)
              </label>
            </div>

            <DialogFooter className="pt-2">
              <Button type="button" variant="outline" onClick={() => setIsCreateOpen(false)}>Cancel</Button>
              <Button type="submit" disabled={createMutation.isPending}>
                {createMutation.isPending ? 'Saving...' : 'Save to Catalog'}
              </Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>

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
