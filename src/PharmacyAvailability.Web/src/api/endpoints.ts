export const ENDPOINTS = {
  // Auth
  AUTH_REGISTER: '/auth/register',
  AUTH_LOGIN: '/auth/login',
  AUTH_REFRESH: '/auth/refresh',
  AUTH_REVOKE: '/auth/revoke',
  AUTH_ME: '/auth/me',

  // Availability (Public)
  AVAILABILITY_SEARCH: '/availability',
  MEDICINE_AVAILABILITY: (medicineId: string) => `/medicines/${medicineId}/availability`,

  // Public Catalog & Branches
  MEDICINES: '/medicines',
  MEDICINE_BY_ID: (id: string) => `/medicines/${id}`,
  CATEGORIES: '/categories',
  MANUFACTURERS: '/manufacturers',
  ACTIVE_PHARMACIES: '/pharmacies',
  PHARMACY_BY_ID: (id: string) => `/pharmacies/${id}`,
  ACTIVE_BRANCHES: '/branches',
  BRANCH_BY_ID: (id: string) => `/branches/${id}`,
  REGISTER_PHARMACY: '/pharmacies/register',

  // Customer Reservations
  RESERVATIONS: '/reservations',
  RESERVATION_BY_ID: (id: string) => `/reservations/${id}`,
  CANCEL_RESERVATION: (id: string) => `/reservations/${id}/cancel`,
  CONFIRM_RESERVATION: (id: string) => `/reservations/${id}/confirm`,
  RELEASE_RESERVATION: (id: string) => `/reservations/${id}/release`,

  // Pharmacy Operations
  BRANCH_OPERATIONS: (pharmacyId: string, branchId: string) => ({
    DASHBOARD: `/pharmacies/${pharmacyId}/branches/${branchId}/operations/dashboard`,
    QUEUE: `/pharmacies/${pharmacyId}/branches/${branchId}/operations/reservations/queue`,
    LOW_STOCK: `/pharmacies/${pharmacyId}/branches/${branchId}/operations/inventory/low-stock`,
    ACTIVITY: `/pharmacies/${pharmacyId}/branches/${branchId}/operations/activity`,
  }),

  // Pharmacy Branch Inventory Management
  BRANCH_INVENTORY: (pharmacyId: string, branchId: string) => ({
    LIST: `/pharmacies/${pharmacyId}/branches/${branchId}/inventory`,
    ITEM: (medicineId: string) => `/pharmacies/${pharmacyId}/branches/${branchId}/inventory/${medicineId}`,
    ADJUST_STOCK: (medicineId: string) => `/pharmacies/${pharmacyId}/branches/${branchId}/inventory/${medicineId}/quantity`,
    UPDATE_PRICE: (medicineId: string) => `/pharmacies/${pharmacyId}/branches/${branchId}/inventory/${medicineId}/price`,
    TRANSACTIONS: (medicineId: string) => `/pharmacies/${pharmacyId}/branches/${branchId}/inventory/${medicineId}/transactions`,
    PRICE_HISTORY: (medicineId: string) => `/pharmacies/${pharmacyId}/branches/${branchId}/inventory/${medicineId}/price-history`,
  }),

  // Pharmacy Management (Admin)
  PHARMACY_MANAGEMENT: (pharmacyId: string) => ({
    PROFILE: `/pharmacies/${pharmacyId}/profile`,
    BRANCHES: `/pharmacies/${pharmacyId}/branches`,
    BRANCH_STATUS: (branchId: string) => `/pharmacies/${pharmacyId}/branches/${branchId}/status`,
    STAFF: `/pharmacies/${pharmacyId}/staff`,
  }),

  // Platform Admin
  ADMIN: {
    PHARMACIES: '/admin/pharmacies',
    APPROVE_PHARMACY: (id: string) => `/admin/pharmacies/${id}/approve`,
    SUSPEND_PHARMACY: (id: string) => `/admin/pharmacies/${id}/suspend`,
    REACTIVATE_PHARMACY: (id: string) => `/admin/pharmacies/${id}/reactivate`,
    DEACTIVATE_PHARMACY: (id: string) => `/admin/pharmacies/${id}/deactivate`,

    MEDICINES: '/admin/medicines',
    UPDATE_MEDICINE: (id: string) => `/admin/medicines/${id}`,
    UPDATE_MEDICINE_STATUS: (id: string) => `/admin/medicines/${id}/status`,
    ADD_BARCODE: (medicineId: string) => `/admin/medicines/${medicineId}/barcodes`,
    ADD_ALIAS: (medicineId: string) => `/admin/medicines/${medicineId}/aliases`,

    CATEGORIES: '/admin/categories',
    UPDATE_CATEGORY: (id: string) => `/admin/categories/${id}`,

    MANUFACTURERS: '/admin/manufacturers',
    UPDATE_MANUFACTURER: (id: string) => `/admin/manufacturers/${id}`,
  },
};
