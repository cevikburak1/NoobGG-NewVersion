import { createContext, useContext, useEffect, useRef, useState, useCallback, type ReactNode } from 'react';
import type { HubConnection } from '@microsoft/signalr';
import { useQueryClient } from '@tanstack/react-query';
import { createNotificationConnection, startConnection, stopConnection, type ConnectionStatus } from '@/lib/signalr';
import { useAuthStore } from '@/stores/authStore';
import { useToast } from '@/components/ui/toast';
import { queryKeys } from '@/lib/queryKeys';
import type { NotificationResponse } from '@/features/notifications/types';

interface NotificationContextValue {
  unreadCount: number;
  refreshUnreadCount: () => void;
}

const NotificationContext = createContext<NotificationContextValue>({ unreadCount: 0, refreshUnreadCount: () => {} });

export function useNotificationContext() {
  return useContext(NotificationContext);
}

const TYPE_ICONS: Record<string, string> = {
  FriendRequest: '👋',
  FriendAccepted: '🤝',
  RoomInvite: '🚪',
  RoomJoined: '✅',
  RoomLeft: '👋',
  RoomClosed: '🔒',
  DirectMessage: '💬',
  ReportResolved: '✔️',
  SubscriptionChanged: '⭐',
  SystemMessage: '📢',
};

export function NotificationProvider({ children }: { children: ReactNode }) {
  const connectionRef = useRef<HubConnection | null>(null);
  const qc = useQueryClient();
  const accessToken = useAuthStore((s) => s.accessToken);
  const { addToast } = useToast();
  const [unreadCount, setUnreadCount] = useState(0);

  const refreshUnreadCount = useCallback(async () => {
    try {
      const { data } = await (await import('@/lib/api')).api.get<number>('/api/notifications/unread-count');
      setUnreadCount(data);
      qc.setQueryData(queryKeys.notifications.unreadCount(), data);
    } catch {
      /* ignore */
    }
  }, [qc]);

  useEffect(() => {
    if (!accessToken) {
      if (connectionRef.current) {
        stopConnection(connectionRef.current);
        connectionRef.current = null;
      }
      setUnreadCount(0);
      return;
    }

    const conn = createNotificationConnection(() => useAuthStore.getState().accessToken);
    connectionRef.current = conn;

    conn.on('ReceiveNotification', (notification: NotificationResponse) => {
      qc.invalidateQueries({ queryKey: ['notifications'] });

      const icon = TYPE_ICONS[notification.type] ?? '🔔';
      addToast({
        title: `${icon} ${notification.title}`,
        message: notification.body.length > 80 ? notification.body.slice(0, 80) + '...' : notification.body,
        type: 'info',
        duration: 5000,
      });
    });

    conn.on('UnreadCountChanged', (count: number) => {
      setUnreadCount(count);
      qc.setQueryData(queryKeys.notifications.unreadCount(), count);
    });

    conn.onreconnecting(() => {});
    conn.onreconnected(() => {});
    conn.onclose(() => {});

    startConnection(conn).catch(() => {});

    return () => {
      stopConnection(conn);
      connectionRef.current = null;
    };
  }, [accessToken, qc, addToast]);

  return (
    <NotificationContext.Provider value={{ unreadCount, refreshUnreadCount }}>
      {children}
    </NotificationContext.Provider>
  );
}
