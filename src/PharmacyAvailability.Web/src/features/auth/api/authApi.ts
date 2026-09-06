import { apiClient } from '@/api/apiClient';
import { ENDPOINTS } from '@/api/endpoints';
import {
  AuthResponse,
  LoginRequest,
  RegisterRequest,
  RefreshTokenRequest,
  RevokeTokenRequest,
  UserDto,
} from '@/types/auth';

export const authApi = {
  login: async (request: LoginRequest): Promise<AuthResponse> => {
    const res = await apiClient.post<AuthResponse>(ENDPOINTS.AUTH_LOGIN, request);
    return res.data;
  },

  register: async (request: RegisterRequest): Promise<AuthResponse> => {
    const res = await apiClient.post<AuthResponse>(ENDPOINTS.AUTH_REGISTER, request);
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
};
