import { type ReactNode, useState } from 'react';
import { Navigate, useLocation } from 'react-router-dom';
import { useAuthStore } from '@/stores/authStore';
import { Spinner, Button, AnimatedPage, Card } from '@/components/ui';
import { api } from '@/lib/api';
import { useToast } from '@/components/ui/toast';

interface ProtectedRouteProps {
  children: ReactNode;
}

export function ProtectedRoute({ children }: ProtectedRouteProps) {
  const isAuth = useAuthStore((s) => s.isAuthenticated());
  const isInitialized = useAuthStore((s) => s.isInitialized);
  const isDeactivated = useAuthStore((s) => s.isDeactivated);
  const location = useLocation();

  if (!isInitialized) {
    return (
      <div className="flex h-64 items-center justify-center">
        <Spinner size="lg" />
      </div>
    );
  }

  if (!isAuth) {
    return <Navigate to="/login" state={{ from: location }} replace />;
  }

  if (isDeactivated && location.pathname !== '/settings') {
    return <DeactivatedScreen />;
  }

  return <>{children}</>;
}

function DeactivatedScreen() {
  const [isLoading, setIsLoading] = useState(false);
  const { addToast } = useToast();
  const logout = useAuthStore((s) => s.logout);

  const handleReactivate = async () => {
    setIsLoading(true);
    try {
      await api.post('/api/settings/reactivate');
      useAuthStore.setState({ isDeactivated: false });
      addToast({ title: 'Welcome back!', message: 'Your account has been reactivated.', type: 'success' });
    } catch {
      addToast({ title: 'Error', message: 'Failed to reactivate account.', type: 'error' });
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <AnimatedPage className="flex items-center justify-center min-h-[60vh]">
      <Card className="max-w-md w-full text-center p-8">
        <div className="text-5xl mb-4">⏸️</div>
        <h2 className="text-xl font-bold text-foreground">Account Deactivated</h2>
        <p className="mt-2 text-foreground-muted">
          Your account is currently deactivated. You can reactivate it to regain full access.
        </p>
        <div className="mt-6 flex flex-col gap-3">
          <Button onClick={handleReactivate} isLoading={isLoading}>
            Reactivate Account
          </Button>
          <Button variant="ghost" onClick={logout}>
            Sign Out
          </Button>
        </div>
      </Card>
    </AnimatedPage>
  );
}
