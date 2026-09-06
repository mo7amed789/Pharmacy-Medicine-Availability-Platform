import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { Mail, Phone, CheckCircle2, AlertCircle, RefreshCw, LogOut, ShieldCheck } from 'lucide-react';
import { useAuthStore } from '@/app/store/authStore';
import { authApi } from '../api/authApi';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Card, CardHeader, CardTitle, CardDescription, CardContent, CardFooter } from '@/components/ui/card';
import { Alert, AlertDescription } from '@/components/ui/alert';
import { Badge } from '@/components/ui/badge';
import { ApiError } from '@/api/apiClient';

import { verifyCodeSchema } from '@/lib/validations';

export function VerificationPage() {
  const { user, setUser, logout } = useAuthStore();
  const navigate = useNavigate();

  const [emailCode, setEmailCode] = useState('');
  const [phoneCode, setPhoneCode] = useState('');
  const [emailLoading, setEmailLoading] = useState(false);
  const [phoneLoading, setPhoneLoading] = useState(false);
  const [resendEmailLoading, setResendEmailLoading] = useState(false);
  const [resendPhoneLoading, setResendPhoneLoading] = useState(false);

  const [emailCooldown, setEmailCooldown] = useState(0);
  const [phoneCooldown, setPhoneCooldown] = useState(0);

  const [emailError, setEmailError] = useState<string | null>(null);
  const [phoneError, setPhoneError] = useState<string | null>(null);
  const [emailSuccess, setEmailSuccess] = useState<string | null>(null);
  const [phoneSuccess, setPhoneSuccess] = useState<string | null>(null);

  // Auto-redirect if already fully verified
  useEffect(() => {
    if (user?.isEmailVerified && user?.isPhoneVerified) {
      if (user.role === 'PlatformAdmin') {
        navigate('/admin/dashboard', { replace: true });
      } else if (user.role === 'PharmacyStaff' || user.role === 'PharmacyAdmin') {
        navigate('/pharmacy/dashboard', { replace: true });
      } else {
        navigate('/customer/search', { replace: true });
      }
    }
  }, [user, navigate]);

  // Timers for resend countdowns
  useEffect(() => {
    if (emailCooldown <= 0) return;
    const timer = setInterval(() => {
      setEmailCooldown((prev) => (prev > 0 ? prev - 1 : 0));
    }, 1000);
    return () => clearInterval(timer);
  }, [emailCooldown]);

  useEffect(() => {
    if (phoneCooldown <= 0) return;
    const timer = setInterval(() => {
      setPhoneCooldown((prev) => (prev > 0 ? prev - 1 : 0));
    }, 1000);
    return () => clearInterval(timer);
  }, [phoneCooldown]);

  const handleVerifyEmail = async (e: React.FormEvent) => {
    e.preventDefault();
    setEmailError(null);
    setEmailSuccess(null);

    const validation = verifyCodeSchema.safeParse({ type: 'Email', code: emailCode.trim() });
    if (!validation.success) {
      setEmailError(validation.error.issues[0]?.message || 'Invalid verification code.');
      return;
    }

    setEmailLoading(true);

    try {
      const res = await authApi.verifyCode({ type: 'Email', code: emailCode.trim() });
      if (user) {
        setUser({ ...user, isEmailVerified: res.isEmailVerified, isPhoneVerified: res.isPhoneVerified });
      }
      setEmailSuccess('Email verified successfully!');
      setEmailCode('');
    } catch (err: unknown) {
      if (err instanceof ApiError) {
        setEmailError(err.detail || err.title);
      } else if (err instanceof Error) {
        setEmailError(err.message);
      } else {
        setEmailError('Failed to verify email code.');
      }
    } finally {
      setEmailLoading(false);
    }
  };

  const handleVerifyPhone = async (e: React.FormEvent) => {
    e.preventDefault();
    setPhoneError(null);
    setPhoneSuccess(null);

    const validation = verifyCodeSchema.safeParse({ type: 'Phone', code: phoneCode.trim() });
    if (!validation.success) {
      setPhoneError(validation.error.issues[0]?.message || 'Invalid verification code.');
      return;
    }

    setPhoneLoading(true);

    try {
      const res = await authApi.verifyCode({ type: 'Phone', code: phoneCode.trim() });
      if (user) {
        setUser({ ...user, isEmailVerified: res.isEmailVerified, isPhoneVerified: res.isPhoneVerified });
      }
      setPhoneSuccess('Phone number verified successfully!');
      setPhoneCode('');
    } catch (err: unknown) {
      if (err instanceof ApiError) {
        setPhoneError(err.detail || err.title);
      } else if (err instanceof Error) {
        setPhoneError(err.message);
      } else {
        setPhoneError('Failed to verify phone code.');
      }
    } finally {
      setPhoneLoading(false);
    }
  };

  const handleResendEmail = async () => {
    if (emailCooldown > 0) return;
    setEmailError(null);
    setEmailSuccess(null);
    setResendEmailLoading(true);

    try {
      await authApi.resendCode({ type: 'Email' });
      setEmailSuccess('New verification code sent to your email.');
      setEmailCooldown(60);
    } catch (err: unknown) {
      if (err instanceof ApiError) {
        setEmailError(err.detail || err.title);
      } else if (err instanceof Error) {
        setEmailError(err.message);
      } else {
        setEmailError('Failed to resend code.');
      }
    } finally {
      setResendEmailLoading(false);
    }
  };

  const handleResendPhone = async () => {
    if (phoneCooldown > 0) return;
    setPhoneError(null);
    setPhoneSuccess(null);
    setResendPhoneLoading(true);

    try {
      await authApi.resendCode({ type: 'Phone' });
      setPhoneSuccess('New verification code sent via SMS.');
      setPhoneCooldown(60);
    } catch (err: unknown) {
      if (err instanceof ApiError) {
        setPhoneError(err.detail || err.title);
      } else if (err instanceof Error) {
        setPhoneError(err.message);
      } else {
        setPhoneError('Failed to resend code.');
      }
    } finally {
      setResendPhoneLoading(false);
    }
  };

  return (
    <div className="flex min-h-screen items-center justify-center bg-slate-50 px-4 py-12 sm:px-6 lg:px-8">
      <Card className="w-full max-w-xl shadow-lg border-slate-200">
        <CardHeader className="text-center space-y-2">
          <div className="mx-auto flex h-14 w-14 items-center justify-center rounded-2xl bg-blue-600 text-white shadow-md">
            <ShieldCheck className="h-8 w-8" />
          </div>
          <CardTitle className="text-2xl font-bold text-slate-900">Verify Your Identity</CardTitle>
          <CardDescription className="text-slate-500">
            Please verify both your email and phone number to activate your account.
          </CardDescription>
          <div className="mt-2 text-xs bg-amber-50 text-amber-800 p-2.5 rounded-lg border border-amber-200 text-left">
            <span className="font-semibold">Developer Tip:</span> In development mode, verification OTP codes are logged directly to the backend API console terminal.
          </div>
        </CardHeader>

        <CardContent className="space-y-6">
          {/* Email Verification Section */}
          <div className="rounded-xl border border-slate-200 bg-white p-4 shadow-xs space-y-3">
            <div className="flex items-center justify-between">
              <div className="flex items-center space-x-2.5">
                <div className="flex h-9 w-9 items-center justify-center rounded-lg bg-blue-50 text-blue-600">
                  <Mail className="h-5 w-5" />
                </div>
                <div>
                  <h4 className="text-sm font-semibold text-slate-900">Email Address</h4>
                  <p className="text-xs text-slate-500">{user?.email}</p>
                </div>
              </div>
              {user?.isEmailVerified ? (
                <Badge variant="default" className="bg-emerald-600 hover:bg-emerald-600 text-white gap-1">
                  <CheckCircle2 className="h-3.5 w-3.5" /> Verified
                </Badge>
              ) : (
                <Badge variant="outline" className="text-amber-600 border-amber-300 bg-amber-50">
                  Pending
                </Badge>
              )}
            </div>

            {emailError && (
              <Alert variant="destructive" className="py-2">
                <AlertCircle className="h-4 w-4" />
                <AlertDescription className="text-xs">{emailError}</AlertDescription>
              </Alert>
            )}

            {emailSuccess && (
              <Alert className="py-2 border-emerald-300 bg-emerald-50 text-emerald-800">
                <CheckCircle2 className="h-4 w-4 text-emerald-600" />
                <AlertDescription className="text-xs">{emailSuccess}</AlertDescription>
              </Alert>
            )}

            {!user?.isEmailVerified && (
              <form onSubmit={handleVerifyEmail} className="flex flex-col sm:flex-row gap-2 pt-1">
                <Input
                  type="text"
                  placeholder="Enter 6-digit email code"
                  value={emailCode}
                  onChange={(e) => setEmailCode(e.target.value)}
                  maxLength={6}
                  className="flex-1 tracking-widest font-mono text-center sm:text-left"
                  required
                />
                <div className="flex gap-2">
                  <Button type="submit" size="sm" disabled={emailLoading || !emailCode}>
                    {emailLoading ? 'Verifying...' : 'Verify'}
                  </Button>
                  <Button
                    type="button"
                    variant="outline"
                    size="sm"
                    onClick={handleResendEmail}
                    disabled={resendEmailLoading || emailCooldown > 0}
                  >
                    {resendEmailLoading ? (
                      <RefreshCw className="h-4 w-4 animate-spin" />
                    ) : emailCooldown > 0 ? (
                      `Resend (${emailCooldown}s)`
                    ) : (
                      'Resend Code'
                    )}
                  </Button>
                </div>
              </form>
            )}
          </div>

          {/* Phone Verification Section */}
          <div className="rounded-xl border border-slate-200 bg-white p-4 shadow-xs space-y-3">
            <div className="flex items-center justify-between">
              <div className="flex items-center space-x-2.5">
                <div className="flex h-9 w-9 items-center justify-center rounded-lg bg-emerald-50 text-emerald-600">
                  <Phone className="h-5 w-5" />
                </div>
                <div>
                  <h4 className="text-sm font-semibold text-slate-900">Phone Number</h4>
                  <p className="text-xs text-slate-500">{user?.phoneNumber}</p>
                </div>
              </div>
              {user?.isPhoneVerified ? (
                <Badge variant="default" className="bg-emerald-600 hover:bg-emerald-600 text-white gap-1">
                  <CheckCircle2 className="h-3.5 w-3.5" /> Verified
                </Badge>
              ) : (
                <Badge variant="outline" className="text-amber-600 border-amber-300 bg-amber-50">
                  Pending
                </Badge>
              )}
            </div>

            {phoneError && (
              <Alert variant="destructive" className="py-2">
                <AlertCircle className="h-4 w-4" />
                <AlertDescription className="text-xs">{phoneError}</AlertDescription>
              </Alert>
            )}

            {phoneSuccess && (
              <Alert className="py-2 border-emerald-300 bg-emerald-50 text-emerald-800">
                <CheckCircle2 className="h-4 w-4 text-emerald-600" />
                <AlertDescription className="text-xs">{phoneSuccess}</AlertDescription>
              </Alert>
            )}

            {!user?.isPhoneVerified && (
              <form onSubmit={handleVerifyPhone} className="flex flex-col sm:flex-row gap-2 pt-1">
                <Input
                  type="text"
                  placeholder="Enter 6-digit phone code"
                  value={phoneCode}
                  onChange={(e) => setPhoneCode(e.target.value)}
                  maxLength={6}
                  className="flex-1 tracking-widest font-mono text-center sm:text-left"
                  required
                />
                <div className="flex gap-2">
                  <Button type="submit" size="sm" disabled={phoneLoading || !phoneCode}>
                    {phoneLoading ? 'Verifying...' : 'Verify'}
                  </Button>
                  <Button
                    type="button"
                    variant="outline"
                    size="sm"
                    onClick={handleResendPhone}
                    disabled={resendPhoneLoading || phoneCooldown > 0}
                  >
                    {resendPhoneLoading ? (
                      <RefreshCw className="h-4 w-4 animate-spin" />
                    ) : phoneCooldown > 0 ? (
                      `Resend (${phoneCooldown}s)`
                    ) : (
                      'Resend Code'
                    )}
                  </Button>
                </div>
              </form>
            )}
          </div>
        </CardContent>

        <CardFooter className="flex justify-between items-center border-t border-slate-100 pt-4">
          <Button
            type="button"
            variant="ghost"
            size="sm"
            onClick={() => {
              logout();
              navigate('/login', { replace: true });
            }}
            className="text-slate-500 hover:text-slate-800 gap-1.5"
          >
            <LogOut className="h-4 w-4" /> Sign Out
          </Button>

          {user?.isEmailVerified && user?.isPhoneVerified && (
            <Button
              type="button"
              onClick={() => navigate('/customer/search', { replace: true })}
              className="bg-emerald-600 hover:bg-emerald-700 text-white"
            >
              Continue to App
            </Button>
          )}
        </CardFooter>
      </Card>
    </div>
  );
}
