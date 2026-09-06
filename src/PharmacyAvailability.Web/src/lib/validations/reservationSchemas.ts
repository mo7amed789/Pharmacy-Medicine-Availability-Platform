import { z } from 'zod';

export const createReservationSchema = z.object({
  branchId: z.string().uuid('Invalid branch identifier'),
  medicineId: z.string().uuid('Invalid medicine identifier'),
  quantity: z
    .number({ invalid_type_error: 'Quantity must be a number' })
    .int('Quantity must be a whole number')
    .min(1, 'Quantity must be at least 1')
    .max(100, 'Quantity cannot exceed 100 units'),
});

export type CreateReservationFormData = z.infer<typeof createReservationSchema>;

export const adminReleaseReservationSchema = z.object({
  reason: z
    .string()
    .trim()
    .min(3, 'Reason must be at least 3 characters')
    .max(500, 'Reason cannot exceed 500 characters'),
});

export type AdminReleaseReservationFormData = z.infer<typeof adminReleaseReservationSchema>;
