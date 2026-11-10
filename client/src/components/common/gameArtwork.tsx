import { useState } from 'react';
import { Icon } from '@/components/ui/icon';

const palette = ['#9cc764', '#aa87d2', '#6caac4', '#d09a6f', '#699c9a'];

/** Keep catalogue cards useful when a cover is absent, a demo placeholder, or unavailable. */
export function GameArtwork({ src, name = 'Game', className = '' }: { src?: string | null; name?: string; className?: string }) {
  const [failedSrc, setFailedSrc] = useState<string | null>(null);
  const color = palette[[...name].reduce((n, c) => n + c.charCodeAt(0), 0) % palette.length];
  const imageAvailable = src && !src.includes('placehold.co') && src !== failedSrc;
  return <div role="img" aria-label={name} className={`relative isolate overflow-hidden ${className}`} style={{ background: `radial-gradient(ellipse at 70% 15%, ${color}55, transparent 65%), linear-gradient(135deg, #222a34, #11141d)` }}>
    <svg aria-hidden="true" viewBox="0 0 400 240" preserveAspectRatio="xMidYMid slice" className="absolute inset-0 h-full w-full" fill="none">
      <path d="m0 200 130-160 130 200M140 240 285 10 430 220M-40 120 110-30 310 170l-140 140" stroke={color} strokeWidth=".6" opacity=".45" />
      <circle cx="275" cy="85" r="70" stroke={color} strokeWidth="1" opacity=".35" /><circle cx="275" cy="85" r="52" stroke={color} strokeWidth=".5" opacity=".4" />
      <path d="m245 55 60 60m-60 0 60-60" stroke={color} strokeWidth="2" opacity=".55" />
      <path d="M0 215h400M0 200h400M0 185h400" stroke={color} opacity=".08" />
    </svg>
    <span aria-hidden="true" className="absolute left-[10%] top-[14%] font-display text-4xl font-extrabold tracking-tighter opacity-25" style={{ color }}>{name.split(/\s+/).slice(0, 2).map(w => w[0]).join('')}</span>
    <Icon name="game" className="absolute bottom-[15%] right-[12%] h-9 w-9 opacity-20" style={{ color }} />
    {imageAvailable && <img src={src} alt="" loading="lazy" onError={() => setFailedSrc(src)} className="absolute inset-0 h-full w-full object-cover" />}
  </div>;
}
