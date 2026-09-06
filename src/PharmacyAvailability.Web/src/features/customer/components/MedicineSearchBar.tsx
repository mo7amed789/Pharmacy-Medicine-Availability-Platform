import { Search, Navigation, SlidersHorizontal, X, MapPin } from 'lucide-react';
import { Input } from '@/components/ui/input';
import { Button } from '@/components/ui/button';

interface MedicineSearchBarProps {
  searchTerm: string;
  onSearchChange: (value: string) => void;
  sortBy: string;
  onSortChange: (value: string) => void;
  radiusKm: number;
  onRadiusChange: (value: number) => void;
  hasLocation: boolean;
  onRequestLocation: () => void;
  onClearLocation?: () => void;
  onUseDemoLocation?: () => void;
  isLocating: boolean;
}

const POPULAR_MEDICINES = [
  'Panadol',
  'Augmentin',
  'Congestal',
  'Cataflam',
  'Nexium',
  'Brufen',
  'All Medicines',
];

export function MedicineSearchBar({
  searchTerm,
  onSearchChange,
  sortBy,
  onSortChange,
  radiusKm,
  onRadiusChange,
  hasLocation,
  onRequestLocation,
  onClearLocation,
  onUseDemoLocation,
  isLocating,
}: MedicineSearchBarProps) {
  return (
    <div className="rounded-xl border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 p-4 shadow-xs space-y-3">
      <div className="flex flex-col md:flex-row gap-3">
        {/* Main Search Input */}
        <div className="relative flex-1">
          <Search className="absolute left-3.5 top-3 h-4 w-4 text-slate-400 dark:text-slate-500" />
          <Input
            type="text"
            placeholder="Search medicine brand, active ingredient, barcode, or generic name..."
            value={searchTerm}
            onChange={(e) => onSearchChange(e.target.value)}
            className="pl-10 h-11 text-sm bg-slate-50/50 dark:bg-slate-800/50 border-slate-200 dark:border-slate-700 focus:bg-white dark:focus:bg-slate-800"
          />
          {searchTerm && (
            <button
              type="button"
              onClick={() => onSearchChange('')}
              className="absolute right-3 top-3 text-slate-400 hover:text-slate-600 dark:hover:text-slate-200"
              title="Clear search"
            >
              <X className="h-5 w-5" />
            </button>
          )}
        </div>

        {/* GPS Geolocation Controls */}
        <div className="flex items-center gap-2">
          {hasLocation ? (
            <div className="flex items-center rounded-md border border-blue-200 dark:border-blue-900/60 bg-blue-50 dark:bg-blue-950/40 p-1">
              <Button
                type="button"
                variant="default"
                size="sm"
                className="h-9 flex items-center gap-1.5 text-xs bg-blue-600 hover:bg-blue-700"
                onClick={onRequestLocation}
                disabled={isLocating}
                title="Refresh current location"
              >
                <Navigation className={`h-3.5 w-3.5 ${isLocating ? 'animate-spin' : ''}`} />
                <span>{isLocating ? 'Locating...' : 'Nearby Active'}</span>
              </Button>
              {onClearLocation && (
                <Button
                  type="button"
                  variant="ghost"
                  size="sm"
                  className="h-9 px-2 text-xs text-blue-700 dark:text-blue-300 hover:bg-blue-100 dark:hover:bg-blue-900/40"
                  onClick={onClearLocation}
                  title="Remove location filter (Show nationwide)"
                >
                  <X className="h-3.5 w-3.5 mr-1" />
                  Clear GPS
                </Button>
              )}
            </div>
          ) : (
            <div className="flex items-center gap-2">
              <Button
                type="button"
                variant="outline"
                className="h-11 flex items-center gap-2 whitespace-nowrap text-xs"
                onClick={onRequestLocation}
                disabled={isLocating}
              >
                <Navigation className={`h-4 w-4 ${isLocating ? 'animate-spin' : ''}`} />
                <span>{isLocating ? 'Locating...' : 'Use My GPS'}</span>
              </Button>
              {onUseDemoLocation && (
                <Button
                  type="button"
                  variant="secondary"
                  className="h-11 flex items-center gap-1.5 whitespace-nowrap text-xs text-slate-700 dark:text-slate-200 bg-slate-100 dark:bg-slate-800 hover:bg-slate-200 dark:hover:bg-slate-700 border border-slate-200 dark:border-slate-700"
                  onClick={onUseDemoLocation}
                  title="Test with Cairo coordinates (Downtown Cairo)"
                >
                  <MapPin className="h-3.5 w-3.5 text-blue-600 dark:text-blue-400" />
                  <span>Cairo Demo GPS</span>
                </Button>
              )}
            </div>
          )}
        </div>
      </div>

      {/* Quick Test Chips */}
      <div className="flex flex-wrap items-center gap-1.5 pt-1 text-xs">
        <span className="text-slate-400 dark:text-slate-500 font-medium mr-1">Quick Test:</span>
        {POPULAR_MEDICINES.map((tag) => {
          const isAll = tag === 'All Medicines';
          const isSelected = (isAll && !searchTerm) || searchTerm.toLowerCase() === tag.toLowerCase();

          return (
            <button
              key={tag}
              type="button"
              onClick={() => onSearchChange(isAll ? '' : tag)}
              className={`px-2.5 py-1 rounded-full text-xs font-medium transition-colors ${
                isSelected
                  ? 'bg-blue-600 text-white shadow-xs'
                  : 'bg-slate-100 dark:bg-slate-800 text-slate-700 dark:text-slate-300 hover:bg-slate-200 dark:hover:bg-slate-700'
              }`}
            >
              {tag}
            </button>
          );
        })}
      </div>

      {/* Secondary Controls: Sort and Radius */}
      <div className="flex flex-wrap items-center justify-between gap-3 pt-2 border-t border-slate-100 dark:border-slate-800 text-xs">
        <div className="flex items-center gap-2 text-slate-600 dark:text-slate-400">
          <SlidersHorizontal className="h-3.5 w-3.5 text-slate-400 dark:text-slate-500" />
          <span className="font-semibold">Sort By:</span>
          <select
            value={sortBy}
            onChange={(e) => onSortChange(e.target.value)}
            className="rounded border border-slate-200 dark:border-slate-700 bg-white dark:bg-slate-800 px-2 py-1 text-slate-800 dark:text-slate-200 focus:outline-none focus:ring-1 focus:ring-blue-500"
          >
            <option value="distance">Nearest Distance</option>
            <option value="price">Lowest Price</option>
            <option value="quantity">Highest Stock</option>
            <option value="freshness">Freshest Inventory</option>
          </select>
        </div>

        {hasLocation ? (
          <div className="flex items-center gap-2 text-slate-600 dark:text-slate-400">
            <span className="font-semibold">Radius:</span>
            <select
              value={radiusKm}
              onChange={(e) => onRadiusChange(Number(e.target.value))}
              className="rounded border border-slate-200 dark:border-slate-700 bg-white dark:bg-slate-800 px-2 py-1 text-slate-800 dark:text-slate-200 focus:outline-none focus:ring-1 focus:ring-blue-500"
            >
              <option value="5">5 km</option>
              <option value="10">10 km</option>
              <option value="15">15 km</option>
              <option value="25">25 km</option>
              <option value="50">50 km</option>
            </select>
          </div>
        ) : (
          <span className="text-[11px] text-slate-400 dark:text-slate-500 italic">
            Showing all pharmacies nationwide (no radius limit)
          </span>
        )}
      </div>
    </div>
  );
}
