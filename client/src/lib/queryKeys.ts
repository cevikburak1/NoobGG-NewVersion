import type { GuildFilters, ReportFilters, RoomFilters } from '@/types/api';
import type { GameBrowseParams } from '@/features/games/types';
import type { PlayerDiscoverParams } from '@/features/users/types';

export const queryKeys = {
  community: {
    boards: (params?: unknown) => ['community', 'boards', params] as const,
    topics: (board: string, sort = 'latest', page = 1, pageSize = 12) =>
      ['community', 'topics', board, sort, page, pageSize] as const,
    topicDetail: (topicId: string) => ['community', 'topic', topicId] as const,
    feed: (gameId: string, page = 1) => ['community', 'feed', gameId, page] as const,
    comments: (postId: string, page = 1, pageSize = 20) =>
      ['community', 'comments', postId, page, pageSize] as const,
  },
  guides: {
    list: (gameId?: string, sortBy?: string, page = 1) => ['guides', 'list', gameId, sortBy, page] as const,
    detail: (id: string) => ['guides', 'detail', id] as const,
  },
  guildAnalytics: {
    stats: (guildId: string, gameId?: string, days = 30) => ['guildAnalytics', 'stats', guildId, gameId, days] as const,
  },
  guildEvents: {
    list: (guildId: string, from?: string, to?: string) => ['guildEvents', guildId, from, to] as const,
  },
  tournaments: {
    list: (params: Record<string, unknown>) => ['tournaments', 'list', params] as const,
    detail: (id: string) => ['tournaments', 'detail', id] as const,
  },
  auth: {
    me: () => ['auth', 'me'] as const,
  },
  rooms: {
    all: () => ['rooms'] as const,
    list: (filters: RoomFilters) => ['rooms', 'list', filters] as const,
    detail: (id: string) => ['rooms', 'detail', id] as const,
    invites: () => ['rooms', 'invites'] as const,
  },
  guilds: {
    all: () => ['guilds'] as const,
    list: (filters: GuildFilters) => ['guilds', 'list', filters] as const,
    detail: (id: string) => ['guilds', 'detail', id] as const,
    invites: () => ['guilds', 'invites'] as const,
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
    recentActivity: () => ['users', 'recentActivity'] as const,
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
    players: (limit: number) => ['recommendations', 'players', limit] as const,
    playersAi: (limit: number) => ['recommendations', 'players', 'ai', limit] as const,
    rooms: (limit: number) => ['recommendations', 'rooms', limit] as const,
  },
  recent: {
    players: (limit: number) => ['recent', 'players', limit] as const,
    rooms: (limit: number) => ['recent', 'rooms', limit] as const,
  },
  elo: {
    leaderboard: (gameId: string, page: number) => ['elo', 'leaderboard', gameId, page] as const,
    history: (userId: string, gameId: string) => ['elo', 'history', userId, gameId] as const,
  },
  matchmaking: {
    queueStatus: () => ['matchmaking', 'queueStatus'] as const,
  },
} as const;
