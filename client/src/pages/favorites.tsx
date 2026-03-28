import { Link, useNavigate } from 'react-router-dom';
import { motion } from 'framer-motion';
import { useMyFavorites, useToggleFavorite } from '@/features/favorites/hooks';
import { Button, AnimatedPage, Spinner, staggerContainer, staggerItem } from '@/components/ui';
import { UserAvatar } from '@/components/common/userAvatar';
import { useToast } from '@/components/ui/toast';
import type { FavoritePlayerResponse } from '@/features/favorites/types';

export default function FavoritesPage() {
  const { data: favorites, isLoading } = useMyFavorites();
  const navigate = useNavigate();
  const { addToast } = useToast();

  return (
    <AnimatedPage>
      <div className="space-y-6">
        <div>
          <h1 className="text-2xl font-bold text-foreground">Favorites</h1>
          <p className="mt-1 text-sm text-foreground-muted">Players you've bookmarked</p>
        </div>

        {isLoading ? (
          <div className="flex justify-center py-20">
            <Spinner size="lg" />
          </div>
        ) : favorites && favorites.length > 0 ? (
          <motion.div
            variants={staggerContainer}
            initial="hidden"
            animate="show"
            className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3"
          >
            {favorites.map((fav) => (
              <motion.div key={fav.userId} variants={staggerItem}>
                <FavoriteCard
                  favorite={fav}
                  onMessage={() => navigate(`/messages?user=${fav.userId}`)}
                  addToast={addToast}
                />
              </motion.div>
            ))}
          </motion.div>
        ) : (
          <motion.div
            initial={{ opacity: 0, scale: 0.95 }}
            animate={{ opacity: 1, scale: 1 }}
            className="flex flex-col items-center py-20 text-center"
          >
            <motion.div
              animate={{ y: [0, -8, 0] }}
              transition={{ duration: 2, repeat: Infinity }}
              className="text-5xl"
            >
              ⭐
            </motion.div>
            <h3 className="mt-4 text-lg font-bold text-foreground">No favorites yet</h3>
            <p className="mt-1.5 text-sm text-foreground-muted">
              Visit player profiles and tap the star to add them here
            </p>
            <Link to="/discover" className="mt-4 inline-block">
              <Button variant="outline">Discover Players</Button>
            </Link>
          </motion.div>
        )}
      </div>
    </AnimatedPage>
  );
}

function FavoriteCard({
  favorite,
  onMessage,
  addToast,
}: {
  favorite: FavoritePlayerResponse;
  onMessage: () => void;
  addToast: (t: { title: string; type: 'success' | 'error' | 'info' }) => void;
}) {
  const { remove, isLoading } = useToggleFavorite(favorite.userId);

  const handleRemove = async () => {
    try {
      await remove.mutateAsync();
      addToast({ title: `${favorite.username} removed from favorites`, type: 'info' });
    } catch {
      addToast({ title: 'Could not remove favorite', type: 'error' });
    }
  };

  return (
    <div className="group flex items-center gap-4 rounded-xl border border-border bg-surface p-4 transition-colors hover:border-primary/30">
      <Link to={`/profile/${favorite.userId}`} className="relative">
        <UserAvatar username={favorite.username} avatarUrl={favorite.avatarUrl} size="md" />
        {favorite.isOnline && (
          <div className="absolute -bottom-0.5 -right-0.5 h-3 w-3 rounded-full border-2 border-surface bg-green-500" />
        )}
      </Link>
      <div className="min-w-0 flex-1">
        <Link to={`/profile/${favorite.userId}`} className="block">
          <h3 className="truncate font-semibold text-foreground hover:text-primary transition-colors">
            {favorite.username}
          </h3>
        </Link>
        <p className="text-xs text-foreground-muted">
          {favorite.isOnline ? (
            <span className="text-green-500 font-medium">Online</span>
          ) : (
            `Added ${new Date(favorite.favoritedAt).toLocaleDateString()}`
          )}
        </p>
      </div>
      <div className="flex gap-2">
        <Button variant="outline" size="sm" onClick={onMessage}>
          Message
        </Button>
        <Button
          variant="ghost"
          size="sm"
          onClick={handleRemove}
          isLoading={isLoading}
          className="text-foreground-muted hover:text-danger"
          title="Remove from favorites"
        >
          <svg className="h-4 w-4" fill="currentColor" stroke="currentColor" viewBox="0 0 24 24" strokeWidth={0.5}>
            <path strokeLinecap="round" strokeLinejoin="round" d="M11.48 3.499a.562.562 0 011.04 0l2.125 5.111a.563.563 0 00.475.345l5.518.442c.499.04.701.663.321.988l-4.204 3.602a.563.563 0 00-.182.557l1.285 5.385a.562.562 0 01-.84.61l-4.725-2.885a.562.562 0 00-.586 0L6.982 20.54a.562.562 0 01-.84-.61l1.285-5.386a.562.562 0 00-.182-.557l-4.204-3.602a.562.562 0 01.321-.988l5.518-.442a.563.563 0 00.475-.345L11.48 3.5z" />
          </svg>
        </Button>
      </div>
    </div>
  );
}
