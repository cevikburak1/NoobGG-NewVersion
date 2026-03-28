import { useState } from 'react';
import { Link } from 'react-router-dom';
import { motion, AnimatePresence } from 'framer-motion';
import { useNotifications, useMarkRead, useMarkAllRead } from '@/features/notifications/hooks';
import { useAcceptInvite, useDeclineInvite } from '@/features/rooms/hooks';
import { Button, Badge } from '@/components/ui';
import type { NotificationResponse, NotificationType } from '@/features/notifications/types';
import { useToast } from '@/components/ui/toast';

const FILTER_OPTIONS = [
  { label: 'All', value: undefined },
  { label: 'Unread', value: true },
] as const;

const TYPE_META: Record<NotificationType, { icon: JSX.Element; color: string }> = {
  FriendRequest: { icon: <PersonAddIcon />, color: 'text-blue-400' },
  FriendAccepted: { icon: <HandshakeIcon />, color: 'text-green-400' },
  RoomInvite: { icon: <DoorIcon />, color: 'text-purple-400' },
  RoomJoined: { icon: <CheckCircleIcon />, color: 'text-green-400' },
  RoomLeft: { icon: <ExitIcon />, color: 'text-amber-400' },
  RoomClosed: { icon: <LockIcon />, color: 'text-red-400' },
  DirectMessage: { icon: <ChatIcon />, color: 'text-sky-400' },
  ReportResolved: { icon: <ShieldCheckIcon />, color: 'text-emerald-400' },
  SubscriptionChanged: { icon: <StarIcon />, color: 'text-yellow-400' },
  SystemMessage: { icon: <MegaphoneIcon />, color: 'text-foreground-muted' },
};

export default function NotificationsPage() {
  const [unreadOnly, setUnreadOnly] = useState<boolean | undefined>(undefined);
  const [page, setPage] = useState(1);
  const pageSize = 20;

  const { data, isLoading, isError } = useNotifications({ unreadOnly, page, pageSize });
  const markRead = useMarkRead();
  const markAllRead = useMarkAllRead();
  const acceptInvite = useAcceptInvite();
  const declineInvite = useDeclineInvite();
  const { addToast } = useToast();

  const handleMarkRead = (id: string) => {
    markRead.mutate(id);
  };

  const handleMarkAllRead = () => {
    markAllRead.mutate();
  };

  const handleFilterChange = (value: boolean | undefined) => {
    setUnreadOnly(value);
    setPage(1);
  };

  return (
    <div className="mx-auto max-w-3xl space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-foreground">Notifications</h1>
          <p className="mt-1 text-sm text-foreground-muted">
            Stay updated on activity across NoobGg
          </p>
        </div>
        <Button
          variant="ghost"
          size="sm"
          onClick={handleMarkAllRead}
          disabled={markAllRead.isPending || !data?.items.some((n) => !n.isRead)}
        >
          Mark all read
        </Button>
      </div>

      <div className="flex gap-2">
        {FILTER_OPTIONS.map((opt) => (
          <button
            key={String(opt.value)}
            onClick={() => handleFilterChange(opt.value)}
            className={`rounded-lg px-3 py-1.5 text-sm font-medium transition-colors ${
              unreadOnly === opt.value
                ? 'bg-primary text-white'
                : 'bg-surface-hover text-foreground-muted hover:text-foreground'
            }`}
          >
            {opt.label}
          </button>
        ))}
      </div>

      {isLoading && <LoadingSkeleton />}

      {isError && (
        <div className="rounded-xl border border-danger/20 bg-danger/5 p-8 text-center">
          <p className="text-danger">Failed to load notifications. Please try again.</p>
        </div>
      )}

      {!isLoading && !isError && data && data.items.length === 0 && (
        <EmptyState unreadOnly={unreadOnly} />
      )}

      {!isLoading && !isError && data && data.items.length > 0 && (
        <>
          <div className="space-y-2">
            <AnimatePresence mode="popLayout">
              {data.items.map((notification) => (
                <NotificationItem
                  key={notification.id}
                  notification={notification}
                  onMarkRead={handleMarkRead}
                  isPending={markRead.isPending}
                  onAcceptInvite={async (inviteId) => {
                    try {
                      await acceptInvite.mutateAsync(inviteId);
                      handleMarkRead(notification.id);
                      addToast({ title: 'Invite accepted!', type: 'success' });
                    } catch {
                      addToast({ title: 'Could not accept invite', type: 'error' });
                    }
                  }}
                  onDeclineInvite={async (inviteId) => {
                    try {
                      await declineInvite.mutateAsync(inviteId);
                      handleMarkRead(notification.id);
                      addToast({ title: 'Invite declined', type: 'info' });
                    } catch {
                      addToast({ title: 'Could not decline invite', type: 'error' });
                    }
                  }}
                  inviteActionPending={acceptInvite.isPending || declineInvite.isPending}
                />
              ))}
            </AnimatePresence>
          </div>

          {(data.hasPreviousPage || data.hasNextPage) && (
            <div className="flex items-center justify-between pt-2">
              <Button
                variant="ghost"
                size="sm"
                disabled={!data.hasPreviousPage}
                onClick={() => setPage((p) => p - 1)}
              >
                Previous
              </Button>
              <span className="text-sm text-foreground-muted">
                Page {data.page} of {Math.ceil(data.totalCount / pageSize)}
              </span>
              <Button
                variant="ghost"
                size="sm"
                disabled={!data.hasNextPage}
                onClick={() => setPage((p) => p + 1)}
              >
                Next
              </Button>
            </div>
          )}
        </>
      )}
    </div>
  );
}

function NotificationItem({
  notification,
  onMarkRead,
  isPending,
  onAcceptInvite,
  onDeclineInvite,
  inviteActionPending,
}: {
  notification: NotificationResponse;
  onMarkRead: (id: string) => void;
  isPending: boolean;
  onAcceptInvite: (inviteId: string) => void;
  onDeclineInvite: (inviteId: string) => void;
  inviteActionPending: boolean;
}) {
  const meta = TYPE_META[notification.type] ?? TYPE_META.SystemMessage;
  const linkTarget = getNotificationLink(notification);
  const isRoomInvite = notification.type === 'RoomInvite' && !notification.isRead && notification.data?.inviteId;

  const content = (
    <motion.div
      layout
      initial={{ opacity: 0, y: 8 }}
      animate={{ opacity: 1, y: 0 }}
      exit={{ opacity: 0, x: -20 }}
      transition={{ duration: 0.2 }}
      className={`group flex items-start gap-4 rounded-xl border p-4 transition-colors ${
        notification.isRead
          ? 'border-border bg-surface'
          : 'border-primary/20 bg-primary/5'
      }`}
    >
      <div className={`mt-0.5 shrink-0 ${meta.color}`}>{meta.icon}</div>

      <div className="min-w-0 flex-1">
        <div className="flex items-start justify-between gap-2">
          <p className={`text-sm font-medium ${notification.isRead ? 'text-foreground-muted' : 'text-foreground'}`}>
            {notification.title}
          </p>
          {!notification.isRead && (
            <span className="mt-1 h-2 w-2 shrink-0 rounded-full bg-primary" />
          )}
        </div>
        <p className="mt-0.5 text-sm text-foreground-muted line-clamp-2">{notification.body}</p>

        {isRoomInvite && (
          <div className="mt-2 flex gap-2">
            <Button
              size="sm"
              onClick={(e) => {
                e.preventDefault();
                e.stopPropagation();
                onAcceptInvite(notification.data!.inviteId);
              }}
              isLoading={inviteActionPending}
              className="gap-1.5"
            >
              <svg className="h-3.5 w-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth={2}>
                <path strokeLinecap="round" strokeLinejoin="round" d="M4.5 12.75l6 6 9-13.5" />
              </svg>
              Accept
            </Button>
            <Button
              size="sm"
              variant="ghost"
              onClick={(e) => {
                e.preventDefault();
                e.stopPropagation();
                onDeclineInvite(notification.data!.inviteId);
              }}
              isLoading={inviteActionPending}
            >
              Decline
            </Button>
          </div>
        )}

        <div className="mt-2 flex items-center gap-3">
          <time className="text-xs text-foreground-muted/60">
            {formatRelativeTime(notification.createdAt)}
          </time>
          {!notification.isRead && (
            <button
              onClick={(e) => {
                e.preventDefault();
                e.stopPropagation();
                onMarkRead(notification.id);
              }}
              disabled={isPending}
              className="text-xs font-medium text-primary hover:text-primary/80 transition-colors"
            >
              Mark read
            </button>
          )}
        </div>
      </div>
    </motion.div>
  );

  if (linkTarget) {
    return <Link to={linkTarget}>{content}</Link>;
  }

  return content;
}

function getNotificationLink(notification: NotificationResponse): string | null {
  const data = notification.data;
  if (!data) return null;

  switch (notification.type) {
    case 'RoomInvite':
    case 'RoomJoined':
    case 'RoomLeft':
    case 'RoomClosed':
      return data.roomId ? `/rooms/${data.roomId}` : null;
    case 'DirectMessage':
      return '/messages';
    case 'FriendRequest':
    case 'FriendAccepted':
      return data.userId ? `/profile/${data.userId}` : null;
    case 'SubscriptionChanged':
      return '/subscriptions';
    default:
      return null;
  }
}

function formatRelativeTime(dateStr: string): string {
  const now = Date.now();
  const date = new Date(dateStr).getTime();
  const diffMs = now - date;
  const diffSec = Math.floor(diffMs / 1000);

  if (diffSec < 60) return 'Just now';
  const diffMin = Math.floor(diffSec / 60);
  if (diffMin < 60) return `${diffMin}m ago`;
  const diffHr = Math.floor(diffMin / 60);
  if (diffHr < 24) return `${diffHr}h ago`;
  const diffDay = Math.floor(diffHr / 24);
  if (diffDay < 7) return `${diffDay}d ago`;
  return new Date(dateStr).toLocaleDateString();
}

function EmptyState({ unreadOnly }: { unreadOnly: boolean | undefined }) {
  return (
    <div className="flex flex-col items-center justify-center rounded-xl border border-border bg-surface py-16">
      <div className="mb-4 flex h-16 w-16 items-center justify-center rounded-full bg-surface-hover">
        <BellOffIcon />
      </div>
      <p className="text-lg font-semibold text-foreground">
        {unreadOnly ? 'All caught up!' : 'No notifications yet'}
      </p>
      <p className="mt-1 text-sm text-foreground-muted">
        {unreadOnly
          ? "You've read all your notifications."
          : "When you get notifications, they'll appear here."}
      </p>
    </div>
  );
}

function LoadingSkeleton() {
  return (
    <div className="space-y-2">
      {Array.from({ length: 5 }).map((_, i) => (
        <div key={i} className="flex items-start gap-4 rounded-xl border border-border bg-surface p-4">
          <div className="h-5 w-5 animate-pulse rounded bg-surface-hover" />
          <div className="flex-1 space-y-2">
            <div className="h-4 w-3/4 animate-pulse rounded bg-surface-hover" />
            <div className="h-3 w-1/2 animate-pulse rounded bg-surface-hover" />
          </div>
        </div>
      ))}
    </div>
  );
}

function PersonAddIcon() {
  return (
    <svg className="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth={1.5}>
      <path strokeLinecap="round" strokeLinejoin="round" d="M19 7.5v3m0 0v3m0-3h3m-3 0h-3m-2.25-4.125a3.375 3.375 0 11-6.75 0 3.375 3.375 0 016.75 0zM4 19.235v-.11a6.375 6.375 0 0112.75 0v.109A12.318 12.318 0 0110.374 21c-2.331 0-4.512-.645-6.374-1.766z" />
    </svg>
  );
}

function HandshakeIcon() {
  return (
    <svg className="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth={1.5}>
      <path strokeLinecap="round" strokeLinejoin="round" d="M15 19.128a9.38 9.38 0 002.625.372 9.337 9.337 0 004.121-.952 4.125 4.125 0 00-7.533-2.493M15 19.128v-.003c0-1.113-.285-2.16-.786-3.07M15 19.128v.106A12.318 12.318 0 018.624 21c-2.331 0-4.512-.645-6.374-1.766l-.001-.109a6.375 6.375 0 0111.964-3.07M12 6.375a3.375 3.375 0 11-6.75 0 3.375 3.375 0 016.75 0zm8.25 2.25a2.625 2.625 0 11-5.25 0 2.625 2.625 0 015.25 0z" />
    </svg>
  );
}

function DoorIcon() {
  return (
    <svg className="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth={1.5}>
      <path strokeLinecap="round" strokeLinejoin="round" d="M15.75 9V5.25A2.25 2.25 0 0013.5 3h-6a2.25 2.25 0 00-2.25 2.25v13.5A2.25 2.25 0 007.5 21h6a2.25 2.25 0 002.25-2.25V15M12 9l-3 3m0 0l3 3m-3-3h12.75" />
    </svg>
  );
}

function CheckCircleIcon() {
  return (
    <svg className="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth={1.5}>
      <path strokeLinecap="round" strokeLinejoin="round" d="M9 12.75L11.25 15 15 9.75M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
    </svg>
  );
}

function ExitIcon() {
  return (
    <svg className="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth={1.5}>
      <path strokeLinecap="round" strokeLinejoin="round" d="M15.75 9V5.25A2.25 2.25 0 0013.5 3h-6a2.25 2.25 0 00-2.25 2.25v13.5A2.25 2.25 0 007.5 21h6a2.25 2.25 0 002.25-2.25V15m3 0l3-3m0 0l-3-3m3 3H9" />
    </svg>
  );
}

function LockIcon() {
  return (
    <svg className="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth={1.5}>
      <path strokeLinecap="round" strokeLinejoin="round" d="M16.5 10.5V6.75a4.5 4.5 0 10-9 0v3.75m-.75 11.25h10.5a2.25 2.25 0 002.25-2.25v-6.75a2.25 2.25 0 00-2.25-2.25H6.75a2.25 2.25 0 00-2.25 2.25v6.75a2.25 2.25 0 002.25 2.25z" />
    </svg>
  );
}

function ChatIcon() {
  return (
    <svg className="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth={1.5}>
      <path strokeLinecap="round" strokeLinejoin="round" d="M8.625 12a.375.375 0 11-.75 0 .375.375 0 01.75 0zm0 0H8.25m4.125 0a.375.375 0 11-.75 0 .375.375 0 01.75 0zm0 0H12m4.125 0a.375.375 0 11-.75 0 .375.375 0 01.75 0zm0 0h-.375M21 12c0 4.556-4.03 8.25-9 8.25a9.764 9.764 0 01-2.555-.337A5.972 5.972 0 015.41 20.97a5.969 5.969 0 01-.474-.065 4.48 4.48 0 00.978-2.025c.09-.457-.133-.901-.467-1.226C3.93 16.178 3 14.189 3 12c0-4.556 4.03-8.25 9-8.25s9 3.694 9 8.25z" />
    </svg>
  );
}

function ShieldCheckIcon() {
  return (
    <svg className="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth={1.5}>
      <path strokeLinecap="round" strokeLinejoin="round" d="M9 12.75L11.25 15 15 9.75m-3-7.036A11.959 11.959 0 013.598 6 11.99 11.99 0 003 9.749c0 5.592 3.824 10.29 9 11.623 5.176-1.332 9-6.03 9-11.622 0-1.31-.21-2.571-.598-3.751h-.152c-3.196 0-6.1-1.248-8.25-3.285z" />
    </svg>
  );
}

function StarIcon() {
  return (
    <svg className="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth={1.5}>
      <path strokeLinecap="round" strokeLinejoin="round" d="M11.48 3.499a.562.562 0 011.04 0l2.125 5.111a.563.563 0 00.475.345l5.518.442c.499.04.701.663.321.988l-4.204 3.602a.563.563 0 00-.182.557l1.285 5.385a.562.562 0 01-.84.61l-4.725-2.885a.563.563 0 00-.586 0L6.982 20.54a.562.562 0 01-.84-.61l1.285-5.386a.562.562 0 00-.182-.557l-4.204-3.602a.563.563 0 01.321-.988l5.518-.442a.563.563 0 00.475-.345L11.48 3.5z" />
    </svg>
  );
}

function MegaphoneIcon() {
  return (
    <svg className="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth={1.5}>
      <path strokeLinecap="round" strokeLinejoin="round" d="M10.34 15.84c-.688-.06-1.386-.09-2.09-.09H7.5a4.5 4.5 0 110-9h.75c.704 0 1.402-.03 2.09-.09m0 9.18c.253.962.584 1.892.985 2.783.247.55.06 1.21-.463 1.511l-.657.38c-.551.318-1.26.117-1.527-.461a20.845 20.845 0 01-1.44-4.282m3.102.069a18.03 18.03 0 01-.59-4.59c0-1.586.205-3.124.59-4.59m0 9.18a23.848 23.848 0 018.835 2.535M10.34 6.66a23.847 23.847 0 008.835-2.535m0 0A23.74 23.74 0 0018.795 3m.38 1.125a23.91 23.91 0 011.014 5.395m-1.014 8.855c-.118.38-.245.754-.38 1.125m.38-1.125a23.91 23.91 0 001.014-5.395m0-3.46c.495.413.811 1.035.811 1.73 0 .695-.316 1.317-.811 1.73m0-3.46a24.347 24.347 0 010 3.46" />
    </svg>
  );
}

function BellOffIcon() {
  return (
    <svg className="h-8 w-8 text-foreground-muted" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth={1.5}>
      <path strokeLinecap="round" strokeLinejoin="round" d="M9.143 17.082a24.248 24.248 0 003.714 0m-7.071-2.57a8.39 8.39 0 01-.9-3.762 6.75 6.75 0 0113.5 0 8.39 8.39 0 01-.9 3.762m-11.7 0a24.319 24.319 0 005.85.774c2.027 0 3.991-.238 5.85-.774m-11.7 0l-.244.292a2.25 2.25 0 001.714 3.696h8.46a2.25 2.25 0 001.714-3.696l-.244-.292" />
    </svg>
  );
}
