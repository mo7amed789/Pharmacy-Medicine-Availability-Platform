import { describe, it, expect, vi, beforeEach } from 'vitest';
import { screen, fireEvent, waitFor } from '@testing-library/react';
import { RegisterPage } from '@/features/auth/pages/RegisterPage';
import { renderWithProviders } from './test-utils';
import { useLanguageStore } from '@/app/store/languageStore';
import { authApi } from '@/features/auth/api/authApi';

vi.mock('@/features/auth/api/authApi', () => ({
  authApi: {
    register: vi.fn(),
    registerPharmacy: vi.fn(),
  },
}));

describe('RegisterPage Role Distinction', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    useLanguageStore.getState().setLanguage('ar');
  });

  it('renders Customer registration tab by default in Arabic', () => {
    renderWithProviders(<RegisterPage />);

    expect(screen.getByRole('tab', { name: /حساب مريض \/ عميل/i })).toBeInTheDocument();
    expect(screen.getByRole('tab', { name: /تسجيل صيدلية شريكة/i })).toBeInTheDocument();

    // Customer fields are present
    expect(screen.getByPlaceholderText('محمد أحمد')).toBeInTheDocument();
    expect(screen.getByPlaceholderText('name@example.com')).toBeInTheDocument();
    expect(screen.getByPlaceholderText('+20 100 000 0000')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /إنشاء الحساب/i })).toBeInTheDocument();
  });

  it('switches to Pharmacy registration tab and reveals pharmacy & admin fields', () => {
    renderWithProviders(<RegisterPage />);

    const pharmacyTab = screen.getByRole('tab', { name: /تسجيل صيدلية شريكة/i });
    fireEvent.click(pharmacyTab);

    // Header updates
    expect(screen.getByText(/تسجيل صيدلية شريكة جديدة/i)).toBeInTheDocument();

    // Pharmacy fields
    expect(screen.getByPlaceholderText('صيدلية الأمل الحديثة')).toBeInTheDocument();
    expect(screen.getByPlaceholderText('PH-EG-2026-001')).toBeInTheDocument();
    expect(screen.getByPlaceholderText('15 شارع عباس العقاد، مدينة نصر')).toBeInTheDocument();

    // Admin fields
    expect(screen.getByPlaceholderText('د. أحمد محمود')).toBeInTheDocument();
    expect(screen.getByPlaceholderText('admin@pharmacy.com')).toBeInTheDocument();

    // Pharmacy submit button
    expect(screen.getByRole('button', { name: /تقديم طلب تسجيل الصيدلية/i })).toBeInTheDocument();
  });

  it('submits pharmacy registration and displays success review notice', async () => {
    const mockPharmacyResponse = {
      id: 'pharmacy-123',
      name: 'صيدلية النور الحديثة',
      licenseNumber: 'PH-EG-2026-999',
      address: '10 شارع التحرير',
      city: 'القاهرة',
      latitude: 30.0444,
      longitude: 31.2357,
      phone: '+201012345678',
      email: 'info@nourpharmacy.com',
      openingHours: '24 ساعة',
      status: 'Pending' as const,
      branchesCount: 1,
      createdAt: new Date().toISOString(),
    };

    vi.mocked(authApi.registerPharmacy).mockResolvedValueOnce(mockPharmacyResponse);

    renderWithProviders(<RegisterPage />);

    // Switch to pharmacy tab
    fireEvent.click(screen.getByRole('tab', { name: /تسجيل صيدلية شريكة/i }));

    // Fill pharmacy fields
    fireEvent.change(screen.getByPlaceholderText('صيدلية الأمل الحديثة'), {
      target: { value: 'صيدلية النور الحديثة' },
    });
    fireEvent.change(screen.getByPlaceholderText('PH-EG-2026-001'), {
      target: { value: 'PH-EG-2026-999' },
    });
    fireEvent.change(screen.getByPlaceholderText('القاهرة'), {
      target: { value: 'القاهرة' },
    });
    fireEvent.change(screen.getByPlaceholderText('15 شارع عباس العقاد، مدينة نصر'), {
      target: { value: '10 شارع التحرير، وسط البلد' },
    });
    fireEvent.change(screen.getByPlaceholderText('+20 2 2345 6789'), {
      target: { value: '+201012345678' },
    });

    // Fill admin fields
    fireEvent.change(screen.getByPlaceholderText('د. أحمد محمود'), {
      target: { value: 'د. خالد إبراهيم' },
    });
    fireEvent.change(screen.getByPlaceholderText('admin@pharmacy.com'), {
      target: { value: 'admin@nourpharmacy.com' },
    });
    fireEvent.change(screen.getByPlaceholderText('8 أحرف على الأقل'), {
      target: { value: 'AdminSecurePass123!' },
    });

    // Submit
    fireEvent.click(screen.getByRole('button', { name: /تقديم طلب تسجيل الصيدلية/i }));

    await waitFor(() => {
      expect(authApi.registerPharmacy).toHaveBeenCalledTimes(1);
      expect(screen.getByText('تم استلام طلب تسجيل الصيدلية بنجاح!')).toBeInTheDocument();
      expect(screen.getByText('صيدلية النور الحديثة')).toBeInTheDocument();
      expect(screen.getByText(/طلب قيد المراجعة والاعتماد/i)).toBeInTheDocument();
    });
  });

  it('switches between languages smoothly on the register screen', () => {
    useLanguageStore.getState().setLanguage('en');
    renderWithProviders(<RegisterPage />);

    expect(screen.getByRole('tab', { name: /Patient \/ Customer/i })).toBeInTheDocument();
    expect(screen.getByRole('tab', { name: /Partner Pharmacy/i })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /Register Account/i })).toBeInTheDocument();

    fireEvent.click(screen.getByRole('tab', { name: /Partner Pharmacy/i }));
    expect(screen.getByText(/Register Partner Pharmacy/i)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /Submit Pharmacy Registration/i })).toBeInTheDocument();
  });
});
