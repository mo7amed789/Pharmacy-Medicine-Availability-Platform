import { describe, it, expect } from 'vitest';
import {
  registerSchema,
  loginSchema,
  verifyCodeSchema,
  createReservationSchema,
  adjustStockSchema,
  updatePriceSchema,
} from '@/lib/validations';

describe('Frontend Zod Validation Schemas', () => {
  describe('registerSchema', () => {
    it('validates correct registration data', () => {
      const result = registerSchema.safeParse({
        fullName: 'Ahmed Ali',
        email: 'ahmed@example.com',
        phoneNumber: '+201012345678',
        password: 'Password123!',
      });
      expect(result.success).toBe(true);
    });

    it('rejects invalid email and short password', () => {
      const result = registerSchema.safeParse({
        fullName: 'A',
        email: 'not-an-email',
        phoneNumber: '123',
        password: 'short',
      });
      expect(result.success).toBe(false);
      if (!result.success) {
        const paths = result.error.issues.map((i) => i.path[0]);
        expect(paths).toContain('fullName');
        expect(paths).toContain('email');
        expect(paths).toContain('phoneNumber');
        expect(paths).toContain('password');
      }
    });
  });

  describe('loginSchema', () => {
    it('validates correct login data', () => {
      const result = loginSchema.safeParse({
        email: 'user@test.com',
        password: 'any-password',
      });
      expect(result.success).toBe(true);
    });

    it('rejects missing password', () => {
      const result = loginSchema.safeParse({
        email: 'user@test.com',
        password: '',
      });
      expect(result.success).toBe(false);
    });
  });

  describe('verifyCodeSchema', () => {
    it('accepts valid 6-digit numeric OTP', () => {
      const result = verifyCodeSchema.safeParse({
        type: 'Email',
        code: '123456',
      });
      expect(result.success).toBe(true);
    });

    it('rejects non-6-digit or non-numeric codes', () => {
      expect(verifyCodeSchema.safeParse({ type: 'Email', code: '12345' }).success).toBe(false);
      expect(verifyCodeSchema.safeParse({ type: 'Email', code: 'abcdef' }).success).toBe(false);
    });
  });

  describe('createReservationSchema', () => {
    it('validates positive quantity', () => {
      const result = createReservationSchema.safeParse({
        branchId: 'a1b2c3d4-e5f6-7a8b-9c0d-1e2f3a4b5c6d',
        medicineId: 'b2c3d4e5-f6a7-8b9c-0d1e-2f3a4b5c6d7e',
        quantity: 5,
      });
      expect(result.success).toBe(true);
    });

    it('rejects zero or negative quantity', () => {
      const result = createReservationSchema.safeParse({
        branchId: 'a1b2c3d4-e5f6-7a8b-9c0d-1e2f3a4b5c6d',
        medicineId: 'b2c3d4e5-f6a7-8b9c-0d1e-2f3a4b5c6d7e',
        quantity: 0,
      });
      expect(result.success).toBe(false);
    });
  });

  describe('adjustStockSchema', () => {
    it('rejects zero delta', () => {
      const result = adjustStockSchema.safeParse({
        deltaQuantity: 0,
        reason: 'Restock',
      });
      expect(result.success).toBe(false);
    });

    it('accepts positive or negative non-zero delta', () => {
      expect(adjustStockSchema.safeParse({ deltaQuantity: 15, reason: 'Restock' }).success).toBe(true);
      expect(adjustStockSchema.safeParse({ deltaQuantity: -3, reason: 'Damaged' }).success).toBe(true);
    });
  });

  describe('updatePriceSchema', () => {
    it('rejects negative price', () => {
      expect(updatePriceSchema.safeParse({ newPrice: -5 }).success).toBe(false);
    });

    it('accepts zero or positive price', () => {
      expect(updatePriceSchema.safeParse({ newPrice: 0 }).success).toBe(true);
      expect(updatePriceSchema.safeParse({ newPrice: 99.5 }).success).toBe(true);
    });
  });
});
