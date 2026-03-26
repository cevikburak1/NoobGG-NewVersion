import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { queryKeys } from '@/lib/queryKeys';
import {
  createConversation,
  getConversations,
  getMessages,
  markConversationRead,
  sendMessage,
} from './api';
import type { CreateConversationRequest, SendDirectMessageRequest } from './types';

export function useConversations(enabled = true) {
  return useQuery({
    queryKey: queryKeys.dm.conversations(),
    queryFn: getConversations,
    enabled,
  });
}

export function useMessages(conversationId: string | null) {
  return useQuery({
    queryKey: conversationId
      ? queryKeys.dm.messages(conversationId)
      : (['dm', 'messages', 'none'] as const),
    queryFn: () => getMessages(conversationId!),
    enabled: Boolean(conversationId),
  });
}

export function useCreateConversation() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: CreateConversationRequest) => createConversation(data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: queryKeys.dm.conversations() });
    },
  });
}

export function useSendMessage() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({
      conversationId,
      ...body
    }: SendDirectMessageRequest & { conversationId: string }) =>
      sendMessage(conversationId, body),
    onSuccess: (_, vars) => {
      qc.invalidateQueries({ queryKey: queryKeys.dm.messages(vars.conversationId) });
      qc.invalidateQueries({ queryKey: queryKeys.dm.conversations() });
    },
  });
}

export function useMarkRead() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (conversationId: string) => markConversationRead(conversationId),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: queryKeys.dm.conversations() });
    },
  });
}
