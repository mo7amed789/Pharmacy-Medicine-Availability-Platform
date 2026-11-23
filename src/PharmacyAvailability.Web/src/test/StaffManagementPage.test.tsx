import { fireEvent, screen, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { renderWithProviders as render } from './test-utils';
import { StaffManagementPage } from '@/features/pharmacy/pages/StaffManagementPage';
import { useAuthStore } from '@/app/store/authStore';
import { useLanguageStore } from '@/app/store/languageStore';
import { pharmacyOpsApi } from '@/features/pharmacy/api/pharmacyOpsApi';
vi.mock('@/features/pharmacy/api/pharmacyOpsApi', () => ({ pharmacyOpsApi: {
 getStaff: vi.fn(), getBranches: vi.fn(), assignStaffBranch: vi.fn()
}}));
describe('Staff branch assignment', () => {
 beforeEach(() => {
  useLanguageStore.getState().setLanguage('en');
  useAuthStore.setState({user:{id:'admin',pharmacyId:'pharmacy',role:'PharmacyAdmin'} as any});
  vi.mocked(pharmacyOpsApi.getStaff).mockResolvedValue([{id:'staff',pharmacyId:'pharmacy',fullName:'Synthetic Staff',email:'staff@example.invalid',role:'PharmacyStaff',isActive:true,createdAt:'2026-01-01',phoneNumber:null,assignedBranchId:null}]);
  vi.mocked(pharmacyOpsApi.getBranches).mockResolvedValue([{id:'a',name:'Branch A',isActive:true} as any]);
 });
 it('shows unassigned denial and sends an explicit selected branch',async()=>{
  vi.mocked(pharmacyOpsApi.assignStaffBranch).mockResolvedValue({} as any);
  render(<StaffManagementPage />);
  const select=await screen.findByRole('combobox',{name:'Assigned branch: Synthetic Staff'});
  expect(screen.getByText('Unassigned, operations denied')).toBeInTheDocument();
  fireEvent.change(select,{target:{value:'a'}});
  await waitFor(()=>expect(pharmacyOpsApi.assignStaffBranch).toHaveBeenCalledWith('pharmacy','staff','a'));
 });
 it('keeps failure visible rather than claiming a saved assignment',async()=>{
  vi.mocked(pharmacyOpsApi.assignStaffBranch).mockRejectedValue(new Error('Synthetic failure'));
  render(<StaffManagementPage />);
  fireEvent.change(await screen.findByRole('combobox',{name:'Assigned branch: Synthetic Staff'}),{target:{value:'a'}});
  expect(await screen.findByRole('alert')).toHaveTextContent('Could not save branch assignment. Try again.');
 });
});
