import { describe, it, expect, vi } from 'vitest';
import { screen, fireEvent } from '@testing-library/react';
import { AvailabilityCard } from '@/features/customer/components/AvailabilityCard';
import { MedicineAvailabilityDto } from '@/types/availability';
import { renderWithProviders } from './test-utils';

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
  it('renders medicine details, price, distance, and in-stock badge', () => {
    const handleReserve = vi.fn();
    renderWithProviders(<AvailabilityCard item={mockItem} onReserve={handleReserve} />);

    expect(screen.getByText('Panadol Extra')).toBeInTheDocument();
    expect(screen.getByText(/Paracetamol/i)).toBeInTheDocument();
    expect(screen.getByText('8 in stock')).toBeInTheDocument();
    expect(screen.getByText('35.00 EGP')).toBeInTheDocument();
    expect(screen.getByText('2.3 km')).toBeInTheDocument();
    expect(screen.getByText(/El Ezaby Pharmacy/i)).toBeInTheDocument();
  });

  it('triggers onReserve callback when Reserve Hold button is clicked', () => {
    const handleReserve = vi.fn();
    renderWithProviders(<AvailabilityCard item={mockItem} onReserve={handleReserve} />);

    const reserveButton = screen.getByRole('button', { name: /Reserve Hold/i });
    fireEvent.click(reserveButton);

    expect(handleReserve).toHaveBeenCalledTimes(1);
    expect(handleReserve).toHaveBeenCalledWith(mockItem);
  });

  it('renders out of stock badge and disables reservation when stock is 0', () => {
    const outOfStockItem = { ...mockItem, availableQuantity: 0 };
    const handleReserve = vi.fn();

    renderWithProviders(<AvailabilityCard item={outOfStockItem} onReserve={handleReserve} />);

    expect(screen.getByText('Out of Stock')).toBeInTheDocument();
    const button = screen.getByRole('button', { name: /Unavailable/i });
    expect(button).toBeDisabled();
  });
});
