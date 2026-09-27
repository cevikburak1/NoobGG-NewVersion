import { NavLink } from 'react-router-dom';
import { cn } from '@/lib/cn';
import { useTranslation } from 'react-i18next';

const tabs = [
  { to: '/rooms', label: 'Rooms' },
  { to: '/guilds', label: 'Guilds' },
  { to: '/discover', label: 'Discover' },
  { to: '/community', label: 'Forum' },
  { to: '/compare', label: 'Compare' },
  { to: '/leaderboard', label: 'Ranks' },
  { to: '/tournaments', label: 'Tourneys' },
  { to: '/settings', label: 'Settings' },
];

export function MobileNav() {
  const { t } = useTranslation();
  return (
    <nav className="fixed bottom-0 left-0 right-0 z-40 border-t border-border bg-surface/95 backdrop-blur-md lg:hidden">
      <div className="flex items-center gap-1 overflow-x-auto py-2">
        {tabs.map((tab) => (
          <NavLink
            key={tab.to}
            to={tab.to}
            className={({ isActive }) =>
              cn(
                'shrink-0 px-3 py-1 text-xs font-medium transition-colors',
                isActive ? 'text-primary' : 'text-foreground-subtle',
              )
            }
          >
            {t(tab.label)}
          </NavLink>
        ))}
      </div>
    </nav>
  );
}
