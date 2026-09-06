export type PharmacyStatus = 'Pending' | 'Active' | 'Suspended' | 'Deactivated' | 'Approved';

export interface PharmacyDto {
  id: string;
  name: string;
  licenseNumber: string;
  address: string;
  city: string;
  latitude: number;
  longitude: number;
  phone: string;
  email: string | null;
  openingHours: string | null;
  status: PharmacyStatus;
  branchesCount: number;
  createdAt: string;
}

export interface PharmacyBranchDto {
  id: string;
  pharmacyId: string;
  pharmacyName: string;
  name: string;
  branchCode: string | null;
  address: string;
  city: string;
  latitude: number;
  longitude: number;
  phone: string;
  openingHours: string | null;
  isActive: boolean;
  createdAt: string;
}

export interface PharmacyDetailDto {
  pharmacy: PharmacyDto;
  branches: PharmacyBranchDto[];
}

export interface RegisterPharmacyRequest {
  name: string;
  licenseNumber: string;
  address: string;
  city: string;
  latitude: number;
  longitude: number;
  phone: string;
  email?: string | null;
  openingHours?: string | null;
  adminFullName: string;
  adminEmail: string;
  adminPassword: string;
  adminPhone?: string | null;
}

export interface CreateBranchRequest {
  name: string;
  address: string;
  city: string;
  latitude: number;
  longitude: number;
  phone: string;
  openingHours?: string | null;
  branchCode?: string | null;
  isActive?: boolean;
}

export interface UpdateBranchRequest {
  name: string;
  address: string;
  city: string;
  latitude: number;
  longitude: number;
  phone: string;
  openingHours?: string | null;
  branchCode?: string | null;
}

export interface UpdateBranchStatusRequest {
  isActive: boolean;
}

export interface UpdatePharmacyProfileRequest {
  name: string;
  address: string;
  city: string;
  phone: string;
  email?: string | null;
  openingHours?: string | null;
}

export interface InviteStaffRequest {
  email: string;
  password: string;
  fullName: string;
  phoneNumber?: string | null;
  role?: string;
}

export interface PharmacyStaffDto {
  id: string;
  pharmacyId: string;
  email: string;
  fullName: string;
  phoneNumber: string | null;
  role: string;
  isActive: boolean;
  createdAt: string;
}
