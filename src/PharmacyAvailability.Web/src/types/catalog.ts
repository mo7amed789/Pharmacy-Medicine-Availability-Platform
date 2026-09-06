export type { PagedResult } from './api';

export interface CategoryDto {
  id: string;
  name: string;
  description: string | null;
  isActive: boolean;
}

export interface ManufacturerDto {
  id: string;
  name: string;
  country: string | null;
  isActive: boolean;
}

export interface BarcodeDto {
  id: string;
  barcode: string;
  isPrimary: boolean;
}

export interface AliasDto {
  id: string;
  alias: string;
}

export interface MedicineDto {
  id: string;
  brandName: string;
  genericName: string;
  activeIngredient: string;
  strength: string;
  dosageForm: string;
  categoryId: string;
  categoryName: string;
  manufacturerId: string;
  manufacturerName: string;
  requiresPrescription: boolean;
  isActive: boolean;
  barcodes: BarcodeDto[];
  aliases: AliasDto[];
  createdAt: string;
}

export interface CreateMedicineRequest {
  brandName: string;
  genericName: string;
  activeIngredient: string;
  strength: string;
  dosageForm: string;
  categoryId: string;
  manufacturerId: string;
  requiresPrescription: boolean;
  initialBarcode?: string | null;
}

export interface UpdateMedicineRequest {
  brandName: string;
  genericName: string;
  activeIngredient: string;
  strength: string;
  dosageForm: string;
  categoryId: string;
  manufacturerId: string;
  requiresPrescription: boolean;
}

export interface UpdateMedicineStatusRequest {
  isActive: boolean;
}

export interface CreateCategoryRequest {
  name: string;
  description?: string | null;
}

export interface UpdateCategoryRequest {
  name: string;
  description?: string | null;
  isActive: boolean;
}

export interface CreateManufacturerRequest {
  name: string;
  country: string;
}

export interface UpdateManufacturerRequest {
  name: string;
  country: string;
  isActive: boolean;
}

export interface AddBarcodeRequest {
  barcode: string;
  isPrimary?: boolean;
}

export interface AddAliasRequest {
  alias: string;
}
