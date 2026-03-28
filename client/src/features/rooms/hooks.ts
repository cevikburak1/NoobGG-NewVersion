import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import * as roomsApi from '@/features/rooms/api';
import { queryKeys } from '@/lib/queryKeys';
import type { RoomFilters } from '@/types/api';

export function useRooms(filters: RoomFilters) {
  return useQuery({
    queryKey: queryKeys.rooms.list(filters),
    queryFn: () => roomsApi.getRooms(filters),
    refetchInterval: 30_000,
  });
}

export function useRoomDetail(id: string | undefined) {
  return useQuery({
    queryKey: queryKeys.rooms.detail(id ?? ''),
    queryFn: () => roomsApi.getRoomDetail(id!),
    enabled: Boolean(id),
  });
}

export function useCreateRoom() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: roomsApi.createRoom,
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.rooms.all() });
    },
  });
}

export function useJoinRoom() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: roomsApi.joinRoom,
    onSuccess: (_, roomId) => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.rooms.detail(roomId) });
      void queryClient.invalidateQueries({ queryKey: queryKeys.chat.messages(roomId) });
    },
  });
}

export function useLeaveRoom() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: roomsApi.leaveRoom,
    onSuccess: (_, roomId) => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.rooms.detail(roomId) });
    },
  });
}

export function useCloseRoom() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: roomsApi.closeRoom,
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.rooms.all() });
    },
  });
}

export function useInviteToRoom() {
  return useMutation({
    mutationFn: ({ roomId, userId }: { roomId: string; userId: string }) =>
      roomsApi.inviteToRoom(roomId, userId),
  });
}

export function usePendingInvites() {
  return useQuery({
    queryKey: queryKeys.rooms.invites(),
    queryFn: roomsApi.getPendingInvites,
  });
}

export function useAcceptInvite() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: roomsApi.acceptInvite,
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.rooms.invites() });
      void queryClient.invalidateQueries({ queryKey: queryKeys.rooms.all() });
      void queryClient.invalidateQueries({ queryKey: queryKeys.notifications.list() });
    },
  });
}

export function useDeclineInvite() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: roomsApi.declineInvite,
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.rooms.invites() });
      void queryClient.invalidateQueries({ queryKey: queryKeys.notifications.list() });
    },
  });
}
