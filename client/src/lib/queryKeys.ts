import type { ReportFilters, RoomFilters } from '@/types/api';
import type { GameBrowseParams } from '@/features/games/types';
import type { PlayerDiscoverParams } from '@/features/users/types';

export const queryKeys = {
  auth: {
    me: () => ['auth', 'me'] as const,
  },
  rooms: {
    all: () => ['rooms'] as const,
    list: (filters: RoomFilters) => ['rooms', 'list', filters] as const,
    detail: (id: string) => ['rooms', 'detail', id] as const,
    invites: () => ['rooms', 'invites'] as const,
  },
  chat: {
    messages: (roomId: string) => ['chat', 'messages', roomId] as const,
  },
  games: {
    search: (query: string) => ['games', 'search', query] as const,
    browse: (params: GameBrowseParams) => ['games', 'browse', params] as const,
    detail: (id: string) => ['games', 'detail', id] as const,
  },
  users: {
    discover: (params: PlayerDiscoverParams) => ['users', 'discover', params] as const,
  },
  profile: {
    me: () => ['profile', 'me'] as const,
    detail: (userId: string) => ['profile', userId] as const,
    gameProfiles: (userId: string) => ['profile', userId, 'games'] as const,
  },
  subscriptions: {
    plans: () => ['subscriptions', 'plans'] as const,
    me: () => ['subscriptions', 'me'] as const,
  },
  moderation: {
    reports: (filters: ReportFilters) => ['moderation', 'reports', filters] as const,
    reportDetail: (id: string) => ['moderation', 'report', id] as const,
  },
  dm: {
    conversations: () => ['dm', 'conversations'] as const,
    messages: (conversationId: string) => ['dm', 'messages', conversationId] as const,
    unreadCount: () => ['dm', 'unread'] as const,
  },
  blocks: {
    list: () => ['blocks'] as const,
  },
  friends: {
    list: () => ['friends'] as const,
    requests: () => ['friends', 'requests'] as const,
  },
  notifications: {
    list: (params?: Record<string, unknown>) => ['notifications', 'list', params] as const,
    unreadCount: () => ['notifications', 'unread'] as const,
  },
  settings: {
    me: () => ['settings', 'me'] as const,
  },
  favorites: {
    list: () => ['favorites'] as const,
  },
  recommendations: {
    players: (gameId?: string) => ['recommendations', 'players', gameId] as const,
    rooms: (gameId?: string) => ['recommendations', 'rooms', gameId] as const,
  },
} as const;
