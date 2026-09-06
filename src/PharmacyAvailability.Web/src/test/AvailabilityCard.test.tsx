import { describe, it, expect, vi, beforeEach } from 'vitest';
import { screen, fireEvent } from '@testing-library/react';
import { AvailabilityCard } from '@/features/customer/components/AvailabilityCard';
import { MedicineAvailabilityDto } from '@/types/availability';
import { renderWithProviders } from './test-utils';
import { useLanguageStore } from '@/app/store/languageStore';

const mockItem: MedicineAvailabilityDto = {
  inventoryId: 'inv-1',
  medicineId: 'med-1',
  medicineBrandName: 'Panadol Extra',
  medicineGenericName: 'Paracetamol',
  strength: '500mg',
  dosageForm: 'Tablet',
  pharmacyId: 'pharma-1',
  pharmacyName: 'El Ezaby Pharmacy',
  branchId: 'branch-1',
  branchName: 'Nasr City Branch',
  branchAddress: '15 Abbas El Akkad St',
  branchCity: 'Cairo',
  branchPhone: '0223456789',
  openingHours: '24 Hours',
  branchLatitude: 30.05,
  branchLongitude: 31.35,
  distanceKm: 2.3,
  availableQuantity: 8,
  stockStatus: 'InStock',
  price: 35.0,
  currency: 'EGP',
  freshness: 'Realtime',
  lastUpdatedAt: new Date().toISOString(),
};

describe('AvailabilityCard', () => {
  beforeEach(() => {
    useLanguageStore.getState().setLanguage('ar');
  });

  it('renders medicine details, price, distance, and in-stock badge in Arabic by default', () => {
    const handleReserve = vi.fn();
    renderWithProviders(<AvailabilityCard item={mockItem} onReserve={handleReserve} />);

    expect(screen.getByText('Panadol Extra')).toBeInTheDocument();
    expect(screen.getByText(/Paracetamol/i)).toBeInTheDocument();
    expect(screen.getByText(/متوفر \(8 وحدة\)/i)).toBeInTheDocument();
    expect(screen.getByText('35.00 EGP')).toBeInTheDocument();
    expect(screen.getByText('2.3 km')).toBeInTheDocument();
    expect(screen.getByText(/El Ezaby Pharmacy/i)).toBeInTheDocument();
  });

  it('triggers onReserve callback when Reserve Hold button is clicked in Arabic', () => {
    const handleReserve = vi.fn();
    renderWithProviders(<AvailabilityCard item={mockItem} onReserve={handleReserve} />);

    const reserveButton = screen.getByRole('button', { name: /حجز واستلام/i });
    fireEvent.click(reserveButton);

    expect(handleReserve).toHaveBeenCalledTimes(1);
    expect(handleReserve).toHaveBeenCalledWith(mockItem);
  });

  it('renders out of stock badge and disables reservation when stock is 0 in Arabic', () => {
    const outOfStockItem = { ...mockItem, availableQuantity: 0 };
    const handleReserve = vi.fn();

    renderWithProviders(<AvailabilityCard item={outOfStockItem} onReserve={handleReserve} />);

    const outOfStockBadges = screen.getAllByText('غير متوفر');
    expect(outOfStockBadges.length).toBeGreaterThanOrEqual(1);

    const button = screen.getByRole('button', { name: /غير متوفر/i });
    expect(button).toBeDisabled();
  });

  it('renders English strings when language is set to English', () => {
    useLanguageStore.getState().setLanguage('en');
    const handleReserve = vi.fn();
    renderWithProviders(<AvailabilityCard item={mockItem} onReserve={handleReserve} />);

    expect(screen.getByText('8 in stock')).toBeInTheDocument();
    const reserveButton = screen.getByRole('button', { name: /Reserve Hold/i });
    expect(reserveButton).toBeInTheDocument();
  });
});
