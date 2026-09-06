import { AlertCircle, RotateCcw } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { ApiError } from '@/api/apiClient';
import { useTranslation } from '@/i18n/useTranslation';
import { getFriendlyErrorMessage } from '@/lib/friendlyErrors';

interface ErrorStateProps {
  error: unknown;
  onRetry?: () => void;
  title?: string;
}

export function ErrorState({ error, onRetry, title }: ErrorStateProps) {
  const { language } = useTranslation();
  const displayTitle = title || (language === 'ar' ? 'تعذر تحميل البيانات' : 'Unable to Load Data');
  const errorMessage = getFriendlyErrorMessage(error, language);
  let correlationId: string | undefined;
  let statusCode: number | undefined;
  let validationErrors: Record<string, string[]> | undefined;

  if (error instanceof ApiError) {
    correlationId = error.correlationId;
    statusCode = error.status;
    validationErrors = error.validationErrors;
  }

  return (
    <div className="flex flex-col items-center justify-center rounded-xl border border-red-200 dark:border-red-900/60 bg-red-50/50 dark:bg-red-950/20 p-8 text-center my-4">
      <div className="flex h-12 w-12 items-center justify-center rounded-full bg-red-100 dark:bg-red-950/60 text-red-600 dark:text-red-400 mb-3 shadow-xs">
        <AlertCircle className="h-6 w-6" />
      </div>

      <div className="flex items-center gap-2 mb-1">
        <h4 className="text-base font-semibold text-red-900 dark:text-red-300">{displayTitle}</h4>
        {statusCode && (
          <Badge variant="destructive" className="text-[10px] px-1.5 py-0">
            HTTP {statusCode}
          </Badge>
        )}
      </div>

      <p className="mt-1 max-w-md text-sm text-red-700 dark:text-red-400">{errorMessage}</p>

      {validationErrors && Object.keys(validationErrors).length > 0 && (
        <div className="mt-3 text-start w-full max-w-md rounded-md bg-white dark:bg-slate-900 p-3 border border-red-200 dark:border-red-900/50 text-xs">
          <p className="font-semibold text-red-800 dark:text-red-300 mb-1">
            {language === 'ar' ? 'تفاصيل التحقق من البيانات:' : 'Validation details:'}
          </p>
          <ul className="list-disc list-inside space-y-0.5 text-red-600 dark:text-red-400">
            {Object.entries(validationErrors).flatMap(([field, msgs]) =>
              msgs.map((msg, idx) => (
                <li key={`${field}-${idx}`}>
                  <span className="font-medium text-slate-700 dark:text-slate-300">{field}:</span> {msg}
                </li>
              ))
            )}
          </ul>
        </div>
      )}

      {correlationId && (
        <span className="mt-2 font-mono text-xs text-red-500 dark:text-red-400 bg-red-100/60 dark:bg-red-950/60 px-2 py-0.5 rounded">
          Correlation ID: {correlationId}
        </span>
      )}

      {onRetry && (
        <Button
          onClick={onRetry}
          variant="outline"
          size="sm"
          className="mt-4 border-red-300 dark:border-red-800 text-red-800 dark:text-red-300 hover:bg-red-100/50 dark:hover:bg-red-900/30 flex items-center gap-1.5"
        >
          <RotateCcw className="h-3.5 w-3.5" />
          {language === 'ar' ? 'إعادة المحاولة' : 'Try Again'}
        </Button>
      )}
    </div>
  );
}
