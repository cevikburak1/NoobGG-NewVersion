import type { ReactNode } from 'react';
import { Navigate } from 'react-router-dom';
import { useAuthStore } from '@/stores/authStore';
import { ProtectedRoute } from './protectedRoute';

interface RequireRoleProps {
  roles: string[];
  children: ReactNode;
}

export function RequireRole({ roles, children }: RequireRoleProps) {
  return (
    <ProtectedRoute>
      <RoleCheck roles={roles}>{children}</RoleCheck>
    </ProtectedRoute>
  );
}

function RoleCheck({ roles, children }: RequireRoleProps) {
  const userRole = useAuthStore((s) => s.user?.role);

  if (!userRole || !roles.includes(userRole)) {
    return <Navigate to="/" replace />;
  }

  return <>{children}</>;
}
