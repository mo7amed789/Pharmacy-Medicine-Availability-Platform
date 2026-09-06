import { describe, it, expect } from 'vitest';
import { ApiError } from '@/api/apiClient';

describe('ApiError and RFC 7807 ProblemDetails mapping', () => {
  it('correctly maps status, title, detail, and correlationId', () => {
    const error = new ApiError({
      status: 400,
      title: 'Validation Error',
      detail: 'The provided data is invalid.',
      correlationId: 'test-corr-123',
      errors: {
        Email: ['Email is invalid.'],
        Password: ['Password too short.', 'Password must have number.'],
      },
    });

    expect(error.status).toBe(400);
    expect(error.title).toBe('Validation Error');
    expect(error.detail).toBe('The provided data is invalid.');
    expect(error.correlationId).toBe('test-corr-123');
    expect(error.isValidationError).toBe(true);
    expect(error.isNotFound).toBe(false);
    expect(error.isConflict).toBe(false);
    expect(error.isUnauthorized).toBe(false);
    expect(error.isForbidden).toBe(false);
  });

  it('retrieves field error case-insensitively with getFieldError', () => {
    const error = new ApiError({
      status: 400,
      title: 'Validation Error',
      errors: {
        Email: ['A valid email address is required.'],
        PhoneNumber: ['Phone number must be valid.'],
      },
    });

    expect(error.getFieldError('email')).toBe('A valid email address is required.');
    expect(error.getFieldError('Email')).toBe('A valid email address is required.');
    expect(error.getFieldError('phoneNumber')).toBe('Phone number must be valid.');
    expect(error.getFieldError('nonExistent')).toBeUndefined();
  });

  it('returns flattened dictionary of all field errors with getAllFieldErrors', () => {
    const error = new ApiError({
      status: 400,
      title: 'Validation Error',
      errors: {
        Quantity: ['Must be at least 1.'],
        Reason: ['Reason is required.'],
      },
    });

    const flat = error.getAllFieldErrors();
    expect(flat).toEqual({
      Quantity: 'Must be at least 1.',
      Reason: 'Reason is required.',
    });
  });

  it('correctly identifies HTTP 404, 409, 401, and 403 error flags', () => {
    const notFound = new ApiError({ status: 404, title: 'Not Found' });
    expect(notFound.isNotFound).toBe(true);
    expect(notFound.isConflict).toBe(false);

    const conflict = new ApiError({ status: 409, title: 'Conflict' });
    expect(conflict.isConflict).toBe(true);

    const unauthorized = new ApiError({ status: 401, title: 'Unauthorized' });
    expect(unauthorized.isUnauthorized).toBe(true);

    const forbidden = new ApiError({ status: 403, title: 'Forbidden' });
    expect(forbidden.isForbidden).toBe(true);
  });
});
