import { useQuery } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import { Building2, BookOpen, Clock, ShieldCheck, ArrowRight } from 'lucide-react';
import { adminApi } from '../api/adminApi';
import { Card, CardContent } from '@/components/ui/card';
import { Button } from '@/components/ui/button';

export function AdminDashboardPage() {
  const { data: pharmacies } = useQuery({
    queryKey: ['admin-pharmacies'],
    queryFn: () => adminApi.getAllPharmacies(),
  });

  const { data: reservations } = useQuery({
    queryKey: ['admin-reservations-summary'],
    queryFn: () => adminApi.getAllReservations({ pageSize: 1 }),
  });

  const pendingCount = (pharmacies || []).filter((p) => p.status === 'Pending').length;
  const approvedCount = (pharmacies || []).filter((p) => p.status === 'Approved').length;

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-slate-900">Platform Superuser Overview</h1>
        <p className="text-xs text-slate-500">
          Central governance for partner pharmacy approvals, unified medicine catalog, and system reservation releases.
        </p>
      </div>

      {/* Metric Cards */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-5">
        <Card className="border-slate-200">
          <CardContent className="p-5 flex items-center justify-between">
            <div>
              <p className="text-xs font-semibold text-slate-500">Pending Pharmacy Approvals</p>
              <h3 className="text-3xl font-extrabold text-amber-600 mt-1">{pendingCount}</h3>
              <Button variant="link" size="sm" asChild className="p-0 h-auto mt-2 text-xs">
                <Link to="/admin/pharmacies" className="flex items-center gap-1">
                  Review Applications <ArrowRight className="h-3 w-3" />
                </Link>
              </Button>
            </div>
            <div className="flex h-12 w-12 items-center justify-center rounded-xl bg-amber-50 text-amber-600 border border-amber-100">
              <Building2 className="h-6 w-6" />
            </div>
          </CardContent>
        </Card>

        <Card className="border-slate-200">
          <CardContent className="p-5 flex items-center justify-between">
            <div>
              <p className="text-xs font-semibold text-slate-500">Active Partner Pharmacies</p>
              <h3 className="text-3xl font-extrabold text-slate-900 mt-1">{approvedCount}</h3>
              <Button variant="link" size="sm" asChild className="p-0 h-auto mt-2 text-xs">
                <Link to="/admin/pharmacies" className="flex items-center gap-1">
                  View All Partners <ArrowRight className="h-3 w-3" />
                </Link>
              </Button>
            </div>
            <div className="flex h-12 w-12 items-center justify-center rounded-xl bg-blue-50 text-blue-600 border border-blue-100">
              <ShieldCheck className="h-6 w-6" />
            </div>
          </CardContent>
        </Card>

        <Card className="border-slate-200">
          <CardContent className="p-5 flex items-center justify-between">
            <div>
              <p className="text-xs font-semibold text-slate-500">Total System Holds</p>
              <h3 className="text-3xl font-extrabold text-purple-700 mt-1">
                {reservations?.totalCount ?? 0}
              </h3>
              <Button variant="link" size="sm" asChild className="p-0 h-auto mt-2 text-xs">
                <Link to="/admin/reservations" className="flex items-center gap-1">
                  Audit Holds <ArrowRight className="h-3 w-3" />
                </Link>
              </Button>
            </div>
            <div className="flex h-12 w-12 items-center justify-center rounded-xl bg-purple-50 text-purple-600 border border-purple-100">
              <Clock className="h-6 w-6" />
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Governance Sections */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-5 pt-2">
        <Card className="border-slate-200">
          <CardContent className="p-6 space-y-3">
            <div className="flex items-center gap-3">
              <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-blue-100 text-blue-700">
                <BookOpen className="h-5 w-5" />
              </div>
              <div>
                <h3 className="font-bold text-slate-900 text-base">Central Catalog Management</h3>
                <p className="text-xs text-slate-500">Standardize medicine brands, generics, categories, and barcodes.</p>
              </div>
            </div>
            <p className="text-xs text-slate-600 leading-relaxed">
              Create and edit authoritative medicine records used by all pharmacies when synchronizing inventory.
            </p>
            <Button size="sm" asChild>
              <Link to="/admin/catalog">Open Catalog Studio</Link>
            </Button>
          </CardContent>
        </Card>

        <Card className="border-slate-200">
          <CardContent className="p-6 space-y-3">
            <div className="flex items-center gap-3">
              <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-amber-100 text-amber-700">
                <Clock className="h-5 w-5" />
              </div>
              <div>
                <h3 className="font-bold text-slate-900 text-base">System Reservation Oversight</h3>
                <p className="text-xs text-slate-500">Audit system holds and perform administrative releases.</p>
              </div>
            </div>
            <p className="text-xs text-slate-600 leading-relaxed">
              In case of pharmacy outages or customer disputes, platform administrators can forcibly release reserved stock back to inventory.
            </p>
            <Button size="sm" variant="outline" asChild>
              <Link to="/admin/reservations">Inspect System Holds</Link>
            </Button>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
