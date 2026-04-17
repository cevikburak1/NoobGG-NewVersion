export type CommunityBoardType = 'General' | 'Game';

export interface CommunityPostResponse {
  id: string;
  slug: string;
  title: string;
  authorId: string;
  authorUsername: string;
  authorAvatarUrl: string | null;
  boardType: CommunityBoardType;
  category: string;
  gameId: string | null;
  gameName: string | null;
  gameSlug: string | null;
  gameBackgroundImageUrl: string | null;
  content: string;
  imageUrl: string | null;
  upvoteCount: number;
  commentCount: number;
  hasUpvoted: boolean;
  isPinned: boolean;
  isLocked: boolean;
  lastActivityAt: string;
  createdAt: string;
}

export interface CommunityCommentResponse {
  id: string;
  authorId: string;
  authorUsername: string;
  authorAvatarUrl: string | null;
  content: string;
  upvoteCount: number;
  hasUpvoted: boolean;
  createdAt: string;
}

export interface CommunityFeedResponse {
  posts: CommunityPostResponse[];
  totalCount: number;
  hasMore: boolean;
}

export interface CommunityCommentsResponse {
  comments: CommunityCommentResponse[];
  totalCount: number;
  hasMore: boolean;
  page: number;
  pageSize: number;
}

export interface CommunityTopicListResponse {
  topics: CommunityPostResponse[];
  totalCount: number;
  page: number;
  pageSize: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

export interface CommunityBoardResponse {
  id: string;
  slug: string;
  title: string;
  description: string;
  boardType: CommunityBoardType;
  gameId: string | null;
  gameName: string | null;
  gameSlug: string | null;
  coverImageUrl: string | null;
  topicCount: number;
  lastActivityAt: string | null;
  accent: string;
}

export interface CommunityBoardsOverviewResponse {
  boards: CommunityBoardResponse[];
  trendingTopics: CommunityPostResponse[];
  latestTopics: CommunityPostResponse[];
}

export interface CommunityTopicDetailResponse {
  topic: CommunityPostResponse;
  relatedTopics: CommunityPostResponse[];
}

export interface CreatePostPayload {
  gameId?: string;
  boardType?: CommunityBoardType;
  category?: string;
  title?: string;
  content: string;
  imageUrl?: string;
}

export interface AddCommentPayload {
  postId: string;
  content: string;
}

export type ContentVoteTargetType = 'CommunityPost' | 'Guide' | 'CommunityComment';

export interface ToggleVotePayload {
  targetId: string;
  targetType: number;
}
