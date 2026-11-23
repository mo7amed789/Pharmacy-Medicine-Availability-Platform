export interface QueuedRequest {
  resolve: (token: string) => void;
  reject: (error: unknown) => void;
}

class TokenManager {
  private accessToken: string | null = null;
  private refreshToken: string | null = null;

  constructor() {
    // Discard credentials left by older builds. Never migrate them back to disk.
    try { localStorage.removeItem('refresh_token'); } catch { /* Storage may be unavailable. */ }
  }
  private isRefreshing = false;
  private failedQueue: QueuedRequest[] = [];

  public getAccessToken(): string | null {
    return this.accessToken;
  }

  public setAccessToken(token: string | null): void {
    this.accessToken = token;
  }

  public getRefreshToken(): string | null {
    return this.refreshToken;
  }

  public setRefreshToken(token: string | null): void {
    this.refreshToken = token;
    try { localStorage.removeItem('refresh_token'); } catch { /* Storage may be unavailable. */ }
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
