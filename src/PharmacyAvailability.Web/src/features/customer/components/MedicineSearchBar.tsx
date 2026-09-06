import { Search, Navigation, SlidersHorizontal } from 'lucide-react';
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
  isLocating: boolean;
}

export function MedicineSearchBar({
  searchTerm,
  onSearchChange,
  sortBy,
  onSortChange,
  radiusKm,
  onRadiusChange,
  hasLocation,
  onRequestLocation,
  isLocating,
}: MedicineSearchBarProps) {
  return (
    <div className="rounded-xl border border-slate-200 bg-white p-4 shadow-xs">
      <div className="flex flex-col md:flex-row gap-3">
        {/* Main Search Input */}
        <div className="relative flex-1">
          <Search className="absolute left-3.5 top-3 h-4 w-4 text-slate-400" />
          <Input
            type="text"
            placeholder="Search medicine brand, active ingredient, barcode, or generic name..."
            value={searchTerm}
            onChange={(e) => onSearchChange(e.target.value)}
            className="pl-10 h-11 text-sm bg-slate-50/50 border-slate-200 focus:bg-white"
          />
        </div>

        {/* GPS Geolocation Button */}
        <Button
          type="button"
          variant={hasLocation ? 'default' : 'outline'}
          className="h-11 flex items-center gap-2 whitespace-nowrap"
          onClick={onRequestLocation}
          disabled={isLocating}
        >
          <Navigation className={`h-4 w-4 ${isLocating ? 'animate-spin' : ''}`} />
          <span>{isLocating ? 'Locating...' : hasLocation ? 'Nearby Active' : 'Use Location'}</span>
        </Button>
      </div>

      {/* Secondary Controls: Sort and Radius */}
      <div className="mt-3 flex flex-wrap items-center justify-between gap-3 pt-3 border-t border-slate-100 text-xs">
        <div className="flex items-center gap-2 text-slate-600">
          <SlidersHorizontal className="h-3.5 w-3.5 text-slate-400" />
          <span className="font-semibold">Sort By:</span>
          <select
            value={sortBy}
            onChange={(e) => onSortChange(e.target.value)}
            className="rounded border border-slate-200 bg-white px-2 py-1 text-slate-800 focus:outline-none focus:ring-1 focus:ring-blue-500"
          >
            <option value="distance">Nearest Distance</option>
            <option value="price">Lowest Price</option>
            <option value="quantity">Highest Stock</option>
            <option value="freshness">Freshest Inventory</option>
          </select>
        </div>

        {hasLocation && (
          <div className="flex items-center gap-2 text-slate-600">
            <span className="font-semibold">Radius:</span>
            <select
              value={radiusKm}
              onChange={(e) => onRadiusChange(Number(e.target.value))}
              className="rounded border border-slate-200 bg-white px-2 py-1 text-slate-800 focus:outline-none focus:ring-1 focus:ring-blue-500"
            >
              <option value="5">5 km</option>
              <option value="10">10 km</option>
              <option value="15">15 km</option>
              <option value="25">25 km</option>
              <option value="50">50 km</option>
            </select>
          </div>
        )}
      </div>
    </div>
  );
}
