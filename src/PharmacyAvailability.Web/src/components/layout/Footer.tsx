import { useTranslation } from '@/i18n/useTranslation';

export function Footer() {
  const { language } = useTranslation();

  return (
    <footer className="mt-auto border-t border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 py-6">
      <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
        <div className="flex flex-col sm:flex-row items-center justify-between gap-4 text-xs text-slate-500 dark:text-slate-400">
          <p>
            {language === 'ar'
              ? `© ${new Date().getFullYear()} فارما فايندر — منصة توفر الأدوية الموحدة. جميع الحقوق محفوظة.`
              : `© ${new Date().getFullYear()} PharmaFinder Platform. All rights reserved.`}
          </p>
          <div className="flex items-center gap-4">
            <span>{language === 'ar' ? 'مخزون لحظي' : 'Real-Time Inventory'}</span>
            <span>•</span>
            <span>{language === 'ar' ? 'عمليات الفروع' : 'Branch Operations'}</span>
            <span>•</span>
            <span>{language === 'ar' ? 'صيدليات معتمدة' : 'Verified Pharmacies'}</span>
          </div>
        </div>
      </div>
    </footer>
  );
}
