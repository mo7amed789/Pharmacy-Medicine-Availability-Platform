export interface QueuedRequest {
  resolve: (token: string) => void;
  reject: (error: unknown) => void;
}

class TokenManager {
  private accessToken: string | null = null;
  private isRefreshing = false;
  private failedQueue: QueuedRequest[] = [];

  public getAccessToken(): string | null {
    return this.accessToken;
  }

  public setAccessToken(token: string | null): void {
    this.accessToken = token;
  }

  public getRefreshToken(): string | null {
    try {
      return localStorage.getItem('refresh_token');
    } catch {
      return null;
    }
  }

  public setRefreshToken(token: string | null): void {
    try {
      if (token) {
        localStorage.setItem('refresh_token', token);
      } else {
        localStorage.removeItem('refresh_token');
      }
    } catch {
      // Ignore localStorage errors in private mode or non-browser environments
    }
  }

  public clearTokens(): void {
    this.accessToken = null;
    this.setRefreshToken(null);
  }

  public getIsRefreshing(): boolean {
    return this.isRefreshing;
  }

  public setIsRefreshing(state: boolean): void {
    this.isRefreshing = state;
  }

  public addQueue(resolve: (token: string) => void, reject: (error: unknown) => void): void {
    this.failedQueue.push({ resolve, reject });
  }

  public processQueue(error: unknown, token: string | null = null): void {
    this.failedQueue.forEach(prom => {
      if (error) {
        prom.reject(error);
      } else if (token) {
        prom.resolve(token);
      }
    });
    this.failedQueue = [];
  }
}

export const tokenManager = new TokenManager();
