import { describe, it, expect, beforeEach } from 'vitest';
import { screen } from '@testing-library/react';
import { Routes, Route } from 'react-router-dom';
import { ProtectedRoute } from '@/components/layout/ProtectedRoute';
import { useAuthStore } from '@/app/store/authStore';
import { renderWithProviders } from './test-utils';

function TestRouteSetup({ allowedRoles }: { allowedRoles?: ('Customer' | 'PharmacyStaff' | 'PharmacyAdmin' | 'PlatformAdmin')[] }) {
  return (
    <Routes>
      <Route
        path="/protected"
        element={
          <ProtectedRoute allowedRoles={allowedRoles}>
            <div>Protected Content</div>
          </ProtectedRoute>
        }
      />
      <Route path="/login" element={<div>Login Screen Destination</div>} />
      <Route path="/unauthorized" element={<div>Unauthorized Destination</div>} />
    </Routes>
  );
}

describe('ProtectedRoute Guard', () => {
  beforeEach(() => {
    useAuthStore.getState().logout();
  });

  it('renders loading spinner when session is not yet initialized', () => {
    useAuthStore.setState({ isInitialized: false });

    renderWithProviders(<TestRouteSetup allowedRoles={['Customer']} />, {
      initialEntries: ['/protected'],
    });

    expect(screen.getByText(/Initializing session/i)).toBeInTheDocument();
    expect(screen.queryByText('Protected Content')).not.toBeInTheDocument();
  });

  it('redirects to /login when user is unauthenticated', () => {
    useAuthStore.setState({ isInitialized: true, isAuthenticated: false, user: null });

    renderWithProviders(<TestRouteSetup allowedRoles={['Customer']} />, {
      initialEntries: ['/protected'],
    });

    expect(screen.getByText('Login Screen Destination')).toBeInTheDocument();
    expect(screen.queryByText('Protected Content')).not.toBeInTheDocument();
  });

  it('renders children when user role matches allowedRoles', () => {
    useAuthStore.setState({
      isInitialized: true,
      isAuthenticated: true,
      user: {
        id: '123',
        email: 'customer@test.com',
        fullName: 'Test Customer',
        phoneNumber: null,
        role: 'Customer',
        pharmacyId: null,
      },
    });

    renderWithProviders(<TestRouteSetup allowedRoles={['Customer']} />, {
      initialEntries: ['/protected'],
    });

    expect(screen.getByText('Protected Content')).toBeInTheDocument();
    expect(screen.queryByText('Login Screen Destination')).not.toBeInTheDocument();
  });

  it('redirects to /unauthorized when user role does not match allowedRoles', () => {
    useAuthStore.setState({
      isInitialized: true,
      isAuthenticated: true,
      user: {
        id: '123',
        email: 'customer@test.com',
        fullName: 'Test Customer',
        phoneNumber: null,
        role: 'Customer',
        pharmacyId: null,
      },
    });

    renderWithProviders(<TestRouteSetup allowedRoles={['PlatformAdmin']} />, {
      initialEntries: ['/protected'],
    });

    expect(screen.getByText('Unauthorized Destination')).toBeInTheDocument();
    expect(screen.queryByText('Protected Content')).not.toBeInTheDocument();
  });
});
