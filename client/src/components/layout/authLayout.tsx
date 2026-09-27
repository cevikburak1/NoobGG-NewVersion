import { Outlet, Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { LanguageToggle } from './languageToggle';

export function AuthLayout() {
  const { t } = useTranslation();
  return (
    <div className="flex min-h-screen items-center justify-center p-4">
      <div className="absolute right-4 top-4"><LanguageToggle /></div>
      <div className="relative w-full max-w-md">
        <div className="mb-8 text-center">
          <Link to="/" className="inline-flex items-center gap-1 text-2xl font-bold">
            <span className="text-primary">Noob</span>
            <span className="text-accent">Gg</span>
          </Link>
          <p className="mt-2 text-sm text-foreground-muted">{t('Find your teammates, level up together')}</p>
        </div>
        <div className="rounded-xl border border-border bg-surface p-6 shadow-lg">
          <Outlet />
        </div>
      </div>
    </div>
  );
}
