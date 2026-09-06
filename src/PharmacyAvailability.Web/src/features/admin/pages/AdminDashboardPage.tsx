import { useQuery } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import { Building2, BookOpen, Clock, ShieldCheck, ArrowRight } from 'lucide-react';
import { adminApi } from '../api/adminApi';
import { Card, CardContent } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { useTranslation } from '@/i18n/useTranslation';

export function AdminDashboardPage() {
  const { t, language } = useTranslation();

  const { data: pharmacies } = useQuery({
    queryKey: ['admin-pharmacies'],
    queryFn: () => adminApi.getAllPharmacies(),
  });

  const { data: reservations } = useQuery({
    queryKey: ['admin-reservations-summary'],
    queryFn: () => adminApi.getAllReservations({ pageSize: 1 }),
  });

  const pendingCount = (pharmacies || []).filter((p) => p.status === 'Pending').length;
  const approvedCount = (pharmacies || []).filter((p) => p.status === 'Active' || (p.status as string) === 'Approved').length;

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-slate-900 dark:text-slate-100">{t('admin.superuserTitle')}</h1>
        <p className="text-xs text-slate-500 dark:text-slate-400">
          {t('admin.superuserSubtitle')}
        </p>
      </div>

      {/* Metric Cards */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-5">
        <Card className="border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900">
          <CardContent className="p-5 flex items-center justify-between">
            <div>
              <p className="text-xs font-semibold text-slate-500 dark:text-slate-400">{t('admin.pendingApprovals')}</p>
              <h3 className="text-3xl font-extrabold text-amber-600 dark:text-amber-400 mt-1">{pendingCount}</h3>
              <Button variant="link" size="sm" asChild className="p-0 h-auto mt-2 text-xs">
                <Link to="/admin/pharmacies" className="flex items-center gap-1">
                  {t('admin.reviewApplications')} <ArrowRight className="h-3 w-3 rtl:rotate-180" />
                </Link>
              </Button>
            </div>
            <div className="flex h-12 w-12 items-center justify-center rounded-xl bg-amber-50 dark:bg-amber-950/40 text-amber-600 dark:text-amber-400 border border-amber-100 dark:border-amber-800/40">
              <Building2 className="h-6 w-6" />
            </div>
          </CardContent>
        </Card>

        <Card className="border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900">
          <CardContent className="p-5 flex items-center justify-between">
            <div>
              <p className="text-xs font-semibold text-slate-500 dark:text-slate-400">{t('admin.activePharmacies')}</p>
              <h3 className="text-3xl font-extrabold text-slate-900 dark:text-slate-100 mt-1">{approvedCount}</h3>
              <Button variant="link" size="sm" asChild className="p-0 h-auto mt-2 text-xs">
                <Link to="/admin/pharmacies" className="flex items-center gap-1">
                  {t('admin.viewAllPartners')} <ArrowRight className="h-3 w-3 rtl:rotate-180" />
                </Link>
              </Button>
            </div>
            <div className="flex h-12 w-12 items-center justify-center rounded-xl bg-blue-50 dark:bg-blue-950/40 text-blue-600 dark:text-blue-400 border border-blue-100 dark:border-blue-800/40">
              <ShieldCheck className="h-6 w-6" />
            </div>
          </CardContent>
        </Card>

        <Card className="border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900">
          <CardContent className="p-5 flex items-center justify-between">
            <div>
              <p className="text-xs font-semibold text-slate-500 dark:text-slate-400">{t('admin.totalSystemHolds')}</p>
              <h3 className="text-3xl font-extrabold text-purple-700 dark:text-purple-400 mt-1">
                {reservations?.totalCount ?? 0}
              </h3>
              <Button variant="link" size="sm" asChild className="p-0 h-auto mt-2 text-xs">
                <Link to="/admin/reservations" className="flex items-center gap-1">
                  {t('admin.auditHolds')} <ArrowRight className="h-3 w-3 rtl:rotate-180" />
                </Link>
              </Button>
            </div>
            <div className="flex h-12 w-12 items-center justify-center rounded-xl bg-purple-50 dark:bg-purple-950/40 text-purple-600 dark:text-purple-400 border border-purple-100 dark:border-purple-800/40">
              <Clock className="h-6 w-6" />
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Governance Sections */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-5 pt-2">
        <Card className="border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900">
          <CardContent className="p-6 space-y-3">
            <div className="flex items-center gap-3">
              <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-blue-100 dark:bg-blue-950/50 text-blue-700 dark:text-blue-300">
                <BookOpen className="h-5 w-5" />
              </div>
              <div>
                <h3 className="font-bold text-slate-900 dark:text-slate-100 text-base">{t('admin.centralCatalog')}</h3>
                <p className="text-xs text-slate-500 dark:text-slate-400">{t('admin.centralCatalogSub')}</p>
              </div>
            </div>
            <p className="text-xs text-slate-600 dark:text-slate-300 leading-relaxed">
              {language === 'ar'
                ? 'إنشاء وتعديل سجلات الأدوية المعتمدة التي تعتمد عليها جميع الصيدليات عند مزامنة المخزون وتحديد الأسعار.'
                : 'Create and edit authoritative medicine records used by all pharmacies when synchronizing inventory.'}
            </p>
            <Button size="sm" asChild>
              <Link to="/admin/catalog">{t('admin.openCatalog')}</Link>
            </Button>
          </CardContent>
        </Card>

        <Card className="border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900">
          <CardContent className="p-6 space-y-3">
            <div className="flex items-center gap-3">
              <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-amber-100 dark:bg-amber-950/50 text-amber-700 dark:text-amber-300">
                <Clock className="h-5 w-5" />
              </div>
              <div>
                <h3 className="font-bold text-slate-900 dark:text-slate-100 text-base">{t('admin.reservationsOversight')}</h3>
                <p className="text-xs text-slate-500 dark:text-slate-400">{t('admin.reservationsOversightSub')}</p>
              </div>
            </div>
            <p className="text-xs text-slate-600 dark:text-slate-300 leading-relaxed">
              {language === 'ar'
                ? 'في حالات انقطاع الخدمة أو بلاغات العملاء، يمتلك مسؤولو النظام صلاحية التدقيق وإلغاء الحجوزات وإعادة المخزون فورياً.'
                : 'In case of pharmacy outages or customer disputes, platform administrators can forcibly release reserved stock back to inventory.'}
            </p>
            <Button size="sm" variant="outline" asChild>
              <Link to="/admin/reservations">{t('admin.inspectHolds')}</Link>
            </Button>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
