import { useState } from 'react';
import { useParams, Link } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { ArrowLeft, Pill, Building2 } from 'lucide-react';
import { customerApi } from '../api/customerApi';
import { AvailabilityCard } from '../components/AvailabilityCard';
import { ReservationDialog } from '../components/ReservationDialog';
import { MedicineAvailabilityDto } from '@/types/availability';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Card, CardContent } from '@/components/ui/card';
import { LoadingSpinner } from '@/components/feedback/LoadingSpinner';
import { ErrorState } from '@/components/feedback/ErrorState';
import { EmptyState } from '@/components/feedback/EmptyState';

export function MedicineDetailPage() {
  const { id } = useParams<{ id: string }>();
  const [selectedItem, setSelectedItem] = useState<MedicineAvailabilityDto | null>(null);
  const [isDialogOpen, setIsDialogOpen] = useState(false);

  // 1. Fetch medicine catalog details
  const {
    data: medicine,
    isLoading: isMedLoading,
    error: medError,
  } = useQuery({
    queryKey: ['medicine-detail', id],
    queryFn: () => (id ? customerApi.getMedicineById(id) : Promise.reject('No ID')),
    enabled: !!id,
  });

  // 2. Fetch branches with availability for this medicine
  const {
    data: availability,
    isLoading: isAvailLoading,
    error: availError,
    refetch,
  } = useQuery({
    queryKey: ['medicine-availability', id],
    queryFn: () => (id ? customerApi.searchMedicineAvailability(id, { page: 1, pageSize: 50 }) : Promise.reject('No ID')),
    enabled: !!id,
  });

  if (isMedLoading) {
    return (
      <div className="py-24">
        <LoadingSpinner size="lg" text="Loading medicine details..." />
      </div>
    );
  }

  if (medError || !medicine) {
    return (
      <div className="mx-auto max-w-4xl px-4 py-12">
        <ErrorState error={medError} title="Medicine Not Found" />
      </div>
    );
  }

  return (
    <div className="mx-auto max-w-7xl px-4 py-8 sm:px-6 lg:px-8">
      {/* Back link */}
      <div className="mb-6">
        <Button variant="ghost" size="sm" asChild className="gap-1.5 text-slate-600">
          <Link to="/customer/search">
            <ArrowLeft className="h-4 w-4" />
            Back to Medicine Search
          </Link>
        </Button>
      </div>

      {/* Medicine Info Header Card */}
      <Card className="mb-8 border-slate-200">
        <CardContent className="p-6">
          <div className="flex flex-col md:flex-row justify-between gap-4">
            <div className="space-y-3">
              <div className="flex items-center gap-3">
                <div className="flex h-12 w-12 items-center justify-center rounded-xl bg-blue-100 text-blue-700">
                  <Pill className="h-6 w-6" />
                </div>
                <div>
                  <h1 className="text-2xl font-bold text-slate-900">{medicine.brandName}</h1>
                  <p className="text-sm font-medium text-slate-500">{medicine.genericName}</p>
                </div>
              </div>

              <div className="flex flex-wrap gap-2 text-xs">
                <Badge variant="secondary">Category: {medicine.categoryName}</Badge>
                <Badge variant="secondary">Form: {medicine.dosageForm}</Badge>
                <Badge variant="secondary">Strength: {medicine.strength}</Badge>
                {medicine.requiresPrescription && (
                  <Badge variant="destructive">Prescription Required</Badge>
                )}
              </div>
            </div>

            <div className="text-left md:text-right space-y-1 text-xs text-slate-500 border-t md:border-t-0 pt-3 md:pt-0">
              <p>Manufacturer: <span className="font-semibold text-slate-700">{medicine.manufacturerName}</span></p>
              <p>Active Ingredient: <span className="font-semibold text-slate-700">{medicine.activeIngredient}</span></p>
              {medicine.barcodes?.length > 0 && (
                <p>Primary Barcode: <span className="font-mono text-slate-700">{medicine.barcodes[0].barcode}</span></p>
              )}
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Availability by Branch */}
      <div>
        <h2 className="text-xl font-bold text-slate-900 mb-4">Availability Across Branches</h2>

        {isAvailLoading ? (
          <div className="py-12">
            <LoadingSpinner size="md" text="Finding nearby branches with stock..." />
          </div>
        ) : availError ? (
          <ErrorState error={availError} onRetry={() => refetch()} />
        ) : !availability || availability.items.length === 0 ? (
          <EmptyState
            icon={Building2}
            title="Out of Stock Everywhere"
            description="Currently no active pharmacy branches report stock for this medicine."
          />
        ) : (
          <div className="grid grid-cols-1 gap-4">
            {availability.items.map((item) => (
              <AvailabilityCard
                key={`${item.inventoryId}-${item.branchId}`}
                item={item}
                onReserve={(it) => {
                  setSelectedItem(it);
                  setIsDialogOpen(true);
                }}
              />
            ))}
          </div>
        )}
      </div>

      <ReservationDialog
        item={selectedItem}
        isOpen={isDialogOpen}
        onClose={() => setIsDialogOpen(false)}
        onSuccess={() => refetch()}
      />
    </div>
  );
}
