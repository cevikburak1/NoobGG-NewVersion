import { useState, useMemo } from 'react';
import { Link } from 'react-router-dom';
import { motion, AnimatePresence } from 'framer-motion';
import { useGuildEvents, useCreateGuildEvent, useDeleteGuildEvent } from '@/features/guildEvents/hooks';
import type { GuildEventResponse } from '@/features/guildEvents/types';
import {
  Button, Modal, Input, Textarea, Badge, Spinner,
  useToast, staggerContainer, staggerItem,
} from '@/components/ui';

interface GuildEventCalendarProps {
  guildId: string;
  canManage: boolean;
}

function formatDate(iso: string) {
  return new Date(iso).toLocaleDateString('en-US', {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
  });
}

function formatDateRange(start: string, end: string) {
  const s = new Date(start);
  const e = new Date(end);
  const sameDay = s.toDateString() === e.toDateString();
  if (sameDay) return formatDate(start);
  return `${formatDate(start)} — ${formatDate(end)}`;
}

function groupByMonth(events: GuildEventResponse[]): Map<string, GuildEventResponse[]> {
  const groups = new Map<string, GuildEventResponse[]>();
  const sorted = [...events].sort(
    (a, b) => new Date(a.startsAt).getTime() - new Date(b.startsAt).getTime(),
  );

  for (const event of sorted) {
    const key = new Date(event.startsAt).toLocaleDateString('en-US', {
      month: 'long',
      year: 'numeric',
    });
    const group = groups.get(key) ?? [];
    group.push(event);
    groups.set(key, group);
  }

  return groups;
}

function EventCard({
  event,
  canManage,
  onDelete,
  isDeleting,
}: {
  event: GuildEventResponse;
  canManage: boolean;
  onDelete: (id: string) => void;
  isDeleting: boolean;
}) {
  const isPast = new Date(event.endsAt) < new Date();

  return (
    <motion.div
      variants={staggerItem}
      layout
      className={`rounded-xl border border-border bg-surface/60 backdrop-blur-sm p-4 transition-colors hover:border-primary/20 ${
        isPast ? 'opacity-60' : ''
      }`}
    >
      <div className="flex items-start justify-between gap-3">
        <div className="min-w-0 flex-1">
          <h4 className="text-sm font-semibold text-foreground truncate">{event.title}</h4>
          {event.description && (
            <p className="mt-1 text-xs text-foreground-muted line-clamp-2">{event.description}</p>
          )}
        </div>
        {canManage && (
          <button
            onClick={() => onDelete(event.id)}
            disabled={isDeleting}
            className="shrink-0 rounded p-1 text-foreground-subtle hover:bg-danger/10 hover:text-danger transition-colors disabled:opacity-50"
            title="Delete event"
          >
            <svg className="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth={1.5}>
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                d="m14.74 9-.346 9m-4.788 0L9.26 9m9.968-3.21c.342.052.682.107 1.022.166m-1.022-.165L18.16 19.673a2.25 2.25 0 0 1-2.244 2.077H8.084a2.25 2.25 0 0 1-2.244-2.077L4.772 5.79m14.456 0a48.108 48.108 0 0 0-3.478-.397m-12 .562c.34-.059.68-.114 1.022-.165m0 0a48.11 48.11 0 0 1 3.478-.397m7.5 0v-.916c0-1.18-.91-2.164-2.09-2.201a51.964 51.964 0 0 0-3.32 0c-1.18.037-2.09 1.022-2.09 2.201v.916m7.5 0a48.667 48.667 0 0 0-7.5 0"
              />
            </svg>
          </button>
        )}
      </div>

      <div className="mt-3 flex flex-wrap items-center gap-2">
        <span className="inline-flex items-center gap-1 text-xs text-foreground-muted">
          <svg className="h-3.5 w-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth={1.5}>
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              d="M6.75 3v2.25M17.25 3v2.25M3 18.75V7.5a2.25 2.25 0 0 1 2.25-2.25h13.5A2.25 2.25 0 0 1 21 7.5v11.25m-18 0A2.25 2.25 0 0 0 5.25 21h13.5A2.25 2.25 0 0 0 21 18.75m-18 0v-7.5A2.25 2.25 0 0 1 5.25 9h13.5A2.25 2.25 0 0 1 21 11.25v7.5"
            />
          </svg>
          {formatDateRange(event.startsAt, event.endsAt)}
        </span>
        {event.gameId && <Badge variant="primary">Game</Badge>}
        {isPast && <Badge variant="default">Past</Badge>}
      </div>

      {event.tournamentId && (
        <Link
          to={`/tournaments/${event.tournamentId}`}
          className="mt-2 inline-flex items-center gap-1 text-xs font-medium text-accent hover:underline"
        >
          View Tournament
          <svg className="h-3 w-3" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth={2}>
            <path strokeLinecap="round" strokeLinejoin="round" d="M13.5 4.5 21 12m0 0-7.5 7.5M21 12H3" />
          </svg>
        </Link>
      )}
    </motion.div>
  );
}

function CreateEventModal({
  isOpen,
  onClose,
  guildId,
}: {
  isOpen: boolean;
  onClose: () => void;
  guildId: string;
}) {
  const { addToast } = useToast();
  const createEvent = useCreateGuildEvent();
  const [form, setForm] = useState({
    title: '',
    description: '',
    startsAt: '',
    endsAt: '',
    gameId: '',
  });
  const [errors, setErrors] = useState<Record<string, string>>({});

  const handleChange = (field: string, value: string) => {
    setForm((prev) => ({ ...prev, [field]: value }));
    setErrors((prev) => ({ ...prev, [field]: '' }));
  };

  const validate = (): boolean => {
    const next: Record<string, string> = {};
    if (!form.title.trim()) next.title = 'Title is required';
    if (!form.startsAt) next.startsAt = 'Start date is required';
    if (!form.endsAt) next.endsAt = 'End date is required';
    if (form.startsAt && form.endsAt && new Date(form.endsAt) < new Date(form.startsAt)) {
      next.endsAt = 'End date must be after start date';
    }
    setErrors(next);
    return Object.keys(next).length === 0;
  };

  const handleSubmit = () => {
    if (!validate()) return;
    createEvent.mutate(
      {
        guildId,
        title: form.title.trim(),
        description: form.description.trim() || undefined,
        startsAt: new Date(form.startsAt).toISOString(),
        endsAt: new Date(form.endsAt).toISOString(),
        gameId: form.gameId.trim() || undefined,
      },
      {
        onSuccess: () => {
          addToast({ title: 'Event Created', message: 'The event has been added.', type: 'success' });
          setForm({ title: '', description: '', startsAt: '', endsAt: '', gameId: '' });
          setErrors({});
          onClose();
        },
        onError: () => {
          addToast({ title: 'Error', message: 'Failed to create event.', type: 'error' });
        },
      },
    );
  };

  return (
    <Modal isOpen={isOpen} onClose={onClose} title="Add Event" className="max-w-md">
      <div className="space-y-4">
        <Input
          id="event-title"
          label="Title"
          placeholder="Event name"
          value={form.title}
          onChange={(e) => handleChange('title', e.target.value)}
          error={errors.title}
        />
        <Textarea
          id="event-desc"
          label="Description"
          placeholder="Optional description..."
          rows={3}
          value={form.description}
          onChange={(e) => handleChange('description', e.target.value)}
        />
        <div className="grid grid-cols-2 gap-3">
          <Input
            id="event-start"
            label="Start Date"
            type="datetime-local"
            value={form.startsAt}
            onChange={(e) => handleChange('startsAt', e.target.value)}
            error={errors.startsAt}
          />
          <Input
            id="event-end"
            label="End Date"
            type="datetime-local"
            value={form.endsAt}
            onChange={(e) => handleChange('endsAt', e.target.value)}
            error={errors.endsAt}
          />
        </div>
        <Input
          id="event-game"
          label="Game ID (optional)"
          placeholder="Link to a specific game"
          value={form.gameId}
          onChange={(e) => handleChange('gameId', e.target.value)}
        />

        <div className="flex justify-end gap-2 pt-2">
          <Button variant="ghost" onClick={onClose}>
            Cancel
          </Button>
          <Button onClick={handleSubmit} isLoading={createEvent.isPending}>
            Create Event
          </Button>
        </div>
      </div>
    </Modal>
  );
}

export function GuildEventCalendar({ guildId, canManage }: GuildEventCalendarProps) {
  const { data: eventList, isLoading } = useGuildEvents(guildId);
  const deleteEvent = useDeleteGuildEvent();
  const { addToast } = useToast();
  const [showCreateModal, setShowCreateModal] = useState(false);

  const grouped = useMemo(() => {
    if (!eventList?.events) return new Map<string, GuildEventResponse[]>();
    return groupByMonth(eventList.events);
  }, [eventList]);

  const handleDelete = (eventId: string) => {
    deleteEvent.mutate(eventId, {
      onSuccess: () => addToast({ title: 'Deleted', message: 'Event removed.', type: 'success' }),
      onError: () => addToast({ title: 'Error', message: 'Could not delete event.', type: 'error' }),
    });
  };

  if (isLoading) {
    return (
      <div className="flex justify-center py-16">
        <Spinner size="lg" />
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h2 className="text-lg font-semibold text-foreground">Events</h2>
        {canManage && (
          <Button size="sm" onClick={() => setShowCreateModal(true)}>
            <svg className="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth={2}>
              <path strokeLinecap="round" strokeLinejoin="round" d="M12 4.5v15m7.5-7.5h-15" />
            </svg>
            Add Event
          </Button>
        )}
      </div>

      {grouped.size === 0 ? (
        <Card>
          <div className="flex flex-col items-center py-12 text-center">
            <span className="text-4xl">📅</span>
            <p className="mt-3 text-sm text-foreground-muted">No upcoming events</p>
            {canManage && (
              <Button size="sm" variant="outline" className="mt-4" onClick={() => setShowCreateModal(true)}>
                Create first event
              </Button>
            )}
          </div>
        </Card>
      ) : (
        <div className="space-y-6">
          {Array.from(grouped.entries()).map(([month, events]) => (
            <motion.div
              key={month}
              initial={{ opacity: 0, y: 12 }}
              animate={{ opacity: 1, y: 0 }}
              transition={{ duration: 0.4 }}
            >
              <h3 className="mb-3 flex items-center gap-2 text-sm font-semibold text-foreground-muted uppercase tracking-wide">
                <span className="h-px flex-1 bg-border" />
                {month}
                <span className="h-px flex-1 bg-border" />
              </h3>

              <motion.div
                variants={staggerContainer}
                initial="hidden"
                animate="show"
                className="space-y-3"
              >
                <AnimatePresence>
                  {events.map((event) => (
                    <EventCard
                      key={event.id}
                      event={event}
                      canManage={canManage}
                      onDelete={handleDelete}
                      isDeleting={deleteEvent.isPending}
                    />
                  ))}
                </AnimatePresence>
              </motion.div>
            </motion.div>
          ))}
        </div>
      )}

      {canManage && (
        <CreateEventModal
          isOpen={showCreateModal}
          onClose={() => setShowCreateModal(false)}
          guildId={guildId}
        />
      )}
    </div>
  );
}
