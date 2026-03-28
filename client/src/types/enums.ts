export type UserRole = 'User' | 'Premium' | 'Moderator' | 'Admin';

export type Region = 'EU' | 'NA' | 'SA' | 'AS' | 'OCE' | 'ME' | 'AF' | 'TR' | 'CIS' | 'SEA';

export type Language =
  | 'English' | 'Turkish' | 'German' | 'French' | 'Spanish'
  | 'Portuguese' | 'Russian' | 'Arabic' | 'Chinese' | 'Japanese' | 'Korean';

export type RoomStatus = 'Open' | 'Full' | 'InProgress' | 'Closed';

export type RoomMemberRole = 'Owner' | 'Member';

export type MessageType = 'Text' | 'System';

export type FriendshipStatus = 'Pending' | 'Accepted' | 'Rejected';

export type PresenceStatus = 'Online' | 'Away' | 'Offline';

export type ReportReason = 'Harassment' | 'Spam' | 'Cheating' | 'Inappropriate' | 'Other';

export type ReportStatus = 'Pending' | 'Reviewed' | 'Resolved' | 'Dismissed';

export type ReportTargetType = 'User' | 'Room';

export type SubscriptionTier = 'Free' | 'Plus' | 'Pro';

export type SubscriptionStatus = 'Active' | 'Cancelled' | 'Expired' | 'PastDue';

export type NotificationType =
  | 'FriendRequest' | 'FriendAccepted' | 'RoomInvite'
  | 'ReportResolved' | 'SubscriptionChanged' | 'SystemMessage';

export type ExperienceLevel = 'Beginner' | 'Intermediate' | 'Advanced' | 'Expert';

export type CommunicationPreference = 'Text' | 'Voice' | 'Both' | 'None';

export type ProfileVisibility = 'Public' | 'FriendsOnly' | 'Private';

export type DmPermission = 'Everyone' | 'FriendsOnly' | 'Nobody';
