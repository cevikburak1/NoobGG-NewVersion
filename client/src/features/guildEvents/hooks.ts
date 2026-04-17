import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { queryKeys } from '@/lib/queryKeys';
import { getGuildEvents, createGuildEvent, deleteGuildEvent } from './api';
import type { CreateGuildEventPayload } from './types';

export function useGuildEvents(guildId: string | undefined, from?: string, to?: string) {
  return useQuery({
    queryKey: queryKeys.guildEvents.list(guildId ?? '', from, to),
    queryFn: () => getGuildEvents(guildId!, from, to),
    enabled: Boolean(guildId),
  });
}

export function useCreateGuildEvent() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (payload: CreateGuildEventPayload) => createGuildEvent(payload),
    onSuccess: (_data, variables) => {
      qc.invalidateQueries({ queryKey: ['guildEvents', variables.guildId] });
    },
  });
}

export function useDeleteGuildEvent() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (eventId: string) => deleteGuildEvent(eventId),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['guildEvents'] });
    },
  });
}
