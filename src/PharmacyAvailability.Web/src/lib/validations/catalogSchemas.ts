import { z } from 'zod';

export const medicineSchema = z.object({
  brandName: z.string().trim().min(2, 'Brand name is required').max(150),
  genericName: z.string().trim().min(2, 'Generic name is required').max(150),
  activeIngredient: z.string().trim().min(2, 'Active ingredient is required').max(200),
  strength: z.string().trim().min(1, 'Strength is required').max(50),
  dosageForm: z.string().trim().min(1, 'Dosage form is required').max(50),
  categoryId: z.string().uuid('Please select a valid category'),
  manufacturerId: z.string().uuid('Please select a valid manufacturer'),
  requiresPrescription: z.boolean(),
  initialBarcode: z.string().max(50).optional().or(z.literal('')),
});

export type MedicineFormData = z.infer<typeof medicineSchema>;

export const categorySchema = z.object({
  name: z.string().trim().min(2, 'Category name is required').max(100),
  description: z.string().max(500).optional().or(z.literal('')),
});

export type CategoryFormData = z.infer<typeof categorySchema>;

export const manufacturerSchema = z.object({
  name: z.string().trim().min(2, 'Manufacturer name is required').max(150),
  country: z.string().trim().min(2, 'Country is required').max(100),
});

export type ManufacturerFormData = z.infer<typeof manufacturerSchema>;

export const barcodeSchema = z.object({
  barcode: z.string().trim().min(4, 'Barcode must be at least 4 characters').max(50),
  isPrimary: z.boolean().default(false),
});

export type BarcodeFormData = z.infer<typeof barcodeSchema>;

export const aliasSchema = z.object({
  alias: z.string().trim().min(2, 'Alias must be at least 2 characters').max(150),
});

export type AliasFormData = z.infer<typeof aliasSchema>;
