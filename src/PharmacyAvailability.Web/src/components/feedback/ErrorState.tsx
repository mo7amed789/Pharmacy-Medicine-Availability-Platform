import { AlertCircle, RotateCcw } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { ApiError } from '@/api/apiClient';

interface ErrorStateProps {
  error: unknown;
  onRetry?: () => void;
  title?: string;
}

export function ErrorState({ error, onRetry, title = 'Unable to Load Data' }: ErrorStateProps) {
  let errorMessage = 'An unexpected error occurred while communicating with the server.';
  let correlationId: string | undefined;

  if (error instanceof ApiError) {
    errorMessage = error.detail || error.message;
    correlationId = error.correlationId;
  } else if (error instanceof Error) {
    errorMessage = error.message;
  }

  return (
    <div className="flex flex-col items-center justify-center rounded-xl border border-red-200 bg-red-50/50 p-8 text-center my-4">
      <div className="flex h-12 w-12 items-center justify-center rounded-full bg-red-100 text-red-600 mb-3">
        <AlertCircle className="h-6 w-6" />
      </div>
      <h4 className="text-base font-semibold text-red-900">{title}</h4>
      <p className="mt-1 max-w-md text-sm text-red-700">{errorMessage}</p>

      {correlationId && (
        <span className="mt-2 font-mono text-xs text-red-500 bg-red-100/60 px-2 py-0.5 rounded">
          Correlation ID: {correlationId}
        </span>
      )}

      {onRetry && (
        <Button
          onClick={onRetry}
          variant="outline"
          size="sm"
          className="mt-4 border-red-300 text-red-800 hover:bg-red-100/50 flex items-center gap-1.5"
        >
          <RotateCcw className="h-3.5 w-3.5" />
          Try Again
        </Button>
      )}
    </div>
  );
}
