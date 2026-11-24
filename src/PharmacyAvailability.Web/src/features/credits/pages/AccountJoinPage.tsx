import { useState } from 'react';
import { Navigate, useNavigate } from 'react-router-dom';
import { Phone, Lock, User } from 'lucide-react';
import { useAuthStore } from '@/app/store/authStore';
import { authApi } from '@/features/auth/api/authApi';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card';
import { Alert, AlertDescription } from '@/components/ui/alert';
import { useTranslation } from '@/i18n/useTranslation';
import { normalizeEgyptPhone } from '../phone';
import { creditStrings } from '../strings';

export function AccountJoinPage() {
  const { language } = useTranslation();
  const s = creditStrings[language === 'ar' ? 'ar' : 'en'];
  const { isAuthenticated, setSession } = useAuthStore();
  const navigate = useNavigate();
  const [mode, setMode] = useState<'up' | 'in'>('up');
  const [phone, setPhone] = useState('');
  const [password, setPassword] = useState('');
  const [name, setName] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  if (isAuthenticated) return <Navigate to="/account" replace />;

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    const normalized = normalizeEgyptPhone(phone);
    if (!normalized) return setError(s.phoneInvalid);
    if (mode === 'up' && password.length < 8) return setError(s.passwordShort);
    setBusy(true);
    try {
      const res =
        mode === 'up'
          ? await authApi.registerPhone({ phoneNumber: normalized, password, fullName: name.trim() || undefined })
          : await authApi.loginPhone({ phoneNumber: normalized, password });
      setSession(res.user, res.accessToken, res.refreshToken);
      navigate(res.user.isPhoneVerified ? '/account' : '/verify', { replace: true });
    } catch (err) {
      setError((err as Error).message || s.error);
    } finally {
      setBusy(false);
    }
  };

  return (
    <div className="mx-auto max-w-md px-4 py-10">
      <Card>
        <CardHeader>
          <CardTitle>{s.joinTitle}</CardTitle>
          <CardDescription>{mode === 'up' ? s.verifyNote : ''}</CardDescription>
          <div className="mt-3 flex gap-2" role="tablist">
            <Button type="button" variant={mode === 'up' ? 'default' : 'outline'} size="sm" onClick={() => setMode('up')}>
              {s.signUp}
            </Button>
            <Button type="button" variant={mode === 'in' ? 'default' : 'outline'} size="sm" onClick={() => setMode('in')}>
              {s.signIn}
            </Button>
          </div>
        </CardHeader>
        <CardContent>
          <form onSubmit={submit} className="space-y-4" noValidate>
            {error && (
              <Alert variant="destructive">
                <AlertDescription>{error}</AlertDescription>
              </Alert>
            )}
            {mode === 'up' && (
              <label className="block text-sm">
                <span className="mb-1 flex items-center gap-1.5 font-medium"><User className="h-4 w-4" />{s.name}</span>
                <Input value={name} onChange={(e) => setName(e.target.value)} maxLength={150} autoComplete="name" />
              </label>
            )}
            <label className="block text-sm">
              <span className="mb-1 flex items-center gap-1.5 font-medium"><Phone className="h-4 w-4" />{s.phone}</span>
              <Input
                dir="ltr"
                inputMode="tel"
                autoComplete="tel"
                placeholder={s.phoneHint}
                value={phone}
                onChange={(e) => setPhone(e.target.value)}
              />
            </label>
            <label className="block text-sm">
              <span className="mb-1 flex items-center gap-1.5 font-medium"><Lock className="h-4 w-4" />{s.password}</span>
              <Input
                type="password"
                dir="ltr"
                autoComplete={mode === 'up' ? 'new-password' : 'current-password'}
                value={password}
                onChange={(e) => setPassword(e.target.value)}
              />
            </label>
            <Button type="submit" className="w-full" disabled={busy}>
              {mode === 'up' ? s.submitUp : s.submitIn}
            </Button>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}
