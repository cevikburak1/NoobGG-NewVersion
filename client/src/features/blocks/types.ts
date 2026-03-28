export interface BlockedUserResponse {
  blockId: string;
  userId: string;
  username: string;
  avatarUrl: string | null;
  blockedAt: string;
}
