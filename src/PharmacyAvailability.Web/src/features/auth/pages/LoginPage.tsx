import React, { useState } from 'react';
import { Link, useNavigate, useLocation } from 'react-router-dom';
import { Pill, Lock, Mail, AlertCircle } from 'lucide-react';
import { useAuthStore } from '@/app/store/authStore';
import { authApi } from '../api/authApi';
import { loginSchema } from '@/lib/validations';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Card, CardHeader, CardTitle, CardDescription, CardContent, CardFooter } from '@/components/ui/card';
import { Alert, AlertDescription } from '@/components/ui/alert';
import { ApiError } from '@/api/apiClient';
import { ThemeToggle } from '@/components/common/ThemeToggle';

export function LoginPage() {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({});
  const [isLoading, setIsLoading] = useState(false);

  const { setSession } = useAuthStore();
  const navigate = useNavigate();
  const location = useLocation();

  const from = (location.state as { from?: { pathname: string } })?.from?.pathname || '/';

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setFieldErrors({});

    const validationResult = loginSchema.safeParse({ email, password });
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
      const response = await authApi.login({ email: email.trim(), password });
      setSession(response.user, response.accessToken, response.refreshToken);

      // Role-based smart redirect if from is root
      if (from === '/' || from === '/login') {
        if (response.user.role === 'PlatformAdmin') {
          navigate('/admin/dashboard', { replace: true });
        } else if (response.user.role === 'PharmacyStaff' || response.user.role === 'PharmacyAdmin') {
          navigate('/pharmacy/dashboard', { replace: true });
        } else {
          navigate('/customer/search', { replace: true });
        }
      } else {
        navigate(from, { replace: true });
      }
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
        setError('Failed to sign in. Please verify your credentials.');
      }
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="relative flex min-h-screen items-center justify-center bg-slate-50 dark:bg-slate-950 px-4 py-12 sm:px-6 lg:px-8">
      <div className="absolute top-4 right-4">
        <ThemeToggle />
      </div>

      <Card className="w-full max-w-md shadow-lg border-slate-200 dark:border-slate-800">
        <CardHeader className="text-center space-y-2">
          <div className="mx-auto flex h-12 w-12 items-center justify-center rounded-xl bg-blue-600 text-white shadow-sm">
            <Pill className="h-6 w-6" />
          </div>
          <CardTitle className="text-2xl font-bold text-slate-900 dark:text-slate-100">Sign in to your account</CardTitle>
          <CardDescription className="text-slate-500 dark:text-slate-400">
            Access real-time medicine availability or your branch operations
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
              <label className="text-xs font-semibold text-slate-700 dark:text-slate-300">Email Address</label>
              <div className="relative">
                <Mail className="absolute left-3 top-3 h-4 w-4 text-slate-400 dark:text-slate-500" />
                <Input
                  type="email"
                  placeholder="name@example.com"
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  className={`pl-9 ${fieldErrors.email ? 'border-red-500 focus-visible:ring-red-500' : ''}`}
                  required
                />
              </div>
              {fieldErrors.email && (
                <p className="text-xs text-red-600 dark:text-red-400 mt-1">{fieldErrors.email}</p>
              )}
            </div>

            <div className="space-y-1.5">
              <label className="text-xs font-semibold text-slate-700 dark:text-slate-300">Password</label>
              <div className="relative">
                <Lock className="absolute left-3 top-3 h-4 w-4 text-slate-400 dark:text-slate-500" />
                <Input
                  type="password"
                  placeholder="••••••••"
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  className={`pl-9 ${fieldErrors.password ? 'border-red-500 focus-visible:ring-red-500' : ''}`}
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
              {isLoading ? 'Signing In...' : 'Sign In'}
            </Button>

            <div className="text-center text-xs text-slate-600 dark:text-slate-400">
              Don't have an account?{' '}
              <Link to="/register" className="font-semibold text-blue-600 dark:text-blue-400 hover:underline">
                Create one now
              </Link>
            </div>

            {/* Quick Demo Fill Buttons */}
            <div className="w-full pt-3 border-t border-slate-100 dark:border-slate-800 space-y-2">
              <p className="text-[11px] font-semibold text-slate-400 dark:text-slate-500 text-center uppercase tracking-wider">
                Demo Accounts (1-Click Fill)
              </p>
              <div className="grid grid-cols-3 gap-2">
                <button
                  type="button"
                  onClick={() => {
                    setEmail('admin@pharma.com');
                    setPassword('Admin123!');
                  }}
                  className="rounded-lg border border-purple-200 dark:border-purple-800 bg-purple-50 dark:bg-purple-950/40 px-2 py-1.5 text-center text-xs font-semibold text-purple-700 dark:text-purple-300 hover:bg-purple-100 dark:hover:bg-purple-900/40 transition-colors"
                  title="Platform Admin: admin@pharma.com"
                >
                  👑 Admin
                </button>
                <button
                  type="button"
                  onClick={() => {
                    setEmail('ezaby.admin@pharma.com');
                    setPassword('Pharmacy123!');
                  }}
                  className="rounded-lg border border-blue-200 dark:border-blue-800 bg-blue-50 dark:bg-blue-950/40 px-2 py-1.5 text-center text-xs font-semibold text-blue-700 dark:text-blue-300 hover:bg-blue-100 dark:hover:bg-blue-900/40 transition-colors"
                  title="Pharmacy Admin: ezaby.admin@pharma.com"
                >
                  🏥 Pharmacy
                </button>
                <button
                  type="button"
                  onClick={() => {
                    setEmail('customer@test.com');
                    setPassword('Customer123!');
                  }}
                  className="rounded-lg border border-emerald-200 dark:border-emerald-800 bg-emerald-50 dark:bg-emerald-950/40 px-2 py-1.5 text-center text-xs font-semibold text-emerald-700 dark:text-emerald-300 hover:bg-emerald-100 dark:hover:bg-emerald-900/40 transition-colors"
                  title="Customer: customer@test.com"
                >
                  👤 Customer
                </button>
              </div>
            </div>
          </CardFooter>
        </form>
      </Card>
    </div>
  );
}
