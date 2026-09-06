import { describe, it, expect } from 'vitest';
import { formatCurrency, formatDistance, formatDateTime } from '@/lib/formatters';

describe('Formatters', () => {
  describe('formatCurrency', () => {
    it('formats numbers with 2 decimal places and currency symbol', () => {
      expect(formatCurrency(45.5)).toBe('45.50 EGP');
      expect(formatCurrency(100, 'USD')).toBe('100.00 USD');
      expect(formatCurrency(0)).toBe('0.00 EGP');
    });
  });

  describe('formatDistance', () => {
    it('formats kilometers with 1 decimal place', () => {
      expect(formatDistance(3.456)).toBe('3.5 km');
      expect(formatDistance(12.0)).toBe('12.0 km');
    });

    it('formats meters when distance is under 1 km', () => {
      expect(formatDistance(0.45)).toBe('450 m');
      expect(formatDistance(0.08)).toBe('80 m');
    });

    it('handles null or undefined gracefully', () => {
      expect(formatDistance(null)).toBe('Distance unavailable');
      expect(formatDistance(undefined)).toBe('Distance unavailable');
    });
  });

  describe('formatDateTime', () => {
    it('returns hyphen for null or undefined dates', () => {
      expect(formatDateTime(null)).toBe('-');
      expect(formatDateTime(undefined)).toBe('-');
    });

    it('formats valid ISO dates', () => {
      const formatted = formatDateTime('2026-09-06T15:30:00Z');
      expect(formatted).toContain('2026');
    });
  });
});
