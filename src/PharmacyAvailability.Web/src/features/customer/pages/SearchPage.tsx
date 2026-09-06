import { useState, useEffect } from 'react';
import { useQuery } from '@tanstack/react-query';
import { SearchX, CheckCircle2 } from 'lucide-react';
import { customerApi } from '../api/customerApi';
import { MedicineSearchBar } from '../components/MedicineSearchBar';
import { AvailabilityCard } from '../components/AvailabilityCard';
import { ReservationDialog } from '../components/ReservationDialog';
import { MedicineAvailabilityDto } from '@/types/availability';
import { LoadingSpinner } from '@/components/feedback/LoadingSpinner';
import { ErrorState } from '@/components/feedback/ErrorState';
import { EmptyState } from '@/components/feedback/EmptyState';
import { Alert, AlertDescription } from '@/components/ui/alert';

export function SearchPage() {
  const [searchTerm, setSearchTerm] = useState('');
  const [debouncedSearch, setDebouncedSearch] = useState('');
  const [sortBy, setSortBy] = useState('distance');
  const [radiusKm, setRadiusKm] = useState(15);
  const [coords, setCoords] = useState<{ latitude: number; longitude: number } | null>(null);
  const [isLocating, setIsLocating] = useState(false);
  const [locationError, setLocationError] = useState<string | null>(null);

  // Active reservation dialog state
  const [selectedItem, setSelectedItem] = useState<MedicineAvailabilityDto | null>(null);
  const [isDialogOpen, setIsDialogOpen] = useState(false);
  const [successNotice, setSuccessNotice] = useState<string | null>(null);

  // Debounce search input by 350ms
  useEffect(() => {
    const timer = setTimeout(() => {
      setDebouncedSearch(searchTerm);
    }, 350);
    return () => clearTimeout(timer);
  }, [searchTerm]);

  const handleRequestLocation = () => {
    if (!navigator.geolocation) {
      setLocationError('Geolocation is not supported by your browser.');
      return;
    }

    setIsLocating(true);
    setLocationError(null);

    navigator.geolocation.getCurrentPosition(
      (position) => {
        setCoords({
          latitude: position.coords.latitude,
          longitude: position.coords.longitude,
        });
        setIsLocating(false);
      },
      (error) => {
        setLocationError(`Location request failed: ${error.message}`);
        setIsLocating(false);
      },
      { timeout: 10000, enableHighAccuracy: false }
    );
  };

  const { data, isLoading, isError, error, refetch } = useQuery({
    queryKey: ['availability-search', debouncedSearch, coords, sortBy, radiusKm],
    queryFn: () =>
      customerApi.searchAvailability({
        q: debouncedSearch || undefined,
        latitude: coords?.latitude,
        longitude: coords?.longitude,
        radiusKm: coords ? radiusKm : undefined,
        sortBy: sortBy,
        page: 1,
        pageSize: 30,
      }),
  });

  const handleOpenReserve = (item: MedicineAvailabilityDto) => {
    setSelectedItem(item);
    setIsDialogOpen(true);
  };

  const handleReservationSuccess = () => {
    setSuccessNotice(`Hold placed successfully! Your order is reserved for 120 minutes.`);
    refetch();
    setTimeout(() => setSuccessNotice(null), 6000);
  };

  return (
    <div className="mx-auto max-w-7xl px-4 py-8 sm:px-6 lg:px-8">
      {/* Banner / Hero */}
      <div className="mb-6 space-y-1">
        <h1 className="text-3xl font-extrabold tracking-tight text-slate-900">
          Find Medicine Availability Nearby
        </h1>
        <p className="text-sm text-slate-600">
          Locate verified pharmacy stock, compare prices, and place hold reservations instantly.
        </p>
      </div>

      {/* Success Notification */}
      {successNotice && (
        <Alert variant="success" className="mb-6">
          <CheckCircle2 className="h-4 w-4" />
          <AlertDescription className="font-semibold">{successNotice}</AlertDescription>
        </Alert>
      )}

      {locationError && (
        <Alert variant="warning" className="mb-4">
          <AlertDescription>{locationError}</AlertDescription>
        </Alert>
      )}

      {/* Search Header Bar */}
      <MedicineSearchBar
        searchTerm={searchTerm}
        onSearchChange={setSearchTerm}
        sortBy={sortBy}
        onSortChange={setSortBy}
        radiusKm={radiusKm}
        onRadiusChange={setRadiusKm}
        hasLocation={!!coords}
        onRequestLocation={handleRequestLocation}
        isLocating={isLocating}
      />

      {/* Results Section */}
      <div className="mt-8 space-y-4">
        {isLoading ? (
          <div className="py-16">
            <LoadingSpinner size="lg" text="Searching medicine availability across pharmacies..." />
          </div>
        ) : isError ? (
          <ErrorState error={error} onRetry={() => refetch()} />
        ) : !data || data.items.length === 0 ? (
          <EmptyState
            icon={SearchX}
            title="No Medicine Inventory Found"
            description="No active pharmacy inventory matched your query or location criteria. Try broadening your search or adjusting your location."
            actionLabel="Reset Filters"
            onAction={() => {
              setSearchTerm('');
              setDebouncedSearch('');
              setCoords(null);
            }}
          />
        ) : (
          <div className="space-y-4">
            <div className="flex items-center justify-between text-xs text-slate-500 font-semibold px-1">
              <span>Found {data.totalCount} available medicine offerings</span>
              <span>Page {data.page} of {data.totalPages}</span>
            </div>

            <div className="grid grid-cols-1 gap-4">
              {data.items.map((item) => (
                <AvailabilityCard
                  key={`${item.inventoryId}-${item.branchId}`}
                  item={item}
                  onReserve={handleOpenReserve}
                />
              ))}
            </div>
          </div>
        )}
      </div>

      {/* Reservation Hold Modal */}
      <ReservationDialog
        item={selectedItem}
        isOpen={isDialogOpen}
        onClose={() => setIsDialogOpen(false)}
        onSuccess={handleReservationSuccess}
      />
    </div>
  );
}
