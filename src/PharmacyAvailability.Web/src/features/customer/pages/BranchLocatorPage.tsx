import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { Building2, MapPin, Phone, Clock, Search } from 'lucide-react';
import { customerApi } from '../api/customerApi';
import { PharmacyBranchDto } from '@/types/pharmacies';
import { Input } from '@/components/ui/input';
import { Card, CardContent } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { LoadingSpinner } from '@/components/feedback/LoadingSpinner';
import { ErrorState } from '@/components/feedback/ErrorState';
import { EmptyState } from '@/components/feedback/EmptyState';
import { useTranslation } from '@/i18n/useTranslation';

export function BranchLocatorPage() {
  const { t } = useTranslation();
  const [cityFilter, setCityFilter] = useState('');
  const [searchFilter, setSearchFilter] = useState('');

  const { data: branches, isLoading, isError, error, refetch } = useQuery({
    queryKey: ['active-branches', cityFilter],
    queryFn: () => customerApi.getActiveBranches(cityFilter || undefined),
  });

  const filtered = (branches || []).filter((b: PharmacyBranchDto) => {
    if (!searchFilter) return true;
    const term = searchFilter.toLowerCase();
    return (
      b.name.toLowerCase().includes(term) ||
      b.pharmacyName.toLowerCase().includes(term) ||
      b.city.toLowerCase().includes(term) ||
      b.address.toLowerCase().includes(term)
    );
  });

  return (
    <div className="mx-auto max-w-7xl px-4 py-8 sm:px-6 lg:px-8">
      <div className="mb-6 space-y-1">
        <h1 className="text-3xl font-extrabold tracking-tight text-slate-900 dark:text-slate-100">{t('customer.branchesPageTitle')}</h1>
        <p className="text-sm text-slate-500 dark:text-slate-400">
          {t('customer.branchesPageSubtitle')}
        </p>
      </div>

      {/* Filter bar */}
      <div className="mb-8 grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-3">
        <div className="relative">
          <Search className="absolute start-3 top-3 h-4 w-4 text-slate-400" />
          <Input
            placeholder={t('customer.searchBranchPlaceholder')}
            value={searchFilter}
            onChange={(e) => setSearchFilter(e.target.value)}
            className="ps-9 bg-white dark:bg-slate-850 dark:border-slate-700"
          />
        </div>

        <select
          value={cityFilter}
          onChange={(e) => setCityFilter(e.target.value)}
          className="h-10 rounded-md border border-slate-300 dark:border-slate-700 bg-white dark:bg-slate-800 px-3 text-sm text-slate-800 dark:text-slate-200 focus:outline-none focus:ring-2 focus:ring-blue-500"
        >
          <option value="">{t('common.allCities')}</option>
          <option value="Cairo">القاهرة / Cairo</option>
          <option value="Giza">الجيزة / Giza</option>
          <option value="Alexandria">الإسكندرية / Alexandria</option>
          <option value="Mansoura">المنصورة / Mansoura</option>
        </select>
      </div>

      {isLoading ? (
        <div className="py-16">
          <LoadingSpinner size="lg" text={t('common.loading')} />
        </div>
      ) : isError ? (
        <ErrorState error={error} onRetry={() => refetch()} />
      ) : filtered.length === 0 ? (
        <EmptyState
          icon={Building2}
          title={t('feedback.noDataTitle')}
          description={t('feedback.noDataDesc')}
        />
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-5">
          {filtered.map((branch: PharmacyBranchDto) => (
            <Card key={branch.id} className="border-slate-200 dark:border-slate-800 hover:shadow-md transition-shadow">
              <CardContent className="p-5 space-y-3">
                <div className="flex items-start justify-between gap-2">
                  <div>
                    <h3 className="font-bold text-slate-900 dark:text-slate-100 text-base">{branch.pharmacyName}</h3>
                    <p className="text-xs font-semibold text-blue-600 dark:text-blue-400">{branch.name}</p>
                  </div>
                  <Badge variant={branch.isActive ? 'success' : 'secondary'} className="text-[11px]">
                    {branch.isActive ? t('pharmacy.branchOpen') : t('pharmacy.branchClosed')}
                  </Badge>
                </div>

                <div className="space-y-1 text-xs text-slate-600 dark:text-slate-300">
                  <div className="flex items-start gap-1.5">
                    <MapPin className="h-3.5 w-3.5 text-slate-400 shrink-0 mt-0.5" />
                    <span>{branch.address}, {branch.city}</span>
                  </div>
                  {branch.phone && (
                    <div className="flex items-center gap-1.5">
                      <Phone className="h-3.5 w-3.5 text-slate-400 shrink-0" />
                      <span>{branch.phone}</span>
                    </div>
                  )}
                  {branch.openingHours && (
                    <div className="flex items-center gap-1.5">
                      <Clock className="h-3.5 w-3.5 text-slate-400 shrink-0" />
                      <span>{branch.openingHours}</span>
                    </div>
                  )}
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
      )}
    </div>
  );
}
