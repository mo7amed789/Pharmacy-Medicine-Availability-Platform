import { useState, useEffect } from 'react';
import { useQuery } from '@tanstack/react-query';
import { SearchX, CheckCircle2, MapPin, Globe } from 'lucide-react';
import { customerApi } from '../api/customerApi';
import { MedicineSearchBar } from '../components/MedicineSearchBar';
import { AvailabilityCard } from '../components/AvailabilityCard';
import { ReservationDialog } from '../components/ReservationDialog';
import { MedicineAvailabilityDto } from '@/types/availability';
import { LoadingSpinner } from '@/components/feedback/LoadingSpinner';
import { ErrorState } from '@/components/feedback/ErrorState';
import { EmptyState } from '@/components/feedback/EmptyState';
import { Alert, AlertDescription } from '@/components/ui/alert';
import { Button } from '@/components/ui/button';

// Cairo Downtown coordinates for quick 1-click testing
const CAIRO_DEMO_COORDS = { latitude: 30.0444, longitude: 31.2357 };

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
        setCoords(null);
        setIsLocating(false);
      },
      { timeout: 10000, enableHighAccuracy: false }
    );
  };

  const handleUseDemoLocation = () => {
    setCoords(CAIRO_DEMO_COORDS);
    setLocationError(null);
  };

  const handleClearLocation = () => {
    setCoords(null);
    setLocationError(null);
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
    setSuccessNotice(`Hold placed successfully! Your order is reserved for 60 minutes (1 hour).`);
    refetch();
    setTimeout(() => setSuccessNotice(null), 6000);
  };

  return (
    <div className="mx-auto max-w-7xl px-4 py-8 sm:px-6 lg:px-8">
      {/* Banner / Hero */}
      <div className="mb-6 space-y-1">
        <h1 className="text-3xl font-extrabold tracking-tight text-slate-900 dark:text-slate-100">
          Find Medicine Availability Nearby
        </h1>
        <p className="text-sm text-slate-600 dark:text-slate-400">
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
        <Alert variant="warning" className="mb-4 flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3 p-3.5">
          <div className="flex-1">
            <AlertDescription className="text-xs font-medium text-amber-900">
              {locationError}
              <span className="block mt-0.5 text-amber-700 font-normal">
                Don't worry — you can browse nationwide or use the Cairo Demo GPS coordinates to test nearby availability.
              </span>
            </AlertDescription>
          </div>
          <div className="flex items-center gap-2 shrink-0">
            <Button
              type="button"
              variant="outline"
              size="sm"
              className="h-8 text-xs bg-white text-slate-800 border-amber-300 hover:bg-amber-50"
              onClick={handleUseDemoLocation}
            >
              <MapPin className="h-3 w-3 mr-1 text-blue-600" />
              Use Cairo Demo GPS
            </Button>
            <Button
              type="button"
              variant="ghost"
              size="sm"
              className="h-8 text-xs text-amber-900 hover:bg-amber-100/50"
              onClick={() => setLocationError(null)}
            >
              Dismiss
            </Button>
          </div>
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
        onClearLocation={handleClearLocation}
        onUseDemoLocation={handleUseDemoLocation}
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
          <div className="space-y-4">
            <EmptyState
              icon={SearchX}
              title="No Medicine Inventory Found"
              description={
                coords
                  ? `No active pharmacy inventory matched "${searchTerm || 'all medicines'}" within ${radiusKm}km of your current location. Cairo branches may be outside this radius.`
                  : `No active pharmacy inventory matched "${searchTerm}". Try choosing one of the popular test medicines or clear your search to see all available inventory.`
              }
              actionLabel={coords ? "Switch to Cairo Demo GPS" : "Reset & Show All"}
              onAction={() => {
                if (coords) {
                  handleUseDemoLocation();
                } else {
                  setSearchTerm('');
                  setDebouncedSearch('');
                  setCoords(null);
                }
              }}
            />
            {coords && (
              <div className="flex justify-center gap-3">
                <Button
                  type="button"
                  variant="outline"
                  size="sm"
                  onClick={handleClearLocation}
                  className="text-xs"
                >
                  <Globe className="h-3.5 w-3.5 mr-1.5 text-slate-500" />
                  Show Nationwide (Disable GPS Filter)
                </Button>
              </div>
            )}
          </div>
        ) : (
          <div className="space-y-4">
            <div className="flex items-center justify-between text-xs text-slate-500 dark:text-slate-400 font-semibold px-1">
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
