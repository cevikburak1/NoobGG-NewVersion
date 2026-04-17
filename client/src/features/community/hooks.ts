import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { queryKeys } from '@/lib/queryKeys';
import {
  addTopicComment,
  getCommunityFeed,
  getCommunityBoardsOverview,
  getCommunityTopicDetail,
  getCommunityTopics,
  createCommunityPost,
  createCommunityTopic,
  getPostComments,
  getTopicComments,
  addComment,
  toggleVote,
} from './api';
import type { CreatePostPayload, AddCommentPayload, ToggleVotePayload } from './types';

export function useCommunityFeed(gameId: string | undefined, page = 1) {
  return useQuery({
    queryKey: queryKeys.community.feed(gameId ?? '', page),
    queryFn: () => getCommunityFeed(gameId!, page),
    enabled: Boolean(gameId),
  });
}

export function useCommunityBoardsOverview() {
  return useQuery({
    queryKey: queryKeys.community.boards(),
    queryFn: getCommunityBoardsOverview,
  });
}

export function useCommunityTopics(board: string | undefined, sort = 'latest', page = 1, pageSize = 12) {
  return useQuery({
    queryKey: queryKeys.community.topics(board ?? 'general', sort, page, pageSize),
    queryFn: () => getCommunityTopics(board ?? 'general', sort, page, pageSize),
    enabled: Boolean(board),
  });
}

export function useCommunityTopicDetail(topicId: string | undefined) {
  return useQuery({
    queryKey: queryKeys.community.topicDetail(topicId ?? ''),
    queryFn: () => getCommunityTopicDetail(topicId!),
    enabled: Boolean(topicId),
  });
}

export function useCreatePost() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (payload: CreatePostPayload) => createCommunityPost(payload),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['community'] });
    },
  });
}

export function useCreateTopic() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (payload: CreatePostPayload) => createCommunityTopic(payload),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['community'] });
    },
  });
}

export function usePostComments(postId: string | undefined, page = 1, pageSize = 20) {
  return useQuery({
    queryKey: queryKeys.community.comments(postId ?? '', page, pageSize),
    queryFn: () => getPostComments(postId!, page, pageSize),
    enabled: Boolean(postId),
  });
}

export function useTopicComments(topicId: string | undefined, page = 1, pageSize = 20) {
  return useQuery({
    queryKey: queryKeys.community.comments(topicId ?? '', page, pageSize),
    queryFn: () => getTopicComments(topicId!, page, pageSize),
    enabled: Boolean(topicId),
  });
}

export function useAddComment() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (payload: AddCommentPayload) => addComment(payload),
    onSuccess: (_data, variables) => {
      qc.invalidateQueries({ queryKey: queryKeys.community.comments(variables.postId) });
      qc.invalidateQueries({ queryKey: ['community'] });
    },
  });
}

export function useAddTopicComment() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (payload: AddCommentPayload) => addTopicComment(payload),
    onSuccess: (_data, variables) => {
      qc.invalidateQueries({ queryKey: queryKeys.community.comments(variables.postId) });
      qc.invalidateQueries({ queryKey: ['community'] });
    },
  });
}

export function useToggleVote() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (payload: ToggleVotePayload) => toggleVote(payload),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['community'] });
      qc.invalidateQueries({ queryKey: ['guides'] });
    },
  });
}
