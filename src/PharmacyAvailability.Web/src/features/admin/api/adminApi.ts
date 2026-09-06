import { apiClient } from '@/api/apiClient';
import { ENDPOINTS } from '@/api/endpoints';
import {
  AddAliasRequest,
  AddBarcodeRequest,
  BarcodeDto,
  CategoryDto,
  CreateCategoryRequest,
  CreateManufacturerRequest,
  CreateMedicineRequest,
  ManufacturerDto,
  MedicineDto,
  PagedResult,
  UpdateCategoryRequest,
  UpdateManufacturerRequest,
  UpdateMedicineRequest,
} from '@/types/catalog';
import { PharmacyDto, PharmacyStatus } from '@/types/pharmacies';
import {
  AdminReleaseReservationRequest,
  GetReservationsQuery,
  ReservationDto,
} from '@/types/reservations';

export const adminApi = {
  // Pharmacy Onboarding and Lifecycle
  getAllPharmacies: async (status?: PharmacyStatus, signal?: AbortSignal): Promise<PharmacyDto[]> => {
    const res = await apiClient.get<PharmacyDto[]>(ENDPOINTS.ADMIN.PHARMACIES, {
      params: status ? { status } : undefined,
      signal,
    });
    return res.data;
  },

  approvePharmacy: async (id: string): Promise<PharmacyDto> => {
    const res = await apiClient.post<PharmacyDto>(ENDPOINTS.ADMIN.APPROVE_PHARMACY(id));
    return res.data;
  },

  suspendPharmacy: async (id: string): Promise<PharmacyDto> => {
    const res = await apiClient.post<PharmacyDto>(ENDPOINTS.ADMIN.SUSPEND_PHARMACY(id));
    return res.data;
  },

  reactivatePharmacy: async (id: string): Promise<PharmacyDto> => {
    const res = await apiClient.post<PharmacyDto>(ENDPOINTS.ADMIN.REACTIVATE_PHARMACY(id));
    return res.data;
  },

  deactivatePharmacy: async (id: string): Promise<PharmacyDto> => {
    const res = await apiClient.post<PharmacyDto>(ENDPOINTS.ADMIN.DEACTIVATE_PHARMACY(id));
    return res.data;
  },

  // Central Catalog Management
  createMedicine: async (request: CreateMedicineRequest): Promise<MedicineDto> => {
    const res = await apiClient.post<MedicineDto>(ENDPOINTS.ADMIN.MEDICINES, request);
    return res.data;
  },

  updateMedicine: async (id: string, request: UpdateMedicineRequest): Promise<MedicineDto> => {
    const res = await apiClient.put<MedicineDto>(ENDPOINTS.ADMIN.UPDATE_MEDICINE(id), request);
    return res.data;
  },

  updateMedicineStatus: async (id: string, isActive: boolean): Promise<MedicineDto> => {
    const res = await apiClient.patch<MedicineDto>(
      ENDPOINTS.ADMIN.UPDATE_MEDICINE_STATUS(id),
      { isActive }
    );
    return res.data;
  },

  addBarcode: async (medicineId: string, request: AddBarcodeRequest): Promise<BarcodeDto> => {
    const res = await apiClient.post<BarcodeDto>(
      ENDPOINTS.ADMIN.ADD_BARCODE(medicineId),
      request
    );
    return res.data;
  },

  addAlias: async (medicineId: string, request: AddAliasRequest): Promise<void> => {
    await apiClient.post(ENDPOINTS.ADMIN.ADD_ALIAS(medicineId), request);
  },

  createCategory: async (request: CreateCategoryRequest): Promise<CategoryDto> => {
    const res = await apiClient.post<CategoryDto>(ENDPOINTS.ADMIN.CATEGORIES, request);
    return res.data;
  },

  updateCategory: async (id: string, request: UpdateCategoryRequest): Promise<CategoryDto> => {
    const res = await apiClient.put<CategoryDto>(
      ENDPOINTS.ADMIN.UPDATE_CATEGORY(id),
      request
    );
    return res.data;
  },

  createManufacturer: async (request: CreateManufacturerRequest): Promise<ManufacturerDto> => {
    const res = await apiClient.post<ManufacturerDto>(ENDPOINTS.ADMIN.MANUFACTURERS, request);
    return res.data;
  },

  updateManufacturer: async (
    id: string,
    request: UpdateManufacturerRequest
  ): Promise<ManufacturerDto> => {
    const res = await apiClient.put<ManufacturerDto>(
      ENDPOINTS.ADMIN.UPDATE_MANUFACTURER(id),
      request
    );
    return res.data;
  },

  // Administrative Reservation Release & Oversight
  getAllReservations: async (
    query?: GetReservationsQuery,
    signal?: AbortSignal
  ): Promise<PagedResult<ReservationDto>> => {
    const res = await apiClient.get<PagedResult<ReservationDto>>(ENDPOINTS.RESERVATIONS, {
      params: query,
      signal,
    });
    return res.data;
  },

  releaseReservation: async (
    reservationId: string,
    request: AdminReleaseReservationRequest
  ): Promise<ReservationDto> => {
    const res = await apiClient.post<ReservationDto>(
      ENDPOINTS.RELEASE_RESERVATION(reservationId),
      request
    );
    return res.data;
  },
};
