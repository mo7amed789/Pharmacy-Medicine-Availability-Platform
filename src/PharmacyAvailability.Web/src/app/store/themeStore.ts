import { create } from 'zustand';

export type Theme = 'light' | 'dark' | 'system';

interface ThemeState {
  theme: Theme;
  resolvedTheme: 'light' | 'dark';
  setTheme: (theme: Theme) => void;
  toggleTheme: () => void;
}

const THEME_STORAGE_KEY = 'pharma-theme';

function getSystemTheme(): 'light' | 'dark' {
  if (typeof window === 'undefined' || typeof window.matchMedia !== 'function') {
    return 'light';
  }
  try {
    return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
  } catch {
    return 'light';
  }
}

function resolveTheme(theme: Theme): 'light' | 'dark' {
  if (theme === 'system') {
    return getSystemTheme();
  }
  return theme;
}

function applyThemeToDOM(resolved: 'light' | 'dark') {
  if (typeof document === 'undefined') return;
  const root = document.documentElement;
  if (resolved === 'dark') {
    root.classList.add('dark');
  } else {
    root.classList.remove('dark');
  }
}

const initialTheme: Theme = (() => {
  if (typeof window === 'undefined') return 'system';
  try {
    const saved = localStorage.getItem(THEME_STORAGE_KEY) as Theme | null;
    if (saved === 'light' || saved === 'dark' || saved === 'system') {
      return saved;
    }
  } catch {
    // Fallback to system on storage access error
  }
  return 'system';
})();

const initialResolved = resolveTheme(initialTheme);
applyThemeToDOM(initialResolved);

export const useThemeStore = create<ThemeState>((set, get) => ({
  theme: initialTheme,
  resolvedTheme: initialResolved,

  setTheme: (newTheme: Theme) => {
    try {
      localStorage.setItem(THEME_STORAGE_KEY, newTheme);
    } catch {
      // Ignore storage errors
    }
    const resolved = resolveTheme(newTheme);
    applyThemeToDOM(resolved);
    set({ theme: newTheme, resolvedTheme: resolved });
  },

  toggleTheme: () => {
    const current = get().theme;
    // Cycle: light -> dark -> system
    const next: Theme = current === 'light' ? 'dark' : current === 'dark' ? 'system' : 'light';
    get().setTheme(next);
  },
}));

// Listen for system theme changes if in 'system' mode
if (typeof window !== 'undefined' && typeof window.matchMedia === 'function') {
  try {
    const mediaQuery = window.matchMedia('(prefers-color-scheme: dark)');
    const handleChange = () => {
      const { theme } = useThemeStore.getState();
      if (theme === 'system') {
        const resolved = getSystemTheme();
        applyThemeToDOM(resolved);
        useThemeStore.setState({ resolvedTheme: resolved });
      }
    };

    if (mediaQuery.addEventListener) {
      mediaQuery.addEventListener('change', handleChange);
    } else if ('addListener' in mediaQuery) {
      (mediaQuery as any).addListener(handleChange);
    }
  } catch {
    // Unsupported in environment
  }
}
