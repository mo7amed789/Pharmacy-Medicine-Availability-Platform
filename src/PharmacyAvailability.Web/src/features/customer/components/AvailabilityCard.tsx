import { MapPin, Phone, Clock, Sparkles } from 'lucide-react';
import { MedicineAvailabilityDto } from '@/types/availability';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Card, CardContent } from '@/components/ui/card';
import { formatCurrency, formatDistance, formatRelativeTime } from '@/lib/formatters';

interface AvailabilityCardProps {
  item: MedicineAvailabilityDto;
  onReserve: (item: MedicineAvailabilityDto) => void;
}

export function AvailabilityCard({ item, onReserve }: AvailabilityCardProps) {
  const isAvailable = item.availableQuantity > 0;

  return (
    <Card className="overflow-hidden border-slate-200 transition-all hover:shadow-md">
      <CardContent className="p-5">
        <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
          {/* Main Info */}
          <div className="space-y-2 flex-1">
            <div className="flex flex-wrap items-center gap-2">
              <h3 className="text-lg font-bold text-slate-900">{item.medicineBrandName}</h3>
              <Badge variant={isAvailable ? 'success' : 'destructive'} className="text-xs">
                {isAvailable ? `${item.availableQuantity} in stock` : 'Out of Stock'}
              </Badge>
              {item.freshness === 'Realtime' && (
                <Badge variant="outline" className="text-[10px] text-blue-700 border-blue-200 bg-blue-50/50 flex items-center gap-1">
                  <Sparkles className="h-3 w-3" />
                  Live Freshness
                </Badge>
              )}
            </div>

            <p className="text-xs text-slate-500 font-medium">
              Generic: <span className="text-slate-700">{item.medicineGenericName}</span> • Form: {item.dosageForm} • Strength: {item.strength}
            </p>

            {/* Branch and Location */}
            <div className="flex flex-wrap items-center gap-y-1 gap-x-4 pt-1 text-xs text-slate-600">
              <div className="flex items-center gap-1 font-semibold text-slate-800">
                <span>{item.pharmacyName}</span> — <span>{item.branchName}</span>
              </div>
              <div className="flex items-center gap-1 text-slate-500">
                <MapPin className="h-3.5 w-3.5 text-slate-400" />
                <span>{item.branchAddress}, {item.branchCity}</span>
              </div>
              {item.distanceKm != null && (
                <div className="flex items-center gap-1 font-semibold text-blue-600">
                  <span>{formatDistance(item.distanceKm)}</span>
                </div>
              )}
            </div>

            {/* Timings & Contact */}
            <div className="flex flex-wrap items-center gap-x-4 gap-y-1 text-[11px] text-slate-400">
              {item.openingHours && (
                <div className="flex items-center gap-1">
                  <Clock className="h-3 w-3" />
                  <span>Hours: {item.openingHours}</span>
                </div>
              )}
              {item.branchPhone && (
                <div className="flex items-center gap-1">
                  <Phone className="h-3 w-3" />
                  <span>{item.branchPhone}</span>
                </div>
              )}
              <span>Updated {formatRelativeTime(item.lastUpdatedAt)}</span>
            </div>
          </div>

          {/* Pricing & Reservation CTA */}
          <div className="flex md:flex-col items-center md:items-end justify-between md:justify-center border-t md:border-t-0 md:border-l border-slate-100 pt-3 md:pt-0 md:pl-6 gap-3">
            <div className="text-left md:text-right">
              <div className="text-xl font-bold text-slate-900">
                {formatCurrency(item.price, item.currency)}
              </div>
              <p className="text-[11px] text-slate-500">per unit</p>
            </div>

            <Button
              size="sm"
              disabled={!isAvailable}
              onClick={() => onReserve(item)}
              className="px-5 shadow-xs"
            >
              {isAvailable ? 'Reserve Hold' : 'Unavailable'}
            </Button>
          </div>
        </div>
      </CardContent>
    </Card>
  );
}
