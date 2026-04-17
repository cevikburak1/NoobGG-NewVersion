export interface JoinMatchQueueRequest {
  gameId: string;
  region?: string;
  language?: string;
}

export interface JoinMatchQueueResponse {
  status: string;
  matchedRoomId: string | null;
  fallbackReady: boolean;
}

export interface GetMatchQueueStatusResponse {
  status: string;
  matchedRoomId: string | null;
  fallbackReady: boolean;
  gameId: string | null;
  secondsInQueue: number | null;
}
