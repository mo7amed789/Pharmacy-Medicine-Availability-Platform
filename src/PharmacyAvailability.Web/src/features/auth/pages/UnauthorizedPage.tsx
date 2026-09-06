import { Link } from 'react-router-dom';
import { ShieldX, Home } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card, CardHeader, CardTitle, CardDescription, CardContent, CardFooter } from '@/components/ui/card';
import { useAuthStore } from '@/app/store/authStore';

export function UnauthorizedPage() {
  const { user } = useAuthStore();

  return (
    <div className="flex min-h-screen items-center justify-center bg-slate-50 px-4 py-12">
      <Card className="w-full max-w-md text-center shadow-lg border-slate-200">
        <CardHeader className="space-y-2">
          <div className="mx-auto flex h-14 w-14 items-center justify-center rounded-full bg-red-100 text-red-600">
            <ShieldX className="h-8 w-8" />
          </div>
          <CardTitle className="text-2xl font-bold text-slate-900">Access Denied (403)</CardTitle>
          <CardDescription className="text-slate-500">
            You do not have the required role or permissions to access this area.
          </CardDescription>
        </CardHeader>

        <CardContent>
          <p className="text-xs text-slate-600">
            Logged in as <span className="font-semibold">{user?.fullName || 'Anonymous'}</span> ({user?.role || 'Guest'}).
          </p>
        </CardContent>

        <CardFooter className="flex justify-center gap-3">
          <Button variant="outline" asChild size="sm">
            <Link to="/" className="flex items-center gap-1.5">
              <Home className="h-4 w-4" />
              Go to Homepage
            </Link>
          </Button>
          {user?.role === 'PharmacyStaff' || user?.role === 'PharmacyAdmin' ? (
            <Button asChild size="sm">
              <Link to="/pharmacy/dashboard">Staff Dashboard</Link>
            </Button>
          ) : user?.role === 'PlatformAdmin' ? (
            <Button asChild size="sm">
              <Link to="/admin/dashboard">Admin Portal</Link>
            </Button>
          ) : (
            <Button asChild size="sm">
              <Link to="/customer/search">Find Medicine</Link>
            </Button>
          )}
        </CardFooter>
      </Card>
    </div>
  );
}
