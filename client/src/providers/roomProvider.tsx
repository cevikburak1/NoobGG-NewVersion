import { useEffect, useRef, type ReactNode } from 'react';
import type { HubConnection } from '@microsoft/signalr';
import { useQueryClient } from '@tanstack/react-query';
import { createRoomConnection, startConnection, stopConnection } from '@/lib/signalr';
import { useAuthStore } from '@/stores/authStore';
import { queryKeys } from '@/lib/queryKeys';

export function RoomProvider({ children }: { children: ReactNode }) {
  const connectionRef = useRef<HubConnection | null>(null);
  const qc = useQueryClient();
  const accessToken = useAuthStore((s) => s.accessToken);

  useEffect(() => {
    if (!accessToken) {
      if (connectionRef.current) {
        stopConnection(connectionRef.current);
        connectionRef.current = null;
      }
      return;
    }

    const conn = createRoomConnection(() => useAuthStore.getState().accessToken);
    connectionRef.current = conn;

    conn.on('roomListUpdated', () => {
      void qc.invalidateQueries({ queryKey: queryKeys.rooms.all() });
    });

    conn.onreconnecting(() => {});
    conn.onreconnected(() => {});
    conn.onclose(() => {});

    startConnection(conn).catch(() => {});

    return () => {
      stopConnection(conn);
      connectionRef.current = null;
    };
  }, [accessToken, qc]);

  return <>{children}</>;
}
