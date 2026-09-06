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
import { useTranslation } from '@/i18n/useTranslation';

export function MedicineDetailPage() {
  const { t } = useTranslation();
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
        <LoadingSpinner size="lg" text={t('common.loading')} />
      </div>
    );
  }

  if (medError || !medicine) {
    return (
      <div className="mx-auto max-w-4xl px-4 py-12">
        <ErrorState error={medError} title={t('feedback.errorTitle')} />
      </div>
    );
  }

  return (
    <div className="mx-auto max-w-7xl px-4 py-8 sm:px-6 lg:px-8">
      {/* Back link */}
      <div className="mb-6">
        <Button variant="ghost" size="sm" asChild className="gap-1.5 text-slate-600 dark:text-slate-400">
          <Link to="/customer/search">
            <ArrowLeft className="h-4 w-4 rtl:rotate-180" />
            {t('customer.backToSearch')}
          </Link>
        </Button>
      </div>

      {/* Medicine Info Header Card */}
      <Card className="mb-8 border-slate-200 dark:border-slate-800">
        <CardContent className="p-6">
          <div className="flex flex-col md:flex-row justify-between gap-4">
            <div className="space-y-3">
              <div className="flex items-center gap-3">
                <div className="flex h-12 w-12 items-center justify-center rounded-xl bg-blue-100 dark:bg-blue-950/50 text-blue-700 dark:text-blue-300">
                  <Pill className="h-6 w-6" />
                </div>
                <div>
                  <h1 className="text-2xl font-bold text-slate-900 dark:text-slate-100">{medicine.brandName}</h1>
                  <p className="text-sm font-medium text-slate-500 dark:text-slate-400">{medicine.genericName}</p>
                </div>
              </div>

              <div className="flex flex-wrap gap-2 text-xs">
                <Badge variant="secondary">{t('admin.categoryLabel')}: {medicine.categoryName}</Badge>
                <Badge variant="secondary">{t('admin.dosageFormLabel')}: {medicine.dosageForm}</Badge>
                <Badge variant="secondary">{t('admin.strengthLabel')}: {medicine.strength}</Badge>
                {medicine.requiresPrescription ? (
                  <Badge variant="destructive">{t('admin.prescriptionRequired')}</Badge>
                ) : (
                  <Badge variant="success">{t('admin.otc')}</Badge>
                )}
              </div>
            </div>

            <div className="text-start md:text-end space-y-1 text-xs text-slate-500 dark:text-slate-400 border-t md:border-t-0 pt-3 md:pt-0 dark:border-slate-800">
              <p>{t('admin.mfgLabel')}: <span className="font-semibold text-slate-700 dark:text-slate-300">{medicine.manufacturerName}</span></p>
              <p>{t('admin.activeIngredientLabel')}: <span className="font-semibold text-slate-700 dark:text-slate-300">{medicine.activeIngredient}</span></p>
              {medicine.barcodes?.length > 0 && (
                <p>{t('admin.barcodeLabel')}: <span className="font-mono text-slate-700 dark:text-slate-300">{medicine.barcodes[0].barcode}</span></p>
              )}
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Availability by Branch */}
      <div>
        <h2 className="text-xl font-bold text-slate-900 dark:text-slate-100 mb-4">{t('customer.medicineDetailTitle')}</h2>

        {isAvailLoading ? (
          <div className="py-12">
            <LoadingSpinner size="md" text={t('common.loading')} />
          </div>
        ) : availError ? (
          <ErrorState error={availError} onRetry={() => refetch()} />
        ) : !availability || availability.items.length === 0 ? (
          <EmptyState
            icon={Building2}
            title={t('customer.noAvailabilityTitle')}
            description={t('customer.noAvailabilityDesc')}
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
