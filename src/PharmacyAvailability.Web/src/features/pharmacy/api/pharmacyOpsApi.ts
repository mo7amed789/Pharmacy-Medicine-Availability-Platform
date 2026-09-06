import { apiClient } from '@/api/apiClient';
import { ENDPOINTS } from '@/api/endpoints';
import { PagedResult } from '@/types/catalog';
import {
  AdjustStockRequest,
  InventoryItemDto,
  InventoryTransactionDto,
  UpdatePriceRequest,
} from '@/types/inventory';
import {
  BranchActivityFeedItemDto,
  BranchDashboardDto,
  LowStockAlertDto,
  StaffReservationQueueItemDto,
  StaffReservationQueueQuery,
} from '@/types/operations';
import {
  InviteStaffRequest,
  PharmacyBranchDto,
  PharmacyDetailDto,
  PharmacyStaffDto,
} from '@/types/pharmacies';
import { ReservationDto } from '@/types/reservations';

export const pharmacyOpsApi = {
  // Operational Dashboard
  getDashboard: async (
    pharmacyId: string,
    branchId: string,
    signal?: AbortSignal
  ): Promise<BranchDashboardDto> => {
    const res = await apiClient.get<BranchDashboardDto>(
      ENDPOINTS.BRANCH_OPERATIONS(pharmacyId, branchId).DASHBOARD,
      { signal }
    );
    return res.data;
  },

  // Staff Reservation Queue
  getReservationQueue: async (
    pharmacyId: string,
    branchId: string,
    query?: StaffReservationQueueQuery,
    signal?: AbortSignal
  ): Promise<PagedResult<StaffReservationQueueItemDto>> => {
    const res = await apiClient.get<PagedResult<StaffReservationQueueItemDto>>(
      ENDPOINTS.BRANCH_OPERATIONS(pharmacyId, branchId).QUEUE,
      { params: query, signal }
    );
    return res.data;
  },

  // Confirm Pickup (Fulfillment)
  confirmPickup: async (reservationId: string): Promise<ReservationDto> => {
    const res = await apiClient.post<ReservationDto>(
      ENDPOINTS.CONFIRM_RESERVATION(reservationId)
    );
    return res.data;
  },

  // Low Stock Alerts
  getLowStockAlerts: async (
    pharmacyId: string,
    branchId: string,
    threshold: number = 5,
    page: number = 1,
    pageSize: number = 20,
    signal?: AbortSignal
  ): Promise<PagedResult<LowStockAlertDto>> => {
    const res = await apiClient.get<PagedResult<LowStockAlertDto>>(
      ENDPOINTS.BRANCH_OPERATIONS(pharmacyId, branchId).LOW_STOCK,
      { params: { threshold, page, pageSize }, signal }
    );
    return res.data;
  },

  // Branch Chronological Activity Feed
  getBranchActivity: async (
    pharmacyId: string,
    branchId: string,
    page: number = 1,
    pageSize: number = 20,
    signal?: AbortSignal
  ): Promise<PagedResult<BranchActivityFeedItemDto>> => {
    const res = await apiClient.get<PagedResult<BranchActivityFeedItemDto>>(
      ENDPOINTS.BRANCH_OPERATIONS(pharmacyId, branchId).ACTIVITY,
      { params: { page, pageSize }, signal }
    );
    return res.data;
  },

  // Inventory Management
  getBranchInventory: async (
    pharmacyId: string,
    branchId: string,
    signal?: AbortSignal
  ): Promise<InventoryItemDto[]> => {
    const res = await apiClient.get<InventoryItemDto[]>(
      ENDPOINTS.BRANCH_INVENTORY(pharmacyId, branchId).LIST,
      { signal }
    );
    return res.data;
  },

  adjustStock: async (
    pharmacyId: string,
    branchId: string,
    medicineId: string,
    request: AdjustStockRequest
  ): Promise<InventoryItemDto> => {
    const res = await apiClient.patch<InventoryItemDto>(
      ENDPOINTS.BRANCH_INVENTORY(pharmacyId, branchId).ADJUST_STOCK(medicineId),
      request
    );
    return res.data;
  },

  updatePrice: async (
    pharmacyId: string,
    branchId: string,
    medicineId: string,
    request: UpdatePriceRequest
  ): Promise<InventoryItemDto> => {
    const res = await apiClient.patch<InventoryItemDto>(
      ENDPOINTS.BRANCH_INVENTORY(pharmacyId, branchId).UPDATE_PRICE(medicineId),
      request
    );
    return res.data;
  },

  getTransactions: async (
    pharmacyId: string,
    branchId: string,
    medicineId: string
  ): Promise<InventoryTransactionDto[]> => {
    const res = await apiClient.get<InventoryTransactionDto[]>(
      ENDPOINTS.BRANCH_INVENTORY(pharmacyId, branchId).TRANSACTIONS(medicineId)
    );
    return res.data;
  },

  // Pharmacy Profile & Staff Management
  getPharmacyProfile: async (pharmacyId: string): Promise<PharmacyDetailDto> => {
    const res = await apiClient.get<PharmacyDetailDto>(
      ENDPOINTS.PHARMACY_MANAGEMENT(pharmacyId).PROFILE
    );
    return res.data;
  },

  getBranches: async (pharmacyId: string): Promise<PharmacyBranchDto[]> => {
    const res = await apiClient.get<PharmacyBranchDto[]>(
      ENDPOINTS.PHARMACY_MANAGEMENT(pharmacyId).BRANCHES
    );
    return res.data;
  },

  toggleBranchStatus: async (
    pharmacyId: string,
    branchId: string,
    isActive: boolean
  ): Promise<PharmacyBranchDto> => {
    const res = await apiClient.patch<PharmacyBranchDto>(
      ENDPOINTS.PHARMACY_MANAGEMENT(pharmacyId).BRANCH_STATUS(branchId),
      { isActive }
    );
    return res.data;
  },

  getStaff: async (pharmacyId: string): Promise<PharmacyStaffDto[]> => {
    const res = await apiClient.get<PharmacyStaffDto[]>(
      ENDPOINTS.PHARMACY_MANAGEMENT(pharmacyId).STAFF
    );
    return res.data;
  },

  inviteStaff: async (
    pharmacyId: string,
    request: InviteStaffRequest
  ): Promise<PharmacyStaffDto> => {
    const res = await apiClient.post<PharmacyStaffDto>(
      ENDPOINTS.PHARMACY_MANAGEMENT(pharmacyId).STAFF,
      request
    );
    return res.data;
  },
};
