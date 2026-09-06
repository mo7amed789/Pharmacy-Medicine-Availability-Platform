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

  return (
    <Dialog open={isOpen} onOpenChange={onClose}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle className="text-xl font-bold text-slate-900">Place Reservation Hold</DialogTitle>
          <DialogDescription className="text-slate-500">
            Lock inventory units at the branch for customer pickup.
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
            <div className="rounded-lg bg-slate-50 p-4 border border-slate-200 text-sm space-y-2">
              <div>
                <span className="font-bold text-slate-900 text-base">{item.medicineBrandName}</span>
                <p className="text-xs text-slate-500">{item.medicineGenericName} • {item.strength} • {item.dosageForm}</p>
              </div>
              <div className="pt-2 border-t border-slate-200">
                <p className="font-semibold text-slate-800">{item.pharmacyName} — {item.branchName}</p>
                <p className="text-xs text-slate-500">{item.branchAddress}, {item.branchCity}</p>
              </div>
            </div>

            {/* Quantity Selector */}
            <div className="space-y-1 border-y border-slate-200 py-3">
              <div className="flex items-center justify-between">
                <div>
                  <label className="text-sm font-semibold text-slate-800">Reservation Quantity</label>
                  <p className="text-xs text-slate-500">Available: {item.availableQuantity} units</p>
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
                  <span className="w-8 text-center font-bold text-slate-900">{quantity}</span>
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
              <span className="text-slate-600">Total Price:</span>
              <span className="text-lg font-bold text-blue-700">{formatCurrency(totalCost, item.currency)}</span>
            </div>

            <div className="flex items-start gap-2 rounded-md bg-blue-50/70 p-3 text-xs text-blue-900 border border-blue-100">
              <Clock className="h-4 w-4 text-blue-600 shrink-0 mt-0.5" />
              <span>
                Reservations are held for <strong>120 minutes</strong>. Stock will be automatically released back into inventory if not picked up before expiration.
              </span>
            </div>
          </div>

          <DialogFooter className="gap-2 sm:gap-0 mt-4">
            <Button type="button" variant="outline" onClick={onClose} disabled={isSubmitting}>
              Cancel
            </Button>
            <Button type="submit" disabled={isSubmitting || item.availableQuantity <= 0}>
              {isSubmitting ? 'Reserving...' : isAuthenticated ? 'Confirm Hold' : 'Sign In & Reserve'}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
