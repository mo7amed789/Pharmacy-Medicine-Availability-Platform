import { describe, it, expect, beforeEach } from 'vitest';
import { tokenManager } from '@/api/tokenManager';

describe('TokenManager', () => {
  beforeEach(() => {
    tokenManager.clearTokens();
    tokenManager.setIsRefreshing(false);
  });

  it('stores access token in memory only and never in localStorage', () => {
    tokenManager.setAccessToken('sample-jwt-token');

    expect(tokenManager.getAccessToken()).toBe('sample-jwt-token');
    expect(localStorage.getItem('access_token')).toBeNull();
  });

  it('stores refresh token in localStorage and retrieves correctly', () => {
    tokenManager.setRefreshToken('sample-refresh-token');

    expect(tokenManager.getRefreshToken()).toBe('sample-refresh-token');
    expect(localStorage.getItem('refresh_token')).toBe('sample-refresh-token');
  });

  it('clears both tokens on clearTokens call', () => {
    tokenManager.setAccessToken('sample-jwt-token');
    tokenManager.setRefreshToken('sample-refresh-token');

    tokenManager.clearTokens();

    expect(tokenManager.getAccessToken()).toBeNull();
    expect(tokenManager.getRefreshToken()).toBeNull();
  });

  it('queues concurrent requests and resolves them all when processQueue is called with new token', async () => {
    const p1 = new Promise<string>((resolve, reject) => tokenManager.addQueue(resolve, reject));
    const p2 = new Promise<string>((resolve, reject) => tokenManager.addQueue(resolve, reject));

    tokenManager.processQueue(null, 'new-renewed-token');

    const [res1, res2] = await Promise.all([p1, p2]);
    expect(res1).toBe('new-renewed-token');
    expect(res2).toBe('new-renewed-token');
  });

  it('rejects all queued requests when processQueue is called with an error', async () => {
    const p1 = new Promise<string>((resolve, reject) => tokenManager.addQueue(resolve, reject));
    const p2 = new Promise<string>((resolve, reject) => tokenManager.addQueue(resolve, reject));

    const testError = new Error('Refresh rejected');
    tokenManager.processQueue(testError, null);

    await expect(p1).rejects.toThrow('Refresh rejected');
    await expect(p2).rejects.toThrow('Refresh rejected');
  });
});
