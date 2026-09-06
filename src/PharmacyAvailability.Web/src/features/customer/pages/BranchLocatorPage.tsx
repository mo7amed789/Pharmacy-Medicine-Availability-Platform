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

export function BranchLocatorPage() {
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
        <h1 className="text-3xl font-extrabold tracking-tight text-slate-900">Partner Pharmacies & Branches</h1>
        <p className="text-sm text-slate-500">
          Find verified partner pharmacy locations across Egypt for pickup and stock inquiry.
        </p>
      </div>

      {/* Filter bar */}
      <div className="mb-8 grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-3">
        <div className="relative">
          <Search className="absolute left-3 top-3 h-4 w-4 text-slate-400" />
          <Input
            placeholder="Search branch name, pharmacy, or address..."
            value={searchFilter}
            onChange={(e) => setSearchFilter(e.target.value)}
            className="pl-9 bg-white"
          />
        </div>

        <select
          value={cityFilter}
          onChange={(e) => setCityFilter(e.target.value)}
          className="h-10 rounded-md border border-slate-300 bg-white px-3 text-sm text-slate-800 focus:outline-none focus:ring-2 focus:ring-blue-500"
        >
          <option value="">All Cities</option>
          <option value="Cairo">Cairo</option>
          <option value="Giza">Giza</option>
          <option value="Alexandria">Alexandria</option>
          <option value="Mansoura">Mansoura</option>
        </select>
      </div>

      {isLoading ? (
        <div className="py-16">
          <LoadingSpinner size="lg" text="Loading active pharmacy branches..." />
        </div>
      ) : isError ? (
        <ErrorState error={error} onRetry={() => refetch()} />
      ) : filtered.length === 0 ? (
        <EmptyState
          icon={Building2}
          title="No Branches Found"
          description="No active branches matched your filter criteria."
        />
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-5">
          {filtered.map((branch: PharmacyBranchDto) => (
            <Card key={branch.id} className="border-slate-200 hover:shadow-md transition-shadow">
              <CardContent className="p-5 space-y-3">
                <div className="flex items-start justify-between gap-2">
                  <div>
                    <h3 className="font-bold text-slate-900 text-base">{branch.pharmacyName}</h3>
                    <p className="text-xs font-semibold text-blue-600">{branch.name}</p>
                  </div>
                  <Badge variant={branch.isActive ? 'success' : 'secondary'} className="text-[11px]">
                    {branch.isActive ? 'Open' : 'Closed'}
                  </Badge>
                </div>

                <div className="space-y-1 text-xs text-slate-600">
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
