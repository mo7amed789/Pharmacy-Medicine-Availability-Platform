import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Users, UserPlus, Phone } from 'lucide-react';
import { useAuthStore } from '@/app/store/authStore';
import { pharmacyOpsApi } from '../api/pharmacyOpsApi';
import { PharmacyStaffDto } from '@/types/pharmacies';
import { Input } from '@/components/ui/input';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import {
  Table,
  TableHeader,
  TableBody,
  TableRow,
  TableHead,
  TableCell,
} from '@/components/ui/table';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogFooter,
} from '@/components/ui/dialog';
import { LoadingSpinner } from '@/components/feedback/LoadingSpinner';
import { ErrorState } from '@/components/feedback/ErrorState';
import { EmptyState } from '@/components/feedback/EmptyState';
import { formatDateTime } from '@/lib/formatters';

export function StaffManagementPage() {
  const { user } = useAuthStore();
  const queryClient = useQueryClient();
  const pharmacyId = user?.pharmacyId;

  const [isInviteOpen, setIsInviteOpen] = useState(false);
  const [fullName, setFullName] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [phoneNumber, setPhoneNumber] = useState('');
  const [role, setRole] = useState('PharmacyStaff');
  const [inviteError, setInviteError] = useState<string | null>(null);

  const { data: staff, isLoading, isError, error, refetch } = useQuery({
    queryKey: ['pharmacy-staff', pharmacyId],
    queryFn: () => (pharmacyId ? pharmacyOpsApi.getStaff(pharmacyId) : Promise.resolve([])),
    enabled: !!pharmacyId,
  });

  const inviteMutation = useMutation({
    mutationFn: async () => {
      if (!pharmacyId) return;
      return pharmacyOpsApi.inviteStaff(pharmacyId, {
        fullName,
        email,
        password,
        phoneNumber: phoneNumber || null,
        role,
      });
    },
    onSuccess: () => {
      setIsInviteOpen(false);
      setFullName('');
      setEmail('');
      setPassword('');
      setPhoneNumber('');
      queryClient.invalidateQueries({ queryKey: ['pharmacy-staff'] });
    },
    onError: (err: any) => {
      setInviteError(err?.detail || err?.message || 'Failed to add staff member.');
    },
  });

  return (
    <div className="space-y-5">
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
        <div>
          <h1 className="text-2xl font-bold text-slate-900">Pharmacy Staff Management</h1>
          <p className="text-xs text-slate-500">
            Manage operational pharmacists and branch staff team members.
          </p>
        </div>
        <Button
          size="sm"
          onClick={() => {
            setInviteError(null);
            setIsInviteOpen(true);
          }}
          className="flex items-center gap-1.5 text-xs"
        >
          <UserPlus className="h-4 w-4" />
          Add Staff Member
        </Button>
      </div>

      {isLoading ? (
        <div className="py-16">
          <LoadingSpinner size="lg" text="Loading staff members..." />
        </div>
      ) : isError ? (
        <ErrorState error={error} onRetry={() => refetch()} />
      ) : !staff || staff.length === 0 ? (
        <EmptyState
          icon={Users}
          title="No Staff Accounts Found"
          description="No additional staff members have been registered for this pharmacy."
          actionLabel="Add First Staff"
          onAction={() => setIsInviteOpen(true)}
        />
      ) : (
        <div className="rounded-xl border border-slate-200 bg-white shadow-xs overflow-hidden">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Staff Member</TableHead>
                <TableHead>Role</TableHead>
                <TableHead>Contact</TableHead>
                <TableHead className="text-center">Status</TableHead>
                <TableHead className="text-right">Joined</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {staff.map((member: PharmacyStaffDto) => (
                <TableRow key={member.id}>
                  <TableCell>
                    <div className="font-bold text-slate-900">{member.fullName}</div>
                    <div className="text-xs text-slate-500">{member.email}</div>
                  </TableCell>

                  <TableCell>
                    <Badge variant={member.role === 'PharmacyAdmin' ? 'default' : 'secondary'}>
                      {member.role}
                    </Badge>
                  </TableCell>

                  <TableCell className="text-xs text-slate-600">
                    {member.phoneNumber ? (
                      <div className="flex items-center gap-1">
                        <Phone className="h-3 w-3 text-slate-400" />
                        <span>{member.phoneNumber}</span>
                      </div>
                    ) : (
                      <span className="text-slate-400">-</span>
                    )}
                  </TableCell>

                  <TableCell className="text-center">
                    <Badge variant={member.isActive ? 'success' : 'destructive'} className="text-[10px]">
                      {member.isActive ? 'Active' : 'Inactive'}
                    </Badge>
                  </TableCell>

                  <TableCell className="text-right text-xs text-slate-400">
                    {formatDateTime(member.createdAt)}
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </div>
      )}

      {/* Add Staff Modal */}
      <Dialog open={isInviteOpen} onOpenChange={setIsInviteOpen}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle className="text-lg font-bold text-slate-900">Add Staff Member</DialogTitle>
          </DialogHeader>

          <form
            onSubmit={(e) => {
              e.preventDefault();
              inviteMutation.mutate();
            }}
            className="space-y-4 py-2"
          >
            {inviteError && (
              <div className="rounded-md bg-red-50 p-3 text-xs text-red-700 border border-red-200">
                {inviteError}
              </div>
            )}

            <div className="space-y-1.5">
              <label className="text-xs font-semibold text-slate-700">Full Name</label>
              <Input
                value={fullName}
                onChange={(e) => setFullName(e.target.value)}
                placeholder="Dr. Ahmed Hassan"
                required
              />
            </div>

            <div className="space-y-1.5">
              <label className="text-xs font-semibold text-slate-700">Email Address</label>
              <Input
                type="email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                placeholder="ahmed@pharmacy.com"
                required
              />
            </div>

            <div className="space-y-1.5">
              <label className="text-xs font-semibold text-slate-700">Initial Password</label>
              <Input
                type="password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                placeholder="Minimum 8 characters"
                required
              />
            </div>

            <div className="space-y-1.5">
              <label className="text-xs font-semibold text-slate-700">Phone Number (Optional)</label>
              <Input
                type="tel"
                value={phoneNumber}
                onChange={(e) => setPhoneNumber(e.target.value)}
                placeholder="+20 100 000 0000"
              />
            </div>

            <div className="space-y-1.5">
              <label className="text-xs font-semibold text-slate-700">Role</label>
              <select
                value={role}
                onChange={(e) => setRole(e.target.value)}
                className="h-10 w-full rounded-md border border-slate-300 bg-white px-3 text-xs text-slate-800"
              >
                <option value="PharmacyStaff">PharmacyStaff (Fulfillment & Queue)</option>
                <option value="PharmacyAdmin">PharmacyAdmin (Full Branch & Stock Control)</option>
              </select>
            </div>

            <DialogFooter>
              <Button type="button" variant="outline" onClick={() => setIsInviteOpen(false)}>
                Cancel
              </Button>
              <Button type="submit" disabled={inviteMutation.isPending}>
                {inviteMutation.isPending ? 'Adding...' : 'Create Account'}
              </Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>
    </div>
  );
}
