import { create } from 'zustand';
import { PharmacyBranchDto } from '@/types/pharmacies';

interface BranchState {
  currentBranch: PharmacyBranchDto | null;
  setCurrentBranch: (branch: PharmacyBranchDto | null) => void;
}

const getStoredBranch = (): PharmacyBranchDto | null => {
  try {
    const raw = localStorage.getItem('pharma-current-branch');
    return raw ? JSON.parse(raw) : null;
  } catch {
    return null;
  }
};

export const useBranchStore = create<BranchState>((set) => ({
  currentBranch: getStoredBranch(),
  setCurrentBranch: (branch) => {
    try {
      if (branch) {
        localStorage.setItem('pharma-current-branch', JSON.stringify(branch));
      } else {
        localStorage.removeItem('pharma-current-branch');
      }
    } catch {
      // Ignore storage errors
    }
    set({ currentBranch: branch });
  },
}));
