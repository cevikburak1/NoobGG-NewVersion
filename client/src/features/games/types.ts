export interface GameResponse {
  id: string;
  rawgId: number;
  slug: string;
  name: string;
  description: string | null;
  backgroundImageUrl: string | null;
  releasedAt: string | null;
  rating: number | null;
  metacritic: number | null;
  genres: string[];
  tags: string[];
  platforms: string[];
  isMultiplayer: boolean;
  isCoop: boolean;
  isPvp: boolean;
  isFreeToPlay: boolean;
}

export interface GameSearchParams {
  q: string;
  limit?: number;
  multiplayer?: boolean;
  coop?: boolean;
  genre?: string;
}

export interface GameBrowseParams {
  search?: string;
  genre?: string;
  multiplayer?: boolean;
  freeToPlay?: boolean;
  page?: number;
  pageSize?: number;
}
