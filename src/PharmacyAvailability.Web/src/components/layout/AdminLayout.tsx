import { NavLink, Outlet } from 'react-router-dom';
import { ShieldAlert, Building2, BookOpen, Clock } from 'lucide-react';
import { Navbar } from './Navbar';
import { Badge } from '@/components/ui/badge';

export function AdminLayout() {
  const navItems = [
    { label: 'Platform Overview', path: '/admin/dashboard', icon: ShieldAlert },
    { label: 'Pharmacy Approvals', path: '/admin/pharmacies', icon: Building2 },
    { label: 'Central Catalog', path: '/admin/catalog', icon: BookOpen },
    { label: 'Reservations Oversight', path: '/admin/reservations', icon: Clock },
  ];

  return (
    <div className="flex min-h-screen flex-col bg-slate-50">
      <Navbar />

      {/* Admin Header */}
      <div className="border-b border-purple-200 bg-purple-950 text-white">
        <div className="mx-auto flex max-w-7xl items-center justify-between px-4 py-4 sm:px-6 lg:px-8">
          <div className="flex items-center gap-3">
            <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-purple-800 text-purple-200">
              <ShieldAlert className="h-6 w-6" />
            </div>
            <div>
              <div className="flex items-center gap-2">
                <span className="font-bold text-white text-base">Platform Administration</span>
                <Badge variant="default" className="bg-purple-600 text-white text-xs">
                  Superuser
                </Badge>
              </div>
              <p className="text-xs text-purple-200">
                Centralized catalog management, pharmacy onboarding approval, and system reservation releases.
              </p>
            </div>
          </div>
        </div>
      </div>

      {/* Admin Subnav */}
      <div className="border-b border-slate-200 bg-white">
        <div className="mx-auto flex max-w-7xl overflow-x-auto px-4 sm:px-6 lg:px-8">
          {navItems.map((item) => {
            const Icon = item.icon;
            return (
              <NavLink
                key={item.path}
                to={item.path}
                className={({ isActive }) =>
                  `flex items-center gap-2 whitespace-nowrap border-b-2 px-4 py-3 text-sm font-medium transition-colors ${
                    isActive
                      ? 'border-purple-600 text-purple-700 font-semibold'
                      : 'border-transparent text-slate-600 hover:border-slate-300 hover:text-slate-900'
                  }`
                }
              >
                <Icon className="h-4 w-4" />
                {item.label}
              </NavLink>
            );
          })}
        </div>
      </div>

      {/* Main Outlet */}
      <main className="mx-auto w-full max-w-7xl flex-1 px-4 py-8 sm:px-6 lg:px-8">
        <Outlet />
      </main>
    </div>
  );
}
