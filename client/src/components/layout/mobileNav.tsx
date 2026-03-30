import { NavLink } from 'react-router-dom';
import { cn } from '@/lib/cn';

const tabs = [
  { to: '/rooms', label: 'Rooms' },
  { to: '/guilds', label: 'Guilds' },
  { to: '/discover', label: 'Discover' },
  { to: '/leaderboard', label: 'Ranks' },
  { to: '/settings', label: 'Settings' },
];

export function MobileNav() {
  return (
    <nav className="fixed bottom-0 left-0 right-0 z-40 border-t border-border bg-surface/95 backdrop-blur-md lg:hidden">
      <div className="flex items-center justify-around py-2">
        {tabs.map((tab) => (
          <NavLink
            key={tab.to}
            to={tab.to}
            className={({ isActive }) =>
              cn(
                'flex flex-col items-center gap-0.5 px-3 py-1 text-xs font-medium transition-colors',
                isActive ? 'text-primary' : 'text-foreground-subtle',
              )
            }
          >
            {tab.label}
          </NavLink>
        ))}
      </div>
    </nav>
  );
}
