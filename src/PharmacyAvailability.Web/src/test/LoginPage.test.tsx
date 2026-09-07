import { describe, it, expect } from 'vitest';
import { getSafeRedirectPath } from '@/features/auth/pages/LoginPage';
import { getFriendlyErrorMessage } from '@/lib/friendlyErrors';
import { ApiError } from '@/api/apiClient';

describe('LoginPage Safe Redirect & 401 Error Handling', () => {
  describe('getSafeRedirectPath', () => {
    it('redirects platform admin to /admin/dashboard for root or admin paths', () => {
      expect(getSafeRedirectPath('PlatformAdmin', '/')).toBe('/admin/dashboard');
      expect(getSafeRedirectPath('PlatformAdmin', '/login')).toBe('/admin/dashboard');
      expect(getSafeRedirectPath('PlatformAdmin', '/unauthorized')).toBe('/admin/dashboard');
      expect(getSafeRedirectPath('PlatformAdmin', '/admin/dashboard')).toBe('/admin/dashboard');
      expect(getSafeRedirectPath('PlatformAdmin', '/admin/pharmacies')).toBe('/admin/pharmacies');
    });

    it('prevents customer from being redirected to /admin/* and redirects to /customer/search instead', () => {
      // Prevents the route guard Unauthorized trap!
      expect(getSafeRedirectPath('Customer', '/admin/dashboard')).toBe('/customer/search');
      expect(getSafeRedirectPath('Customer', '/admin/catalog')).toBe('/customer/search');
      expect(getSafeRedirectPath('Customer', '/pharmacy/dashboard')).toBe('/customer/search');
      expect(getSafeRedirectPath('Customer', '/unauthorized')).toBe('/customer/search');
      expect(getSafeRedirectPath('Customer', '/')).toBe('/customer/search');
    });

    it('prevents pharmacy staff from being redirected to /admin/* and redirects to /pharmacy/dashboard', () => {
      expect(getSafeRedirectPath('PharmacyAdmin', '/admin/dashboard')).toBe('/pharmacy/dashboard');
      expect(getSafeRedirectPath('PharmacyStaff', '/admin/pharmacies')).toBe('/pharmacy/dashboard');
      expect(getSafeRedirectPath('PharmacyAdmin', '/pharmacy/inventory')).toBe('/pharmacy/inventory');
    });

    it('preserves legitimate customer paths', () => {
      expect(getSafeRedirectPath('Customer', '/customer/reservations')).toBe('/customer/reservations');
      expect(getSafeRedirectPath('Customer', '/customer/medicines/med-123')).toBe('/customer/medicines/med-123');
    });
  });

  describe('401 Friendly Error Handling', () => {
    it('translates raw 401 Unauthorized API error to polite Arabic error message', () => {
      const err = new ApiError({
        status: 401,
        title: 'Unauthorized',
        detail: 'Invalid email or password.',
      });

      const messageAr = getFriendlyErrorMessage(err, 'ar');
      expect(messageAr).toBe('البريد الإلكتروني أو كلمة المرور غير صحيحة. يرجى التحقق من البيانات والمحاولة مرة أخرى.');

      const messageEn = getFriendlyErrorMessage(err, 'en');
      expect(messageEn).toBe('The email address or password is incorrect. Please verify your credentials and try again.');
    });

    it('translates literal "Unauthorized" string to friendly message', () => {
      expect(getFriendlyErrorMessage('Unauthorized', 'ar')).toBe(
        'البريد الإلكتروني أو كلمة المرور غير صحيحة. يرجى التحقق من البيانات والمحاولة مرة أخرى.'
      );
      expect(getFriendlyErrorMessage('Unauthorized', 'en')).toBe(
        'The email address or password is incorrect. Please verify your credentials and try again.'
      );
    });

    it('translates 403 Forbidden to friendly message', () => {
      const err = new ApiError({
        status: 403,
        title: 'Forbidden',
        detail: 'User does not have required permissions.',
      });

      expect(getFriendlyErrorMessage(err, 'ar')).toBe('ليس لديك الصلاحيات الكافية للوصول إلى هذا المحتوى أو المورد.');
      expect(getFriendlyErrorMessage(err, 'en')).toBe('You do not have sufficient permissions to access this content.');
    });
  });
});
