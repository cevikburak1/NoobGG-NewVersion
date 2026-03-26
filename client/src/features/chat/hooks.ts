import { HubConnectionState, type HubConnection } from '@microsoft/signalr';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { useCallback, useEffect, useRef, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import * as chatApi from '@/features/chat/api';
import type {
  ChatMessageResponse,
  ChatPresenceEvent,
  MessageDeletedEvent,
  MessageEditedEvent,
  OnlineUser,
  RoomClosedEvent,
  RoomMemberEvent,
  RoomPresenceResponse,
  TypingEvent,
} from '@/features/chat/types';
import { queryKeys } from '@/lib/queryKeys';
import {
  createChatConnection,
  getConnectionStatus,
  startConnection,
  stopConnection,
  type ConnectionStatus,
} from '@/lib/signalr';
import { useAuthStore } from '@/stores/authStore';
import type { GetChatHistoryParams } from '@/features/chat/api';

/* ─── History ─── */

export function useChatHistory(roomId: string, params?: GetChatHistoryParams) {
  return useQuery({
    queryKey: queryKeys.chat.messages(roomId),
    queryFn: () => chatApi.getChatHistory(roomId, params),
    enabled: Boolean(roomId),
  });
}

/* ─── Typing helpers ─── */

export interface TypingUser {
  userId: string;
  username: string;
}

const TYPING_STALE_MS = 5_000;

/* ─── Core connection hook ─── */

export function useChatConnection(roomId: string | undefined) {
  const connectionRef = useRef<HubConnection | null>(null);
  const [messages, setMessages] = useState<ChatMessageResponse[]>([]);
  const [onlineUsers, setOnlineUsers] = useState<OnlineUser[]>([]);
  const [typingUsers, setTypingUsers] = useState<Map<string, { username: string; at: number }>>(() => new Map());
  const [status, setStatus] = useState<ConnectionStatus>('disconnected');
  const [reconnectAttempt, setReconnectAttempt] = useState(0);
  const messageIdsRef = useRef<Set<string>>(new Set());
  const queryClient = useQueryClient();
  const navigate = useNavigate();

  const addMessage = useCallback((msg: ChatMessageResponse) => {
    if (messageIdsRef.current.has(msg.id)) return;
    messageIdsRef.current.add(msg.id);
    setMessages((prev) => [...prev, msg]);
  }, []);

  useEffect(() => {
    if (!roomId) return;

    const connection = createChatConnection(() => useAuthStore.getState().accessToken);
    connectionRef.current = connection;

    connection.on('receiveMessage', (message: ChatMessageResponse) => {
      addMessage(message);
    });

    connection.on('messageDeleted', (event: MessageDeletedEvent) => {
      if (event.roomId !== roomId) return;
      messageIdsRef.current.delete(event.messageId);
      setMessages((prev) => prev.filter((m) => m.id !== event.messageId));
    });

    connection.on('messageEdited', (event: MessageEditedEvent) => {
      if (event.roomId !== roomId) return;
      setMessages((prev) =>
        prev.map((m) =>
          m.id === event.messageId
            ? { ...m, content: event.content, isEdited: true, editedAt: event.editedAt }
            : m,
        ),
      );
    });

    connection.on('userJoined', (event: ChatPresenceEvent) => {
      if (event.roomId !== roomId) return;
      setOnlineUsers((prev) => {
        if (prev.some((u) => u.userId === event.userId)) return prev;
        return [...prev, { userId: event.userId, username: event.username }];
      });
    });

    connection.on('userLeft', (event: ChatPresenceEvent) => {
      if (event.roomId !== roomId) return;
      setOnlineUsers((prev) => prev.filter((u) => u.userId !== event.userId));
      setTypingUsers((prev) => {
        if (!prev.has(event.userId)) return prev;
        const next = new Map(prev);
        next.delete(event.userId);
        return next;
      });
    });

    connection.on('userStartedTyping', (event: TypingEvent) => {
      if (event.roomId !== roomId) return;
      setTypingUsers((prev) =>
        new Map(prev).set(event.userId, { username: event.username, at: Date.now() }),
      );
    });

    connection.on('userStoppedTyping', (event: TypingEvent) => {
      if (event.roomId !== roomId) return;
      setTypingUsers((prev) => {
        if (!prev.has(event.userId)) return prev;
        const next = new Map(prev);
        next.delete(event.userId);
        return next;
      });
    });

    connection.on('roomPresenceUpdated', (presence: RoomPresenceResponse) => {
      if (presence.roomId !== roomId) return;
      setOnlineUsers(presence.onlineUsers);
    });

    connection.on('roomMemberJoined', (event: RoomMemberEvent) => {
      if (event.roomId !== roomId) return;
      void queryClient.invalidateQueries({ queryKey: queryKeys.rooms.detail(roomId) });
      void queryClient.invalidateQueries({ queryKey: queryKeys.rooms.all() });
    });

    connection.on('roomMemberLeft', (event: RoomMemberEvent) => {
      if (event.roomId !== roomId) return;
      void queryClient.invalidateQueries({ queryKey: queryKeys.rooms.detail(roomId) });
      void queryClient.invalidateQueries({ queryKey: queryKeys.rooms.all() });
    });

    connection.on('roomClosed', (event: RoomClosedEvent) => {
      if (event.roomId !== roomId) return;
      void queryClient.invalidateQueries({ queryKey: queryKeys.rooms.all() });
      navigate('/rooms');
    });

    let cancelled = false;

    const joinRoom = async () => {
      try {
        await connection.invoke('joinRoom', roomId);
      } catch {
        /* server rejects non-members */
      }
    };

    const syncStatus = () => {
      if (!cancelled) setStatus(getConnectionStatus(connection));
    };

    void (async () => {
      setStatus('connecting');
      try {
        await startConnection(connection);
        if (cancelled) return;
        syncStatus();
        await joinRoom();
      } catch {
        if (!cancelled) setStatus('disconnected');
      }
    })();

    connection.onclose(() => {
      if (!cancelled) setStatus('disconnected');
    });

    connection.onreconnecting(() => {
      if (!cancelled) {
        setStatus('reconnecting');
        setReconnectAttempt((n) => n + 1);
      }
    });

    connection.onreconnected(() => {
      if (!cancelled) {
        setStatus(getConnectionStatus(connection));
        setReconnectAttempt(0);
        void joinRoom();
      }
    });

    const ids = messageIdsRef.current;

    return () => {
      cancelled = true;
      const conn = connectionRef.current;
      connectionRef.current = null;
      if (conn) {
        void (async () => {
          try {
            await conn.invoke('leaveRoom', roomId);
          } catch {
            /* already disconnected */
          }
          await stopConnection(conn);
        })();
      }
      setMessages([]);
      setOnlineUsers([]);
      setTypingUsers(new Map());
      setStatus('disconnected');
      setReconnectAttempt(0);
      ids.clear();
    };
  }, [roomId, addMessage, queryClient, navigate]);

  /* Auto-clear stale typing indicators */
  useEffect(() => {
    const interval = setInterval(() => {
      setTypingUsers((prev) => {
        const now = Date.now();
        let changed = false;
        const next = new Map(prev);
        for (const [uid, entry] of next) {
          if (now - entry.at > TYPING_STALE_MS) {
            next.delete(uid);
            changed = true;
          }
        }
        return changed ? next : prev;
      });
    }, 2_000);
    return () => clearInterval(interval);
  }, []);

  const sendMessage = useCallback(
    async (content: string) => {
      const conn = connectionRef.current;
      if (!conn || conn.state !== HubConnectionState.Connected || !roomId) return;
      await conn.invoke('sendMessage', roomId, content);
    },
    [roomId],
  );

  const deleteMessage = useCallback(
    async (messageId: string) => {
      const conn = connectionRef.current;
      if (!conn || conn.state !== HubConnectionState.Connected || !roomId) return;
      try {
        await conn.invoke('deleteMessage', roomId, messageId);
      } catch {
        await chatApi.deleteMessage(roomId, messageId);
      }
    },
    [roomId],
  );

  const editMessage = useCallback(
    async (messageId: string, content: string) => {
      const conn = connectionRef.current;
      if (!conn || conn.state !== HubConnectionState.Connected || !roomId) return;
      try {
        await conn.invoke('editMessage', roomId, messageId, content);
      } catch {
        await chatApi.editMessage(roomId, messageId, content);
      }
    },
    [roomId],
  );

  const startTyping = useCallback(async () => {
    const conn = connectionRef.current;
    if (!conn || conn.state !== HubConnectionState.Connected || !roomId) return;
    try {
      await conn.invoke('startTyping', roomId);
    } catch {
      /* swallow if disconnected mid-call */
    }
  }, [roomId]);

  const stopTyping = useCallback(async () => {
    const conn = connectionRef.current;
    if (!conn || conn.state !== HubConnectionState.Connected || !roomId) return;
    try {
      await conn.invoke('stopTyping', roomId);
    } catch {
      /* swallow */
    }
  }, [roomId]);

  const typingUsersList: TypingUser[] = Array.from(typingUsers, ([userId, { username }]) => ({
    userId,
    username,
  }));

  return {
    messages,
    onlineUsers,
    typingUsers: typingUsersList,
    status,
    reconnectAttempt,
    sendMessage,
    deleteMessage,
    editMessage,
    startTyping,
    stopTyping,
  };
}

/* ─── Typing debounce hook (for ChatInput) ─── */

export function useTypingIndicator(
  onStart: () => Promise<void>,
  onStop: () => Promise<void>,
) {
  const isTypingRef = useRef(false);
  const timeoutRef = useRef<ReturnType<typeof setTimeout>>(undefined);

  const keystroke = useCallback(() => {
    if (!isTypingRef.current) {
      isTypingRef.current = true;
      void onStart();
    }
    if (timeoutRef.current) clearTimeout(timeoutRef.current);
    timeoutRef.current = setTimeout(() => {
      if (isTypingRef.current) {
        isTypingRef.current = false;
        void onStop();
      }
    }, 2_000);
  }, [onStart, onStop]);

  const flush = useCallback(() => {
    if (timeoutRef.current) clearTimeout(timeoutRef.current);
    if (isTypingRef.current) {
      isTypingRef.current = false;
      void onStop();
    }
  }, [onStop]);

  useEffect(() => {
    return () => {
      if (timeoutRef.current) clearTimeout(timeoutRef.current);
    };
  }, []);

  return { keystroke, flush };
}
