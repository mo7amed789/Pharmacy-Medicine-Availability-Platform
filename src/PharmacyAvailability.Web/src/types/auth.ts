export type UserRole = 'Customer' | 'PharmacyStaff' | 'PharmacyAdmin' | 'PlatformAdmin';

export interface UserDto {
  id: string;
  email: string;
  fullName: string;
  phoneNumber: string;
  role: UserRole;
  pharmacyId: string | null;
  isEmailVerified: boolean;
  isPhoneVerified: boolean;
}

export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
  user: UserDto;
}

export interface RegisterRequest {
  email: string;
  password: string;
  fullName: string;
  phoneNumber: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RefreshTokenRequest {
  refreshToken: string;
}

export interface RevokeTokenRequest {
  refreshToken: string;
}

export interface VerifyCodeRequest {
  type: 'Email' | 'Phone';
  code: string;
}

export interface SendVerificationRequest {
  type: 'Email' | 'Phone';
}

export interface VerificationStatusResponse {
  isEmailVerified: boolean;
  isPhoneVerified: boolean;
}
