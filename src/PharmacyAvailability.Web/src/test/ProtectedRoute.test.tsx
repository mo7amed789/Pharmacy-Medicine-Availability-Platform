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
      <Route path="/verify" element={<div>Verification Screen Destination</div>} />
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

  it('redirects to /verify when user is authenticated but not fully verified', () => {
    useAuthStore.setState({
      isInitialized: true,
      isAuthenticated: true,
      user: {
        id: '123',
        email: 'customer@test.com',
        fullName: 'Test Customer',
        phoneNumber: '+201000000000',
        role: 'Customer',
        pharmacyId: null,
        isEmailVerified: false,
        isPhoneVerified: false,
      },
    });

    renderWithProviders(<TestRouteSetup allowedRoles={['Customer']} />, {
      initialEntries: ['/protected'],
    });

    expect(screen.getByText('Verification Screen Destination')).toBeInTheDocument();
    expect(screen.queryByText('Protected Content')).not.toBeInTheDocument();
  });

  it('renders children when user role matches allowedRoles and user is verified', () => {
    useAuthStore.setState({
      isInitialized: true,
      isAuthenticated: true,
      user: {
        id: '123',
        email: 'customer@test.com',
        fullName: 'Test Customer',
        phoneNumber: '+201000000000',
        role: 'Customer',
        pharmacyId: null,
        isEmailVerified: true,
        isPhoneVerified: true,
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
        phoneNumber: '+201000000000',
        role: 'Customer',
        pharmacyId: null,
        isEmailVerified: true,
        isPhoneVerified: true,
      },
    });

    renderWithProviders(<TestRouteSetup allowedRoles={['PlatformAdmin']} />, {
      initialEntries: ['/protected'],
    });

    expect(screen.getByText('Unauthorized Destination')).toBeInTheDocument();
    expect(screen.queryByText('Protected Content')).not.toBeInTheDocument();
  });
});
