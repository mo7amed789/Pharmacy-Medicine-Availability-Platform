import * as React from 'react';
import { cn } from '@/lib/utils';

export interface BadgeProps extends React.HTMLAttributes<HTMLDivElement> {
  variant?: 'default' | 'secondary' | 'destructive' | 'outline' | 'success' | 'warning';
}

function Badge({ className, variant = 'default', ...props }: BadgeProps) {
  const variantStyles = {
    default: 'border-transparent bg-blue-600 text-white shadow',
    secondary: 'border-transparent bg-slate-100 text-slate-800',
    destructive: 'border-transparent bg-red-100 text-red-800 font-semibold',
    outline: 'border-slate-300 text-slate-800',
    success: 'border-transparent bg-emerald-100 text-emerald-800 font-semibold',
    warning: 'border-transparent bg-amber-100 text-amber-800 font-semibold',
  }[variant];

  return (
    <div
      className={cn(
        'inline-flex items-center rounded-full border px-2.5 py-0.5 text-xs font-semibold transition-colors focus:outline-none focus:ring-2 focus:ring-ring focus:ring-offset-2',
        variantStyles,
        className
      )}
      {...props}
    />
  );
}

export { Badge };
