import { useState } from 'react';
import { AnimatedPage } from '@/components/ui/animatedPage';
import { Tabs } from '@/components/ui/tabs';
import { Spinner } from '@/components/ui/spinner';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Toggle } from '@/components/ui/toggle';
import { Select } from '@/components/ui/select';
import { Modal } from '@/components/ui/modal';
import { EmptyState } from '@/components/common/emptyState';
import { UserAvatar } from '@/components/common/userAvatar';
import { useToast } from '@/components/ui/toast';
import { useAuthStore } from '@/stores/authStore';
import {
  useSettings,
  useUpdatePrivacy,
  useUpdateNotifications,
  useDeactivateAccount,
  useReactivateAccount,
  useRequestDeletion,
} from '@/features/settings/hooks';
import { useBlockedUsers, useUnblockUser } from '@/features/blocks/hooks';
import type { UserSettingsResponse } from '@/features/settings/types';
import type { DmPermission, ProfileVisibility } from '@/types/enums';

export default function SettingsPage() {
  const { data: settings, isLoading, isError, refetch } = useSettings();

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-24">
        <Spinner size="lg" />
      </div>
    );
  }

  if (isError || !settings) {
    return (
      <div className="mx-auto max-w-3xl py-12 text-center">
        <p className="text-foreground-muted">Failed to load settings.</p>
        <Button variant="outline" className="mt-4" onClick={() => refetch()}>
          Try again
        </Button>
      </div>
    );
  }

  return (
    <AnimatedPage>
      <div className="mx-auto max-w-3xl space-y-6">
        <div>
          <h1 className="text-2xl font-bold text-foreground">Settings</h1>
          <p className="mt-1 text-sm text-foreground-muted">
            Manage your privacy, notifications, and account preferences.
          </p>
        </div>

        <Tabs
          tabs={[
            { id: 'privacy', label: 'Privacy', content: <PrivacySection settings={settings} /> },
            {
              id: 'notifications',
              label: 'Notifications',
              content: <NotificationsSection settings={settings} />,
            },
            { id: 'blocked', label: 'Blocked Users', content: <BlockedUsersSection /> },
            { id: 'account', label: 'Account', content: <AccountSection settings={settings} /> },
          ]}
        />
      </div>
    </AnimatedPage>
  );
}

const VISIBILITY_OPTIONS = [
  { value: 'Public', label: 'Public' },
  { value: 'FriendsOnly', label: 'Friends Only' },
  { value: 'Private', label: 'Private' },
];

const DM_OPTIONS = [
  { value: 'Everyone', label: 'Everyone' },
  { value: 'FriendsOnly', label: 'Friends Only' },
  { value: 'Nobody', label: 'Nobody' },
];

function PrivacySection({ settings }: { settings: UserSettingsResponse }) {
  const [profileVisibility, setProfileVisibility] = useState<ProfileVisibility>(
    settings.profileVisibility,
  );
  const [dmPermission, setDmPermission] = useState<DmPermission>(settings.dmPermission);
  const [showOnlineStatus, setShowOnlineStatus] = useState(settings.showOnlineStatus);
  const [defaultLookingForTeam, setDefaultLookingForTeam] = useState(
    settings.defaultLookingForTeam,
  );

  const updatePrivacy = useUpdatePrivacy();
  const { addToast } = useToast();

  const hasChanges =
    profileVisibility !== settings.profileVisibility ||
    dmPermission !== settings.dmPermission ||
    showOnlineStatus !== settings.showOnlineStatus ||
    defaultLookingForTeam !== settings.defaultLookingForTeam;

  const handleSave = () => {
    updatePrivacy.mutate(
      { profileVisibility, dmPermission, showOnlineStatus, defaultLookingForTeam },
      {
        onSuccess: () => addToast({ title: 'Saved', message: 'Privacy settings updated.', type: 'success' }),
        onError: () => addToast({ title: 'Error', message: 'Failed to update privacy settings.', type: 'error' }),
      },
    );
  };

  return (
    <div className="space-y-4">
      <Card>
        <CardHeader>
          <CardTitle>Profile Visibility</CardTitle>
        </CardHeader>
        <CardContent>
          <p className="mb-3 text-foreground-muted">
            Control who can see your full profile information.
          </p>
          <Select
            options={VISIBILITY_OPTIONS}
            value={profileVisibility}
            onChange={(e) => setProfileVisibility(e.target.value as ProfileVisibility)}
          />
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Direct Messages</CardTitle>
        </CardHeader>
        <CardContent>
          <p className="mb-3 text-foreground-muted">Choose who can send you direct messages.</p>
          <Select
            options={DM_OPTIONS}
            value={dmPermission}
            onChange={(e) => setDmPermission(e.target.value as DmPermission)}
          />
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Online Status</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm font-medium text-foreground">Show online status</p>
              <p className="text-xs text-foreground-muted">
                Let other users see when you are online.
              </p>
            </div>
            <Toggle checked={showOnlineStatus} onChange={setShowOnlineStatus} />
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Looking for Team</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm font-medium text-foreground">Default LFT preference</p>
              <p className="text-xs text-foreground-muted">
                Appear in "Looking for Team" listings by default.
              </p>
            </div>
            <Toggle checked={defaultLookingForTeam} onChange={setDefaultLookingForTeam} />
          </div>
        </CardContent>
      </Card>

      <div className="flex justify-end">
        <Button onClick={handleSave} disabled={!hasChanges} isLoading={updatePrivacy.isPending}>
          Save Changes
        </Button>
      </div>
    </div>
  );
}

function NotificationsSection({ settings }: { settings: UserSettingsResponse }) {
  const [notifyFriendRequests, setNotifyFriendRequests] = useState(settings.notifyFriendRequests);
  const [notifyDirectMessages, setNotifyDirectMessages] = useState(settings.notifyDirectMessages);
  const [notifyRoomActivity, setNotifyRoomActivity] = useState(settings.notifyRoomActivity);
  const [notifySystemMessages, setNotifySystemMessages] = useState(settings.notifySystemMessages);

  const updateNotifications = useUpdateNotifications();
  const { addToast } = useToast();

  const hasChanges =
    notifyFriendRequests !== settings.notifyFriendRequests ||
    notifyDirectMessages !== settings.notifyDirectMessages ||
    notifyRoomActivity !== settings.notifyRoomActivity ||
    notifySystemMessages !== settings.notifySystemMessages;

  const handleSave = () => {
    updateNotifications.mutate(
      { notifyFriendRequests, notifyDirectMessages, notifyRoomActivity, notifySystemMessages },
      {
        onSuccess: () =>
          addToast({ title: 'Saved', message: 'Notification preferences updated.', type: 'success' }),
        onError: () =>
          addToast({ title: 'Error', message: 'Failed to update notification preferences.', type: 'error' }),
      },
    );
  };

  const NOTIFICATION_TOGGLES = [
    {
      label: 'Friend Requests',
      description: 'Receive notifications when someone sends you a friend request.',
      checked: notifyFriendRequests,
      onChange: setNotifyFriendRequests,
    },
    {
      label: 'Direct Messages',
      description: 'Receive notifications for new direct messages.',
      checked: notifyDirectMessages,
      onChange: setNotifyDirectMessages,
    },
    {
      label: 'Room Activity',
      description: 'Receive notifications about room events (joins, closures, invites).',
      checked: notifyRoomActivity,
      onChange: setNotifyRoomActivity,
    },
    {
      label: 'System Messages',
      description: 'Receive system-wide announcements and updates.',
      checked: notifySystemMessages,
      onChange: setNotifySystemMessages,
    },
  ];

  return (
    <div className="space-y-4">
      <Card>
        <CardHeader>
          <CardTitle>In-App Notifications</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="divide-y divide-border">
            {NOTIFICATION_TOGGLES.map((item) => (
              <div key={item.label} className="flex items-center justify-between py-3 first:pt-0 last:pb-0">
                <div>
                  <p className="text-sm font-medium text-foreground">{item.label}</p>
                  <p className="text-xs text-foreground-muted">{item.description}</p>
                </div>
                <Toggle checked={item.checked} onChange={item.onChange} />
              </div>
            ))}
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Email & Push</CardTitle>
        </CardHeader>
        <CardContent>
          <p className="text-sm text-foreground-muted">
            Email and push notification preferences are coming soon.
          </p>
        </CardContent>
      </Card>

      <div className="flex justify-end">
        <Button onClick={handleSave} disabled={!hasChanges} isLoading={updateNotifications.isPending}>
          Save Changes
        </Button>
      </div>
    </div>
  );
}

function BlockedUsersSection() {
  const { data: blockedUsers, isLoading, isError } = useBlockedUsers();
  const unblock = useUnblockUser();
  const { addToast } = useToast();

  const handleUnblock = (userId: string, username: string) => {
    unblock.mutate(userId, {
      onSuccess: () =>
        addToast({ title: 'Unblocked', message: `${username} has been unblocked.`, type: 'success' }),
      onError: () =>
        addToast({ title: 'Error', message: 'Failed to unblock user.', type: 'error' }),
    });
  };

  if (isLoading) {
    return (
      <div className="flex justify-center py-12">
        <Spinner />
      </div>
    );
  }

  if (isError) {
    return (
      <div className="py-8 text-center text-sm text-foreground-muted">
        Failed to load blocked users.
      </div>
    );
  }

  if (!blockedUsers || blockedUsers.length === 0) {
    return (
      <EmptyState
        title="No blocked users"
        description="You haven't blocked anyone. Users you block will appear here."
        icon={<ShieldIcon />}
      />
    );
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle>Blocked Users ({blockedUsers.length})</CardTitle>
      </CardHeader>
      <CardContent>
        <div className="divide-y divide-border">
          {blockedUsers.map((user) => (
            <div key={user.blockId} className="flex items-center justify-between py-3 first:pt-0 last:pb-0">
              <div className="flex items-center gap-3">
                <UserAvatar username={user.username} avatarUrl={user.avatarUrl} size="sm" />
                <div>
                  <p className="text-sm font-medium text-foreground">{user.username}</p>
                  <p className="text-xs text-foreground-muted">
                    Blocked {new Date(user.blockedAt).toLocaleDateString()}
                  </p>
                </div>
              </div>
              <Button
                variant="outline"
                size="sm"
                onClick={() => handleUnblock(user.userId, user.username)}
                isLoading={unblock.isPending}
              >
                Unblock
              </Button>
            </div>
          ))}
        </div>
      </CardContent>
    </Card>
  );
}

function AccountSection({ settings }: { settings: UserSettingsResponse }) {
  const user = useAuthStore((s) => s.user);
  const { addToast } = useToast();

  const [showDeactivateModal, setShowDeactivateModal] = useState(false);
  const [showDeleteModal, setShowDeleteModal] = useState(false);
  const [deactivateReason, setDeactivateReason] = useState('');
  const [deleteConfirmation, setDeleteConfirmation] = useState('');

  const deactivate = useDeactivateAccount();
  const reactivate = useReactivateAccount();
  const requestDeletion = useRequestDeletion();

  const handleDeactivate = () => {
    deactivate.mutate(
      { reason: deactivateReason || undefined },
      {
        onSuccess: () => {
          setShowDeactivateModal(false);
          setDeactivateReason('');
          addToast({ title: 'Account Deactivated', message: 'Your account has been deactivated.', type: 'success' });
        },
        onError: () =>
          addToast({ title: 'Error', message: 'Failed to deactivate account.', type: 'error' }),
      },
    );
  };

  const handleReactivate = () => {
    reactivate.mutate(undefined, {
      onSuccess: () =>
        addToast({ title: 'Account Reactivated', message: 'Your account is active again.', type: 'success' }),
      onError: () =>
        addToast({ title: 'Error', message: 'Failed to reactivate account.', type: 'error' }),
    });
  };

  const handleRequestDeletion = () => {
    requestDeletion.mutate(undefined, {
      onSuccess: () => {
        setShowDeleteModal(false);
        setDeleteConfirmation('');
        addToast({
          title: 'Deletion Requested',
          message: 'Your account deletion request has been submitted.',
          type: 'success',
        });
      },
      onError: () =>
        addToast({ title: 'Error', message: 'Failed to submit deletion request.', type: 'error' }),
    });
  };

  return (
    <div className="space-y-4">
      <Card>
        <CardHeader>
          <CardTitle>Account Information</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="space-y-2">
            <div className="flex justify-between">
              <span className="text-sm text-foreground-muted">Email</span>
              <span className="text-sm font-medium text-foreground">{user?.email ?? '—'}</span>
            </div>
            <div className="flex justify-between">
              <span className="text-sm text-foreground-muted">Username</span>
              <span className="text-sm font-medium text-foreground">{user?.username ?? '—'}</span>
            </div>
            <div className="flex justify-between">
              <span className="text-sm text-foreground-muted">Member since</span>
              <span className="text-sm font-medium text-foreground">
                {user?.createdAt ? new Date(user.createdAt).toLocaleDateString() : '—'}
              </span>
            </div>
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Deactivate Account</CardTitle>
        </CardHeader>
        <CardContent>
          <p className="mb-3 text-sm text-foreground-muted">
            Deactivating your account will hide your profile and pause all activity. You can
            reactivate at any time.
          </p>
          {settings.isDeactivated ? (
            <Button
              variant="secondary"
              onClick={handleReactivate}
              isLoading={reactivate.isPending}
            >
              Reactivate Account
            </Button>
          ) : (
            <Button variant="danger" onClick={() => setShowDeactivateModal(true)}>
              Deactivate Account
            </Button>
          )}
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Delete Account</CardTitle>
        </CardHeader>
        <CardContent>
          <p className="mb-3 text-sm text-foreground-muted">
            Request permanent deletion of your account and all associated data. This action cannot
            be undone once processed.
          </p>
          {settings.deletionRequestedAt ? (
            <p className="text-sm font-medium text-amber-400">
              Deletion requested on{' '}
              {new Date(settings.deletionRequestedAt).toLocaleDateString()}. Processing may take up
              to 30 days.
            </p>
          ) : (
            <Button variant="danger" onClick={() => setShowDeleteModal(true)}>
              Request Account Deletion
            </Button>
          )}
        </CardContent>
      </Card>

      <Modal
        isOpen={showDeactivateModal}
        onClose={() => setShowDeactivateModal(false)}
        title="Deactivate Account"
      >
        <p className="mb-4 text-sm text-foreground-muted">
          Your profile will be hidden and activity paused. You can reactivate any time by logging
          in and visiting settings.
        </p>
        <textarea
          className="mb-4 w-full rounded-md border border-border bg-surface px-3 py-2 text-sm text-foreground placeholder:text-foreground-subtle focus:outline-none focus:ring-2 focus:ring-primary/50"
          placeholder="Reason for deactivation (optional)"
          rows={3}
          value={deactivateReason}
          onChange={(e) => setDeactivateReason(e.target.value)}
        />
        <div className="flex justify-end gap-2">
          <Button variant="ghost" onClick={() => setShowDeactivateModal(false)}>
            Cancel
          </Button>
          <Button variant="danger" onClick={handleDeactivate} isLoading={deactivate.isPending}>
            Deactivate
          </Button>
        </div>
      </Modal>

      <Modal
        isOpen={showDeleteModal}
        onClose={() => setShowDeleteModal(false)}
        title="Delete Account"
      >
        <p className="mb-4 text-sm text-foreground-muted">
          This will permanently delete your account and all data. Type{' '}
          <span className="font-semibold text-danger">DELETE</span> to confirm.
        </p>
        <input
          className="mb-4 w-full rounded-md border border-border bg-surface px-3 py-2 text-sm text-foreground placeholder:text-foreground-subtle focus:outline-none focus:ring-2 focus:ring-primary/50"
          placeholder='Type "DELETE" to confirm'
          value={deleteConfirmation}
          onChange={(e) => setDeleteConfirmation(e.target.value)}
        />
        <div className="flex justify-end gap-2">
          <Button variant="ghost" onClick={() => setShowDeleteModal(false)}>
            Cancel
          </Button>
          <Button
            variant="danger"
            disabled={deleteConfirmation !== 'DELETE'}
            onClick={handleRequestDeletion}
            isLoading={requestDeletion.isPending}
          >
            Delete My Account
          </Button>
        </div>
      </Modal>
    </div>
  );
}

function ShieldIcon() {
  return (
    <svg className="h-12 w-12" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth={1}>
      <path
        strokeLinecap="round"
        strokeLinejoin="round"
        d="M9 12.75L11.25 15 15 9.75m-3-7.036A11.959 11.959 0 013.598 6 11.99 11.99 0 003 9.749c0 5.592 3.824 10.29 9 11.623 5.176-1.332 9-6.03 9-11.622 0-1.31-.21-2.571-.598-3.751h-.152c-3.196 0-6.1-1.248-8.25-3.285z"
      />
    </svg>
  );
}
