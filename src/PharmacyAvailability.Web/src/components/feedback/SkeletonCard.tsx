interface SkeletonCardProps {
  count?: number;
}

export function SkeletonCard({ count = 3 }: SkeletonCardProps) {
  return (
    <div className="space-y-4 w-full" aria-busy="true" aria-live="polite">
      {Array.from({ length: count }).map((_, idx) => (
        <div
          key={idx}
          role="status"
          aria-label="Loading skeleton"
          className="rounded-2xl border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 p-5 shadow-xs animate-pulse"
        >
          <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
            {/* Left/Main Column */}
            <div className="space-y-3 flex-1">
              <div className="flex flex-wrap items-center gap-2">
                {/* Brand Name */}
                <div className="h-6 w-48 rounded-md bg-slate-200 dark:bg-slate-800" />
                {/* Stock Badge */}
                <div className="h-5 w-24 rounded-full bg-emerald-100 dark:bg-emerald-950/40" />
                {/* Live Pill */}
                <div className="h-5 w-20 rounded-full bg-blue-100 dark:bg-blue-950/40" />
              </div>

              {/* Subtitle / Generic & Strength */}
              <div className="h-4 w-72 rounded bg-slate-200 dark:bg-slate-800" />

              {/* Pharmacy & Location */}
              <div className="flex flex-wrap items-center gap-4 pt-1">
                <div className="h-4 w-40 rounded bg-slate-200 dark:bg-slate-800" />
                <div className="h-4 w-32 rounded bg-slate-200 dark:bg-slate-800" />
                <div className="h-4 w-16 rounded bg-slate-200 dark:bg-slate-800" />
              </div>

              {/* Hours and phone */}
              <div className="flex items-center gap-4 pt-1">
                <div className="h-3 w-28 rounded bg-slate-100 dark:bg-slate-850" />
                <div className="h-3 w-24 rounded bg-slate-100 dark:bg-slate-850" />
              </div>
            </div>

            {/* Right/Action Column */}
            <div className="flex md:flex-col items-center md:items-end justify-between md:justify-center border-t md:border-t-0 md:border-s border-slate-100 dark:border-slate-800 pt-3 md:pt-0 md:ps-6 gap-3 shrink-0">
              <div className="space-y-1 text-start md:text-end">
                <div className="h-7 w-20 rounded bg-slate-200 dark:bg-slate-800 ms-auto" />
                <div className="h-3 w-12 rounded bg-slate-100 dark:bg-slate-850 ms-auto" />
              </div>
              {/* Button placeholder */}
              <div className="h-9 w-28 rounded-lg bg-emerald-600/30 dark:bg-emerald-600/20" />
            </div>
          </div>
        </div>
      ))}
    </div>
  );
}
