import { useEffect } from 'react';
import { NavLink, Outlet } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  LayoutDashboard,
  ClipboardList,
  Boxes,
  AlertTriangle,
  History,
  Users,
  Building2,
  Power,
} from 'lucide-react';
import { Navbar } from './Navbar';
import { useAuthStore } from '@/app/store/authStore';
import { useBranchStore } from '@/app/store/branchStore';
import { pharmacyOpsApi } from '@/features/pharmacy/api/pharmacyOpsApi';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { LoadingSpinner } from '@/components/feedback/LoadingSpinner';

export function PharmacyLayout() {
  const { user } = useAuthStore();
  const { currentBranch, setCurrentBranch } = useBranchStore();
  const queryClient = useQueryClient();

  // If PlatformAdmin has no pharmacyId assigned, they can test with any pharmacy, but staff has user.pharmacyId
  const pharmacyId = user?.pharmacyId;

  const { data: branches, isLoading } = useQuery({
    queryKey: ['pharmacy-branches', pharmacyId],
    queryFn: () => (pharmacyId ? pharmacyOpsApi.getBranches(pharmacyId) : Promise.resolve([])),
    enabled: !!pharmacyId,
  });

  // Auto-select first active branch if not yet selected
  useEffect(() => {
    if (branches && branches.length > 0 && !currentBranch) {
      setCurrentBranch(branches[0]);
    }
  }, [branches, currentBranch, setCurrentBranch]);

  // Toggle branch operational status mutation
  const toggleStatusMutation = useMutation({
    mutationFn: async ({ branchId, isActive }: { branchId: string; isActive: boolean }) => {
      if (!pharmacyId) return;
      return pharmacyOpsApi.toggleBranchStatus(pharmacyId, branchId, isActive);
    },
    onSuccess: (updated) => {
      if (updated) {
        setCurrentBranch(updated);
        queryClient.invalidateQueries({ queryKey: ['pharmacy-branches'] });
      }
    },
  });

  const navItems = [
    { label: 'Dashboard', path: '/pharmacy/dashboard', icon: LayoutDashboard },
    { label: 'Pickup Queue', path: '/pharmacy/queue', icon: ClipboardList },
    { label: 'Inventory', path: '/pharmacy/inventory', icon: Boxes },
    { label: 'Low Stock Alerts', path: '/pharmacy/low-stock', icon: AlertTriangle },
    { label: 'Activity Feed', path: '/pharmacy/activity', icon: History },
  ];

  if (user?.role === 'PharmacyAdmin' || user?.role === 'PlatformAdmin') {
    navItems.push({ label: 'Staff Management', path: '/pharmacy/staff', icon: Users });
  }

  return (
    <div className="flex min-h-screen flex-col bg-slate-50 dark:bg-slate-950">
      <Navbar />

      {/* Pharmacy Operations Top Header */}
      <div className="border-b border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 shadow-xs">
        <div className="mx-auto flex max-w-7xl flex-wrap items-center justify-between gap-4 px-4 py-3 sm:px-6 lg:px-8">
          <div className="flex items-center gap-3">
            <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-blue-50 dark:bg-blue-950/50 text-blue-700 dark:text-blue-400">
              <Building2 className="h-6 w-6" />
            </div>
            <div>
              <div className="flex items-center gap-2">
                <span className="font-bold text-slate-900 dark:text-slate-100 text-base">Branch Operations Portal</span>
                {currentBranch && (
                  <Badge variant={currentBranch.isActive ? 'success' : 'destructive'} className="text-xs">
                    {currentBranch.isActive ? 'Branch Open' : 'Branch Closed'}
                  </Badge>
                )}
              </div>
              <p className="text-xs text-slate-500 dark:text-slate-400">
                {currentBranch ? `${currentBranch.pharmacyName} — ${currentBranch.name}` : 'Select a branch'}
              </p>
            </div>
          </div>

          {/* Branch Selector and Status Toggle */}
          <div className="flex items-center gap-3">
            {branches && branches.length > 1 && (
              <select
                value={currentBranch?.id || ''}
                onChange={(e) => {
                  const selected = branches.find((b) => b.id === e.target.value);
                  if (selected) setCurrentBranch(selected);
                }}
                className="h-9 rounded-md border border-slate-300 dark:border-slate-700 bg-white dark:bg-slate-800 px-3 py-1 text-xs font-medium text-slate-700 dark:text-slate-200 focus:outline-none focus:ring-2 focus:ring-blue-500"
              >
                {branches.map((b) => (
                  <option key={b.id} value={b.id}>
                    {b.name} ({b.city})
                  </option>
                ))}
              </select>
            )}

            {currentBranch && (
              <Button
                size="sm"
                variant={currentBranch.isActive ? 'outline' : 'default'}
                className="text-xs flex items-center gap-1.5"
                disabled={toggleStatusMutation.isPending}
                onClick={() =>
                  toggleStatusMutation.mutate({
                    branchId: currentBranch.id,
                    isActive: !currentBranch.isActive,
                  })
                }
              >
                <Power className="h-3.5 w-3.5" />
                {currentBranch.isActive ? 'Close Branch' : 'Open Branch'}
              </Button>
            )}
          </div>
        </div>
      </div>

      {/* Main Content Area with Navigation Tabs */}
      <div className="mx-auto w-full max-w-7xl flex-1 px-4 py-6 sm:px-6 lg:px-8">
        {/* Navigation Bar / Tabs */}
        <div className="mb-6 flex overflow-x-auto border-b border-slate-200 dark:border-slate-800 pb-px">
          {navItems.map((item) => {
            const Icon = item.icon;
            return (
              <NavLink
                key={item.path}
                to={item.path}
                className={({ isActive }) =>
                  `flex items-center gap-2 whitespace-nowrap border-b-2 px-4 py-2.5 text-sm font-medium transition-colors ${
                    isActive
                      ? 'border-blue-600 text-blue-600 dark:text-blue-400 font-semibold'
                      : 'border-transparent text-slate-600 dark:text-slate-400 hover:border-slate-300 dark:hover:border-slate-700 hover:text-slate-900 dark:hover:text-slate-100'
                  }`
                }
              >
                <Icon className="h-4 w-4" />
                {item.label}
              </NavLink>
            );
          })}
        </div>

        {isLoading ? (
          <div className="py-12">
            <LoadingSpinner size="lg" text="Loading pharmacy branches..." />
          </div>
        ) : !pharmacyId && user?.role !== 'PlatformAdmin' ? (
          <div className="rounded-xl border border-amber-200 bg-amber-50 p-6 text-center text-amber-800">
            You are not currently linked to any active pharmacy. Please contact support.
          </div>
        ) : (
          <Outlet />
        )}
      </div>
    </div>
  );
}
