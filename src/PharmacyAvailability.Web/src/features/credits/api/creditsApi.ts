import { apiClient } from '@/api/apiClient';
import { ENDPOINTS } from '@/api/endpoints';

export interface MyCredit {
  publicCode: string;
  balance: number;
  freeQueriesRemaining: number;
  chargingEnabled: boolean;
}

export interface LedgerEntry {
  id: string;
  type: string;
  points: number;
  balanceAfter: number;
  transferReference: string | null;
  performedByUserId: string | null;
  note: string | null;
  createdAt: string;
}

export interface AdminCreditAccount {
  publicCode: string;
  userId: string;
  fullName: string;
  phoneNumber: string;
  isPhoneVerified: boolean;
  balance: number;
  freeQueriesRemaining: number;
  recentEntries: LedgerEntry[];
}

export interface UsageSummary {
  fromUtc: string;
  toUtc: string;
  totalQueries: number;
  successQueries: number;
  unknownQueries: number;
  noResultQueries: number;
  anonymousQueries: number;
  pointsCharged: number;
  freeQueriesUsed: number;
  distinctUsers: number;
}

export const creditsApi = {
  me: async (): Promise<MyCredit> => (await apiClient.get<MyCredit>(ENDPOINTS.CREDITS_ME)).data,
  lookup: async (code: string): Promise<AdminCreditAccount> =>
    (await apiClient.get<AdminCreditAccount>(ENDPOINTS.ADMIN_CREDIT_ACCOUNT(code))).data,
  topUp: async (req: { publicCode: string; points: number; transferReference: string; note?: string }): Promise<AdminCreditAccount> =>
    (await apiClient.post<AdminCreditAccount>(ENDPOINTS.ADMIN_CREDIT_TOPUPS, req)).data,
  usage: async (): Promise<UsageSummary> => (await apiClient.get<UsageSummary>(ENDPOINTS.ADMIN_CREDIT_USAGE)).data,
};
