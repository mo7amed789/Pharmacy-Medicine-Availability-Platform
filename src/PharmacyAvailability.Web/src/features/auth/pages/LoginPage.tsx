import React, { useState } from 'react';
import { Link, useNavigate, useLocation } from 'react-router-dom';
import { Pill, Lock, Mail, AlertCircle } from 'lucide-react';
import { useAuthStore } from '@/app/store/authStore';
import { authApi } from '../api/authApi';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Card, CardHeader, CardTitle, CardDescription, CardContent, CardFooter } from '@/components/ui/card';
import { Alert, AlertDescription } from '@/components/ui/alert';
import { ApiError } from '@/api/apiClient';

export function LoginPage() {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(false);

  const { setSession } = useAuthStore();
  const navigate = useNavigate();
  const location = useLocation();

  const from = (location.state as { from?: { pathname: string } })?.from?.pathname || '/';

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setIsLoading(true);

    try {
      const response = await authApi.login({ email, password });
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
    <div className="flex min-h-screen items-center justify-center bg-slate-50 px-4 py-12 sm:px-6 lg:px-8">
      <Card className="w-full max-w-md shadow-lg border-slate-200">
        <CardHeader className="text-center space-y-2">
          <div className="mx-auto flex h-12 w-12 items-center justify-center rounded-xl bg-blue-600 text-white shadow-sm">
            <Pill className="h-6 w-6" />
          </div>
          <CardTitle className="text-2xl font-bold text-slate-900">Sign in to your account</CardTitle>
          <CardDescription className="text-slate-500">
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
              <label className="text-xs font-semibold text-slate-700">Email Address</label>
              <div className="relative">
                <Mail className="absolute left-3 top-3 h-4 w-4 text-slate-400" />
                <Input
                  type="email"
                  placeholder="name@example.com"
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  className="pl-9"
                  required
                />
              </div>
            </div>

            <div className="space-y-1.5">
              <label className="text-xs font-semibold text-slate-700">Password</label>
              <div className="relative">
                <Lock className="absolute left-3 top-3 h-4 w-4 text-slate-400" />
                <Input
                  type="password"
                  placeholder="••••••••"
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  className="pl-9"
                  required
                />
              </div>
            </div>
          </CardContent>

          <CardFooter className="flex flex-col space-y-4">
            <Button type="submit" className="w-full" disabled={isLoading}>
              {isLoading ? 'Signing In...' : 'Sign In'}
            </Button>

            <div className="text-center text-xs text-slate-600">
              Don't have an account?{' '}
              <Link to="/register" className="font-semibold text-blue-600 hover:underline">
                Create one now
              </Link>
            </div>
          </CardFooter>
        </form>
      </Card>
    </div>
  );
}
