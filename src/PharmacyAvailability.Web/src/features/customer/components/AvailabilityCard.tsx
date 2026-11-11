import { MapPin, Phone, Clock, Sparkles } from 'lucide-react';
import { MedicineAvailabilityDto } from '@/types/availability';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Card, CardContent } from '@/components/ui/card';
import { formatCurrency, formatDistance, formatRelativeTime } from '@/lib/formatters';
import { useTranslation } from '@/i18n/useTranslation';

interface AvailabilityCardProps {
  item: MedicineAvailabilityDto;
  onReserve?: (item: MedicineAvailabilityDto) => void;
}

export function AvailabilityCard({ item }: AvailabilityCardProps) {
  const { language } = useTranslation();
  const isAvailable = item.availableQuantity > 0;

  return (
    <Card className="overflow-hidden rounded-2xl border-slate-200/80 dark:border-slate-800 bg-white dark:bg-slate-900 transition-all duration-200 hover:shadow-md hover:border-slate-300 dark:hover:border-slate-700">
      <CardContent className="p-5">
        <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
          {/* Main Info */}
          <div className="space-y-2 flex-1">
            <div className="flex flex-wrap items-center gap-2">
              <h3 className="text-lg font-bold text-slate-900 dark:text-slate-100">{item.medicineBrandName}</h3>
              <Badge variant={isAvailable ? 'success' : 'destructive'} className="text-xs">
                {isAvailable
                  ? (language === 'ar' ? `متوفر (${item.availableQuantity} وحدة)` : `${item.availableQuantity} in stock`)
                  : (language === 'ar' ? 'غير متوفر' : 'Out of Stock')}
              </Badge>
              {item.freshness === 'Realtime' && (
                <Badge variant="outline" className="text-[10px] text-blue-700 dark:text-blue-300 border-blue-200 dark:border-blue-900/60 bg-blue-50/50 dark:bg-blue-950/40 flex items-center gap-1">
                  <Sparkles className="h-3 w-3" />
                  {language === 'ar' ? 'تحديث لحظي' : 'Live Freshness'}
                </Badge>
              )}
            </div>

            <p className="text-xs text-slate-500 dark:text-slate-400 font-medium">
              {language === 'ar' ? 'المادة الفعالة:' : 'Generic:'} <span className="text-slate-700 dark:text-slate-300">{item.medicineGenericName}</span> • {language === 'ar' ? 'الشكل:' : 'Form:'} {item.dosageForm} • {language === 'ar' ? 'التركيز:' : 'Strength:'} {item.strength}
            </p>

            {/* Branch and Location */}
            <div className="flex flex-wrap items-center gap-y-1 gap-x-4 pt-1 text-xs text-slate-600 dark:text-slate-400">
              <div className="flex items-center gap-1 font-semibold text-slate-800 dark:text-slate-200">
                <span>{item.pharmacyName}</span> — <span>{item.branchName}</span>
              </div>
              <div className="flex items-center gap-1 text-slate-500 dark:text-slate-400">
                <MapPin className="h-3.5 w-3.5 text-slate-400 dark:text-slate-500" />
                <span>{item.branchAddress}, {item.branchCity}</span>
              </div>
              {item.distanceKm != null && (
                <div className="flex items-center gap-1 font-semibold text-blue-600 dark:text-blue-400">
                  <span>{formatDistance(item.distanceKm)}</span>
                </div>
              )}
            </div>

            {/* Timings & Contact */}
            <div className="flex flex-wrap items-center gap-x-4 gap-y-1 text-[11px] text-slate-400 dark:text-slate-500">
              {item.openingHours && (
                <div className="flex items-center gap-1">
                  <Clock className="h-3 w-3" />
                  <span>{language === 'ar' ? 'ساعات العمل:' : 'Hours:'} {item.openingHours}</span>
                </div>
              )}
              {item.branchPhone && (
                <div className="flex items-center gap-1">
                  <Phone className="h-3 w-3" />
                  <span>{item.branchPhone}</span>
                </div>
              )}
              <span>{language === 'ar' ? 'تم التحديث منذ' : 'Updated'} {formatRelativeTime(item.lastUpdatedAt)}</span>
            </div>
          </div>

          {/* Pricing & Contact action */}
          <div className="flex md:flex-col items-center md:items-end justify-between md:justify-center border-t md:border-t-0 md:border-s border-slate-100 dark:border-slate-800 pt-3 md:pt-0 md:ps-6 gap-3">
            <div className="text-start md:text-end">
              <div className="text-xl font-bold text-slate-900 dark:text-slate-100">
                {formatCurrency(item.price, item.currency)}
              </div>
              <p className="text-[11px] text-slate-500 dark:text-slate-400">
                {language === 'ar' ? 'لكل عبوة' : 'per unit'}
              </p>
            </div>

            {item.branchPhone && (
              <Button
                asChild
                size="sm"
                variant="outline"
                className="px-4 gap-1.5 text-xs font-semibold rounded-xl border-blue-200 dark:border-blue-900 text-blue-700 dark:text-blue-300 hover:bg-blue-50 dark:hover:bg-blue-950/50"
              >
                <a href={`tel:${item.branchPhone}`}>
                  <Phone className="h-3.5 w-3.5" />
                  {language === 'ar' ? 'اتصال بالصيدلية' : 'Call Pharmacy'}
                </a>
              </Button>
            )}
          </div>
        </div>
      </CardContent>
    </Card>
  );
}
