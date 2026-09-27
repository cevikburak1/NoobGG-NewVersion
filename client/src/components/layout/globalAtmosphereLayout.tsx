import { Outlet, useLocation } from 'react-router-dom';
import { ArenaBackdrop } from '@/components/scene/arenaBackdrop';

export function GlobalAtmosphereLayout() {
  const { pathname } = useLocation();
  return (
    <div className="relative isolate min-h-screen">
      <ArenaBackdrop pathname={pathname} />
      <div className="relative z-10 min-h-screen"><Outlet /></div>
    </div>
  );
}
