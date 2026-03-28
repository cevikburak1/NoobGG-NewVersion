export interface FavoritePlayerResponse {
  userId: string;
  username: string;
  avatarUrl: string | null;
  isOnline: boolean;
  favoritedAt: string;
}
