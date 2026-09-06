import { create } from 'zustand';
import { PharmacyBranchDto } from '@/types/pharmacies';

interface BranchState {
  currentBranch: PharmacyBranchDto | null;
  setCurrentBranch: (branch: PharmacyBranchDto | null) => void;
}

export const useBranchStore = create<BranchState>((set) => ({
  currentBranch: null,
  setCurrentBranch: (branch) => set({ currentBranch: branch }),
}));
