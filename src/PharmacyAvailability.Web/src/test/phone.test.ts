import { describe, expect, it } from 'vitest';
import { normalizeEgyptPhone } from '@/features/credits/phone';

describe('normalizeEgyptPhone', () => {
  it('accepts local and international forms', () => {
    expect(normalizeEgyptPhone('01012345678')).toBe('+201012345678');
    expect(normalizeEgyptPhone('+20 101 234 5678')).toBe('+201012345678');
    expect(normalizeEgyptPhone('201512345678')).toBe('+201512345678');
  });
  it('rejects other numbers', () => {
    expect(normalizeEgyptPhone('01312345678')).toBeNull();
    expect(normalizeEgyptPhone('0101234567')).toBeNull();
    expect(normalizeEgyptPhone('+14155550123')).toBeNull();
    expect(normalizeEgyptPhone('')).toBeNull();
  });
});
