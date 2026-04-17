export interface GuideListItemResponse {
  id: string;
  title: string;
  authorId: string;
  authorUsername: string;
  authorAvatarUrl: string | null;
  gameId: string;
  coverImageUrl: string | null;
  tags: string[];
  upvoteCount: number;
  viewCount: number;
  hasUpvoted: boolean;
  createdAt: string;
}

export interface GuideDetailResponse {
  id: string;
  title: string;
  content: string;
  authorId: string;
  authorUsername: string;
  authorAvatarUrl: string | null;
  gameId: string;
  coverImageUrl: string | null;
  tags: string[];
  status: string;
  upvoteCount: number;
  viewCount: number;
  hasUpvoted: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface GuideListResponse {
  guides: GuideListItemResponse[];
  totalCount: number;
  hasMore: boolean;
}

export interface CreateGuidePayload {
  gameId: string;
  title: string;
  content: string;
  coverImageUrl?: string;
  tags?: string[];
}
