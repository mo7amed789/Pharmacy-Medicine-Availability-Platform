import { apiClient } from '@/api/apiClient';
import { ENDPOINTS } from '@/api/endpoints';
import {
  AuthResponse,
  LoginRequest,
  RegisterRequest,
  RefreshTokenRequest,
  RevokeTokenRequest,
  UserDto,
  VerifyCodeRequest,
  SendVerificationRequest,
  VerificationStatusResponse,
} from '@/types/auth';
import { RegisterPharmacyRequest, PharmacyDto } from '@/types/pharmacies';

export const authApi = {
  login: async (request: LoginRequest): Promise<AuthResponse> => {
    const res = await apiClient.post<AuthResponse>(ENDPOINTS.AUTH_LOGIN, request);
    return res.data;
  },

  register: async (request: RegisterRequest): Promise<AuthResponse> => {
    const res = await apiClient.post<AuthResponse>(ENDPOINTS.AUTH_REGISTER, request);
    return res.data;
  },

  registerPharmacy: async (request: RegisterPharmacyRequest): Promise<PharmacyDto> => {
    const res = await apiClient.post<PharmacyDto>(ENDPOINTS.REGISTER_PHARMACY, request);
    return res.data;
  },

  refreshToken: async (request: RefreshTokenRequest): Promise<AuthResponse> => {
    const res = await apiClient.post<AuthResponse>(ENDPOINTS.AUTH_REFRESH, request);
    return res.data;
  },

  revokeToken: async (request: RevokeTokenRequest): Promise<void> => {
    await apiClient.post(ENDPOINTS.AUTH_REVOKE, request);
  },

  getMe: async (): Promise<UserDto> => {
    const res = await apiClient.get<UserDto>(ENDPOINTS.AUTH_ME);
    return res.data;
  },

  verifyCode: async (request: VerifyCodeRequest): Promise<VerificationStatusResponse> => {
    const res = await apiClient.post<VerificationStatusResponse>(ENDPOINTS.AUTH_VERIFY, request);
    return res.data;
  },

  resendCode: async (request: SendVerificationRequest): Promise<{ message: string }> => {
    const res = await apiClient.post<{ message: string }>(ENDPOINTS.AUTH_VERIFY_RESEND, request);
    return res.data;
  },

  getVerificationStatus: async (): Promise<VerificationStatusResponse> => {
    const res = await apiClient.get<VerificationStatusResponse>(ENDPOINTS.AUTH_VERIFY_STATUS);
    return res.data;
  },
};
