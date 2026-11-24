import { useState } from 'react';
import { Navigate } from 'react-router-dom';
import { useAuthStore } from '@/app/store/authStore';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Alert, AlertDescription } from '@/components/ui/alert';
import { useTranslation } from '@/i18n/useTranslation';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { creditsApi, AdminCreditAccount } from '../api/creditsApi';
import { creditStrings } from '../strings';

export function AdminCreditsPage() {
  const { language } = useTranslation();
  const s = creditStrings[language === 'ar' ? 'ar' : 'en'];
  const { user, isAuthenticated, isInitialized } = useAuthStore();
  const qc = useQueryClient();
  const [code, setCode] = useState('');
  const [account, setAccount] = useState<AdminCreditAccount | null>(null);
  const [points, setPoints] = useState('');
  const [reference, setReference] = useState('');
  const [note, setNote] = useState('');
  const [msg, setMsg] = useState<{ kind: 'ok' | 'err'; text: string } | null>(null);
  const [busy, setBusy] = useState(false);

  const isAdmin = isAuthenticated && user?.role === 'PlatformAdmin';
  const usage = useQuery({ queryKey: ['credits', 'usage'], queryFn: creditsApi.usage, enabled: isAdmin });

  if (!isInitialized) return null;
  if (!isAuthenticated) return <Navigate to="/account/join" replace />;
  if (!isAdmin) {
    return <div className="mx-auto max-w-md p-10 text-center text-sm text-slate-600">{s.adminOnly}</div>;
  }

  const find = async (e: React.FormEvent) => {
    e.preventDefault();
    setMsg(null);
    setAccount(null);
    try {
      setAccount(await creditsApi.lookup(code.trim()));
    } catch (err) {
      setMsg({ kind: 'err', text: (err as Error).message || s.notFound });
    }
  };

  const topUp = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!account) return;
    const n = Number.parseInt(points, 10);
    if (!Number.isInteger(n) || n <= 0 || !reference.trim()) return;
    setBusy(true);
    setMsg(null);
    try {
      const updated = await creditsApi.topUp({
        publicCode: account.publicCode,
        points: n,
        transferReference: reference.trim(),
        note: note.trim() || undefined,
      });
      setAccount(updated);
      setPoints('');
      setReference('');
      setNote('');
      setMsg({ kind: 'ok', text: s.topUpDone });
      qc.invalidateQueries({ queryKey: ['credits', 'usage'] });
    } catch (err) {
      setMsg({ kind: 'err', text: (err as Error).message || s.error });
    } finally {
      setBusy(false);
    }
  };

  const u = usage.data;
  const stats: [string, number][] = u
    ? [
        [s.uTotal, u.totalQueries],
        [s.uSuccess, u.successQueries],
        [s.uUnknown, u.unknownQueries],
        [s.uNone, u.noResultQueries],
        [s.uCharged, u.pointsCharged],
        [s.uFree, u.freeQueriesUsed],
        [s.uUsers, u.distinctUsers],
      ]
    : [];

  return (
    <div className="mx-auto max-w-3xl space-y-4 px-4 py-8">
      <h1 className="text-2xl font-black">{s.adminTitle}</h1>

      {stats.length > 0 && (
        <Card>
          <CardHeader><CardTitle className="text-base">{s.usage}</CardTitle></CardHeader>
          <CardContent className="grid grid-cols-2 gap-3 sm:grid-cols-4">
            {stats.map(([label, value]) => (
              <div key={label} className="rounded-xl bg-slate-50 p-3 text-center dark:bg-slate-900">
                <p className="text-2xl font-black">{value}</p>
                <p className="text-xs text-slate-500">{label}</p>
              </div>
            ))}
          </CardContent>
        </Card>
      )}

      <Card>
        <CardHeader><CardTitle className="text-base">{s.lookup}</CardTitle></CardHeader>
        <CardContent>
          <form onSubmit={find} className="flex gap-2">
            <Input dir="ltr" placeholder={s.codePlaceholder} value={code} onChange={(e) => setCode(e.target.value)} />
            <Button type="submit">{s.find}</Button>
          </form>
        </CardContent>
      </Card>

      {msg && (
        <Alert variant={msg.kind === 'err' ? 'destructive' : 'default'}>
          <AlertDescription>{msg.text}</AlertDescription>
        </Alert>
      )}

      {account && (
        <>
          <Card>
            <CardHeader>
              <CardTitle className="text-base">
                <span dir="ltr">{account.publicCode}</span> · {account.fullName}
              </CardTitle>
              <p dir="ltr" className="text-sm text-slate-500">
                {account.phoneNumber} {!account.isPhoneVerified && `(${s.unverified})`}
              </p>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="grid grid-cols-2 gap-3 text-center">
                <div className="rounded-xl bg-slate-50 p-3 dark:bg-slate-900"><p className="text-2xl font-black">{account.balance}</p><p className="text-xs text-slate-500">{s.balance}</p></div>
                <div className="rounded-xl bg-slate-50 p-3 dark:bg-slate-900"><p className="text-2xl font-black">{account.freeQueriesRemaining}</p><p className="text-xs text-slate-500">{s.freeLeft}</p></div>
              </div>
              <Alert><AlertDescription>{s.confirmBank}</AlertDescription></Alert>
              <form onSubmit={topUp} className="grid gap-3 sm:grid-cols-2">
                <label className="text-sm">{s.pointsLabel}
                  <Input dir="ltr" inputMode="numeric" value={points} onChange={(e) => setPoints(e.target.value.replace(/\D/g, ''))} />
                </label>
                <label className="text-sm">{s.reference}
                  <Input dir="ltr" value={reference} onChange={(e) => setReference(e.target.value)} maxLength={100} />
                </label>
                <label className="text-sm sm:col-span-2">{s.note}
                  <Input value={note} onChange={(e) => setNote(e.target.value)} maxLength={300} />
                </label>
                <Button type="submit" disabled={busy || !points || !reference.trim()} className="sm:col-span-2">{s.topUp}</Button>
              </form>
            </CardContent>
          </Card>

          <Card>
            <CardHeader><CardTitle className="text-base">{s.ledger}</CardTitle></CardHeader>
            <CardContent className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="text-start text-xs text-slate-500">
                    <th className="p-2 text-start">{s.when}</th><th className="p-2 text-start">{s.type}</th>
                    <th className="p-2 text-start">{s.amount}</th><th className="p-2 text-start">{s.after}</th><th className="p-2 text-start">{s.ref}</th>
                  </tr>
                </thead>
                <tbody>
                  {account.recentEntries.map((e) => (
                    <tr key={e.id} className="border-t border-slate-200 dark:border-slate-800">
                      <td dir="ltr" className="p-2">{new Date(e.createdAt).toLocaleString()}</td>
                      <td className="p-2">{e.type}</td>
                      <td dir="ltr" className="p-2">{e.points > 0 ? `+${e.points}` : e.points}</td>
                      <td dir="ltr" className="p-2">{e.balanceAfter}</td>
                      <td dir="ltr" className="p-2">{e.transferReference ?? ''}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </CardContent>
          </Card>
        </>
      )}
    </div>
  );
}
