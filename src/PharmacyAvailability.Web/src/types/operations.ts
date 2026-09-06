export interface BranchDashboardDto {
  pharmacyId: string;
  pharmacyName: string;
  branchId: string;
  branchName: string;
  activeReservationsCount: number;
  expiringSoonReservationsCount: number;
  todayConfirmedPickupsCount: number;
  todayCancelledReservationsCount: number;
  todayExpiredReservationsCount: number;
  lowStockItemsCount: number;
  totalActiveMedicinesCount: number;
  generatedAtUtc: string;
}

export interface StaffReservationQueueItemDto {
  reservationId: string;
  customerId: string;
  customerName: string;
  customerPhone: string;
  customerEmail: string;
  medicineId: string;
  medicineBrandName: string;
  medicineGenericName: string;
  dosageForm: string;
  strength: string;
  quantity: number;
  unitPrice: number;
  totalAmountDue: number;
  status: string;
  createdAt: string;
  expiresAt: string;
  remainingMinutes: number;
  isExpiringSoon: boolean;
}

export interface StaffReservationQueueQuery {
  search?: string;
  status?: string;
  expiringSoonOnly?: boolean;
  page?: number;
  pageSize?: number;
}

export interface LowStockAlertDto {
  inventoryId: string;
  medicineId: string;
  medicineBrandName: string;
  medicineGenericName: string;
  categoryName: string;
  manufacturerName: string;
  availableQuantity: number;
  reservedQuantity: number;
  totalPhysicalStock: number;
  price: number;
  threshold: number;
  lastUpdated: string;
}

export interface BranchActivityFeedItemDto {
  id: string;
  activityType: string;
  description: string;
  medicineId: string | null;
  medicineBrandName: string | null;
  deltaQuantity: number | null;
  resultingQuantity: number | null;
  actorRole: string | null;
  timestamp: string;
}
