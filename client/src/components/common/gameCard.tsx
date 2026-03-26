import type { GameResponse } from '@/features/games/types';
import { Badge } from '@/components/ui';
import { cn } from '@/lib/cn';

interface GameCardProps {
  game: GameResponse;
  onClick?: () => void;
  className?: string;
}

export function GameCard({ game, onClick, className }: GameCardProps) {
  return (
    <div
      onClick={onClick}
      className={cn(
        'group overflow-hidden rounded-lg border border-border bg-surface transition-all',
        'hover:border-border-hover hover:shadow-lg hover:shadow-primary/5',
        onClick && 'cursor-pointer',
        className,
      )}
    >
      {game.backgroundImageUrl && (
        <div className="aspect-video overflow-hidden">
          <img
            src={game.backgroundImageUrl}
            alt={game.name}
            className="h-full w-full object-cover transition-transform group-hover:scale-105"
          />
        </div>
      )}
      <div className="p-3">
        <h3 className="font-semibold text-foreground">{game.name}</h3>
        <div className="mt-2 flex flex-wrap gap-1">
          {game.genres.slice(0, 3).map((genre) => (
            <Badge key={genre} variant="default">{genre}</Badge>
          ))}
          {game.isMultiplayer && <Badge variant="primary">Multiplayer</Badge>}
          {game.isFreeToPlay && <Badge variant="accent">F2P</Badge>}
        </div>
      </div>
    </div>
  );
}
