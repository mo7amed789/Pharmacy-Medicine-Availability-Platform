import React, { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import {
  Lock,
  Mail,
  User,
  Phone,
  AlertCircle,
  Store,
  MapPin,
  Clock,
  CheckCircle2,
  FileText,
  Navigation,
  Sparkles,
} from 'lucide-react';
import { useAuthStore } from '@/app/store/authStore';
import { authApi } from '../api/authApi';
import {
  registerSchema,
  registerPharmacySchema,
  RegisterPharmacyFormData,
} from '@/lib/validations';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import {
  Card,
  CardHeader,
  CardTitle,
  CardDescription,
  CardContent,
  CardFooter,
} from '@/components/ui/card';
import { Alert, AlertDescription } from '@/components/ui/alert';
import { Badge } from '@/components/ui/badge';
import { ApiError } from '@/api/apiClient';
import { ThemeToggle } from '@/components/common/ThemeToggle';
import { LanguageToggle } from '@/components/common/LanguageToggle';
import { useTranslation } from '@/i18n/useTranslation';
import { getFriendlyErrorMessage } from '@/lib/friendlyErrors';

export function RegisterPage() {
  const { t, language } = useTranslation();
  const [accountType, setAccountType] = useState<'customer' | 'pharmacy'>('customer');
  const [error, setError] = useState<string | null>(null);
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({});
  const [isLoading, setIsLoading] = useState(false);

  // Customer Form State
  const [fullName, setFullName] = useState('');
  const [email, setEmail] = useState('');
  const [phoneNumber, setPhoneNumber] = useState('');
  const [password, setPassword] = useState('');

  // Pharmacy Form State
  const [pharmacyName, setPharmacyName] = useState('');
  const [licenseNumber, setLicenseNumber] = useState('');
  const [address, setAddress] = useState('');
  const [city, setCity] = useState(language === 'ar' ? 'القاهرة' : 'Cairo');
  const [latitude, setLatitude] = useState(30.0444);
  const [longitude, setLongitude] = useState(31.2357);
  const [pharmacyPhone, setPharmacyPhone] = useState('');
  const [pharmacyEmail, setPharmacyEmail] = useState('');
  const [openingHours, setOpeningHours] = useState(language === 'ar' ? '24 ساعة' : '24 Hours');
  const [adminFullName, setAdminFullName] = useState('');
  const [adminEmail, setAdminEmail] = useState('');
  const [adminPassword, setAdminPassword] = useState('');
  const [adminPhone, setAdminPhone] = useState('');
  const [isDetectingGps, setIsDetectingGps] = useState(false);
  const [gpsDetected, setGpsDetected] = useState(false);

  // Success state for pharmacy registration
  const [isPharmacyRegistered, setIsPharmacyRegistered] = useState(false);
  const [registeredPharmacyName, setRegisteredPharmacyName] = useState('');

  const { setSession } = useAuthStore();
  const navigate = useNavigate();

  const handleTabChange = (type: 'customer' | 'pharmacy') => {
    setAccountType(type);
    setError(null);
    setFieldErrors({});
  };

  const handleDetectGps = () => {
    if (!navigator.geolocation) {
      setError(
        language === 'ar'
          ? 'متصفحك لا يدعم خاصية تحديد الموقع الجغرافي.'
          : 'Geolocation is not supported by your browser.'
      );
      return;
    }

    setIsDetectingGps(true);
    setError(null);

    navigator.geolocation.getCurrentPosition(
      (position) => {
        setLatitude(parseFloat(position.coords.latitude.toFixed(6)));
        setLongitude(parseFloat(position.coords.longitude.toFixed(6)));
        setGpsDetected(true);
        setIsDetectingGps(false);
      },
      (geoError) => {
        setError(
          language === 'ar'
            ? `تعذر تحديد الموقع عبر GPS: ${geoError.message}. تم الاحتفاظ بالإحداثيات الافتراضية.`
            : `GPS detection failed: ${geoError.message}. Default coordinates retained.`
        );
        setIsDetectingGps(false);
      },
      { timeout: 10000, enableHighAccuracy: false }
    );
  };

  const handleCustomerSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setFieldErrors({});

    const validationResult = registerSchema.safeParse({
      fullName,
      email,
      phoneNumber,
      password,
    });

    if (!validationResult.success) {
      const errors: Record<string, string> = {};
      for (const issue of validationResult.error.issues) {
        const pathKey = issue.path[0] as string;
        if (!errors[pathKey]) {
          errors[pathKey] = issue.message;
        }
      }
      setFieldErrors(errors);
      return;
    }

    setIsLoading(true);

    try {
      const response = await authApi.register({
        fullName: fullName.trim(),
        email: email.trim(),
        phoneNumber: phoneNumber.trim(),
        password,
      });

      setSession(response.user, response.accessToken, response.refreshToken);
      navigate('/verify', { replace: true });
    } catch (err: unknown) {
      setError(getFriendlyErrorMessage(err, language));
      if (err instanceof ApiError && err.validationErrors) {
        const mapped: Record<string, string> = {};
        for (const [key, msgs] of Object.entries(err.validationErrors)) {
          const lowerKey = key.charAt(0).toLowerCase() + key.slice(1);
          mapped[lowerKey] = msgs[0] || 'Invalid value';
        }
        setFieldErrors(mapped);
      }
    } finally {
      setIsLoading(false);
    }
  };

  const handlePharmacySubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setFieldErrors({});

    const payload: RegisterPharmacyFormData = {
      name: pharmacyName.trim(),
      licenseNumber: licenseNumber.trim(),
      address: address.trim(),
      city: city.trim(),
      latitude: Number(latitude) || 30.0444,
      longitude: Number(longitude) || 31.2357,
      phone: pharmacyPhone.trim(),
      email: pharmacyEmail.trim() || undefined,
      openingHours: openingHours.trim() || undefined,
      adminFullName: adminFullName.trim(),
      adminEmail: adminEmail.trim(),
      adminPassword: adminPassword,
      adminPhone: adminPhone.trim() || undefined,
    };

    const validationResult = registerPharmacySchema.safeParse(payload);
    if (!validationResult.success) {
      const errors: Record<string, string> = {};
      for (const issue of validationResult.error.issues) {
        const pathKey = issue.path[0] as string;
        if (!errors[pathKey]) {
          errors[pathKey] = issue.message;
        }
      }
      setFieldErrors(errors);
      return;
    }

    setIsLoading(true);

    try {
      const res = await authApi.registerPharmacy(payload);
      setRegisteredPharmacyName(res.name);
      setIsPharmacyRegistered(true);
    } catch (err: unknown) {
      setError(getFriendlyErrorMessage(err, language));
      if (err instanceof ApiError && err.validationErrors) {
        const mapped: Record<string, string> = {};
        for (const [key, msgs] of Object.entries(err.validationErrors)) {
          const lowerKey = key.charAt(0).toLowerCase() + key.slice(1);
          mapped[lowerKey] = msgs[0] || 'Invalid value';
        }
        setFieldErrors(mapped);
      }
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="relative flex min-h-screen items-center justify-center bg-slate-50 dark:bg-slate-950 px-4 py-12 sm:px-6 lg:px-8">
      <div className="absolute top-4 end-4 flex items-center gap-2">
        <LanguageToggle />
        <ThemeToggle />
      </div>

      <Card
        className={`w-full ${
          accountType === 'pharmacy' && !isPharmacyRegistered ? 'max-w-2xl' : 'max-w-md'
        } shadow-lg rounded-2xl border-slate-200 dark:border-slate-800 transition-all duration-300`}
      >
        <CardHeader className="text-center space-y-2 pb-3">
          <img
            src="/logo.png"
            alt="PharmaFinder"
            className="mx-auto h-16 w-16 object-contain drop-shadow-xs rounded-xl"
          />

          {!isPharmacyRegistered && (
            <>
              {/* Role Switcher Tabs */}
              <div
                role="tablist"
                aria-label="Account Type Selection"
                className="flex rounded-xl bg-slate-100 dark:bg-slate-800/80 p-1 mt-3 border border-slate-200/80 dark:border-slate-700/60"
              >
                <button
                  type="button"
                  role="tab"
                  aria-selected={accountType === 'customer'}
                  onClick={() => handleTabChange('customer')}
                  className={`flex-1 flex items-center justify-center gap-2 py-2 px-3 rounded-lg text-xs font-bold transition-all ${
                    accountType === 'customer'
                      ? 'bg-white dark:bg-slate-900 text-blue-900 dark:text-blue-200 shadow-xs'
                      : 'text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-slate-200'
                  }`}
                >
                  <User className="h-4 w-4" />
                  <span>{t('auth.registerPatientTab')}</span>
                </button>
                <button
                  type="button"
                  role="tab"
                  aria-selected={accountType === 'pharmacy'}
                  onClick={() => handleTabChange('pharmacy')}
                  className={`flex-1 flex items-center justify-center gap-2 py-2 px-3 rounded-lg text-xs font-bold transition-all ${
                    accountType === 'pharmacy'
                      ? 'bg-white dark:bg-slate-900 text-emerald-700 dark:text-emerald-300 shadow-xs'
                      : 'text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-slate-200'
                  }`}
                >
                  <Store className="h-4 w-4" />
                  <span>{t('auth.registerPharmacyTab')}</span>
                </button>
              </div>

              <CardTitle className="text-2xl font-bold text-slate-900 dark:text-slate-100 pt-2">
                {accountType === 'customer'
                  ? t('auth.registerCustomerTitle')
                  : t('auth.registerPharmacyTitle')}
              </CardTitle>
              <CardDescription className="text-slate-500 dark:text-slate-400 text-xs sm:text-sm">
                {accountType === 'customer'
                  ? t('auth.registerCustomerDesc')
                  : t('auth.registerPharmacyDesc')}
              </CardDescription>
            </>
          )}
        </CardHeader>

        {/* Pharmacy Registration Success State */}
        {isPharmacyRegistered ? (
          <CardContent className="space-y-6 py-6 text-center">
            <div className="mx-auto flex h-16 w-16 items-center justify-center rounded-full bg-emerald-100 dark:bg-emerald-950/60 text-emerald-600 dark:text-emerald-400 shadow-xs">
              <CheckCircle2 className="h-10 w-10" />
            </div>

            <div className="space-y-2">
              <Badge variant="warning" className="text-xs px-3 py-1">
                {language === 'ar' ? 'طلب قيد المراجعة والاعتماد' : 'Pending Administrative Review'}
              </Badge>
              <h3 className="text-xl font-bold text-slate-900 dark:text-slate-100">
                {t('auth.pharmacySuccessTitle')}
              </h3>
              {registeredPharmacyName && (
                <p className="text-sm font-semibold text-emerald-700 dark:text-emerald-400">
                  {registeredPharmacyName}
                </p>
              )}
              <p className="text-xs text-slate-600 dark:text-slate-400 leading-relaxed max-w-md mx-auto">
                {t('auth.pharmacySuccessDesc')}
              </p>
            </div>

            <div className="rounded-xl bg-blue-50/60 dark:bg-blue-950/30 p-4 border border-blue-100 dark:border-blue-900/40 text-xs text-start space-y-1.5">
              <p className="font-semibold text-blue-900 dark:text-blue-200">
                {language === 'ar' ? 'الخطوات التالية:' : 'Next Steps:'}
              </p>
              <ul className="list-disc list-inside text-slate-600 dark:text-slate-400 space-y-1">
                <li>
                  {language === 'ar'
                    ? 'سيقوم فريق إدارة المنصة بفحص ومراجعة رقم الترخيص الصيدلي.'
                    : 'The platform administration will review your pharmacy license details.'}
                </li>
                <li>
                  {language === 'ar'
                    ? 'يمكنك تسجيل الدخول الآن بحساب المدير المسؤول والاطلاع على لوحة العمليات.'
                    : 'You may sign in now with your administrator credentials to view your portal status.'}
                </li>
              </ul>
            </div>

            <div className="flex flex-col sm:flex-row gap-3 pt-2">
              <Button
                type="button"
                className="flex-1 bg-emerald-600 hover:bg-emerald-700 text-white font-semibold rounded-xl shadow-xs"
                onClick={() => navigate('/login')}
              >
                {t('auth.pharmacySuccessAction')}
              </Button>
              <Button
                type="button"
                variant="outline"
                className="flex-1 rounded-xl"
                onClick={() => navigate('/')}
              >
                {t('auth.backToHome')}
              </Button>
            </div>
          </CardContent>
        ) : accountType === 'customer' ? (
          /* ================= CUSTOMER REGISTRATION FORM ================= */
          <form onSubmit={handleCustomerSubmit}>
            <CardContent className="space-y-4">
              {error && (
                <Alert variant="destructive">
                  <AlertCircle className="h-4 w-4" />
                  <AlertDescription>{error}</AlertDescription>
                </Alert>
              )}

              <div className="space-y-1.5">
                <label className="text-xs font-semibold text-slate-700 dark:text-slate-300">
                  {t('auth.fullNameLabel')}
                </label>
                <div className="relative">
                  <User className="absolute start-3 top-3 h-4 w-4 text-slate-400" />
                  <Input
                    type="text"
                    placeholder={t('auth.fullNamePlaceholder')}
                    value={fullName}
                    onChange={(e) => setFullName(e.target.value)}
                    className={`ps-9 dark:bg-slate-800 dark:border-slate-700 ${
                      fieldErrors.fullName ? 'border-red-500 focus-visible:ring-red-500' : ''
                    }`}
                    required
                  />
                </div>
                {fieldErrors.fullName && (
                  <p className="text-xs text-red-600 dark:text-red-400 mt-1">{fieldErrors.fullName}</p>
                )}
              </div>

              <div className="space-y-1.5">
                <label className="text-xs font-semibold text-slate-700 dark:text-slate-300">
                  {t('auth.emailLabel')}
                </label>
                <div className="relative">
                  <Mail className="absolute start-3 top-3 h-4 w-4 text-slate-400" />
                  <Input
                    type="email"
                    placeholder={t('auth.emailPlaceholder')}
                    value={email}
                    onChange={(e) => setEmail(e.target.value)}
                    className={`ps-9 dark:bg-slate-800 dark:border-slate-700 ${
                      fieldErrors.email ? 'border-red-500 focus-visible:ring-red-500' : ''
                    }`}
                    required
                  />
                </div>
                {fieldErrors.email && (
                  <p className="text-xs text-red-600 dark:text-red-400 mt-1">{fieldErrors.email}</p>
                )}
              </div>

              <div className="space-y-1.5">
                <label className="text-xs font-semibold text-slate-700 dark:text-slate-300">
                  {t('auth.phoneLabel')}
                </label>
                <div className="relative">
                  <Phone className="absolute start-3 top-3 h-4 w-4 text-slate-400" />
                  <Input
                    type="tel"
                    placeholder={t('auth.phonePlaceholder')}
                    value={phoneNumber}
                    onChange={(e) => setPhoneNumber(e.target.value)}
                    className={`ps-9 dark:bg-slate-800 dark:border-slate-700 ${
                      fieldErrors.phoneNumber ? 'border-red-500 focus-visible:ring-red-500' : ''
                    }`}
                    required
                  />
                </div>
                {fieldErrors.phoneNumber && (
                  <p className="text-xs text-red-600 dark:text-red-400 mt-1">
                    {fieldErrors.phoneNumber}
                  </p>
                )}
              </div>

              <div className="space-y-1.5">
                <label className="text-xs font-semibold text-slate-700 dark:text-slate-300">
                  {t('auth.passwordLabel')}
                </label>
                <div className="relative">
                  <Lock className="absolute start-3 top-3 h-4 w-4 text-slate-400" />
                  <Input
                    type="password"
                    placeholder={t('auth.passwordPlaceholder')}
                    value={password}
                    onChange={(e) => setPassword(e.target.value)}
                    className={`ps-9 dark:bg-slate-800 dark:border-slate-700 ${
                      fieldErrors.password ? 'border-red-500 focus-visible:ring-red-500' : ''
                    }`}
                    required
                  />
                </div>
                {fieldErrors.password && (
                  <p className="text-xs text-red-600 dark:text-red-400 mt-1">
                    {fieldErrors.password}
                  </p>
                )}
              </div>
            </CardContent>

            <CardFooter className="flex flex-col space-y-4 pt-2">
              <Button
                type="submit"
                className="w-full bg-blue-900 hover:bg-blue-800 text-white font-semibold rounded-xl shadow-xs"
                disabled={isLoading}
              >
                {isLoading ? t('auth.registering') : t('auth.registerBtn')}
              </Button>

              <div className="text-center text-xs text-slate-600 dark:text-slate-400">
                {t('auth.haveAccount')}{' '}
                <Link to="/login" className="font-semibold text-blue-600 hover:underline">
                  {t('nav.signIn')}
                </Link>
              </div>
            </CardFooter>
          </form>
        ) : (
          /* ================= PHARMACY REGISTRATION FORM ================= */
          <form onSubmit={handlePharmacySubmit}>
            <CardContent className="space-y-5">
              {error && (
                <Alert variant="destructive">
                  <AlertCircle className="h-4 w-4" />
                  <AlertDescription>{error}</AlertDescription>
                </Alert>
              )}

              {/* SECTION 1: Pharmacy Info */}
              <div className="space-y-3 rounded-xl border border-slate-200/80 dark:border-slate-800 p-4 bg-slate-50/50 dark:bg-slate-900/50">
                <div className="flex items-center gap-2 border-b border-slate-200/80 dark:border-slate-800 pb-2">
                  <Store className="h-4 w-4 text-emerald-600 dark:text-emerald-400" />
                  <h4 className="text-xs font-bold text-slate-900 dark:text-slate-100 uppercase tracking-wide">
                    {t('auth.pharmacyInfoSection')}
                  </h4>
                </div>

                <div className="grid grid-cols-1 sm:grid-cols-2 gap-3.5">
                  <div className="space-y-1">
                    <label className="text-xs font-semibold text-slate-700 dark:text-slate-300">
                      {t('auth.pharmacyNameLabel')} *
                    </label>
                    <div className="relative">
                      <Store className="absolute start-3 top-3 h-4 w-4 text-slate-400" />
                      <Input
                        type="text"
                        placeholder={t('auth.pharmacyNamePlaceholder')}
                        value={pharmacyName}
                        onChange={(e) => setPharmacyName(e.target.value)}
                        className={`ps-9 dark:bg-slate-800 dark:border-slate-700 ${
                          fieldErrors.name ? 'border-red-500 focus-visible:ring-red-500' : ''
                        }`}
                        required
                      />
                    </div>
                    {fieldErrors.name && (
                      <p className="text-xs text-red-600 dark:text-red-400 mt-0.5">{fieldErrors.name}</p>
                    )}
                  </div>

                  <div className="space-y-1">
                    <label className="text-xs font-semibold text-slate-700 dark:text-slate-300">
                      {t('auth.licenseNumberLabel')} *
                    </label>
                    <div className="relative">
                      <FileText className="absolute start-3 top-3 h-4 w-4 text-slate-400" />
                      <Input
                        type="text"
                        placeholder={t('auth.licenseNumberPlaceholder')}
                        value={licenseNumber}
                        onChange={(e) => setLicenseNumber(e.target.value)}
                        className={`ps-9 dark:bg-slate-800 dark:border-slate-700 ${
                          fieldErrors.licenseNumber ? 'border-red-500 focus-visible:ring-red-500' : ''
                        }`}
                        required
                      />
                    </div>
                    {fieldErrors.licenseNumber && (
                      <p className="text-xs text-red-600 dark:text-red-400 mt-0.5">
                        {fieldErrors.licenseNumber}
                      </p>
                    )}
                  </div>

                  <div className="space-y-1">
                    <label className="text-xs font-semibold text-slate-700 dark:text-slate-300">
                      {t('auth.cityLabel')} *
                    </label>
                    <div className="relative">
                      <MapPin className="absolute start-3 top-3 h-4 w-4 text-slate-400" />
                      <Input
                        type="text"
                        placeholder={t('auth.cityPlaceholder')}
                        value={city}
                        onChange={(e) => setCity(e.target.value)}
                        className={`ps-9 dark:bg-slate-800 dark:border-slate-700 ${
                          fieldErrors.city ? 'border-red-500 focus-visible:ring-red-500' : ''
                        }`}
                        required
                      />
                    </div>
                    {fieldErrors.city && (
                      <p className="text-xs text-red-600 dark:text-red-400 mt-0.5">{fieldErrors.city}</p>
                    )}
                  </div>

                  <div className="space-y-1">
                    <label className="text-xs font-semibold text-slate-700 dark:text-slate-300">
                      {t('auth.pharmacyPhoneLabel')} *
                    </label>
                    <div className="relative">
                      <Phone className="absolute start-3 top-3 h-4 w-4 text-slate-400" />
                      <Input
                        type="tel"
                        placeholder="+20 2 2345 6789"
                        value={pharmacyPhone}
                        onChange={(e) => setPharmacyPhone(e.target.value)}
                        className={`ps-9 dark:bg-slate-800 dark:border-slate-700 ${
                          fieldErrors.phone ? 'border-red-500 focus-visible:ring-red-500' : ''
                        }`}
                        required
                      />
                    </div>
                    {fieldErrors.phone && (
                      <p className="text-xs text-red-600 dark:text-red-400 mt-0.5">{fieldErrors.phone}</p>
                    )}
                  </div>

                  <div className="space-y-1 sm:col-span-2">
                    <label className="text-xs font-semibold text-slate-700 dark:text-slate-300">
                      {t('auth.addressLabel')} *
                    </label>
                    <div className="relative">
                      <Navigation className="absolute start-3 top-3 h-4 w-4 text-slate-400" />
                      <Input
                        type="text"
                        placeholder={t('auth.addressPlaceholder')}
                        value={address}
                        onChange={(e) => setAddress(e.target.value)}
                        className={`ps-9 dark:bg-slate-800 dark:border-slate-700 ${
                          fieldErrors.address ? 'border-red-500 focus-visible:ring-red-500' : ''
                        }`}
                        required
                      />
                    </div>
                    {fieldErrors.address && (
                      <p className="text-xs text-red-600 dark:text-red-400 mt-0.5">{fieldErrors.address}</p>
                    )}
                  </div>

                  <div className="space-y-1">
                    <label className="text-xs font-semibold text-slate-700 dark:text-slate-300">
                      {t('auth.openingHoursLabel')}
                    </label>
                    <div className="relative">
                      <Clock className="absolute start-3 top-3 h-4 w-4 text-slate-400" />
                      <Input
                        type="text"
                        placeholder={t('auth.openingHoursPlaceholder')}
                        value={openingHours}
                        onChange={(e) => setOpeningHours(e.target.value)}
                        className="ps-9 dark:bg-slate-800 dark:border-slate-700"
                      />
                    </div>
                  </div>

                  <div className="space-y-1">
                    <label className="text-xs font-semibold text-slate-700 dark:text-slate-300">
                      {t('auth.pharmacyEmailLabel')}
                    </label>
                    <div className="relative">
                      <Mail className="absolute start-3 top-3 h-4 w-4 text-slate-400" />
                      <Input
                        type="email"
                        placeholder="pharmacy@example.com"
                        value={pharmacyEmail}
                        onChange={(e) => setPharmacyEmail(e.target.value)}
                        className={`ps-9 dark:bg-slate-800 dark:border-slate-700 ${
                          fieldErrors.email ? 'border-red-500 focus-visible:ring-red-500' : ''
                        }`}
                      />
                    </div>
                  </div>
                </div>

                {/* GPS Assistant */}
                <div className="pt-2 border-t border-slate-200/60 dark:border-slate-800 flex flex-col sm:flex-row sm:items-center justify-between gap-2 text-xs">
                  <div className="flex items-center gap-1.5 text-slate-500 dark:text-slate-400">
                    <MapPin className="h-3.5 w-3.5 text-blue-600 dark:text-blue-400" />
                    <span>
                      {language === 'ar' ? 'الإحداثيات:' : 'GPS:'} {latitude.toFixed(4)}, {longitude.toFixed(4)}
                    </span>
                    {gpsDetected && (
                      <Badge variant="success" className="text-[10px] px-1.5 py-0">
                        {t('auth.gpsDetected')}
                      </Badge>
                    )}
                  </div>
                  <Button
                    type="button"
                    variant="outline"
                    size="sm"
                    onClick={handleDetectGps}
                    disabled={isDetectingGps}
                    className="h-7 text-xs border-slate-300 dark:border-slate-700 self-start sm:self-auto"
                  >
                    <Navigation className="h-3 w-3 me-1 text-emerald-600" />
                    {isDetectingGps ? t('auth.detectingGps') : t('auth.detectGps')}
                  </Button>
                </div>
              </div>

              {/* SECTION 2: Administrator Account Info */}
              <div className="space-y-3 rounded-xl border border-slate-200/80 dark:border-slate-800 p-4 bg-slate-50/50 dark:bg-slate-900/50">
                <div className="flex items-center gap-2 border-b border-slate-200/80 dark:border-slate-800 pb-2">
                  <User className="h-4 w-4 text-blue-600 dark:text-blue-400" />
                  <h4 className="text-xs font-bold text-slate-900 dark:text-slate-100 uppercase tracking-wide">
                    {t('auth.adminInfoSection')}
                  </h4>
                </div>

                <div className="grid grid-cols-1 sm:grid-cols-2 gap-3.5">
                  <div className="space-y-1">
                    <label className="text-xs font-semibold text-slate-700 dark:text-slate-300">
                      {t('auth.adminFullNameLabel')} *
                    </label>
                    <div className="relative">
                      <User className="absolute start-3 top-3 h-4 w-4 text-slate-400" />
                      <Input
                        type="text"
                        placeholder="د. أحمد محمود"
                        value={adminFullName}
                        onChange={(e) => setAdminFullName(e.target.value)}
                        className={`ps-9 dark:bg-slate-800 dark:border-slate-700 ${
                          fieldErrors.adminFullName ? 'border-red-500 focus-visible:ring-red-500' : ''
                        }`}
                        required
                      />
                    </div>
                    {fieldErrors.adminFullName && (
                      <p className="text-xs text-red-600 dark:text-red-400 mt-0.5">
                        {fieldErrors.adminFullName}
                      </p>
                    )}
                  </div>

                  <div className="space-y-1">
                    <label className="text-xs font-semibold text-slate-700 dark:text-slate-300">
                      {t('auth.adminEmailLabel')} *
                    </label>
                    <div className="relative">
                      <Mail className="absolute start-3 top-3 h-4 w-4 text-slate-400" />
                      <Input
                        type="email"
                        placeholder="admin@pharmacy.com"
                        value={adminEmail}
                        onChange={(e) => setAdminEmail(e.target.value)}
                        className={`ps-9 dark:bg-slate-800 dark:border-slate-700 ${
                          fieldErrors.adminEmail ? 'border-red-500 focus-visible:ring-red-500' : ''
                        }`}
                        required
                      />
                    </div>
                    {fieldErrors.adminEmail && (
                      <p className="text-xs text-red-600 dark:text-red-400 mt-0.5">
                        {fieldErrors.adminEmail}
                      </p>
                    )}
                  </div>

                  <div className="space-y-1">
                    <label className="text-xs font-semibold text-slate-700 dark:text-slate-300">
                      {t('auth.adminPasswordLabel')} *
                    </label>
                    <div className="relative">
                      <Lock className="absolute start-3 top-3 h-4 w-4 text-slate-400" />
                      <Input
                        type="password"
                        placeholder={t('auth.passwordPlaceholder')}
                        value={adminPassword}
                        onChange={(e) => setAdminPassword(e.target.value)}
                        className={`ps-9 dark:bg-slate-800 dark:border-slate-700 ${
                          fieldErrors.adminPassword ? 'border-red-500 focus-visible:ring-red-500' : ''
                        }`}
                        required
                      />
                    </div>
                    {fieldErrors.adminPassword && (
                      <p className="text-xs text-red-600 dark:text-red-400 mt-0.5">
                        {fieldErrors.adminPassword}
                      </p>
                    )}
                  </div>

                  <div className="space-y-1">
                    <label className="text-xs font-semibold text-slate-700 dark:text-slate-300">
                      {t('auth.adminPhoneLabel')}
                    </label>
                    <div className="relative">
                      <Phone className="absolute start-3 top-3 h-4 w-4 text-slate-400" />
                      <Input
                        type="tel"
                        placeholder="+20 100 123 4567"
                        value={adminPhone}
                        onChange={(e) => setAdminPhone(e.target.value)}
                        className={`ps-9 dark:bg-slate-800 dark:border-slate-700 ${
                          fieldErrors.adminPhone ? 'border-red-500 focus-visible:ring-red-500' : ''
                        }`}
                      />
                    </div>
                    {fieldErrors.adminPhone && (
                      <p className="text-xs text-red-600 dark:text-red-400 mt-0.5">
                        {fieldErrors.adminPhone}
                      </p>
                    )}
                  </div>
                </div>
              </div>
            </CardContent>

            <CardFooter className="flex flex-col space-y-4 pt-1">
              <Button
                type="submit"
                className="w-full bg-emerald-600 hover:bg-emerald-700 active:bg-emerald-800 text-white font-bold rounded-xl shadow-xs transition-all"
                disabled={isLoading}
              >
                <Sparkles className="h-4 w-4 me-2" />
                {isLoading ? t('auth.registeringPharmacy') : t('auth.registerPharmacyBtn')}
              </Button>

              <div className="text-center text-xs text-slate-600 dark:text-slate-400">
                {t('auth.haveAccount')}{' '}
                <Link to="/login" className="font-semibold text-blue-600 hover:underline">
                  {t('nav.signIn')}
                </Link>
              </div>
            </CardFooter>
          </form>
        )}
      </Card>
    </div>
  );
}
