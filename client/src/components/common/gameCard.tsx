import { Link } from 'react-router-dom';
import { motion } from 'framer-motion';
import type { GameResponse } from '@/features/games/types';
import { Badge } from '@/components/ui';
import { cn } from '@/lib/cn';

interface GameCardProps {
  game: GameResponse;
  onClick?: () => void;
  className?: string;
}

export function GameCard({ game, onClick, className }: GameCardProps) {
  const content = (
    <motion.div
      whileHover={{ y: -4, scale: 1.02 }}
      transition={{ duration: 0.2 }}
      className={cn(
        'group relative overflow-hidden rounded-xl border border-border bg-surface transition-all',
        'hover:border-primary/30 hover:shadow-xl hover:shadow-primary/10',
        className,
      )}
    >
      <div className="relative aspect-video overflow-hidden bg-surface-hover">
        {game.backgroundImageUrl ? (
          <img
            src={game.backgroundImageUrl}
            alt={game.name}
            className="h-full w-full object-cover transition-transform duration-500 group-hover:scale-105"
            loading="lazy"
          />
        ) : (
          <div className="flex h-full w-full items-center justify-center text-4xl text-foreground-subtle">
            🎮
          </div>
        )}
        <div className="absolute inset-0 bg-linear-to-t from-surface via-transparent to-transparent" />
        {game.metacritic != null && (
          <span className={cn(
            'absolute right-2.5 top-2.5 rounded-md px-1.5 py-0.5 text-xs font-bold shadow-sm',
            game.metacritic >= 75 ? 'bg-success/90 text-white' :
            game.metacritic >= 50 ? 'bg-warning/90 text-white' :
            'bg-danger/90 text-white',
          )}>
            {game.metacritic}
          </span>
        )}
      </div>
      <div className="p-4">
        <h3 className="truncate font-semibold text-foreground group-hover:text-primary transition-colors">
          {game.name}
        </h3>
        <div className="mt-2.5 flex flex-wrap gap-1.5">
          {game.genres.slice(0, 3).map((genre) => (
            <Badge key={genre} variant="default">{genre}</Badge>
          ))}
          {game.isMultiplayer && <Badge variant="primary">Multiplayer</Badge>}
          {game.isFreeToPlay && <Badge variant="accent">F2P</Badge>}
        </div>
      </div>
    </motion.div>
  );

  if (onClick) {
    return <div onClick={onClick} className="cursor-pointer">{content}</div>;
  }

  return <Link to={`/games/${game.id}`}>{content}</Link>;
}
