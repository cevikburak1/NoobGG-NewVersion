import { api } from '@/lib/api';
import type { ChatMessageResponse } from '@/features/chat/types';
import type { PagedResult } from '@/types/api';

export interface GetChatHistoryParams {
  page?: number;
  pageSize?: number;
  before?: string;
}

export async function getChatHistory(
  roomId: string,
  params?: GetChatHistoryParams,
): Promise<PagedResult<ChatMessageResponse>> {
  const { data } = await api.get<PagedResult<ChatMessageResponse>>(
    `/api/chat/${roomId}/messages`,
    { params },
  );
  return data;
}

export async function deleteMessage(roomId: string, messageId: string): Promise<void> {
  await api.delete(`/api/chat/${roomId}/messages/${messageId}`);
}

export async function editMessage(
  roomId: string,
  messageId: string,
  content: string,
): Promise<void> {
  await api.put(`/api/chat/${roomId}/messages/${messageId}`, { content });
}
