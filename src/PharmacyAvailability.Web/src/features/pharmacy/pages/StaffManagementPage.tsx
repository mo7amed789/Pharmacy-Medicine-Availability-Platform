import { useState } from 'react';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Users, UserPlus, Phone } from 'lucide-react';
import { useAuthStore } from '@/app/store/authStore';
import { useBranchStore } from '@/app/store/branchStore';
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
import { ApiError } from '@/api/apiClient';
import { inviteStaffSchema, InviteStaffFormData } from '@/lib/validations';
import { useTranslation } from '@/i18n/useTranslation';

export function StaffManagementPage() {
  const { t } = useTranslation();
  const { user } = useAuthStore();
  const { currentBranch } = useBranchStore();
  const queryClient = useQueryClient();
  const pharmacyId = user?.pharmacyId || currentBranch?.pharmacyId;

  const [isInviteOpen, setIsInviteOpen] = useState(false);
  const [inviteError, setInviteError] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    reset,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<InviteStaffFormData>({
    resolver: zodResolver(inviteStaffSchema),
    defaultValues: {
      fullName: '',
      email: '',
      password: '',
      phoneNumber: '',
      role: 'PharmacyStaff',
    },
  });

  const { data: staff, isLoading, isError, error, refetch } = useQuery({
    queryKey: ['pharmacy-staff', pharmacyId],
    queryFn: () => (pharmacyId ? pharmacyOpsApi.getStaff(pharmacyId) : Promise.resolve([])),
    enabled: !!pharmacyId,
  });

  const onInviteSubmit = async (values: InviteStaffFormData) => {
    if (!pharmacyId) return;
    setInviteError(null);

    try {
      await pharmacyOpsApi.inviteStaff(pharmacyId, {
        ...values,
        phoneNumber: values.phoneNumber || null,
      });
      setIsInviteOpen(false);
      reset();
      queryClient.invalidateQueries({ queryKey: ['pharmacy-staff'] });
    } catch (err: unknown) {
      if (err instanceof ApiError) {
        if (err.isValidationError) {
          const fieldErrors = err.getAllFieldErrors();
          Object.entries(fieldErrors).forEach(([field, message]) => {
            const camelKey = (field.charAt(0).toLowerCase() + field.slice(1)) as keyof InviteStaffFormData;
            setError(camelKey, { message });
          });
        }
        setInviteError(err.detail || err.title || 'Failed to add staff member.');
      } else if (err instanceof Error) {
        setInviteError(err.message);
      } else {
        setInviteError('Failed to add staff member.');
      }
    }
  };

  return (
    <div className="space-y-5">
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
        <div>
          <h1 className="text-2xl font-bold text-slate-900 dark:text-slate-100">{t('pharmacy.staffTeamTitle')}</h1>
          <p className="text-xs text-slate-500 dark:text-slate-400">
            {t('pharmacy.staffTeamDesc')}
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
          {t('pharmacy.addStaff')}
        </Button>
      </div>

      {isLoading ? (
        <div className="py-16">
          <LoadingSpinner size="lg" text={t('common.loading')} />
        </div>
      ) : isError ? (
        <ErrorState error={error} onRetry={() => refetch()} />
      ) : !staff || staff.length === 0 ? (
        <EmptyState
          icon={Users}
          title={t('pharmacy.noStaff')}
          description={t('pharmacy.staffTeamDesc')}
          actionLabel={t('pharmacy.addStaff')}
          onAction={() => setIsInviteOpen(true)}
        />
      ) : (
        <div className="rounded-xl border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 shadow-xs overflow-hidden">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>{t('pharmacy.staffMember')}</TableHead>
                <TableHead>{t('common.role')}</TableHead>
                <TableHead>{t('common.phone')}</TableHead>
                <TableHead className="text-center">{t('common.status')}</TableHead>
                <TableHead className="text-end">{t('pharmacy.staffJoined')}</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {staff.map((member: PharmacyStaffDto) => (
                <TableRow key={member.id}>
                  <TableCell>
                    <div className="font-bold text-slate-900 dark:text-slate-100">{member.fullName}</div>
                    <div className="text-xs text-slate-500 dark:text-slate-400">{member.email}</div>
                  </TableCell>

                  <TableCell>
                    <Badge variant={member.role === 'PharmacyAdmin' ? 'default' : 'secondary'}>
                      {member.role === 'PharmacyAdmin' ? t('nav.roles.PharmacyAdmin') : t('nav.roles.PharmacyStaff')}
                    </Badge>
                  </TableCell>

                  <TableCell className="text-xs text-slate-600 dark:text-slate-300">
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
                      {member.isActive ? t('common.active') : t('common.inactive')}
                    </Badge>
                  </TableCell>

                  <TableCell className="text-end text-xs text-slate-400">
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
            <DialogTitle className="text-lg font-bold text-slate-900 dark:text-slate-100">{t('pharmacy.addStaff')}</DialogTitle>
          </DialogHeader>

          <form onSubmit={handleSubmit(onInviteSubmit)} className="space-y-4 py-2">
            {inviteError && (
              <div className="rounded-md bg-red-50 dark:bg-red-950/40 p-3 text-xs text-red-700 dark:text-red-300 border border-red-200 dark:border-red-900/50">
                {inviteError}
              </div>
            )}

            <div className="space-y-1">
              <label className="text-xs font-semibold text-slate-700 dark:text-slate-300">{t('auth.fullNameLabel')}</label>
              <Input
                {...register('fullName')}
                placeholder={t('auth.fullNamePlaceholder')}
                className="dark:bg-slate-800 dark:border-slate-700"
              />
              {errors.fullName && (
                <p className="text-xs text-red-600 dark:text-red-400 font-medium">{errors.fullName.message}</p>
              )}
            </div>

            <div className="space-y-1">
              <label className="text-xs font-semibold text-slate-700 dark:text-slate-300">{t('auth.emailLabel')}</label>
              <Input
                type="email"
                {...register('email')}
                placeholder={t('auth.emailPlaceholder')}
                className="dark:bg-slate-800 dark:border-slate-700"
              />
              {errors.email && (
                <p className="text-xs text-red-600 dark:text-red-400 font-medium">{errors.email.message}</p>
              )}
            </div>

            <div className="space-y-1">
              <label className="text-xs font-semibold text-slate-700 dark:text-slate-300">{t('pharmacy.initialPassword')}</label>
              <Input
                type="password"
                {...register('password')}
                placeholder={t('auth.passwordPlaceholder')}
                className="dark:bg-slate-800 dark:border-slate-700"
              />
              {errors.password && (
                <p className="text-xs text-red-600 dark:text-red-400 font-medium">{errors.password.message}</p>
              )}
            </div>

            <div className="space-y-1">
              <label className="text-xs font-semibold text-slate-700 dark:text-slate-300">{t('auth.phoneLabel')}</label>
              <Input
                type="tel"
                {...register('phoneNumber')}
                placeholder={t('auth.phonePlaceholder')}
                className="dark:bg-slate-800 dark:border-slate-700"
              />
              {errors.phoneNumber && (
                <p className="text-xs text-red-600 dark:text-red-400 font-medium">{errors.phoneNumber.message}</p>
              )}
            </div>

            <div className="space-y-1">
              <label className="text-xs font-semibold text-slate-700 dark:text-slate-300">{t('common.role')}</label>
              <select
                {...register('role')}
                className="h-10 w-full rounded-md border border-slate-300 dark:border-slate-700 bg-white dark:bg-slate-800 px-3 text-xs text-slate-800 dark:text-slate-200"
              >
                <option value="PharmacyStaff">{t('nav.roles.PharmacyStaff')}</option>
                <option value="PharmacyAdmin">{t('nav.roles.PharmacyAdmin')}</option>
              </select>
              {errors.role && (
                <p className="text-xs text-red-600 dark:text-red-400 font-medium">{errors.role.message}</p>
              )}
            </div>

            <DialogFooter>
              <Button type="button" variant="outline" onClick={() => setIsInviteOpen(false)}>
                {t('common.cancel')}
              </Button>
              <Button type="submit" disabled={isSubmitting}>
                {isSubmitting ? t('common.loading') : t('pharmacy.createAccountBtn')}
              </Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>
    </div>
  );
}
