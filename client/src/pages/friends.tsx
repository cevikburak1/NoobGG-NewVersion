import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { motion, AnimatePresence } from 'framer-motion';
import {
  useFriends,
  usePendingRequests,
  useAcceptFriendRequest,
  useRejectFriendRequest,
  useRemoveFriend,
} from '@/features/friends/hooks';
import { Button, Badge, AnimatedPage, Spinner, staggerContainer, staggerItem } from '@/components/ui';
import { UserAvatar } from '@/components/common/userAvatar';
import { useToast } from '@/components/ui/toast';

export default function FriendsPage() {
  const [activeTab, setActiveTab] = useState<'friends' | 'requests'>('friends');
  const { data: friends, isLoading: friendsLoading } = useFriends();
  const { data: pendingRequests, isLoading: requestsLoading } = usePendingRequests();
  const acceptRequest = useAcceptFriendRequest();
  const rejectRequest = useRejectFriendRequest();
  const removeFriend = useRemoveFriend();
  const navigate = useNavigate();
  const { addToast } = useToast();

  const incomingCount = pendingRequests?.incoming.length ?? 0;

  const handleAccept = async (friendshipId: string) => {
    try {
      await acceptRequest.mutateAsync(friendshipId);
      addToast({ title: 'Friend request accepted', type: 'success' });
    } catch {
      addToast({ title: 'Failed to accept request', type: 'error' });
    }
  };

  const handleReject = async (friendshipId: string) => {
    try {
      await rejectRequest.mutateAsync(friendshipId);
      addToast({ title: 'Friend request declined', type: 'info' });
    } catch {
      addToast({ title: 'Failed to decline request', type: 'error' });
    }
  };

  const handleRemove = async (userId: string) => {
    try {
      await removeFriend.mutateAsync(userId);
      addToast({ title: 'Friend removed', type: 'info' });
    } catch {
      addToast({ title: 'Failed to remove friend', type: 'error' });
    }
  };

  return (
    <AnimatedPage>
      <div className="space-y-6">
        <div>
          <h1 className="text-2xl font-bold text-foreground">Friends</h1>
          <p className="mt-1 text-sm text-foreground-muted">Manage your friends and requests</p>
        </div>

        <div className="flex items-center gap-3">
          <div className="flex gap-1 rounded-xl border border-border bg-surface p-1">
            {([
              { key: 'friends' as const, label: 'Friends', count: friends?.length ?? 0 },
              { key: 'requests' as const, label: 'Requests', count: incomingCount },
            ]).map((tab) => (
              <button
                key={tab.key}
                onClick={() => setActiveTab(tab.key)}
                className={`relative flex items-center gap-2 rounded-lg px-5 py-2.5 text-sm font-medium transition-colors ${
                  activeTab === tab.key ? 'text-foreground' : 'text-foreground-muted hover:text-foreground'
                }`}
              >
                {activeTab === tab.key && (
                  <motion.div
                    layoutId="friendsTab"
                    className="absolute inset-0 rounded-lg bg-surface-hover"
                    transition={{ type: 'spring', bounce: 0.2, duration: 0.4 }}
                  />
                )}
                <span className="relative z-10">{tab.label}</span>
                {tab.count > 0 && (
                  <span className="relative z-10 flex h-5 min-w-[20px] items-center justify-center rounded-full bg-primary px-1.5 text-[10px] font-bold text-primary-foreground">
                    {tab.count}
                  </span>
                )}
              </button>
            ))}
          </div>
        </div>

        <AnimatePresence mode="wait">
          {activeTab === 'friends' ? (
            <motion.div key="friends" initial={{ opacity: 0 }} animate={{ opacity: 1 }} exit={{ opacity: 0 }}>
              {friendsLoading ? (
                <div className="flex justify-center py-20"><Spinner size="lg" /></div>
              ) : friends && friends.length > 0 ? (
                <motion.div variants={staggerContainer} initial="hidden" animate="show" className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
                  {friends.map((friend) => (
                    <motion.div key={friend.id} variants={staggerItem}>
                      <div className="group flex items-center gap-4 rounded-xl border border-border bg-surface p-4 transition-colors hover:border-primary/30">
                        <Link to={`/profile/${friend.userId}`}>
                          <UserAvatar username={friend.username} avatarUrl={friend.avatarUrl} size="md" />
                        </Link>
                        <div className="min-w-0 flex-1">
                          <Link to={`/profile/${friend.userId}`} className="block">
                            <h3 className="truncate font-semibold text-foreground hover:text-primary transition-colors">
                              {friend.username}
                            </h3>
                          </Link>
                          <p className="text-xs text-foreground-muted">
                            Friends since {new Date(friend.respondedAt ?? friend.createdAt).toLocaleDateString()}
                          </p>
                        </div>
                        <div className="flex gap-2">
                          <Button
                            variant="outline"
                            size="sm"
                            onClick={() => navigate(`/messages?user=${friend.userId}`)}
                          >
                            Message
                          </Button>
                          <Button
                            variant="ghost"
                            size="sm"
                            onClick={() => handleRemove(friend.userId)}
                            isLoading={removeFriend.isPending}
                            className="text-danger hover:text-danger"
                          >
                            Remove
                          </Button>
                        </div>
                      </div>
                    </motion.div>
                  ))}
                </motion.div>
              ) : (
                <EmptyState
                  icon="👥"
                  title="No friends yet"
                  subtitle="Visit the Discover page to find players and send friend requests"
                  action={
                    <Link to="/discover" className="mt-4 inline-block">
                      <Button variant="outline">Discover Players</Button>
                    </Link>
                  }
                />
              )}
            </motion.div>
          ) : (
            <motion.div key="requests" initial={{ opacity: 0 }} animate={{ opacity: 1 }} exit={{ opacity: 0 }} className="space-y-6">
              {requestsLoading ? (
                <div className="flex justify-center py-20"><Spinner size="lg" /></div>
              ) : (
                <>
                  {(pendingRequests?.incoming.length ?? 0) > 0 && (
                    <div>
                      <h3 className="mb-3 text-sm font-semibold uppercase tracking-wider text-foreground-subtle">
                        Incoming Requests
                      </h3>
                      <motion.div variants={staggerContainer} initial="hidden" animate="show" className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
                        {pendingRequests!.incoming.map((req) => (
                          <motion.div key={req.friendshipId} variants={staggerItem}>
                            <div className="flex items-center gap-4 rounded-xl border border-border bg-surface p-4">
                              <Link to={`/profile/${req.userId}`}>
                                <UserAvatar username={req.username} avatarUrl={req.avatarUrl} size="md" />
                              </Link>
                              <div className="min-w-0 flex-1">
                                <Link to={`/profile/${req.userId}`}>
                                  <h3 className="truncate font-semibold text-foreground hover:text-primary transition-colors">
                                    {req.username}
                                  </h3>
                                </Link>
                                <p className="text-xs text-foreground-muted">
                                  {new Date(req.createdAt).toLocaleDateString()}
                                </p>
                              </div>
                              <div className="flex gap-2">
                                <Button
                                  size="sm"
                                  onClick={() => handleAccept(req.friendshipId)}
                                  isLoading={acceptRequest.isPending}
                                >
                                  Accept
                                </Button>
                                <Button
                                  variant="outline"
                                  size="sm"
                                  onClick={() => handleReject(req.friendshipId)}
                                  isLoading={rejectRequest.isPending}
                                >
                                  Decline
                                </Button>
                              </div>
                            </div>
                          </motion.div>
                        ))}
                      </motion.div>
                    </div>
                  )}

                  {(pendingRequests?.outgoing.length ?? 0) > 0 && (
                    <div>
                      <h3 className="mb-3 text-sm font-semibold uppercase tracking-wider text-foreground-subtle">
                        Sent Requests
                      </h3>
                      <motion.div variants={staggerContainer} initial="hidden" animate="show" className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
                        {pendingRequests!.outgoing.map((req) => (
                          <motion.div key={req.friendshipId} variants={staggerItem}>
                            <div className="flex items-center gap-4 rounded-xl border border-border bg-surface p-4">
                              <Link to={`/profile/${req.userId}`}>
                                <UserAvatar username={req.username} avatarUrl={req.avatarUrl} size="md" />
                              </Link>
                              <div className="min-w-0 flex-1">
                                <Link to={`/profile/${req.userId}`}>
                                  <h3 className="truncate font-semibold text-foreground hover:text-primary transition-colors">
                                    {req.username}
                                  </h3>
                                </Link>
                                <p className="text-xs text-foreground-muted">
                                  Sent {new Date(req.createdAt).toLocaleDateString()}
                                </p>
                              </div>
                              <Badge>Pending</Badge>
                              <Button
                                variant="outline"
                                size="sm"
                                onClick={() => handleReject(req.friendshipId)}
                                isLoading={rejectRequest.isPending}
                              >
                                Cancel
                              </Button>
                            </div>
                          </motion.div>
                        ))}
                      </motion.div>
                    </div>
                  )}

                  {(pendingRequests?.incoming.length ?? 0) === 0 && (pendingRequests?.outgoing.length ?? 0) === 0 && (
                    <EmptyState
                      icon="📬"
                      title="No pending requests"
                      subtitle="You don't have any friend requests at the moment"
                    />
                  )}
                </>
              )}
            </motion.div>
          )}
        </AnimatePresence>
      </div>
    </AnimatedPage>
  );
}

function EmptyState({ icon, title, subtitle, action }: { icon: string; title: string; subtitle: string; action?: React.ReactNode }) {
  return (
    <motion.div
      initial={{ opacity: 0, scale: 0.95 }}
      animate={{ opacity: 1, scale: 1 }}
      className="flex flex-col items-center py-20 text-center"
    >
      <motion.div animate={{ y: [0, -8, 0] }} transition={{ duration: 2, repeat: Infinity }} className="text-5xl">
        {icon}
      </motion.div>
      <h3 className="mt-4 text-lg font-bold text-foreground">{title}</h3>
      <p className="mt-1.5 text-sm text-foreground-muted">{subtitle}</p>
      {action}
    </motion.div>
  );
}
