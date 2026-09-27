import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { api } from '@/lib/api';
import { useAuthStore } from '@/stores/authStore';
import { Button, Pagination } from '@/components/ui';
import { usePagination } from '@/hooks/usePagination';
import { useToast } from '@/components/ui/toast';

interface GameSession {
  id: string;
  roomTitle: string;
  ownerId: string;
  participantIds: string[];
  endedAt: string;
  replayRoomId: string | null;
}

export function ReplaySessions() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const qc = useQueryClient();
  const { addToast } = useToast();
  const userId = useAuthStore((s) => s.user?.id);
  const sessions = useQuery({
    queryKey: ['sessions', 'mine'],
    queryFn: async () => (await api.get<GameSession[]>('/api/sessions/mine')).data,
  });
  const sessionPage = usePagination(sessions.data ?? [], 3);
  const replay = useMutation({
    mutationFn: async (id: string) => (await api.post<{ roomId: string }>(`/api/sessions/${id}/replay`)).data,
    onSuccess: ({ roomId }) => {
      qc.invalidateQueries({ queryKey: ['sessions', 'mine'] });
      navigate(`/rooms/${roomId}`);
    },
    onError: () => addToast({ title: t('Replay failed'), message: t('Please try again.'), type: 'error' }),
  });
  const feedback = useMutation({
    mutationFn: async ({ id, again }: { id: string; again: boolean }) =>
      api.post(`/api/sessions/${id}/feedback`, { wouldPlayAgain: again, tags: [] }),
    onSuccess: () => addToast({ title: t('Feedback saved'), message: t('Thanks for sharing.'), type: 'success' }),
    onError: () => addToast({ title: t('Feedback failed'), message: t('Please try again.'), type: 'error' }),
  });

  if (!sessions.data?.length) return null;
  return (
    <section className="border-y border-border/70 py-4">
      <h2 className="mb-3 text-sm font-semibold text-foreground">{t('Play together again')}</h2>
      <div className="grid gap-3 lg:grid-cols-3">
        {sessionPage.items.map((session) => (
          <div key={session.id} className="border-l-2 border-primary bg-surface/85 px-3 py-2">
            <p className="truncate text-sm font-medium text-foreground">{session.roomTitle}</p>
            <p className="text-xs text-foreground-muted">{session.participantIds.length} {t('players')} · {new Date(session.endedAt).toLocaleDateString()}</p>
            <div className="mt-2 flex flex-wrap items-center gap-2">
              {session.ownerId === userId && (
                <Button size="sm" onClick={() => replay.mutate(session.id)} isLoading={replay.isPending}>
                  {t(session.replayRoomId ? 'Open room' : 'Invite squad')}
                </Button>
              )}
              <button type="button" className="text-xs text-foreground-muted hover:text-primary" onClick={() => feedback.mutate({ id: session.id, again: true })}>{t('Play again')}</button>
              <button type="button" className="text-xs text-foreground-muted hover:text-danger" onClick={() => feedback.mutate({ id: session.id, again: false })}>{t('Pass')}</button>
            </div>
          </div>
        ))}
      </div>
      <Pagination className="mt-3" page={sessionPage.page} pageSize={sessionPage.pageSize}
        totalItems={sessionPage.totalItems} onPageChange={sessionPage.setPage} />
    </section>
  );
}
