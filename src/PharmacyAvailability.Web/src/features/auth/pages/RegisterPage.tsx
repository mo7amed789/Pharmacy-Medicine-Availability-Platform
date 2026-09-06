import React, { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { Lock, Mail, User, Phone, AlertCircle } from 'lucide-react';
import { useAuthStore } from '@/app/store/authStore';
import { authApi } from '../api/authApi';
import { registerSchema } from '@/lib/validations';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Card, CardHeader, CardTitle, CardDescription, CardContent, CardFooter } from '@/components/ui/card';
import { Alert, AlertDescription } from '@/components/ui/alert';
import { ApiError } from '@/api/apiClient';
import { ThemeToggle } from '@/components/common/ThemeToggle';
import { LanguageToggle } from '@/components/common/LanguageToggle';
import { useTranslation } from '@/i18n/useTranslation';

export function RegisterPage() {
  const { t, language } = useTranslation();
  const [fullName, setFullName] = useState('');
  const [email, setEmail] = useState('');
  const [phoneNumber, setPhoneNumber] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({});
  const [isLoading, setIsLoading] = useState(false);

  const { setSession } = useAuthStore();
  const navigate = useNavigate();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setFieldErrors({});

    // Client-side Zod Validation
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
      if (err instanceof ApiError) {
        setError(err.detail || err.title);
        if (err.validationErrors) {
          const mapped: Record<string, string> = {};
          for (const [key, msgs] of Object.entries(err.validationErrors)) {
            const lowerKey = key.charAt(0).toLowerCase() + key.slice(1);
            mapped[lowerKey] = msgs[0] || 'Invalid value';
          }
          setFieldErrors(mapped);
        }
      } else if (err instanceof Error) {
        setError(err.message);
      } else {
        setError(language === 'ar' ? 'تعذر إنشاء الحساب. يرجى مراجعة البيانات.' : 'Failed to create account. Please check your details.');
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

      <Card className="w-full max-w-md shadow-lg border-slate-200 dark:border-slate-800">
        <CardHeader className="text-center space-y-2">
          <img
            src="/logo.png"
            alt="PharmaFinder"
            className="mx-auto h-16 w-16 object-contain drop-shadow-xs rounded-xl"
          />
          <CardTitle className="text-2xl font-bold text-slate-900 dark:text-slate-100">{t('auth.createAccountTitle')}</CardTitle>
          <CardDescription className="text-slate-500 dark:text-slate-400">
            {t('auth.createAccountDesc')}
          </CardDescription>
        </CardHeader>

        <form onSubmit={handleSubmit}>
          <CardContent className="space-y-4">
            {error && (
              <Alert variant="destructive">
                <AlertCircle className="h-4 w-4" />
                <AlertDescription>{error}</AlertDescription>
              </Alert>
            )}

            <div className="space-y-1.5">
              <label className="text-xs font-semibold text-slate-700 dark:text-slate-300">{t('auth.fullNameLabel')}</label>
              <div className="relative">
                <User className="absolute start-3 top-3 h-4 w-4 text-slate-400" />
                <Input
                  type="text"
                  placeholder={t('auth.fullNamePlaceholder')}
                  value={fullName}
                  onChange={(e) => setFullName(e.target.value)}
                  className={`ps-9 dark:bg-slate-800 dark:border-slate-700 ${fieldErrors.fullName ? 'border-red-500 focus-visible:ring-red-500' : ''}`}
                  required
                />
              </div>
              {fieldErrors.fullName && (
                <p className="text-xs text-red-600 dark:text-red-400 mt-1">{fieldErrors.fullName}</p>
              )}
            </div>

            <div className="space-y-1.5">
              <label className="text-xs font-semibold text-slate-700 dark:text-slate-300">{t('auth.emailLabel')}</label>
              <div className="relative">
                <Mail className="absolute start-3 top-3 h-4 w-4 text-slate-400" />
                <Input
                  type="email"
                  placeholder={t('auth.emailPlaceholder')}
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  className={`ps-9 dark:bg-slate-800 dark:border-slate-700 ${fieldErrors.email ? 'border-red-500 focus-visible:ring-red-500' : ''}`}
                  required
                />
              </div>
              {fieldErrors.email && (
                <p className="text-xs text-red-600 dark:text-red-400 mt-1">{fieldErrors.email}</p>
              )}
            </div>

            <div className="space-y-1.5">
              <label className="text-xs font-semibold text-slate-700 dark:text-slate-300">{t('auth.phoneLabel')}</label>
              <div className="relative">
                <Phone className="absolute start-3 top-3 h-4 w-4 text-slate-400" />
                <Input
                  type="tel"
                  placeholder={t('auth.phonePlaceholder')}
                  value={phoneNumber}
                  onChange={(e) => setPhoneNumber(e.target.value)}
                  className={`ps-9 dark:bg-slate-800 dark:border-slate-700 ${fieldErrors.phoneNumber ? 'border-red-500 focus-visible:ring-red-500' : ''}`}
                  required
                />
              </div>
              {fieldErrors.phoneNumber && (
                <p className="text-xs text-red-600 dark:text-red-400 mt-1">{fieldErrors.phoneNumber}</p>
              )}
            </div>

            <div className="space-y-1.5">
              <label className="text-xs font-semibold text-slate-700 dark:text-slate-300">{t('auth.passwordLabel')}</label>
              <div className="relative">
                <Lock className="absolute start-3 top-3 h-4 w-4 text-slate-400" />
                <Input
                  type="password"
                  placeholder={t('auth.passwordPlaceholder')}
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  className={`ps-9 dark:bg-slate-800 dark:border-slate-700 ${fieldErrors.password ? 'border-red-500 focus-visible:ring-red-500' : ''}`}
                  required
                />
              </div>
              {fieldErrors.password && (
                <p className="text-xs text-red-600 dark:text-red-400 mt-1">{fieldErrors.password}</p>
              )}
            </div>
          </CardContent>

          <CardFooter className="flex flex-col space-y-4">
            <Button type="submit" className="w-full" disabled={isLoading}>
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
      </Card>
    </div>
  );
}
