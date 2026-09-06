import { Sun, Moon, Laptop } from 'lucide-react';
import { useThemeStore } from '@/app/store/themeStore';
import { Button } from '@/components/ui/button';

export function ThemeToggle({ className = '' }: { className?: string }) {
  const { theme, setTheme } = useThemeStore();

  return (
    <div className={`flex items-center rounded-lg border border-slate-200 dark:border-slate-800 bg-white/80 dark:bg-slate-900/80 p-0.5 shadow-xs ${className}`}>
      <button
        type="button"
        onClick={() => setTheme('light')}
        className={`rounded-md p-1.5 transition-colors ${
          theme === 'light'
            ? 'bg-amber-50 dark:bg-amber-950/40 text-amber-600 shadow-xs'
            : 'text-slate-400 hover:text-slate-700 dark:hover:text-slate-200'
        }`}
        title="Light Theme"
        aria-label="Light Theme"
      >
        <Sun className="h-3.5 w-3.5" />
      </button>

      <button
        type="button"
        onClick={() => setTheme('dark')}
        className={`rounded-md p-1.5 transition-colors ${
          theme === 'dark'
            ? 'bg-indigo-50 dark:bg-indigo-950/40 text-indigo-600 dark:text-indigo-400 shadow-xs'
            : 'text-slate-400 hover:text-slate-700 dark:hover:text-slate-200'
        }`}
        title="Dark Theme"
        aria-label="Dark Theme"
      >
        <Moon className="h-3.5 w-3.5" />
      </button>

      <button
        type="button"
        onClick={() => setTheme('system')}
        className={`rounded-md p-1.5 transition-colors ${
          theme === 'system'
            ? 'bg-blue-50 dark:bg-blue-950/40 text-blue-600 dark:text-blue-400 shadow-xs'
            : 'text-slate-400 hover:text-slate-700 dark:hover:text-slate-200'
        }`}
        title="System Auto Theme"
        aria-label="System Theme"
      >
        <Laptop className="h-3.5 w-3.5" />
      </button>
    </div>
  );
}

export function ThemeToggleButton({ className = '' }: { className?: string }) {
  const { theme, resolvedTheme, toggleTheme } = useThemeStore();

  return (
    <Button
      variant="ghost"
      size="sm"
      onClick={toggleTheme}
      className={`h-9 px-2.5 text-xs flex items-center gap-1.5 text-slate-600 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-slate-800 ${className}`}
      title={`Current Theme: ${theme.toUpperCase()} (Click to toggle)`}
      aria-label="Toggle theme"
    >
      {theme === 'system' ? (
        <Laptop className="h-4 w-4 text-blue-500" />
      ) : resolvedTheme === 'dark' ? (
        <Moon className="h-4 w-4 text-indigo-400" />
      ) : (
        <Sun className="h-4 w-4 text-amber-500" />
      )}
      <span className="capitalize hidden sm:inline">{theme}</span>
    </Button>
  );
}
