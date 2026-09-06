import { describe, it, expect } from 'vitest';
import { getFriendlyErrorMessage } from '@/lib/friendlyErrors';
import { ApiError } from '@/api/apiClient';

describe('getFriendlyErrorMessage', () => {
  it('translates active hold conflict error to empathetic Arabic', () => {
    const error = new ApiError({
      status: 409,
      title: 'Conflict',
      detail: 'A hold is already placed for this user on this item.',
    });
    const message = getFriendlyErrorMessage(error, 'ar');
    expect(message).toContain('لديك حجز جارٍ بالفعل');
  });

  it('translates insufficient stock error to empathetic Arabic', () => {
    const error = new ApiError({
      status: 409,
      title: 'Conflict',
      detail: 'Insufficient stock remaining to fulfill this hold.',
    });
    const message = getFriendlyErrorMessage(error, 'ar');
    expect(message).toContain('نفدت الكمية المتاحة');
  });

  it('translates invalid credentials error in Arabic and English', () => {
    const error = new ApiError({
      status: 401,
      title: 'Unauthorized',
      detail: 'Invalid credentials provided.',
    });
    expect(getFriendlyErrorMessage(error, 'ar')).toBe('البريد الإلكتروني أو كلمة المرور غير صحيحة. يرجى التحقق من البيانات والمحاولة مرة أخرى.');
    expect(getFriendlyErrorMessage(error, 'en')).toBe('The email address or password is incorrect. Please verify your credentials and try again.');
  });

  it('translates network or fetch errors to friendly Arabic message', () => {
    const networkError = new TypeError('Failed to fetch');
    const message = getFriendlyErrorMessage(networkError, 'ar');
    expect(message).toContain('تعذر الاتصال بالخادم');
  });

  it('returns default fallback when error is unknown', () => {
    const message = getFriendlyErrorMessage(null, 'ar');
    expect(message).toBe('حدث خطأ غير متوقع. يرجى المحاولة مرة أخرى.');
  });
});
