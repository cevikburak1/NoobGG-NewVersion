import { type ReactNode, useEffect, useId, useRef } from 'react';
import { createPortal } from 'react-dom';
import { AnimatePresence, motion } from 'framer-motion';
import { cn } from '@/lib/cn';
import { Icon } from './icon';

interface ModalProps { isOpen: boolean; onClose: () => void; title?: string; children: ReactNode; className?: string; }
export function Modal({ isOpen, onClose, title, children, className }: ModalProps) {
  const titleId = useId();
  const dialogRef = useRef<HTMLDivElement>(null);
  const closeRef = useRef(onClose);
  useEffect(() => { closeRef.current = onClose; }, [onClose]);
  useEffect(() => {
    if (!isOpen) return;
    const previousFocus = document.activeElement as HTMLElement | null;
    const overflow = document.body.style.overflow;
    document.body.style.overflow = 'hidden';
    const frame = requestAnimationFrame(() => dialogRef.current?.focus());
    const handleKey = (e: KeyboardEvent) => {
      if (e.key === 'Escape') closeRef.current();
      if (e.key !== 'Tab') return;
      const nodes = [...(dialogRef.current?.querySelectorAll<HTMLElement>('button:not(:disabled), a[href], input:not(:disabled), select:not(:disabled), textarea:not(:disabled), [tabindex="0"]') ?? [])].filter(el => el.getClientRects().length);
      const first = nodes[0], last = nodes[nodes.length - 1];
      if (!first) { e.preventDefault(); return; }
      if (e.shiftKey && (document.activeElement === first || document.activeElement === dialogRef.current)) { e.preventDefault(); last.focus(); }
      else if (!e.shiftKey && (document.activeElement === last || document.activeElement === dialogRef.current)) { e.preventDefault(); first.focus(); }
    };
    document.addEventListener('keydown', handleKey);
    return () => { cancelAnimationFrame(frame); document.body.style.overflow = overflow; document.removeEventListener('keydown', handleKey); previousFocus?.focus(); };
  }, [isOpen]);
  return createPortal(<AnimatePresence>{isOpen && <div className="fixed inset-0 z-[80] flex items-center justify-center p-4">
    <motion.div initial={{ opacity: 0 }} animate={{ opacity: 1 }} exit={{ opacity: 0 }} className="fixed inset-0 bg-black/75 backdrop-blur-md" onClick={onClose} />
    <motion.div ref={dialogRef} role="dialog" aria-modal="true" aria-labelledby={title ? titleId : undefined} aria-label={title ? undefined : 'Dialog'} tabIndex={-1} initial={{ opacity: 0, scale: .97, y: 12 }} animate={{ opacity: 1, scale: 1, y: 0 }} exit={{ opacity: 0, scale: .97, y: 12 }} transition={{ duration: .18 }} className={cn('relative z-10 max-h-[calc(100dvh-2rem)] w-full max-w-md overflow-y-auto rounded-3xl border border-border bg-surface p-6 shadow-[0_24px_100px_#00000070] outline-none sm:p-7', className)}>
      <div className="mb-6 flex items-center justify-between gap-4">{title && <h2 id={titleId} className="text-xl font-bold text-foreground">{title}</h2>}<button aria-label="Close dialog" onClick={onClose} className="ml-auto flex h-10 w-10 shrink-0 items-center justify-center rounded-xl border border-border text-foreground-muted hover:bg-surface-hover"><Icon name="close" className="h-4 w-4" /></button></div>
      {children}
    </motion.div>
  </div>}</AnimatePresence>, document.body);
}
