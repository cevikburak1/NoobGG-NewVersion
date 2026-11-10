import { useId, useState, type ReactNode } from 'react';
import { motion } from 'framer-motion';
import { cn } from '@/lib/cn';

interface Tab {
  id: string;
  label: string;
  content: ReactNode;
}

interface TabsProps {
  tabs: Tab[];
  defaultTab?: string;
  className?: string;
}

export function Tabs({ tabs, defaultTab, className }: TabsProps) {
  const id = useId();
  const [activeTab, setActiveTab] = useState(defaultTab ?? tabs[0]?.id ?? '');

  return (
    <div className={className}>
      <div role="tablist" aria-label="Sections" className="flex gap-1 overflow-x-auto rounded-xl border border-border bg-background/60 p-1.5">
        {tabs.map((tab) => (
          <button
            key={tab.id}
            id={`${id}-${tab.id}`}
            type="button"
            role="tab"
            aria-selected={activeTab === tab.id}
            aria-controls={`${id}-panel`}
            tabIndex={activeTab === tab.id ? 0 : -1}
            onKeyDown={event => {
              const index = tabs.findIndex(item => item.id === activeTab);
              const next = event.key === 'ArrowRight' ? (index + 1) % tabs.length : event.key === 'ArrowLeft' ? (index - 1 + tabs.length) % tabs.length : event.key === 'Home' ? 0 : event.key === 'End' ? tabs.length - 1 : -1;
              if (next >= 0) { event.preventDefault(); setActiveTab(tabs[next].id); document.getElementById(`${id}-${tabs[next].id}`)?.focus(); }
            }}
            onClick={() => setActiveTab(tab.id)}
            className={cn(
              'relative min-h-10 shrink-0 rounded-lg px-4 py-2 text-sm font-semibold transition-colors',
              activeTab === tab.id
                ? 'text-foreground'
                : 'text-foreground-muted hover:text-foreground',
            )}
          >
            {activeTab === tab.id && (
              <motion.div
                layoutId={`${id}-activeTab`}
                className="absolute inset-0 rounded-lg bg-surface-active"
                transition={{ type: 'spring', bounce: 0.2, duration: 0.4 }}
              />
            )}
            <span className="relative z-10">{tab.label}</span>
          </button>
        ))}
      </div>
      <div id={`${id}-panel`} role="tabpanel" aria-labelledby={`${id}-${activeTab}`} className="mt-5">
        {tabs.find((t) => t.id === activeTab)?.content}
      </div>
    </div>
  );
}
