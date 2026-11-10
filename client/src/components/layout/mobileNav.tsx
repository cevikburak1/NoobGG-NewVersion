import { useEffect, useRef, useState } from 'react';
import { NavLink } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useAuthStore } from '@/stores/authStore';
import { Icon, type IconName } from '@/components/ui/icon';
import { cn } from '@/lib/cn';

const primary: { to: string; label: string; icon: IconName }[] = [
  { to: '/rooms', label: 'Rooms', icon: 'rooms' },
  { to: '/discover', label: 'Discover', icon: 'compass' },
  { to: '/messages', label: 'Messages', icon: 'chat' },
  { to: '/guilds', label: 'Guilds', icon: 'people' },
];
const destinations = [
  { to: '/community', label: 'Forum' }, { to: '/friends', label: 'Friends' },
  { to: '/favorites', label: 'Favorites' }, { to: '/notifications', label: 'Notifications' },
  { to: '/compare', label: 'Compare' }, { to: '/leaderboard', label: 'Leaderboard' },
  { to: '/tournaments', label: 'Tournaments' }, { to: '/subscriptions', label: 'Plans' },
  { to: '/settings', label: 'Settings' },
];
export function MobileNav() {
  const { t } = useTranslation();
  const [open, setOpen] = useState(false);
  const moreRef = useRef<HTMLButtonElement>(null);
  const sheetRef = useRef<HTMLDivElement>(null);
  const role = useAuthStore(s => s.user?.role);
  useEffect(() => {
    if (!open) return;
    const trigger = moreRef.current;
    const previousOverflow = document.body.style.overflow;
    document.body.style.overflow = 'hidden';
    sheetRef.current?.querySelector<HTMLElement>('button, a')?.focus();
    const onKey = (event: KeyboardEvent) => {
      if (event.key === 'Escape') setOpen(false);
      if (event.key === 'Tab') {
        const items = sheetRef.current?.querySelectorAll<HTMLElement>('button, a');
        if (!items?.length) return;
        const first = items[0], last = items[items.length - 1];
        if (event.shiftKey && document.activeElement === first) { event.preventDefault(); last.focus(); }
        else if (!event.shiftKey && document.activeElement === last) { event.preventDefault(); first.focus(); }
      }
    };
    document.addEventListener('keydown', onKey);
    return () => { document.body.style.overflow = previousOverflow; document.removeEventListener('keydown', onKey); trigger?.focus(); };
  }, [open]);
  return <>
    {open && <div className="fixed inset-0 z-50 bg-black/70 backdrop-blur-sm lg:hidden" onClick={() => setOpen(false)}>
      <div ref={sheetRef} role="dialog" aria-modal="true" aria-label={t('More destinations')} onClick={e => e.stopPropagation()} className="absolute bottom-0 left-0 right-0 max-h-[85dvh] overflow-y-auto rounded-t-3xl border border-border bg-surface p-5 pb-[max(1.5rem,env(safe-area-inset-bottom))]">
        <div className="mb-5 flex items-center justify-between"><h2 className="text-xl font-bold">{t('Your gaming space')}</h2><button aria-label={t('Close menu')} onClick={() => setOpen(false)} className="flex h-11 w-11 items-center justify-center rounded-xl bg-surface-hover"><Icon name="close" /></button></div>
        <div className="grid grid-cols-2 gap-2">
          {[...destinations, ...(role === 'Admin' || role === 'Moderator' ? [{ to: '/moderation', label: 'Moderation' }] : [])].map(item => <NavLink key={item.to} to={item.to} onClick={() => setOpen(false)} className={({ isActive }) => cn('flex min-h-12 items-center justify-between rounded-xl border px-4 text-sm', isActive ? 'border-primary/30 bg-primary/10 text-primary' : 'border-border text-foreground-muted')}>
            {t(item.label)} <span aria-hidden="true">↗︎</span>
          </NavLink>)}
        </div>
      </div>
    </div>}
    <nav aria-label={t('Mobile navigation')} className="fixed bottom-0 left-0 right-0 z-40 border-t border-border bg-background/95 pb-[env(safe-area-inset-bottom)] backdrop-blur-xl lg:hidden">
      <div className="grid grid-cols-5 px-2 py-2">
        {primary.map(tab => <NavLink key={tab.to} to={tab.to} className={({ isActive }) => cn('flex min-h-12 flex-col items-center justify-center gap-1 rounded-xl text-[9px] font-semibold', isActive ? 'bg-primary/10 text-primary' : 'text-foreground-subtle')}><Icon name={tab.icon} className="h-5 w-5" />{t(tab.label)}</NavLink>)}
        <button ref={moreRef} aria-expanded={open} aria-haspopup="dialog" onClick={() => setOpen(true)} className="flex min-h-12 flex-col items-center justify-center gap-1 rounded-xl text-[9px] font-semibold text-foreground-subtle"><Icon name="more" />{t('More')}</button>
      </div>
    </nav>
  </>;
}
