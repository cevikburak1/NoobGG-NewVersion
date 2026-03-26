import { motion, AnimatePresence } from 'framer-motion';
import type { ConnectionStatus } from '@/lib/signalr';

interface ConnectionBannerProps {
  status: ConnectionStatus;
  reconnectAttempt: number;
}

export function ConnectionBanner({ status, reconnectAttempt }: ConnectionBannerProps) {
  const showBanner = status === 'reconnecting' || status === 'disconnected' || status === 'connecting';

  return (
    <AnimatePresence>
      {showBanner && (
        <motion.div
          initial={{ height: 0, opacity: 0 }}
          animate={{ height: 'auto', opacity: 1 }}
          exit={{ height: 0, opacity: 0 }}
          transition={{ duration: 0.2 }}
          className="overflow-hidden"
        >
          <div className={`flex items-center justify-center gap-2 px-3 py-1.5 text-xs font-medium ${
            status === 'disconnected'
              ? 'bg-danger/10 text-danger'
              : 'bg-warning/10 text-warning'
          }`}>
            {status === 'reconnecting' && (
              <>
                <PulsingDot className="bg-warning" />
                Reconnecting{reconnectAttempt > 1 ? ` (attempt ${reconnectAttempt})` : ''}...
              </>
            )}
            {status === 'connecting' && (
              <>
                <PulsingDot className="bg-warning" />
                Connecting to chat...
              </>
            )}
            {status === 'disconnected' && (
              <>
                <svg className="h-3 w-3" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth={2}>
                  <path strokeLinecap="round" strokeLinejoin="round" d="M12 9v3.75m9-.75a9 9 0 11-18 0 9 9 0 0118 0zm-9 3.75h.008v.008H12v-.008z" />
                </svg>
                Connection lost. Attempting to reconnect...
              </>
            )}
          </div>
        </motion.div>
      )}
    </AnimatePresence>
  );
}

function PulsingDot({ className }: { className: string }) {
  return (
    <motion.span
      animate={{ scale: [1, 1.4, 1], opacity: [1, 0.5, 1] }}
      transition={{ duration: 1.2, repeat: Infinity }}
      className={`inline-block h-2 w-2 rounded-full ${className}`}
    />
  );
}
