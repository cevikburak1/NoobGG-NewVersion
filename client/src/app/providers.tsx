import type { ReactNode } from 'react';
import { QueryClientProvider } from '@tanstack/react-query';
import { ReactQueryDevtools } from '@tanstack/react-query-devtools';
import { queryClient } from '@/lib/queryClient';
import { ErrorBoundary } from '@/components/common/errorBoundary';
import { ToastProvider } from '@/components/ui/toast';
import { DmProvider } from '@/providers/dmProvider';
import { NotificationProvider } from '@/providers/notificationProvider';
import { RoomProvider } from '@/providers/roomProvider';

interface ProvidersProps {
  children: ReactNode;
}

export function Providers({ children }: ProvidersProps) {
  return (
    <ErrorBoundary>
      <QueryClientProvider client={queryClient}>
        <ToastProvider>
          <NotificationProvider>
            <RoomProvider>
              <DmProvider>
                {children}
              </DmProvider>
            </RoomProvider>
          </NotificationProvider>
        </ToastProvider>
        <ReactQueryDevtools initialIsOpen={false} buttonPosition="bottom-left" />
      </QueryClientProvider>
    </ErrorBoundary>
  );
}
