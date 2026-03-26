import {
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
  type HubConnection,
  type IRetryPolicy,
  type RetryContext,
} from '@microsoft/signalr';

const BASE_URL = import.meta.env.VITE_API_URL ?? '';

export type ConnectionStatus = 'disconnected' | 'connecting' | 'connected' | 'reconnecting';

const RETRY_DELAYS = [0, 1_000, 2_000, 5_000, 10_000, 30_000];

const retryPolicy: IRetryPolicy = {
  nextRetryDelayInMilliseconds(context: RetryContext) {
    if (context.previousRetryCount >= RETRY_DELAYS.length) {
      return RETRY_DELAYS[RETRY_DELAYS.length - 1];
    }
    return RETRY_DELAYS[context.previousRetryCount] ?? 30_000;
  },
};

export function createChatConnection(getAccessToken: () => string | null): HubConnection {
  return new HubConnectionBuilder()
    .withUrl(`${BASE_URL}/hubs/chat`, {
      accessTokenFactory: () => getAccessToken() ?? '',
    })
    .withAutomaticReconnect(retryPolicy)
    .configureLogging(LogLevel.Warning)
    .build();
}

export function createDmConnection(getAccessToken: () => string | null): HubConnection {
  return new HubConnectionBuilder()
    .withUrl(`${BASE_URL}/hubs/dm`, {
      accessTokenFactory: () => getAccessToken() ?? '',
    })
    .withAutomaticReconnect(retryPolicy)
    .configureLogging(LogLevel.Warning)
    .build();
}

export function createRoomConnection(getAccessToken: () => string | null): HubConnection {
  return new HubConnectionBuilder()
    .withUrl(`${BASE_URL}/hubs/room`, {
      accessTokenFactory: () => getAccessToken() ?? '',
    })
    .withAutomaticReconnect(retryPolicy)
    .configureLogging(LogLevel.Warning)
    .build();
}

export function createNotificationConnection(getAccessToken: () => string | null): HubConnection {
  return new HubConnectionBuilder()
    .withUrl(`${BASE_URL}/hubs/notifications`, {
      accessTokenFactory: () => getAccessToken() ?? '',
    })
    .withAutomaticReconnect(retryPolicy)
    .configureLogging(LogLevel.Warning)
    .build();
}

export function getConnectionStatus(connection: HubConnection): ConnectionStatus {
  switch (connection.state) {
    case HubConnectionState.Connected:
      return 'connected';
    case HubConnectionState.Connecting:
    case HubConnectionState.Reconnecting:
      return 'reconnecting';
    case HubConnectionState.Disconnected:
    case HubConnectionState.Disconnecting:
    default:
      return 'disconnected';
  }
}

export async function startConnection(connection: HubConnection): Promise<void> {
  if (connection.state === HubConnectionState.Disconnected) {
    await connection.start();
  }
}

export async function stopConnection(connection: HubConnection): Promise<void> {
  if (connection.state !== HubConnectionState.Disconnected) {
    await connection.stop();
  }
}
