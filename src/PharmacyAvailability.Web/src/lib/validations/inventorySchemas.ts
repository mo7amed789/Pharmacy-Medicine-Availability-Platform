import { z } from 'zod';

export const adjustStockSchema = z.object({
  deltaQuantity: z
    .number({ invalid_type_error: 'Delta quantity must be a number' })
    .int('Quantity change must be a whole number')
    .refine((val) => val !== 0, 'Adjustment delta cannot be zero'),
  reason: z
    .string()
    .trim()
    .min(2, 'Reason must be at least 2 characters')
    .max(100, 'Reason cannot exceed 100 characters'),
  notes: z
    .string()
    .max(500, 'Notes cannot exceed 500 characters')
    .optional(),
});

export type AdjustStockFormData = z.infer<typeof adjustStockSchema>;

export const updatePriceSchema = z.object({
  newPrice: z
    .number({ invalid_type_error: 'Price must be a number' })
    .min(0, 'Price cannot be negative'),
});

export type UpdatePriceFormData = z.infer<typeof updatePriceSchema>;
