import { createBrowserRouter, Navigate, type RouteObject } from 'react-router-dom';
import { AppLayout } from '@/components/layout/appLayout';
import { AuthLayout } from '@/components/layout/authLayout';
import { ProtectedRoute } from './protectedRoute';
import { RequireProfile } from './requireProfile';
import { RequireRole } from './requireRole';

import LandingPage from '@/pages/landing';
import LoginPage from '@/pages/login';
import RegisterPage from '@/pages/register';
import DiscoverPage from '@/pages/discover';
import ProfilePage from '@/pages/profile';
import EditProfilePage from '@/pages/editProfile';
import GameProfilesPage from '@/pages/gameProfiles';
import GameDetailPage from '@/pages/gameDetail';
import RoomListPage from '@/pages/roomList';
import RoomDetailPage from '@/pages/roomDetail';
import SubscriptionsPage from '@/pages/subscriptions';
import NotificationsPage from '@/pages/notifications';
import SettingsPage from '@/pages/settings';
import ModerationPage from '@/pages/moderation';
import VerifyEmailPage from '@/pages/verifyEmail';
import OnboardingPage from '@/pages/onboarding';
import MessagesPage from '@/pages/messages';
import FriendsPage from '@/pages/friends';
import FavoritesPage from '@/pages/favorites';
import GuildListPage from '@/pages/guildList';
import GuildDetailPage from '@/pages/guildDetail';
import GuildStatsPage from '@/pages/guildStats';
import LeaderboardPage from '@/pages/leaderboard';
import ComparePlayersPage from '@/pages/comparePlayers';
import TournamentListPage from '@/pages/tournamentList';
import TournamentDetailPage from '@/pages/tournamentDetail';
import GuideDetailPage from '@/components/guides/guideDetailPage';
import CommunityHomePage from '@/pages/communityHome';
import CommunityBoardPage from '@/pages/communityBoard';
import CommunityTopicDetailPage from '@/pages/communityTopicDetail';

const routes: RouteObject[] = [
  {
    path: '/',
    element: <LandingPage />,
  },
  {
    element: <AuthLayout />,
    children: [
      { path: '/login', element: <LoginPage /> },
      { path: '/register', element: <RegisterPage /> },
    ],
  },
  {
    path: '/verify-email',
    element: <VerifyEmailPage />,
  },
  {
    path: '/onboarding',
    element: (
      <ProtectedRoute>
        <OnboardingPage />
      </ProtectedRoute>
    ),
  },
  {
    element: <AppLayout />,
    children: [
      {
        path: '/rooms',
        element: (
          <ProtectedRoute>
            <RequireProfile>
              <RoomListPage />
            </RequireProfile>
          </ProtectedRoute>
        ),
      },
      {
        path: '/guilds',
        element: (
          <ProtectedRoute>
            <RequireProfile>
              <GuildListPage />
            </RequireProfile>
          </ProtectedRoute>
        ),
      },
      {
        path: '/guilds/:guildId',
        element: (
          <ProtectedRoute>
            <RequireProfile>
              <GuildDetailPage />
            </RequireProfile>
          </ProtectedRoute>
        ),
      },
      {
        path: '/guilds/:guildId/stats',
        element: (
          <ProtectedRoute>
            <RequireProfile>
              <GuildStatsPage />
            </RequireProfile>
          </ProtectedRoute>
        ),
      },
      {
        path: '/rooms/:roomId',
        element: (
          <ProtectedRoute>
            <RequireProfile>
              <RoomDetailPage />
            </RequireProfile>
          </ProtectedRoute>
        ),
      },
      { path: '/subscriptions', element: <SubscriptionsPage /> },
      { path: '/leaderboard', element: <LeaderboardPage /> },
      {
        path: '/games/:gameId',
        element: (
          <ProtectedRoute>
            <RequireProfile>
              <GameDetailPage />
            </RequireProfile>
          </ProtectedRoute>
        ),
      },
      {
        path: '/discover',
        element: (
          <ProtectedRoute>
            <RequireProfile>
              <DiscoverPage />
            </RequireProfile>
          </ProtectedRoute>
        ),
      },
      {
        path: '/community',
        element: (
          <ProtectedRoute>
            <RequireProfile>
              <CommunityHomePage />
            </RequireProfile>
          </ProtectedRoute>
        ),
      },
      {
        path: '/community/boards/:boardSlug',
        element: (
          <ProtectedRoute>
            <RequireProfile>
              <CommunityBoardPage />
            </RequireProfile>
          </ProtectedRoute>
        ),
      },
      {
        path: '/community/topics/:topicId',
        element: (
          <ProtectedRoute>
            <RequireProfile>
              <CommunityTopicDetailPage />
            </RequireProfile>
          </ProtectedRoute>
        ),
      },
      {
        path: '/compare',
        element: (
          <ProtectedRoute>
            <RequireProfile>
              <ComparePlayersPage />
            </RequireProfile>
          </ProtectedRoute>
        ),
      },
      {
        path: '/tournaments',
        element: (
          <ProtectedRoute>
            <RequireProfile>
              <TournamentListPage />
            </RequireProfile>
          </ProtectedRoute>
        ),
      },
      {
        path: '/guides/:guideId',
        element: (
          <ProtectedRoute>
            <RequireProfile>
              <GuideDetailPage />
            </RequireProfile>
          </ProtectedRoute>
        ),
      },
      {
        path: '/tournaments/:tournamentId',
        element: (
          <ProtectedRoute>
            <RequireProfile>
              <TournamentDetailPage />
            </RequireProfile>
          </ProtectedRoute>
        ),
      },
      {
        path: '/profile/:userId',
        element: (
          <ProtectedRoute>
            <RequireProfile>
              <ProfilePage />
            </RequireProfile>
          </ProtectedRoute>
        ),
      },
      {
        path: '/profile/edit',
        element: (
          <ProtectedRoute>
            <RequireProfile>
              <EditProfilePage />
            </RequireProfile>
          </ProtectedRoute>
        ),
      },
      {
        path: '/profile/games',
        element: (
          <ProtectedRoute>
            <RequireProfile>
              <GameProfilesPage />
            </RequireProfile>
          </ProtectedRoute>
        ),
      },
      {
        path: '/messages',
        element: (
          <ProtectedRoute>
            <RequireProfile>
              <MessagesPage />
            </RequireProfile>
          </ProtectedRoute>
        ),
      },
      {
        path: '/friends',
        element: (
          <ProtectedRoute>
            <RequireProfile>
              <FriendsPage />
            </RequireProfile>
          </ProtectedRoute>
        ),
      },
      {
        path: '/favorites',
        element: (
          <ProtectedRoute>
            <RequireProfile>
              <FavoritesPage />
            </RequireProfile>
          </ProtectedRoute>
        ),
      },
      {
        path: '/notifications',
        element: (
          <ProtectedRoute>
            <RequireProfile>
              <NotificationsPage />
            </RequireProfile>
          </ProtectedRoute>
        ),
      },
      {
        path: '/settings',
        element: (
          <ProtectedRoute>
            <RequireProfile>
              <SettingsPage />
            </RequireProfile>
          </ProtectedRoute>
        ),
      },
      {
        path: '/moderation',
        element: (
          <RequireRole roles={['Moderator', 'Admin']}>
            <RequireProfile>
              <ModerationPage />
            </RequireProfile>
          </RequireRole>
        ),
      },
    ],
  },
  { path: '*', element: <Navigate to="/" replace /> },
];

export const router = createBrowserRouter(routes);
