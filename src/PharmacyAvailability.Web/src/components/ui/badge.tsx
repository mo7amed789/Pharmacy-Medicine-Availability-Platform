import * as React from 'react';
import { cn } from '@/lib/utils';

export interface BadgeProps extends React.HTMLAttributes<HTMLDivElement> {
  variant?: 'default' | 'secondary' | 'destructive' | 'outline' | 'success' | 'warning';
}

function Badge({ className, variant = 'default', ...props }: BadgeProps) {
  const variantStyles = {
    default: 'border-transparent bg-blue-600 text-white shadow',
    secondary: 'border-transparent bg-slate-100 dark:bg-slate-800 text-slate-800 dark:text-slate-200',
    destructive: 'border-transparent bg-red-100 dark:bg-red-950/60 text-red-800 dark:text-red-300 font-semibold',
    outline: 'border-slate-300 dark:border-slate-700 text-slate-800 dark:text-slate-200',
    success: 'border-transparent bg-emerald-100 dark:bg-emerald-950/60 text-emerald-800 dark:text-emerald-300 font-semibold',
    warning: 'border-transparent bg-amber-100 dark:bg-amber-950/60 text-amber-800 dark:text-amber-300 font-semibold',
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
