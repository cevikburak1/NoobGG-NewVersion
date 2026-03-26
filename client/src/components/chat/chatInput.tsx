import { useState, type FormEvent, useRef, useEffect } from 'react';
import { motion } from 'framer-motion';
import { useTypingIndicator } from '@/features/chat/hooks';
import type { ConnectionStatus } from '@/lib/signalr';
import { Button } from '@/components/ui';

interface ChatInputProps {
  onSend: (content: string) => Promise<void>;
  onTypingStart: () => Promise<void>;
  onTypingStop: () => Promise<void>;
  status: ConnectionStatus;
}

export function ChatInput({ onSend, onTypingStart, onTypingStop, status }: ChatInputProps) {
  const [message, setMessage] = useState('');
  const inputRef = useRef<HTMLInputElement>(null);
  const { keystroke, flush } = useTypingIndicator(onTypingStart, onTypingStop);
  const disabled = status !== 'connected';

  useEffect(() => {
    if (!disabled) inputRef.current?.focus();
  }, [disabled]);

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    const trimmed = message.trim();
    if (!trimmed || disabled) return;
    setMessage('');
    flush();
    await onSend(trimmed);
    inputRef.current?.focus();
  };

  const handleChange = (value: string) => {
    setMessage(value);
    if (value.length > 0) keystroke();
  };

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      void handleSubmit(e as unknown as FormEvent);
    }
  };

  const placeholder = (() => {
    switch (status) {
      case 'connecting':
        return 'Connecting...';
      case 'reconnecting':
        return 'Reconnecting...';
      case 'disconnected':
        return 'Chat unavailable';
      default:
        return 'Type a message...';
    }
  })();

  return (
    <form onSubmit={handleSubmit} className="border-t border-border px-4 py-3">
      <div className="flex gap-2">
        <input
          ref={inputRef}
          value={message}
          onChange={(e) => handleChange(e.target.value)}
          onKeyDown={handleKeyDown}
          placeholder={placeholder}
          disabled={disabled}
          maxLength={2000}
          className="flex-1 rounded-lg border border-border bg-surface-hover px-3 py-2 text-sm text-foreground placeholder:text-foreground-subtle focus:outline-none focus:ring-2 focus:ring-primary/50 focus:border-primary disabled:opacity-50 transition-colors"
        />
        <motion.div whileHover={disabled ? {} : { scale: 1.05 }} whileTap={disabled ? {} : { scale: 0.95 }}>
          <Button type="submit" disabled={disabled || !message.trim()} size="sm" className="shrink-0">
            <svg className="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth={2}>
              <path strokeLinecap="round" strokeLinejoin="round" d="M6 12L3.269 3.126A59.768 59.768 0 0121.485 12 59.77 59.77 0 013.27 20.876L5.999 12zm0 0h7.5" />
            </svg>
          </Button>
        </motion.div>
      </div>
      {message.length > 1800 && (
        <motion.p
          initial={{ opacity: 0 }}
          animate={{ opacity: 1 }}
          className={`mt-1 text-xs ${message.length > 1950 ? 'text-danger' : 'text-warning'}`}
        >
          {2000 - message.length} characters remaining
        </motion.p>
      )}
    </form>
  );
}
