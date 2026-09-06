import { PagedResult } from './api';

export interface MedicineAvailabilityDto {
  inventoryId: string;
  medicineId: string;
  medicineBrandName: string;
  medicineGenericName: string;
  strength: string;
  dosageForm: string;
  pharmacyId: string;
  pharmacyName: string;
  branchId: string;
  branchName: string;
  branchAddress: string;
  branchCity: string;
  branchPhone: string;
  openingHours: string | null;
  branchLatitude: number;
  branchLongitude: number;
  distanceKm: number | null;
  availableQuantity: number;
  stockStatus: string;
  price: number;
  currency: string;
  freshness: string;
  lastUpdatedAt: string;
}

export interface AvailabilitySearchRequest {
  q?: string;
  query?: string;
  medicineId?: string;
  barcode?: string;
  alias?: string;
  latitude?: number;
  longitude?: number;
  radiusKm?: number;
  sort?: string;
  sortBy?: string;
  page?: number;
  pageSize?: number;
}

export type AvailabilitySearchResult = PagedResult<MedicineAvailabilityDto>;
