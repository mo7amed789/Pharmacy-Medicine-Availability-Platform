import { create } from 'zustand';

export type Language = 'ar' | 'en';

interface LanguageState {
  language: Language;
  isRTL: boolean;
  setLanguage: (lang: Language) => void;
  toggleLanguage: () => void;
}

const LANGUAGE_STORAGE_KEY = 'pharma-language';

function applyLanguageToDOM(lang: Language) {
  if (typeof document === 'undefined') return;
  const root = document.documentElement;
  root.lang = lang;
  root.dir = lang === 'ar' ? 'rtl' : 'ltr';
}

const initialLanguage: Language = (() => {
  if (typeof window === 'undefined') return 'ar';
  try {
    const saved = localStorage.getItem(LANGUAGE_STORAGE_KEY) as Language | null;
    if (saved === 'ar' || saved === 'en') {
      return saved;
    }
  } catch {
    // Fallback to Arabic default on storage access error
  }
  return 'ar';
})();

applyLanguageToDOM(initialLanguage);

export const useLanguageStore = create<LanguageState>((set, get) => ({
  language: initialLanguage,
  isRTL: initialLanguage === 'ar',

  setLanguage: (newLang: Language) => {
    try {
      localStorage.setItem(LANGUAGE_STORAGE_KEY, newLang);
    } catch {
      // Ignore storage errors
    }
    applyLanguageToDOM(newLang);
    set({ language: newLang, isRTL: newLang === 'ar' });
  },

  toggleLanguage: () => {
    const current = get().language;
    const next: Language = current === 'ar' ? 'en' : 'ar';
    get().setLanguage(next);
  },
}));
