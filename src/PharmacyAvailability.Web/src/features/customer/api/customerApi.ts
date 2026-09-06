import { apiClient } from '@/api/apiClient';
import { ENDPOINTS } from '@/api/endpoints';
import { AvailabilitySearchRequest, AvailabilitySearchResult } from '@/types/availability';
import { CategoryDto, MedicineDto, PagedResult } from '@/types/catalog';
import { PharmacyBranchDto, PharmacyDto } from '@/types/pharmacies';
import {
  CreateReservationRequest,
  GetReservationsQuery,
  ReservationDto,
} from '@/types/reservations';

export const customerApi = {
  searchAvailability: async (
    params: AvailabilitySearchRequest,
    signal?: AbortSignal
  ): Promise<AvailabilitySearchResult> => {
    const res = await apiClient.get<AvailabilitySearchResult>(ENDPOINTS.AVAILABILITY_SEARCH, {
      params,
      signal,
    });
    return res.data;
  },

  searchMedicineAvailability: async (
    medicineId: string,
    params: AvailabilitySearchRequest,
    signal?: AbortSignal
  ): Promise<AvailabilitySearchResult> => {
    const res = await apiClient.get<AvailabilitySearchResult>(
      ENDPOINTS.MEDICINE_AVAILABILITY(medicineId),
      { params, signal }
    );
    return res.data;
  },

  getMedicineById: async (id: string): Promise<MedicineDto> => {
    const res = await apiClient.get<MedicineDto>(ENDPOINTS.MEDICINE_BY_ID(id));
    return res.data;
  },

  getCategories: async (): Promise<CategoryDto[]> => {
    const res = await apiClient.get<CategoryDto[]>(ENDPOINTS.CATEGORIES);
    return res.data;
  },

  getActivePharmacies: async (city?: string): Promise<PharmacyDto[]> => {
    const res = await apiClient.get<PharmacyDto[]>(ENDPOINTS.ACTIVE_PHARMACIES, {
      params: city ? { city } : undefined,
    });
    return res.data;
  },

  getActiveBranches: async (city?: string): Promise<PharmacyBranchDto[]> => {
    const res = await apiClient.get<PharmacyBranchDto[]>(ENDPOINTS.ACTIVE_BRANCHES, {
      params: city ? { city } : undefined,
    });
    return res.data;
  },

  getBranchById: async (branchId: string): Promise<PharmacyBranchDto> => {
    const res = await apiClient.get<PharmacyBranchDto>(ENDPOINTS.BRANCH_BY_ID(branchId));
    return res.data;
  },

  // Customer Reservations
  createReservation: async (request: CreateReservationRequest): Promise<ReservationDto> => {
    const res = await apiClient.post<ReservationDto>(ENDPOINTS.RESERVATIONS, request);
    return res.data;
  },

  getMyReservations: async (
    query?: GetReservationsQuery,
    signal?: AbortSignal
  ): Promise<PagedResult<ReservationDto>> => {
    const res = await apiClient.get<PagedResult<ReservationDto>>(ENDPOINTS.RESERVATIONS, {
      params: query,
      signal,
    });
    return res.data;
  },

  getReservationById: async (id: string): Promise<ReservationDto> => {
    const res = await apiClient.get<ReservationDto>(ENDPOINTS.RESERVATION_BY_ID(id));
    return res.data;
  },

  cancelReservation: async (id: string): Promise<ReservationDto> => {
    const res = await apiClient.post<ReservationDto>(ENDPOINTS.CANCEL_RESERVATION(id));
    return res.data;
  },
};
