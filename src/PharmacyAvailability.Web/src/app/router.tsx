import { createBrowserRouter, Navigate } from 'react-router-dom';
import { RootLayout } from '@/components/layout/RootLayout';
import { CustomerLayout } from '@/components/layout/CustomerLayout';
import { PharmacyLayout } from '@/components/layout/PharmacyLayout';
import { AdminLayout } from '@/components/layout/AdminLayout';
import { ProtectedRoute } from '@/components/layout/ProtectedRoute';

// Auth Pages
import { LoginPage } from '@/features/auth/pages/LoginPage';
import { RegisterPage } from '@/features/auth/pages/RegisterPage';
import { VerificationPage } from '@/features/auth/pages/VerificationPage';
import { UnauthorizedPage } from '@/features/auth/pages/UnauthorizedPage';

// Customer Pages
import { SearchPage } from '@/features/customer/pages/SearchPage';
import { MedicineDetailPage } from '@/features/customer/pages/MedicineDetailPage';
import { MyReservationsPage } from '@/features/customer/pages/MyReservationsPage';
import { BranchLocatorPage } from '@/features/customer/pages/BranchLocatorPage';

// Pharmacy Staff Pages
import { DashboardPage } from '@/features/pharmacy/pages/DashboardPage';
import { ReservationQueuePage } from '@/features/pharmacy/pages/ReservationQueuePage';
import { InventoryPage } from '@/features/pharmacy/pages/InventoryPage';
import { LowStockPage } from '@/features/pharmacy/pages/LowStockPage';
import { ActivityPage } from '@/features/pharmacy/pages/ActivityPage';
import { StaffManagementPage } from '@/features/pharmacy/pages/StaffManagementPage';

// Platform Admin Pages
import { AdminDashboardPage } from '@/features/admin/pages/AdminDashboardPage';
import { PharmaciesApprovalPage } from '@/features/admin/pages/PharmaciesApprovalPage';
import { CatalogManagementPage } from '@/features/admin/pages/CatalogManagementPage';
import { AdminReservationsPage } from '@/features/admin/pages/AdminReservationsPage';

export const router = createBrowserRouter([
  {
    path: '/',
    element: <RootLayout />,
    children: [
      // Default entry redirect
      { index: true, element: <Navigate to="/customer/search" replace /> },

      // Auth Public Routes
      { path: 'login', element: <LoginPage /> },
      { path: 'register', element: <RegisterPage /> },
      { path: 'unauthorized', element: <UnauthorizedPage /> },
      {
        path: 'verify',
        element: (
          <ProtectedRoute requireVerified={false}>
            <VerificationPage />
          </ProtectedRoute>
        ),
      },

      // 1. Customer Routes (/customer/*)
      {
        path: 'customer',
        element: <CustomerLayout />,
        children: [
          { index: true, element: <Navigate to="search" replace /> },
          { path: 'search', element: <SearchPage /> },
          { path: 'medicines/:id', element: <MedicineDetailPage /> },
          { path: 'branches', element: <BranchLocatorPage /> },
          {
            path: 'reservations',
            element: (
              <ProtectedRoute allowedRoles={['Customer', 'PlatformAdmin']}>
                <MyReservationsPage />
              </ProtectedRoute>
            ),
          },
        ],
      },

      // 2. Pharmacy Operations Routes (/pharmacy/*)
      {
        path: 'pharmacy',
        element: (
          <ProtectedRoute allowedRoles={['PharmacyStaff', 'PharmacyAdmin', 'PlatformAdmin']}>
            <PharmacyLayout />
          </ProtectedRoute>
        ),
        children: [
          { index: true, element: <Navigate to="dashboard" replace /> },
          { path: 'dashboard', element: <DashboardPage /> },
          { path: 'queue', element: <ReservationQueuePage /> },
          { path: 'inventory', element: <InventoryPage /> },
          { path: 'low-stock', element: <LowStockPage /> },
          { path: 'activity', element: <ActivityPage /> },
          {
            path: 'staff',
            element: (
              <ProtectedRoute allowedRoles={['PharmacyAdmin', 'PlatformAdmin']}>
                <StaffManagementPage />
              </ProtectedRoute>
            ),
          },
        ],
      },

      // 3. Platform Admin Routes (/admin/*)
      {
        path: 'admin',
        element: (
          <ProtectedRoute allowedRoles={['PlatformAdmin']}>
            <AdminLayout />
          </ProtectedRoute>
        ),
        children: [
          { index: true, element: <Navigate to="dashboard" replace /> },
          { path: 'dashboard', element: <AdminDashboardPage /> },
          { path: 'pharmacies', element: <PharmaciesApprovalPage /> },
          { path: 'catalog', element: <CatalogManagementPage /> },
          { path: 'reservations', element: <AdminReservationsPage /> },
        ],
      },

      // Catch-all
      { path: '*', element: <Navigate to="/customer/search" replace /> },
    ],
  },
]);
