import { NavLink } from 'react-router-dom';
import { Search, MapPin } from 'lucide-react';
import { useTranslation } from '@/i18n/useTranslation';

const items = [
  { to: '/customer/search', icon: Search, key: 'nav.findMedicine' },
  { to: '/customer/branches', icon: MapPin, key: 'nav.pharmacies' },
] as const;

/** Floating pill navigation for small screens. Desktop keeps the top navbar links. */
export function MobileBottomNav() {
  const { t } = useTranslation();

  return (
    <nav
      aria-label="Primary"
      className="fixed inset-x-4 bottom-4 z-40 mx-auto flex max-w-sm items-center justify-around rounded-full border border-slate-200 bg-white/90 p-1.5 shadow-lg backdrop-blur-md md:hidden dark:border-white/10 dark:bg-slate-900/80"
    >
      {items.map(({ to, icon: Icon, key }) => (
        <NavLink
          key={to}
          to={to}
          className={({ isActive }) =>
            `flex flex-1 items-center justify-center gap-2 rounded-full px-4 py-2.5 text-sm font-semibold transition-colors ${
              isActive
                ? 'bg-slate-900 text-white dark:bg-slate-50 dark:text-slate-950'
                : 'text-slate-600 hover:text-slate-900 dark:text-slate-300 dark:hover:text-white'
            }`
          }
        >
          <Icon className="h-4 w-4" aria-hidden="true" />
          <span>{t(key)}</span>
        </NavLink>
      ))}
    </nav>
  );
}
