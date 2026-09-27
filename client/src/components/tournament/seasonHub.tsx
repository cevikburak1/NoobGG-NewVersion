import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { api } from '@/lib/api';
import { useAuthStore } from '@/stores/authStore';
import { Button, Input, Select, Pagination } from '@/components/ui';
import { usePagination } from '@/hooks/usePagination';
import { useGameBrowse } from '@/features/games/hooks';

export interface Season {
  id: string;
  name: string;
  gameId: string;
  startsAt: string;
  endsAt: string;
}

interface Standing { userId: string; username: string; wins: number; podiums: number; points: number }

export function SeasonHub() {
  const { t } = useTranslation();
  const qc = useQueryClient();
  const role = useAuthStore((s) => s.user?.role);
  const [selectedId, setSelectedId] = useState('');
  const [name, setName] = useState('');
  const [gameId, setGameId] = useState('');
  const [startsAt, setStartsAt] = useState('');
  const [endsAt, setEndsAt] = useState('');
  const seasons = useQuery({ queryKey: ['seasons'], queryFn: async () => (await api.get<Season[]>('/api/seasons')).data });
  const games = useGameBrowse({ multiplayer: true, pageSize: 100 });
  const chosenId = selectedId || seasons.data?.[0]?.id;
  const standings = useQuery({
    queryKey: ['season-standings', chosenId],
    queryFn: async () => (await api.get<{ standings: Standing[] }>(`/api/seasons/${chosenId}/standings`)).data,
    enabled: Boolean(chosenId),
  });
  const standingPage = usePagination(standings.data?.standings ?? [], 8, chosenId);
  const create = useMutation({
    mutationFn: async () => api.post('/api/seasons', { name, gameId, startsAt: new Date(startsAt).toISOString(), endsAt: new Date(endsAt).toISOString() }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['seasons'] });
      setName(''); setGameId(''); setStartsAt(''); setEndsAt('');
    },
  });

  return (
    <section className="mb-8 border-y border-border py-5">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h2 className="text-lg font-semibold text-foreground">{t('Seasons')}</h2>
        <Select value={chosenId ?? ''} onChange={(event) => setSelectedId(event.target.value)}
          options={(seasons.data ?? []).map((season) => ({ value: season.id, label: season.name }))} />
      </div>
      {standings.isError ? <p className="mt-3 text-sm text-danger">{t('Standings could not be loaded.')}</p> : standings.data?.standings.length ? (
        <>
        <div className="mt-3 grid gap-2 sm:grid-cols-2 lg:grid-cols-4">
          {standingPage.items.map((row, index) => (
            <div key={row.userId} className="flex justify-between border-l-2 border-accent bg-surface/80 px-3 py-2 text-sm">
              <span className="text-foreground">{(standingPage.page - 1) * standingPage.pageSize + index + 1}. {row.username}</span>
              <strong className="text-accent">{row.points} {t('pts')}</strong>
            </div>
          ))}
        </div>
        <Pagination className="mt-3" page={standingPage.page} pageSize={standingPage.pageSize}
          totalItems={standingPage.totalItems} onPageChange={standingPage.setPage} />
        </>
      ) : <p className="mt-3 text-sm text-foreground-muted">{t('No confirmed season results yet.')}</p>}
      {role === 'Admin' && (
        <form className="mt-4 grid gap-2 sm:grid-cols-5" onSubmit={(event) => { event.preventDefault(); if (name && gameId && startsAt && endsAt) create.mutate(); }}>
          <Input aria-label={t('Season name')} placeholder={t('Season name')} value={name} onChange={(event) => setName(event.target.value)} />
          <Select aria-label={t('Game')} placeholder={t('Select game')} value={gameId} onChange={(event) => setGameId(event.target.value)}
            options={(games.data?.items ?? []).map((game) => ({ value: game.id, label: game.name }))} />
          <Input aria-label={t('Start date')} type="datetime-local" value={startsAt} onChange={(event) => setStartsAt(event.target.value)} />
          <Input aria-label={t('End date')} type="datetime-local" value={endsAt} onChange={(event) => setEndsAt(event.target.value)} />
          <Button type="submit" disabled={!name || !gameId || !startsAt || !endsAt} isLoading={create.isPending}>{t('Create season')}</Button>
          {create.isError && <p className="text-sm text-danger sm:col-span-5">{t('Season could not be created.')}</p>}
        </form>
      )}
    </section>
  );
}
