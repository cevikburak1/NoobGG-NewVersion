import { useEffect, useState } from 'react';
import { useParams, Link, useNavigate } from 'react-router-dom';
import { motion } from 'framer-motion';
import { useProfile } from '@/features/profile/hooks';
import { useBlockUser, useUnblockUser } from '@/features/blocks/hooks';
import { useSendFriendRequest, useAcceptFriendRequest, useRemoveFriend } from '@/features/friends/hooks';
import { useToggleFavorite } from '@/features/favorites/hooks';
import { useEloHistory } from '@/features/elo/hooks';
import { useRooms } from '@/features/rooms/hooks';
import {
  Button,
  Badge,
  Card,
  AnimatedPage,
  Spinner,
  ProgressBar,
  staggerContainer,
  staggerItem,
} from '@/components/ui';
import { UserAvatar } from '@/components/common/userAvatar';
import { RankBadge } from '@/components/elo/rankBadge';
import { EloChart } from '@/components/elo/eloChart';
import { resolveFileUrl } from '@/lib/api';
import { useToast } from '@/components/ui/toast';

export default function ProfilePage() {
  const { userId } = useParams<{ userId: string }>();
  const navigate = useNavigate();
  const blockUser = useBlockUser();
  const unblockUser = useUnblockUser();
  const sendFriendRequest = useSendFriendRequest();
  const acceptFriendRequest = useAcceptFriendRequest();
  const removeFriend = useRemoveFriend();
  const { add: addFav, remove: removeFav, isLoading: favLoading } = useToggleFavorite(userId ?? '');
  const { addToast } = useToast();

  const [createdRoomsPage, setCreatedRoomsPage] = useState(1);
  const createdRoomsPageSize = 6;
  const [gamesPage, setGamesPage] = useState(1);
  const gamesPageSize = 6;
  const { data: profile, isLoading, refetch } = useProfile(userId);
  const { data: createdRooms } = useRooms({
    creatorId: userId ?? '__unknown__',
    page: createdRoomsPage,
    pageSize: createdRoomsPageSize,
  });
  const createdRoomsTotalPages = createdRooms ? Math.max(1, Math.ceil(createdRooms.totalCount / createdRoomsPageSize)) : 1;

  useEffect(() => {
    setCreatedRoomsPage(1);
    setGamesPage(1);
  }, [userId]);

  useEffect(() => {
    if (createdRoomsPage > createdRoomsTotalPages) {
      setCreatedRoomsPage(createdRoomsTotalPages);
    }
  }, [createdRoomsPage, createdRoomsTotalPages]);

  const gamesTotalPages = profile?.games ? Math.max(1, Math.ceil(profile.games.length / gamesPageSize)) : 1;
  const paginatedGames = profile?.games ? profile.games.slice((gamesPage - 1) * gamesPageSize, gamesPage * gamesPageSize) : [];

  useEffect(() => {
    if (gamesPage > gamesTotalPages) {
      setGamesPage(gamesTotalPages);
    }
  }, [gamesPage, gamesTotalPages]);

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-32">
        <Spinner size="lg" />
      </div>
    );
  }

  if (!profile) {
    return (
      <AnimatedPage>
        <div className="flex flex-col items-center py-32 text-center">
          <div className="text-5xl">👻</div>
          <h2 className="mt-4 text-xl font-bold text-foreground">Profile not found</h2>
          <p className="mt-2 text-foreground-muted">
            This user doesn&apos;t exist or their profile is private.
          </p>
          <Link to="/discover" className="mt-4">
            <Button variant="outline">Discover Players</Button>
          </Link>
        </div>
      </AnimatedPage>
    );
  }

  if (profile.isRestricted) {
    return (
      <AnimatedPage>
        <div className="flex flex-col items-center py-32 text-center">
          <div className="text-5xl">{profile.isBlockedByThem ? '🚫' : '🔒'}</div>
          <h2 className="mt-4 text-xl font-bold text-foreground">{profile.username}</h2>
          <p className="mt-2 text-foreground-muted">
            {profile.restrictedReason}
          </p>
          {profile.isBlocked && (
            <Button
              variant="outline"
              size="sm"
              className="mt-4"
              onClick={handleUnblock}
              isLoading={unblockUser.isPending}
            >
              Unblock
            </Button>
          )}
          <Link to="/discover" className="mt-4">
            <Button variant="outline">Discover Players</Button>
          </Link>
        </div>
      </AnimatedPage>
    );
  }

  const handleBlock = async () => {
    if (!userId) return;
    await blockUser.mutateAsync(userId);
    refetch();
  };

  const handleUnblock = async () => {
    if (!userId) return;
    await unblockUser.mutateAsync(userId);
    refetch();
  };

  const handleSendMessage = () => {
    navigate(`/messages?user=${userId}`);
  };

  const handleSendFriendRequest = async () => {
    if (!userId) return;
    try {
      await sendFriendRequest.mutateAsync(userId);
      addToast({ title: 'Friend request sent', type: 'success' });
      refetch();
    } catch {
      addToast({ title: 'Could not send friend request', type: 'error' });
    }
  };

  const handleAcceptFriendRequest = async () => {
    if (!profile?.friendshipId) return;
    try {
      await acceptFriendRequest.mutateAsync(profile.friendshipId);
      addToast({ title: 'Friend request accepted', type: 'success' });
      refetch();
    } catch {
      addToast({ title: 'Could not accept friend request', type: 'error' });
    }
  };

  const handleRemoveFriend = async () => {
    if (!userId) return;
    try {
      await removeFriend.mutateAsync(userId);
      addToast({ title: 'Friend removed', type: 'info' });
      refetch();
    } catch {
      addToast({ title: 'Could not remove friend', type: 'error' });
    }
  };

  return (
    <AnimatedPage>
      <div className="space-y-6">
        <motion.div
          initial={{ opacity: 0, y: 20 }}
          animate={{ opacity: 1, y: 0 }}
          className="relative overflow-hidden rounded-2xl border border-border bg-surface"
        >
          {profile.bannerUrl ? (
            <div className="h-32 sm:h-40">
              <img
                src={resolveFileUrl(profile.bannerUrl)}
                alt={`${profile.username}'s banner`}
                className="h-full w-full object-cover"
              />
            </div>
          ) : (
            <div className="h-32 bg-gradient-to-r from-primary/20 via-primary/10 to-accent/20 sm:h-40">
              <div className="absolute inset-0 bg-[radial-gradient(circle_at_30%_50%,_var(--color-primary)_0%,_transparent_60%)] opacity-20" />
            </div>
          )}

          <div className="relative px-6 pb-6">
            <div className="flex flex-col gap-4 sm:flex-row sm:items-end sm:gap-6">
              <motion.div
                initial={{ scale: 0.8, opacity: 0 }}
                animate={{ scale: 1, opacity: 1 }}
                transition={{ delay: 0.2, type: 'spring', bounce: 0.3 }}
                className="-mt-12 sm:-mt-16"
              >
                <div className="rounded-full border-4 border-surface p-0.5">
                  <UserAvatar
                    username={profile.username}
                    avatarUrl={profile.avatarUrl}
                    size="lg"
                    className="!h-24 !w-24 !text-2xl sm:!h-28 sm:!w-28"
                  />
                </div>
              </motion.div>

              <div className="flex-1">
                <motion.div
                  initial={{ opacity: 0, x: -20 }}
                  animate={{ opacity: 1, x: 0 }}
                  transition={{ delay: 0.3 }}
                >
                  <div className="flex items-center gap-2">
                    <h1 className="text-2xl font-bold text-foreground">
                      {profile.displayName || profile.username}
                    </h1>
                    <span
                      className={`inline-flex items-center gap-1.5 rounded-full px-2.5 py-0.5 text-xs font-medium ${
                        profile.isOnline
                          ? 'bg-green-500/10 text-green-500'
                          : 'bg-foreground-subtle/10 text-foreground-subtle'
                      }`}
                    >
                      <span
                        className={`h-2 w-2 rounded-full ${
                          profile.isOnline ? 'bg-green-500 animate-pulse' : 'bg-foreground-subtle'
                        }`}
                      />
                      {profile.isOnline ? 'Online' : 'Offline'}
                    </span>
                  </div>
                  {profile.displayName && (
                    <p className="text-sm text-foreground-muted">@{profile.username}</p>
                  )}
                  <div className="mt-1 flex flex-wrap items-center gap-2">
                    {profile.region && <Badge>{profile.region}</Badge>}
                    {profile.experienceLevel && (
                      <Badge variant="primary">{profile.experienceLevel}</Badge>
                    )}
                    {profile.country && <Badge variant="accent">{profile.country}</Badge>}
                  </div>
                </motion.div>
              </div>

              <motion.div
                initial={{ opacity: 0 }}
                animate={{ opacity: 1 }}
                transition={{ delay: 0.4 }}
                className="flex gap-2"
              >
                {profile.isOwnProfile ? (
                  <>
                    <Link to="/profile/edit">
                      <Button variant="outline" size="sm">
                        Edit Profile
                      </Button>
                    </Link>
                    <Link to="/profile/games">
                      <Button variant="ghost" size="sm">
                        Game Profiles
                      </Button>
                    </Link>
                  </>
                ) : (
                  <>
                    {!profile.isBlockedByThem && !profile.isBlocked && (
                      <FriendActionButton
                        friendshipStatus={profile.friendshipStatus}
                        isFriendRequestSentByMe={profile.isFriendRequestSentByMe}
                        onSendRequest={handleSendFriendRequest}
                        onAcceptRequest={handleAcceptFriendRequest}
                        onRemoveFriend={handleRemoveFriend}
                        isPending={sendFriendRequest.isPending || acceptFriendRequest.isPending || removeFriend.isPending}
                      />
                    )}
                    {!profile.isBlockedByThem && !profile.isBlocked && (
                      <Button
                        variant="ghost"
                        size="sm"
                        onClick={async () => {
                          try {
                            if (profile.isFavorited) {
                              await removeFav.mutateAsync();
                              addToast({ title: 'Removed from favorites', type: 'info' });
                            } else {
                              await addFav.mutateAsync();
                              addToast({ title: 'Added to favorites', type: 'success' });
                            }
                          } catch {
                            addToast({ title: 'Could not update favorite', type: 'error' });
                          }
                        }}
                        isLoading={favLoading}
                        className={`gap-1.5 ${profile.isFavorited ? 'text-yellow-500 hover:text-yellow-600' : ''}`}
                        title={profile.isFavorited ? 'Remove from favorites' : 'Add to favorites'}
                      >
                        <svg className="h-4 w-4" fill={profile.isFavorited ? 'currentColor' : 'none'} stroke="currentColor" viewBox="0 0 24 24" strokeWidth={1.5}>
                          <path strokeLinecap="round" strokeLinejoin="round" d="M11.48 3.499a.562.562 0 011.04 0l2.125 5.111a.563.563 0 00.475.345l5.518.442c.499.04.701.663.321.988l-4.204 3.602a.563.563 0 00-.182.557l1.285 5.385a.562.562 0 01-.84.61l-4.725-2.885a.562.562 0 00-.586 0L6.982 20.54a.562.562 0 01-.84-.61l1.285-5.386a.562.562 0 00-.182-.557l-4.204-3.602a.562.562 0 01.321-.988l5.518-.442a.563.563 0 00.475-.345L11.48 3.5z" />
                        </svg>
                      </Button>
                    )}
                    {!profile.isBlockedByThem && (
                      <Button onClick={handleSendMessage} className="gap-2">
                        <svg className="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth={1.5}>
                          <path strokeLinecap="round" strokeLinejoin="round" d="M8.625 12a.375.375 0 11-.75 0 .375.375 0 01.75 0zm0 0H8.25m4.125 0a.375.375 0 11-.75 0 .375.375 0 01.75 0zm0 0H12m4.125 0a.375.375 0 11-.75 0 .375.375 0 01.75 0zm0 0h-.375M21 12c0 4.556-4.03 8.25-9 8.25a9.764 9.764 0 01-2.555-.337A5.972 5.972 0 015.41 20.97a5.969 5.969 0 01-2.41-.5v.03a.75.75 0 01-.75.75h-.03A8.256 8.256 0 013 12c0-4.556 4.03-8.25 9-8.25s9 3.694 9 8.25z" />
                        </svg>
                        Send Message
                      </Button>
                    )}
                    {profile.isBlocked ? (
                      <Button
                        variant="outline"
                        size="sm"
                        onClick={handleUnblock}
                        isLoading={unblockUser.isPending}
                      >
                        Unblock
                      </Button>
                    ) : (
                      <Button
                        variant="danger"
                        size="sm"
                        onClick={handleBlock}
                        isLoading={blockUser.isPending}
                      >
                        Block
                      </Button>
                    )}
                  </>
                )}
              </motion.div>
            </div>

            {profile.isBlockedByThem && !profile.isOwnProfile && (
              <div className="mt-4 rounded-lg bg-danger/10 border border-danger/20 p-3 text-sm text-danger">
                This user has blocked you. You cannot send messages or interact with them.
              </div>
            )}

            {profile.bio && (
              <motion.p
                initial={{ opacity: 0 }}
                animate={{ opacity: 1 }}
                transition={{ delay: 0.35 }}
                className="mt-4 max-w-2xl text-sm leading-relaxed text-foreground-muted"
              >
                {profile.bio}
              </motion.p>
            )}

            {profile.playSchedule && (
              <motion.div
                initial={{ opacity: 0 }}
                animate={{ opacity: 1 }}
                transition={{ delay: 0.4 }}
                className="mt-3 flex items-center gap-2 text-sm text-foreground-subtle"
              >
                <svg
                  className="h-4 w-4"
                  fill="none"
                  stroke="currentColor"
                  viewBox="0 0 24 24"
                  strokeWidth={1.5}
                >
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    d="M12 6v6h4.5m4.5 0a9 9 0 11-18 0 9 9 0 0118 0z"
                  />
                </svg>
                {profile.playSchedule}
              </motion.div>
            )}
          </div>
        </motion.div>

        <motion.div
          variants={staggerContainer}
          initial="hidden"
          animate="show"
          className="grid grid-cols-2 gap-3 sm:grid-cols-4"
        >
          {[
            { label: 'Rooms Joined', value: profile.stats.roomsJoined, icon: '🚪' },
            { label: 'Rooms Created', value: profile.stats.roomsCreated, icon: '🏠' },
            { label: 'Games', value: profile.stats.gamesPlayed, icon: '🎮' },
            {
              label: 'Since',
              value: new Date(profile.createdAt).toLocaleDateString('en-US', {
                month: 'short',
                year: 'numeric',
              }),
              icon: '📅',
            },
          ].map((stat) => (
            <motion.div
              key={stat.label}
              variants={staggerItem}
              whileHover={{ y: -2, scale: 1.02 }}
              className="rounded-xl border border-border bg-surface p-4 text-center"
            >
              <div className="text-xl">{stat.icon}</div>
              <p className="mt-1 text-lg font-bold text-foreground">{stat.value}</p>
              <p className="text-xs text-foreground-muted">{stat.label}</p>
            </motion.div>
          ))}
        </motion.div>

        <motion.div
          initial={{ opacity: 0, y: 20 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ delay: 0.3 }}
        >
          <Card className="overflow-hidden border-primary/20 bg-linear-to-br from-surface via-surface to-primary/5">
            <div className="flex items-center justify-between border-b border-border/70 p-4">
              <div>
                <h2 className="text-xl font-bold text-foreground">Oyun Profilleri</h2>
                <p className="text-xs text-foreground-muted">
                  Oyun profilleri sayfalı gösterilir.
                </p>
              </div>
              {profile.isOwnProfile && (
                <Link to="/profile/games">
                  <Button variant="outline" size="sm">
                    Oyunları Yönet
                  </Button>
                </Link>
              )}
            </div>

            {profile.games.length > 0 ? (
              <>
                <motion.div
                  variants={staggerContainer}
                  initial="hidden"
                  animate="show"
                  className="grid max-h-112 gap-4 overflow-y-auto p-4 sm:grid-cols-2"
                >
                  {paginatedGames.map((gp) => (
                    <GameProfileCard key={gp.id} gp={gp} userId={profile.userId} />
                  ))}
                </motion.div>

                <div className="flex items-center justify-between border-t border-border/70 px-4 py-3">
                  <p className="text-xs text-foreground-muted">
                    Toplam {profile.games.length} oyun profili · Sayfa {gamesPage} / {gamesTotalPages}
                  </p>
                  <div className="flex items-center gap-2">
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={() => setGamesPage((prev) => Math.max(1, prev - 1))}
                      disabled={gamesPage <= 1}
                    >
                      Önceki
                    </Button>
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={() => setGamesPage((prev) => Math.min(gamesTotalPages, prev + 1))}
                      disabled={gamesPage >= gamesTotalPages}
                    >
                      Sonraki
                    </Button>
                  </div>
                </div>
              </>
            ) : (
              <div className="p-4">
                <Card className="text-center">
                  <div className="py-8">
                    <div className="text-4xl">🎮</div>
                    <p className="mt-2 text-foreground-muted">Henüz oyun profili yok</p>
                    {profile.isOwnProfile && (
                      <Link to="/profile/games" className="mt-3 inline-block">
                        <Button variant="outline" size="sm">
                          Oyun Profili Ekle
                        </Button>
                      </Link>
                    )}
                  </div>
                </Card>
              </div>
            )}
          </Card>
        </motion.div>

        <motion.div
          initial={{ opacity: 0, y: 20 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ delay: 0.4 }}
        >
          <Card className="overflow-hidden border-primary/20 bg-linear-to-br from-surface via-surface to-primary/5">
            <div className="flex items-center justify-between border-b border-border/70 p-4">
              <div>
                <h2 className="text-xl font-bold text-foreground">
                  {profile.isOwnProfile ? 'Açtığım Odalar' : 'Açtığı Odalar'}
                </h2>
                <p className="text-xs text-foreground-muted">
                  Oda geçmişi sayfalı gösterilir, profil sonsuza uzamaz.
                </p>
              </div>
              {profile.isOwnProfile && (
                <Link to="/rooms">
                  <Button variant="outline" size="sm">
                    Tüm Odalar
                  </Button>
                </Link>
              )}
            </div>

            {createdRooms && createdRooms.items.length > 0 ? (
              <>
                <motion.div
                  variants={staggerContainer}
                  initial="hidden"
                  animate="show"
                  className="grid max-h-112 gap-3 overflow-y-auto p-4 sm:grid-cols-2"
                >
                  {createdRooms.items.map((room) => (
                    <motion.div key={room.id} variants={staggerItem}>
                      <Link to={`/rooms/${room.id}`}>
                        <Card className="cursor-pointer border-border/80 p-4 transition-colors hover:border-primary/40">
                          <div className="flex items-start gap-3">
                            {room.gameImageUrl ? (
                              <img
                                src={room.gameImageUrl}
                                alt={room.gameName ?? ''}
                                className="h-12 w-16 shrink-0 rounded object-cover"
                              />
                            ) : (
                              <div className="flex h-12 w-16 shrink-0 items-center justify-center rounded bg-surface-hover text-xl">
                                🎮
                              </div>
                            )}
                            <div className="min-w-0 flex-1">
                              <p className="truncate font-semibold text-foreground">{room.title}</p>
                              <p className="text-xs text-foreground-muted">{room.gameName}</p>
                              <div className="mt-1 flex items-center gap-2 text-xs text-foreground-subtle">
                                <span>{room.currentMemberCount}/{room.maxMembers} üye</span>
                                <span>-</span>
                                <Badge
                                  variant={room.status === 'Open' ? 'success' : room.status === 'Full' ? 'warning' : 'default'}
                                  size="sm"
                                >
                                  {room.status === 'Open' ? 'Açık' : room.status === 'Full' ? 'Dolu' : room.status === 'Closed' ? 'Kapalı' : room.status}
                                </Badge>
                              </div>
                            </div>
                          </div>
                        </Card>
                      </Link>
                    </motion.div>
                  ))}
                </motion.div>

                <div className="flex items-center justify-between border-t border-border/70 px-4 py-3">
                  <p className="text-xs text-foreground-muted">
                    Toplam {createdRooms.totalCount} oda · Sayfa {createdRoomsPage} / {createdRoomsTotalPages}
                  </p>
                  <div className="flex items-center gap-2">
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={() => setCreatedRoomsPage((prev) => Math.max(1, prev - 1))}
                      disabled={createdRoomsPage <= 1}
                    >
                      Önceki
                    </Button>
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={() => setCreatedRoomsPage((prev) => Math.min(createdRoomsTotalPages, prev + 1))}
                      disabled={createdRoomsPage >= createdRoomsTotalPages}
                    >
                      Sonraki
                    </Button>
                  </div>
                </div>
              </>
            ) : (
              <div className="p-4">
                <Card className="text-center">
                  <div className="py-8">
                    <div className="text-4xl">🏠</div>
                    <p className="mt-2 text-foreground-muted">
                      {profile.isOwnProfile ? 'Henüz oda açmadın' : 'Henüz oda açmamış'}
                    </p>
                    {profile.isOwnProfile && (
                      <Link to="/rooms" className="mt-3 inline-block">
                        <Button variant="outline" size="sm">
                          Oda Oluştur
                        </Button>
                      </Link>
                    )}
                  </div>
                </Card>
              </div>
            )}
          </Card>
        </motion.div>
      </div>
    </AnimatedPage>
  );
}

const tierColors: Record<string, string> = {
  Bronze: '#b45309',
  Silver: '#9ca3af',
  Gold: '#eab308',
  Platinum: '#14b8a6',
  Diamond: '#3b82f6',
  Master: '#a855f7',
  Grandmaster: '#ef4444',
};

function GameProfileCard({ gp, userId }: { gp: import('@/features/profile/types').GameProfileResponse; userId: string }) {
  const [showChart, setShowChart] = useState(false);
  const { data: eloData, isLoading: eloLoading } = useEloHistory(
    showChart ? userId : '',
    showChart ? gp.gameId : '',
  );

  return (
    <motion.div variants={staggerItem}>
      <Card className="group hover:border-primary/30 transition-colors">
        <div className="flex items-center gap-3">
          {gp.gameImageUrl ? (
            <img src={gp.gameImageUrl} alt={gp.gameName} className="w-12 h-12 rounded object-cover" />
          ) : (
            <div className="w-12 h-12 rounded bg-border flex items-center justify-center text-foreground-muted">🎮</div>
          )}
          <div className="flex-1 min-w-0">
            <h3 className="font-semibold text-foreground truncate">{gp.gameName}</h3>
            {gp.inGameName && <p className="text-sm text-foreground-muted">IGN: {gp.inGameName}</p>}
          </div>
          {gp.lookingForTeam && <Badge variant="accent">LFT</Badge>}
        </div>

        <div className="mt-3 flex items-center gap-3">
          <RankBadge tier={gp.rankTier} eloPoints={gp.eloPoints} />
          <button
            onClick={() => setShowChart(prev => !prev)}
            className="text-xs text-indigo-400 hover:text-indigo-300 transition-colors underline underline-offset-2"
          >
            {showChart ? 'Hide Chart' : 'Show Elo History'}
          </button>
        </div>

        {showChart && (
          <div className="mt-3 rounded-lg bg-gray-800/30 p-3 border border-gray-700/30">
            {eloLoading ? (
              <div className="flex justify-center py-8"><Spinner size="sm" /></div>
            ) : eloData?.history && eloData.history.length > 0 ? (
              <EloChart
                history={eloData.history}
                tierColor={tierColors[gp.rankTier] ?? '#6366f1'}
                height={180}
              />
            ) : (
              <p className="text-center text-sm text-gray-500 py-4">No elo history available</p>
            )}
          </div>
        )}

        <div className="mt-3 flex flex-wrap gap-2">
          <Badge>{gp.experienceLevel}</Badge>
          <Badge>{gp.region}</Badge>
          <Badge>{gp.communicationPreference}</Badge>
          {gp.hoursPlayed != null && <Badge>{gp.hoursPlayed}h</Badge>}
        </div>
        {gp.hoursPlayed != null && (
          <div className="mt-3">
            <ProgressBar value={Math.min(gp.hoursPlayed, 2000)} max={2000} variant="accent" size="sm" />
            <p className="mt-1 text-xs text-foreground-subtle">{gp.hoursPlayed} / 2000h milestone</p>
          </div>
        )}
        {gp.note && <p className="mt-2 text-xs text-foreground-muted italic">{gp.note}</p>}
      </Card>
    </motion.div>
  );
}

function FriendActionButton({
  friendshipStatus,
  isFriendRequestSentByMe,
  onSendRequest,
  onAcceptRequest,
  onRemoveFriend,
  isPending,
}: {
  friendshipStatus: string | null;
  isFriendRequestSentByMe: boolean;
  onSendRequest: () => void;
  onAcceptRequest: () => void;
  onRemoveFriend: () => void;
  isPending: boolean;
}) {
  if (friendshipStatus === 'Accepted') {
    return (
      <div className="flex gap-1.5">
        <Button variant="outline" size="sm" className="gap-1.5 border-green-500/30 text-green-600" disabled>
          <svg className="h-3.5 w-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth={2}>
            <path strokeLinecap="round" strokeLinejoin="round" d="M4.5 12.75l6 6 9-13.5" />
          </svg>
          Friends
        </Button>
        <Button
          variant="ghost"
          size="sm"
          onClick={onRemoveFriend}
          isLoading={isPending}
          className="text-foreground-muted hover:text-danger"
        >
          Remove
        </Button>
      </div>
    );
  }

  if (friendshipStatus === 'Pending') {
    if (!isFriendRequestSentByMe) {
      return (
        <Button size="sm" onClick={onAcceptRequest} isLoading={isPending} className="gap-1.5">
          <svg className="h-3.5 w-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth={2}>
            <path strokeLinecap="round" strokeLinejoin="round" d="M4.5 12.75l6 6 9-13.5" />
          </svg>
          Accept Request
        </Button>
      );
    }

    return (
      <Button variant="outline" size="sm" disabled className="gap-1.5">
        <svg className="h-3.5 w-3.5 animate-pulse" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth={2}>
          <path strokeLinecap="round" strokeLinejoin="round" d="M12 6v6h4.5m4.5 0a9 9 0 11-18 0 9 9 0 0118 0z" />
        </svg>
        Request Sent
      </Button>
    );
  }

  return (
    <Button variant="outline" size="sm" onClick={onSendRequest} isLoading={isPending} className="gap-1.5">
      <svg className="h-3.5 w-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth={2}>
        <path strokeLinecap="round" strokeLinejoin="round" d="M19 7.5v3m0 0v3m0-3h3m-3 0h-3m-2.25-4.125a3.375 3.375 0 11-6.75 0 3.375 3.375 0 016.75 0zM4 19.235v-.11a6.375 6.375 0 0112.75 0v.109A12.318 12.318 0 0110.374 21c-2.331 0-4.512-.645-6.374-1.766z" />
      </svg>
      Add Friend
    </Button>
  );
}
