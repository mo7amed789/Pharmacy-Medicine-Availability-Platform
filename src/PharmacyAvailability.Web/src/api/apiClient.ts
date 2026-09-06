import axios, { AxiosError, InternalAxiosRequestConfig } from 'axios';
import { tokenManager } from './tokenManager';
import { ApiProblemDetails } from '@/types/api';
import { AuthResponse } from '@/types/auth';
import { useAuthStore } from '@/app/store/authStore';

export class ApiError extends Error {
  public status: number;
  public title: string;
  public detail: string;
  public correlationId?: string;
  public validationErrors?: Record<string, string[]>;

  constructor(problem: ApiProblemDetails) {
    super(problem.detail || problem.title || 'An unexpected error occurred');
    this.name = 'ApiError';
    this.status = problem.status;
    this.title = problem.title;
    this.detail = problem.detail || problem.title;
    this.correlationId = problem.correlationId;
    this.validationErrors = problem.errors;
  }

  public getFieldError(field: string): string | undefined {
    if (!this.validationErrors) return undefined;
    const lower = field.toLowerCase();
    for (const key of Object.keys(this.validationErrors)) {
      if (key.toLowerCase() === lower) {
        const msgs = this.validationErrors[key];
        return msgs && msgs.length > 0 ? msgs[0] : undefined;
      }
    }
    return undefined;
  }

  public getAllFieldErrors(): Record<string, string> {
    if (!this.validationErrors) return {};
    const result: Record<string, string> = {};
    for (const [k, v] of Object.entries(this.validationErrors)) {
      if (v && v.length > 0) {
        result[k] = v[0];
      }
    }
    return result;
  }

  public get isValidationError(): boolean {
    return this.status === 400 && !!this.validationErrors && Object.keys(this.validationErrors).length > 0;
  }

  public get isNotFound(): boolean {
    return this.status === 404;
  }

  public get isConflict(): boolean {
    return this.status === 409;
  }

  public get isUnauthorized(): boolean {
    return this.status === 401;
  }

  public get isForbidden(): boolean {
    return this.status === 403;
  }
}

export const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || '/api/v1',
  timeout: 20000,
  headers: {
    'Content-Type': 'application/json',
    Accept: 'application/json',
  },
  withCredentials: true,
});

// Request Interceptor: Attach Access Token and Correlation ID
apiClient.interceptors.request.use((config: InternalAxiosRequestConfig) => {
  const token = tokenManager.getAccessToken();
  if (token && config.headers) {
    config.headers.Authorization = `Bearer ${token}`;
  }

  // Inject or propagate UUID v4 correlation ID
  if (!config.headers['X-Correlation-Id']) {
    config.headers['X-Correlation-Id'] = crypto.randomUUID();
  }

  return config;
});

// Response Interceptor: Handle RFC 7807 ProblemDetails and Concurrency-Safe 401 Token Refresh
apiClient.interceptors.response.use(
  (response) => response,
  async (error: AxiosError<ApiProblemDetails>) => {
    const originalRequest = error.config as (InternalAxiosRequestConfig & { _retry?: boolean }) | undefined;

    // Check if error response exists
    if (!error.response || !originalRequest) {
      return Promise.reject(
        new ApiError({
          status: 0,
          title: 'Network Error',
          detail: error.message || 'Unable to connect to the server.',
        })
      );
    }

    const isAuthEndpoint =
      originalRequest.url?.includes('/auth/login') ||
      originalRequest.url?.includes('/auth/refresh') ||
      originalRequest.url?.includes('/auth/register');

    // If 401 and not an auth endpoint, handle refresh mutex
    if (error.response.status === 401 && !isAuthEndpoint && !originalRequest._retry) {
      if (tokenManager.getIsRefreshing()) {
        // Queue the request until active refresh completes
        return new Promise<string>((resolve, reject) => {
          tokenManager.addQueue(resolve, reject);
        })
          .then((newToken) => {
            originalRequest.headers.Authorization = `Bearer ${newToken}`;
            return apiClient(originalRequest);
          })
          .catch((err) => Promise.reject(err));
      }

      originalRequest._retry = true;
      tokenManager.setIsRefreshing(true);

      const refreshToken = tokenManager.getRefreshToken();
      if (!refreshToken) {
        tokenManager.setIsRefreshing(false);
        useAuthStore.getState().logout();
        return Promise.reject(
          new ApiError({
            status: 401,
            title: 'Session Expired',
            detail: 'Please sign in to continue.',
          })
        );
      }

      try {
        const refreshResponse = await axios.post<AuthResponse>(
          `${apiClient.defaults.baseURL}/auth/refresh`,
          { refreshToken },
          {
            headers: {
              'Content-Type': 'application/json',
              Accept: 'application/json',
            },
          }
        );

        const { accessToken: newAccess, refreshToken: newRefresh, user } = refreshResponse.data;
        tokenManager.setAccessToken(newAccess);
        tokenManager.setRefreshToken(newRefresh);
        useAuthStore.getState().setSession(user, newAccess, newRefresh);

        // Resume all queued requests with the new token
        tokenManager.processQueue(null, newAccess);

        originalRequest.headers.Authorization = `Bearer ${newAccess}`;
        return apiClient(originalRequest);
      } catch (refreshErr) {
        tokenManager.processQueue(refreshErr, null);
        tokenManager.clearTokens();
        useAuthStore.getState().logout();
        return Promise.reject(
          new ApiError({
            status: 401,
            title: 'Session Invalidation',
            detail: 'Your session has expired. Please sign in again.',
          })
        );
      } finally {
        tokenManager.setIsRefreshing(false);
      }
    }

    // Map RFC 7807 ProblemDetails from ASP.NET Core response
    const data = error.response.data as any;
    let title = error.response.statusText || 'Error';
    let detail = error.message;
    let errors: Record<string, string[]> | undefined = undefined;

    if (typeof data === 'string') {
      detail = data;
    } else if (data && typeof data === 'object') {
      title = data.title || title;
      detail = data.detail || data.title || detail;
      errors = data.errors;
    }

    const problem: ApiProblemDetails = {
      status: error.response.status,
      title,
      detail,
      instance: typeof data === 'object' ? data?.instance : undefined,
      correlationId: (error.response.headers['x-correlation-id'] as string) || (typeof data === 'object' ? data?.correlationId : undefined),
      errors,
    };

    return Promise.reject(new ApiError(problem));
  }
);
