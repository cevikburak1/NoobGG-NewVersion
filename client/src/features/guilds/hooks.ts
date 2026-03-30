import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import * as guildsApi from '@/features/guilds/api';
import { queryKeys } from '@/lib/queryKeys';
import type { GuildFilters } from '@/types/api';

export function useGuilds(filters: GuildFilters) {
  return useQuery({
    queryKey: queryKeys.guilds.list(filters),
    queryFn: () => guildsApi.getGuilds(filters),
    refetchInterval: 60_000,
  });
}

export function useGuildDetail(id: string | undefined) {
  return useQuery({
    queryKey: queryKeys.guilds.detail(id ?? ''),
    queryFn: () => guildsApi.getGuildDetail(id!),
    enabled: Boolean(id),
  });
}

export function useCreateGuild() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: guildsApi.createGuild,
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.guilds.all() });
    },
  });
}

export function useJoinGuild() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: guildsApi.joinGuild,
    onSuccess: (_, guildId) => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.guilds.detail(guildId) });
      void queryClient.invalidateQueries({ queryKey: queryKeys.guilds.all() });
    },
  });
}

export function useLeaveGuild() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: guildsApi.leaveGuild,
    onSuccess: (_, guildId) => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.guilds.detail(guildId) });
      void queryClient.invalidateQueries({ queryKey: queryKeys.guilds.all() });
    },
  });
}

export function useKickGuildMember() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ guildId, userId }: { guildId: string; userId: string }) =>
      guildsApi.kickGuildMember(guildId, userId),
    onSuccess: (_, { guildId }) => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.guilds.detail(guildId) });
    },
  });
}

export function useUpdateGuildMemberRole() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ guildId, userId, newRole }: { guildId: string; userId: string; newRole: string }) =>
      guildsApi.updateGuildMemberRole(guildId, userId, newRole),
    onSuccess: (_, { guildId }) => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.guilds.detail(guildId) });
    },
  });
}

export function useInviteToGuild() {
  return useMutation({
    mutationFn: ({ guildId, userId }: { guildId: string; userId: string }) =>
      guildsApi.inviteToGuild(guildId, userId),
  });
}

export function usePendingGuildInvites() {
  return useQuery({
    queryKey: queryKeys.guilds.invites(),
    queryFn: guildsApi.getPendingGuildInvites,
  });
}

export function useAcceptGuildInvite() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: guildsApi.acceptGuildInvite,
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.guilds.invites() });
      void queryClient.invalidateQueries({ queryKey: queryKeys.guilds.all() });
      void queryClient.invalidateQueries({ queryKey: queryKeys.notifications.list() });
    },
  });
}

export function useDeclineGuildInvite() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: guildsApi.declineGuildInvite,
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.guilds.invites() });
      void queryClient.invalidateQueries({ queryKey: queryKeys.notifications.list() });
    },
  });
}
