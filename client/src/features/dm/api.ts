import { api } from '@/lib/api';
import type {
  ConversationResponse,
  CreateConversationRequest,
  DirectMessageResponse,
  SendDirectMessageRequest,
} from './types';

export async function getConversations(): Promise<ConversationResponse[]> {
  const { data } = await api.get<ConversationResponse[]>('/api/dm/conversations');
  return data;
}

export async function createConversation(
  body: CreateConversationRequest,
): Promise<ConversationResponse> {
  const { data } = await api.post<ConversationResponse>('/api/dm/conversations', body);
  return data;
}

export async function getMessages(
  conversationId: string,
  page = 1,
  pageSize = 50,
): Promise<DirectMessageResponse[]> {
  const { data } = await api.get<DirectMessageResponse[]>(
    `/api/dm/conversations/${conversationId}/messages`,
    { params: { page, pageSize } },
  );
  return data;
}

export async function sendMessage(
  conversationId: string,
  body: SendDirectMessageRequest,
): Promise<DirectMessageResponse> {
  const { data } = await api.post<DirectMessageResponse>(
    `/api/dm/conversations/${conversationId}/messages`,
    body,
  );
  return data;
}

export async function markConversationRead(conversationId: string): Promise<void> {
  await api.post(`/api/dm/conversations/${conversationId}/read`);
}
