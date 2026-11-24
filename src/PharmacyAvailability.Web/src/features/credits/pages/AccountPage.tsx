import { useState } from 'react';
import { Navigate, useNavigate } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { Copy, Check, MessageCircle, Sparkles, LogOut } from 'lucide-react';
import { useAuthStore } from '@/app/store/authStore';
import { tokenManager } from '@/api/tokenManager';
import { authApi } from '@/features/auth/api/authApi';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Alert, AlertDescription } from '@/components/ui/alert';
import { useTranslation } from '@/i18n/useTranslation';
import { creditsApi } from '../api/creditsApi';
import { creditStrings } from '../strings';

// Digits only, country code included, e.g. 201012345678. Set in the web environment, never in code.
const OPERATOR_WHATSAPP = (import.meta.env.VITE_OPERATOR_WHATSAPP as string | undefined)?.replace(/\D/g, '');

export function AccountPage() {
  const { language } = useTranslation();
  const s = creditStrings[language === 'ar' ? 'ar' : 'en'];
  const { user, isAuthenticated, isInitialized, logout } = useAuthStore();
  const navigate = useNavigate();
  const [copied, setCopied] = useState(false);

  const { data, isLoading, isError } = useQuery({
    queryKey: ['credits', 'me'],
    queryFn: creditsApi.me,
    enabled: isAuthenticated,
  });

  if (!isInitialized) return null;
  if (!isAuthenticated || !user) return <Navigate to="/account/join" replace />;

  const copy = async () => {
    if (!data) return;
    try {
      await navigator.clipboard.writeText(data.publicCode);
      setCopied(true);
      setTimeout(() => setCopied(false), 2000);
    } catch {
      /* clipboard can be blocked; the code stays visible on screen */
    }
  };

  const waText = encodeURIComponent(`${s.yourCode}: ${data?.publicCode ?? ''}`);
  const waHref = OPERATOR_WHATSAPP ? `https://wa.me/${OPERATOR_WHATSAPP}?text=${waText}` : null;

  const signOut = async () => {
    try {
      const rt = tokenManager.getRefreshToken();
      if (rt) await authApi.revokeToken({ refreshToken: rt });
    } catch {
      /* ignore */
    }
    logout();
    navigate('/customer/search');
  };

  return (
    <div className="mx-auto max-w-xl space-y-4 px-4 py-8">
      {data && !data.chargingEnabled && (
        <Alert>
          <Sparkles className="h-4 w-4" />
          <AlertDescription>{s.launchBanner}</AlertDescription>
        </Alert>
      )}
      {!user.isPhoneVerified && (
        <Alert variant="destructive">
          <AlertDescription>
            {s.unverified}{' '}
            <Button variant="link" className="px-1" onClick={() => navigate('/verify')}>
              {s.verifyNote}
            </Button>
          </AlertDescription>
        </Alert>
      )}
      <Card>
        <CardHeader>
          <CardTitle>{s.accountTitle}</CardTitle>
          <p dir="ltr" className="text-sm text-slate-500">{user.phoneNumber}</p>
        </CardHeader>
        <CardContent className="space-y-5">
          {isLoading && <p className="text-sm text-slate-500">…</p>}
          {isError && <p className="text-sm text-red-600">{s.error}</p>}
          {data && (
            <>
              <div>
                <p className="mb-1 text-sm font-medium">{s.yourCode}</p>
                <div className="flex items-center gap-2">
                  <code dir="ltr" className="rounded-lg bg-slate-100 px-4 py-2 text-2xl font-black tracking-widest dark:bg-slate-800">
                    {data.publicCode}
                  </code>
                  <Button variant="outline" size="sm" onClick={copy}>
                    {copied ? <Check className="h-4 w-4" /> : <Copy className="h-4 w-4" />}
                    <span className="ms-1">{copied ? s.copied : s.copy}</span>
                  </Button>
                </div>
                {waHref && (
                  <a href={waHref} target="_blank" rel="noopener noreferrer" className="mt-3 inline-flex">
                    <Button size="sm"><MessageCircle className="h-4 w-4" /><span className="ms-1">{s.sendWhatsapp}</span></Button>
                  </a>
                )}
              </div>
              <div className="grid grid-cols-2 gap-3 text-center">
                <div className="rounded-xl bg-slate-50 p-4 dark:bg-slate-900">
                  <p className="text-3xl font-black">{data.balance}</p>
                  <p className="text-xs text-slate-500">{s.balance} ({s.points})</p>
                </div>
                <div className="rounded-xl bg-slate-50 p-4 dark:bg-slate-900">
                  <p className="text-3xl font-black">{data.freeQueriesRemaining}</p>
                  <p className="text-xs text-slate-500">{s.freeLeft}</p>
                </div>
              </div>
              {data.chargingEnabled && <p className="text-sm text-slate-600 dark:text-slate-300">{s.chargingNote}</p>}
              <div className="text-sm">
                <p className="mb-1 font-semibold">{s.howTitle}</p>
                <ol className="list-decimal space-y-1 ps-5 text-slate-600 dark:text-slate-300">
                  <li>{s.how1}</li>
                  <li>{s.how2}</li>
                  <li>{s.how3}</li>
                </ol>
              </div>
            </>
          )}
          <Button variant="ghost" size="sm" onClick={signOut}><LogOut className="h-4 w-4" /><span className="ms-1">{s.signOut}</span></Button>
        </CardContent>
      </Card>
    </div>
  );
}
