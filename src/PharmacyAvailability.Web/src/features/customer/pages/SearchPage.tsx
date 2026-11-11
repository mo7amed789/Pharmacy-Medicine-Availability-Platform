import { useState, useEffect } from 'react';
import { useQuery } from '@tanstack/react-query';
import { SearchX, MapPin, Globe } from 'lucide-react';
import { customerApi } from '../api/customerApi';
import { MedicineSearchBar } from '../components/MedicineSearchBar';
import { AvailabilityCard } from '../components/AvailabilityCard';
import { SkeletonCard } from '@/components/feedback/SkeletonCard';
import { ErrorState } from '@/components/feedback/ErrorState';
import { EmptyState } from '@/components/feedback/EmptyState';
import { Alert, AlertDescription } from '@/components/ui/alert';
import { Button } from '@/components/ui/button';
import { useTranslation } from '@/i18n/useTranslation';

// Cairo Downtown coordinates for quick 1-click testing
const CAIRO_DEMO_COORDS = { latitude: 30.0444, longitude: 31.2357 };

export function SearchPage() {
  const { t, language } = useTranslation();
  const [searchTerm, setSearchTerm] = useState('');
  const [debouncedSearch, setDebouncedSearch] = useState('');
  const [sortBy, setSortBy] = useState('distance');
  const [radiusKm, setRadiusKm] = useState(15);
  const [coords, setCoords] = useState<{ latitude: number; longitude: number } | null>(null);
  const [isLocating, setIsLocating] = useState(false);
  const [locationError, setLocationError] = useState<string | null>(null);

  // Debounce search input by 350ms
  useEffect(() => {
    const timer = setTimeout(() => {
      setDebouncedSearch(searchTerm);
    }, 350);
    return () => clearTimeout(timer);
  }, [searchTerm]);

  const handleRequestLocation = () => {
    if (!navigator.geolocation) {
      setLocationError(language === 'ar' ? 'متصفحك لا يدعم خدمة تحديد الموقع الجغرافي.' : 'Geolocation is not supported by your browser.');
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
        setLocationError(language === 'ar' ? `تعذر تحديد موقعك: ${error.message}` : `Location request failed: ${error.message}`);
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

  return (
    <div className="mx-auto max-w-7xl px-4 py-8 sm:px-6 lg:px-8">
      {/* Banner / Hero */}
      <div className="mb-6 space-y-1">
        <h1 className="text-3xl font-extrabold tracking-tight text-slate-900 dark:text-slate-100">
          {t('customer.heroTitle')}
        </h1>
        <p className="text-sm text-slate-600 dark:text-slate-400">
          {t('customer.heroSubtitle')}
        </p>
      </div>

      {locationError && (
        <Alert variant="warning" className="mb-4 flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3 p-3.5">
          <div className="flex-1">
            <AlertDescription className="text-xs font-medium text-amber-900 dark:text-amber-300">
              {locationError}
              <span className="block mt-0.5 text-amber-700 dark:text-amber-400 font-normal">
                {language === 'ar'
                  ? 'يمكنك التصفح على مستوى الجمهورية أو تجربة إحداثيات القاهرة للتأكد من الميزات التفاعلية.'
                  : "Don't worry — you can browse nationwide or use the Cairo Demo GPS coordinates to test nearby availability."}
              </span>
            </AlertDescription>
          </div>
          <div className="flex items-center gap-2 shrink-0">
            <Button
              type="button"
              variant="outline"
              size="sm"
              className="h-8 text-xs bg-white dark:bg-slate-800 text-slate-800 dark:text-slate-200 border-amber-300 hover:bg-amber-50"
              onClick={handleUseDemoLocation}
            >
              <MapPin className="h-3 w-3 me-1 text-blue-600" />
              {t('customer.cairoDemoGps')}
            </Button>
            <Button
              type="button"
              variant="ghost"
              size="sm"
              className="h-8 text-xs text-amber-900 dark:text-amber-300 hover:bg-amber-100/50"
              onClick={() => setLocationError(null)}
            >
              {language === 'ar' ? 'تجاهل' : 'Dismiss'}
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
          <div className="space-y-4">
            <div className="flex items-center justify-between text-xs text-slate-400 dark:text-slate-500 font-medium px-1">
              <span>{language === 'ar' ? 'جارٍ البحث عن توفر الأدوية في الصيدليات القريبة...' : 'Searching medicine availability across pharmacies...'}</span>
            </div>
            <SkeletonCard count={3} />
          </div>
        ) : isError ? (
          <ErrorState error={error} onRetry={() => refetch()} />
        ) : !data || data.items.length === 0 ? (
          <div className="space-y-4">
            <EmptyState
              icon={SearchX}
              title={language === 'ar' ? 'لم يتم العثور على أدوية متوفرة' : 'No Medicine Inventory Found'}
              description={
                coords
                  ? (language === 'ar'
                      ? `لم نجد أدوية متوفرة مطابقة لـ "${searchTerm || 'جميع الأدوية'}" في نطاق ${radiusKm} كم من موقعك الحالي.`
                      : `No active pharmacy inventory matched "${searchTerm || 'all medicines'}" within ${radiusKm}km of your current location. Cairo branches may be outside this radius.`)
                  : (language === 'ar'
                      ? `لم نجد نتائج مطابقة لـ "${searchTerm}". جرب اختيار أحد الأدوية المقترحة أو مسح البحث لعرض جميع الأدوية.`
                      : `No active pharmacy inventory matched "${searchTerm}". Try choosing one of the popular test medicines or clear your search to see all available inventory.`)
              }
              actionLabel={coords ? (language === 'ar' ? 'التبديل إلى موقع القاهرة (Demo)' : 'Switch to Cairo Demo GPS') : (language === 'ar' ? 'عرض جميع الأدوية' : 'Reset & Show All')}
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
                  <Globe className="h-3.5 w-3.5 me-1.5 text-slate-500" />
                  {language === 'ar' ? 'عرض على مستوى الجمهورية (دون تقييد بالموقع)' : 'Show Nationwide (Disable GPS Filter)'}
                </Button>
              </div>
            )}
          </div>
        ) : (
          <div className="space-y-4">
            <div className="flex items-center justify-between text-xs text-slate-500 dark:text-slate-400 font-semibold px-1">
              <span>
                {language === 'ar' ? `تم العثور على ${data.totalCount} صيدلية يتوفر بها الدواء` : `Found ${data.totalCount} available medicine offerings`}
              </span>
              <span>
                {language === 'ar' ? `صفحة ${data.page} من ${data.totalPages}` : `Page ${data.page} of ${data.totalPages}`}
              </span>
            </div>

            <div className="grid grid-cols-1 gap-4">
              {data.items.map((item) => (
                <AvailabilityCard
                  key={`${item.inventoryId}-${item.branchId}`}
                  item={item}
                />
              ))}
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
