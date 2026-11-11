import { createBrowserRouter, Navigate } from 'react-router-dom';
import { RootLayout } from '@/components/layout/RootLayout';
import { CustomerLayout } from '@/components/layout/CustomerLayout';
import { ProtectedRoute } from '@/components/layout/ProtectedRoute';

// Auth Pages
import { VerificationPage } from '@/features/auth/pages/VerificationPage';
import { UnauthorizedPage } from '@/features/auth/pages/UnauthorizedPage';

// Customer Pages
import { SearchPage } from '@/features/customer/pages/SearchPage';
import { MedicineDetailPage } from '@/features/customer/pages/MedicineDetailPage';
import { BranchLocatorPage } from '@/features/customer/pages/BranchLocatorPage';

export const router = createBrowserRouter([
  {
    path: '/',
    element: <RootLayout />,
    children: [
      // Default entry redirect
      { index: true, element: <Navigate to="/customer/search" replace /> },

      // Auth Public Routes (Redirected in query-only mode)
      { path: 'login', element: <Navigate to="/customer/search" replace /> },
      { path: 'register', element: <Navigate to="/customer/search" replace /> },
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
          { path: 'reservations', element: <Navigate to="/customer/search" replace /> },
        ],
      },

      // 2. Pharmacy Operations Routes (/pharmacy/*) - Temporarily disabled & redirected
      {
        path: 'pharmacy/*',
        element: <Navigate to="/customer/search" replace />,
      },

      // 3. Platform Admin Routes (/admin/*) - Temporarily disabled & redirected
      {
        path: 'admin/*',
        element: <Navigate to="/customer/search" replace />,
      },

      // Catch-all
      { path: '*', element: <Navigate to="/customer/search" replace /> },
    ],
  },
]);
