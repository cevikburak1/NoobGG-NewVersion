import { Outlet, Link } from 'react-router-dom';

export function AuthLayout() {
  return (
    <div className="flex min-h-screen items-center justify-center bg-background p-4">
      <div className="absolute inset-0 bg-[radial-gradient(ellipse_at_top,_var(--color-primary)_0%,_transparent_50%)] opacity-10" />
      <div className="relative w-full max-w-md">
        <div className="mb-8 text-center">
          <Link to="/" className="inline-flex items-center gap-1 text-2xl font-bold">
            <span className="text-primary">Noob</span>
            <span className="text-accent">Gg</span>
          </Link>
          <p className="mt-2 text-sm text-foreground-muted">Find your teammates, level up together</p>
        </div>
        <div className="rounded-xl border border-border bg-surface p-6 shadow-lg">
          <Outlet />
        </div>
      </div>
    </div>
  );
}
