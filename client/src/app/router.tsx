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
import RoomListPage from '@/pages/roomList';
import RoomDetailPage from '@/pages/roomDetail';
import SubscriptionsPage from '@/pages/subscriptions';
import NotificationsPage from '@/pages/notifications';
import SettingsPage from '@/pages/settings';
import ModerationPage from '@/pages/moderation';
import VerifyEmailPage from '@/pages/verifyEmail';
import OnboardingPage from '@/pages/onboarding';
import MessagesPage from '@/pages/messages';

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
