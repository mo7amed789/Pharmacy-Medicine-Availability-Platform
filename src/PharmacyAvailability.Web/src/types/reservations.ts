export type ReservationStatus = 'Active' | 'Confirmed' | 'Cancelled' | 'Expired' | 'Released';

export interface CreateReservationRequest {
  branchId: string;
  medicineId: string;
  quantity: number;
}

export interface ReservationDto {
  id: string;
  customerId: string;
  pharmacyId: string;
  pharmacyName: string;
  branchId: string;
  branchName: string;
  branchAddress: string;
  medicineId: string;
  medicineBrandName: string;
  medicineGenericName: string;
  quantity: number;
  status: ReservationStatus;
  createdAt: string;
  expiresAt: string;
  confirmedAt: string | null;
  releasedAt: string | null;
  cancelledAt: string | null;
  expiredAt: string | null;
}

export interface AdminReleaseReservationRequest {
  reason: string;
}

export interface GetReservationsQuery {
  status?: string;
  page?: number;
  pageSize?: number;
}
