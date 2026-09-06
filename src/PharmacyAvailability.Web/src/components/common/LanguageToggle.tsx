import { Globe } from 'lucide-react';
import { useLanguageStore } from '@/app/store/languageStore';

export function LanguageToggle({ className = '' }: { className?: string }) {
  const { language, setLanguage } = useLanguageStore();

  return (
    <div className={`flex items-center rounded-lg border border-slate-200 dark:border-slate-800 bg-white/80 dark:bg-slate-900/80 p-0.5 shadow-xs ${className}`}>
      <button
        type="button"
        onClick={() => setLanguage('ar')}
        className={`rounded-md px-2 py-1 text-xs font-semibold transition-colors flex items-center gap-1 ${
          language === 'ar'
            ? 'bg-blue-50 dark:bg-blue-950/40 text-blue-700 dark:text-blue-300 shadow-xs'
            : 'text-slate-500 hover:text-slate-800 dark:hover:text-slate-200'
        }`}
        title="اللغة العربية"
        aria-label="اللغة العربية"
      >
        <span>عربي</span>
      </button>

      <button
        type="button"
        onClick={() => setLanguage('en')}
        className={`rounded-md px-2 py-1 text-xs font-semibold transition-colors flex items-center gap-1 ${
          language === 'en'
            ? 'bg-blue-50 dark:bg-blue-950/40 text-blue-700 dark:text-blue-300 shadow-xs'
            : 'text-slate-500 hover:text-slate-800 dark:hover:text-slate-200'
        }`}
        title="English"
        aria-label="English"
      >
        <span>EN</span>
      </button>
    </div>
  );
}

export function LanguageToggleButton({ className = '' }: { className?: string }) {
  const { language, toggleLanguage } = useLanguageStore();

  return (
    <button
      type="button"
      onClick={toggleLanguage}
      className={`h-9 px-2.5 rounded-lg border border-slate-200 dark:border-slate-800 bg-white/80 dark:bg-slate-900/80 text-xs font-semibold flex items-center gap-1.5 text-slate-700 dark:text-slate-200 hover:bg-slate-100 dark:hover:bg-slate-800 transition-colors shadow-xs ${className}`}
      title={language === 'ar' ? 'Switch to English' : 'التحويل إلى العربية'}
      aria-label="Toggle language"
    >
      <Globe className="h-3.5 w-3.5 text-blue-600 dark:text-blue-400" />
      <span>{language === 'ar' ? 'English' : 'عربي'}</span>
    </button>
  );
}
