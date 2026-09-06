import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Clock, AlertCircle } from 'lucide-react';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
  DialogFooter,
} from '@/components/ui/dialog';
import { Button } from '@/components/ui/button';
import { Alert, AlertDescription } from '@/components/ui/alert';
import { MedicineAvailabilityDto } from '@/types/availability';
import { customerApi } from '../api/customerApi';
import { useAuthStore } from '@/app/store/authStore';
import { formatCurrency } from '@/lib/formatters';
import { ApiError } from '@/api/apiClient';
import { createReservationSchema, CreateReservationFormData } from '@/lib/validations';
import { useTranslation } from '@/i18n/useTranslation';

interface ReservationDialogProps {
  item: MedicineAvailabilityDto | null;
  isOpen: boolean;
  onClose: () => void;
  onSuccess: () => void;
}

export function ReservationDialog({ item, isOpen, onClose, onSuccess }: ReservationDialogProps) {
  const [serverError, setServerError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const { isAuthenticated } = useAuthStore();
  const navigate = useNavigate();

  const {
    handleSubmit,
    setValue,
    watch,
    reset,
    setError,
    formState: { errors },
  } = useForm<CreateReservationFormData>({
    resolver: zodResolver(createReservationSchema),
    defaultValues: {
      branchId: item?.branchId || '',
      medicineId: item?.medicineId || '',
      quantity: 1,
    },
  });

  useEffect(() => {
    if (item) {
      reset({
        branchId: item.branchId,
        medicineId: item.medicineId,
        quantity: 1,
      });
      setServerError(null);
    }
  }, [item, reset]);

  if (!item) return null;

  const quantity = watch('quantity') || 1;
  const maxAvailable = Math.min(item.availableQuantity, 10);
  const totalCost = item.price * quantity;

  const onSubmit = async (values: CreateReservationFormData) => {
    if (!isAuthenticated) {
      navigate('/login', { state: { from: { pathname: '/customer/search' } } });
      return;
    }

    setServerError(null);
    setIsSubmitting(true);

    try {
      await customerApi.createReservation(values);
      onSuccess();
      onClose();
    } catch (err: unknown) {
      if (err instanceof ApiError) {
        if (err.isValidationError) {
          const qtyErr = err.getFieldError('quantity');
          if (qtyErr) {
            setError('quantity', { message: qtyErr });
          }
        }
        setServerError(err.detail || err.title);
      } else if (err instanceof Error) {
        setServerError(err.message);
      } else {
        setServerError('Failed to create reservation hold. The stock may have changed.');
      }
    } finally {
      setIsSubmitting(false);
    }
  };

  const { t, language } = useTranslation();

  return (
    <Dialog open={isOpen} onOpenChange={onClose}>
      <DialogContent className="sm:max-w-md dark:bg-slate-900 dark:border-slate-800">
        <DialogHeader>
          <DialogTitle className="text-xl font-bold text-slate-900 dark:text-slate-100">
            {t('customer.reserveDialogTitle')}
          </DialogTitle>
          <DialogDescription className="text-slate-500 dark:text-slate-400">
            {t('customer.reserveDialogDesc')}
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={handleSubmit(onSubmit)}>
          <div className="space-y-4 py-2">
            {serverError && (
              <Alert variant="destructive">
                <AlertCircle className="h-4 w-4" />
                <AlertDescription>{serverError}</AlertDescription>
              </Alert>
            )}

            {/* Medicine & Branch Details */}
            <div className="rounded-lg bg-slate-50 dark:bg-slate-800/60 p-4 border border-slate-200 dark:border-slate-700 text-sm space-y-2">
              <div>
                <span className="font-bold text-slate-900 dark:text-slate-100 text-base">{item.medicineBrandName}</span>
                <p className="text-xs text-slate-500 dark:text-slate-400">{item.medicineGenericName} • {item.strength} • {item.dosageForm}</p>
              </div>
              <div className="pt-2 border-t border-slate-200 dark:border-slate-700">
                <p className="font-semibold text-slate-800 dark:text-slate-200">{item.pharmacyName} — {item.branchName}</p>
                <p className="text-xs text-slate-500 dark:text-slate-400">{item.branchAddress}, {item.branchCity}</p>
              </div>
            </div>

            {/* Quantity Selector */}
            <div className="space-y-1 border-y border-slate-200 dark:border-slate-700 py-3">
              <div className="flex items-center justify-between">
                <div>
                  <label className="text-sm font-semibold text-slate-800 dark:text-slate-200">
                    {language === 'ar' ? 'كمية الحجز المطلوبة' : 'Reservation Quantity'}
                  </label>
                  <p className="text-xs text-slate-500 dark:text-slate-400">
                    {language === 'ar' ? `الكمية المتوفرة: ${item.availableQuantity} وحدة` : `Available: ${item.availableQuantity} units`}
                  </p>
                </div>
                <div className="flex items-center gap-2">
                  <Button
                    type="button"
                    variant="outline"
                    size="sm"
                    disabled={quantity <= 1}
                    onClick={() => setValue('quantity', Math.max(1, quantity - 1), { shouldValidate: true })}
                  >
                    -
                  </Button>
                  <span className="w-8 text-center font-bold text-slate-900 dark:text-slate-100">{quantity}</span>
                  <Button
                    type="button"
                    variant="outline"
                    size="sm"
                    disabled={quantity >= maxAvailable}
                    onClick={() => setValue('quantity', Math.min(maxAvailable, quantity + 1), { shouldValidate: true })}
                  >
                    +
                  </Button>
                </div>
              </div>
              {errors.quantity && (
                <p className="text-xs text-red-600 font-medium">{errors.quantity.message}</p>
              )}
            </div>

            {/* Pricing & Policy Details */}
            <div className="flex justify-between items-center text-sm font-semibold">
              <span className="text-slate-600 dark:text-slate-400">
                {language === 'ar' ? 'السعر الإجمالي:' : 'Total Price:'}
              </span>
              <span className="text-lg font-bold text-blue-700 dark:text-blue-400">{formatCurrency(totalCost, item.currency)}</span>
            </div>

            <div className="flex items-start gap-2 rounded-md bg-blue-50/70 dark:bg-blue-950/40 p-3 text-xs text-blue-900 dark:text-blue-200 border border-blue-100 dark:border-blue-900/60">
              <Clock className="h-4 w-4 text-blue-600 dark:text-blue-400 shrink-0 mt-0.5" />
              <span>
                {language === 'ar' ? (
                  <>
                    يتم حجز الدواء باسمك لمدة <strong>60 دقيقة (ساعة واحدة)</strong>. وسيتم إعادة الكمية للمخزون تلقائياً في حال عدم الاستلام قبل انتهاء المهلة.
                  </>
                ) : (
                  <>
                    Reservations are held for <strong>60 minutes (1 hour)</strong>. Stock will be automatically released back into inventory if not picked up before expiration.
                  </>
                )}
              </span>
            </div>
          </div>

          <DialogFooter className="gap-2 sm:gap-0 mt-4">
            <Button type="button" variant="outline" onClick={onClose} disabled={isSubmitting}>
              {t('common.cancel')}
            </Button>
            <Button type="submit" disabled={isSubmitting || item.availableQuantity <= 0}>
              {isSubmitting
                ? (language === 'ar' ? 'جارٍ تأكيد الحجز...' : 'Reserving...')
                : isAuthenticated
                ? (language === 'ar' ? 'تأكيد الحجز فوراً' : 'Confirm Hold')
                : (language === 'ar' ? 'تسجيل الدخول وتأكيد الحجز' : 'Sign In & Reserve')}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
