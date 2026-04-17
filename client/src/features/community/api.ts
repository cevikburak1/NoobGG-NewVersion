import { api } from '@/lib/api';
import type {
  CommunityBoardsOverviewResponse,
  CommunityFeedResponse,
  CommunityPostResponse,
  CommunityCommentResponse,
  CommunityCommentsResponse,
  CommunityTopicDetailResponse,
  CommunityTopicListResponse,
  CreatePostPayload,
  AddCommentPayload,
  ToggleVotePayload,
} from './types';

export async function getCommunityFeed(
  gameId: string,
  page = 1,
  pageSize = 20,
): Promise<CommunityFeedResponse> {
  const { data } = await api.get<CommunityFeedResponse>(
    `/api/community/feed/${gameId}`,
    { params: { page, pageSize } },
  );
  return data;
}

export async function createCommunityPost(
  payload: CreatePostPayload,
): Promise<CommunityPostResponse> {
  const { data } = await api.post<CommunityPostResponse>('/api/community/posts', payload);
  return data;
}

export async function createCommunityTopic(
  payload: CreatePostPayload,
): Promise<CommunityPostResponse> {
  const { data } = await api.post<CommunityPostResponse>('/api/community/topics', payload);
  return data;
}

export async function getCommunityBoardsOverview(): Promise<CommunityBoardsOverviewResponse> {
  const { data } = await api.get<CommunityBoardsOverviewResponse>('/api/community/boards');
  return data;
}

export async function getCommunityTopics(
  board: string,
  sort = 'latest',
  page = 1,
  pageSize = 12,
): Promise<CommunityTopicListResponse> {
  const { data } = await api.get<CommunityTopicListResponse>('/api/community/topics', {
    params: { board, sort, page, pageSize },
  });
  return data;
}

export async function getCommunityTopicDetail(topicId: string): Promise<CommunityTopicDetailResponse> {
  const { data } = await api.get<CommunityTopicDetailResponse>(`/api/community/topics/${topicId}`);
  return data;
}

export async function getPostComments(
  postId: string,
  page = 1,
  pageSize = 20,
): Promise<CommunityCommentsResponse> {
  const { data } = await api.get<CommunityCommentsResponse>(`/api/community/posts/${postId}/comments`, {
    params: { page, pageSize },
  });
  return data;
}

export async function getTopicComments(
  topicId: string,
  page = 1,
  pageSize = 20,
): Promise<CommunityCommentsResponse> {
  const { data } = await api.get<CommunityCommentsResponse>(`/api/community/topics/${topicId}/comments`, {
    params: { page, pageSize },
  });
  return data;
}

export async function addComment(payload: AddCommentPayload): Promise<CommunityCommentResponse> {
  const { data } = await api.post<CommunityCommentResponse>(
    `/api/community/posts/${payload.postId}/comments`,
    { content: payload.content },
  );
  return data;
}

export async function addTopicComment(payload: AddCommentPayload): Promise<CommunityCommentResponse> {
  const { data } = await api.post<CommunityCommentResponse>(
    `/api/community/topics/${payload.postId}/comments`,
    { content: payload.content },
  );
  return data;
}

export async function toggleVote(payload: ToggleVotePayload): Promise<boolean> {
  const { data } = await api.post<boolean>('/api/community/votes', payload);
  return data;
}
