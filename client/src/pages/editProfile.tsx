import { useEffect, useRef, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { motion } from 'framer-motion';
import { updateProfileSchema, type UpdateProfileFormData } from '@/features/profile/schemas';
import { useMyProfile, useUpdateProfile, useUploadAvatar, useUploadBanner, useRemoveBanner } from '@/features/profile/hooks';
import { useAuthStore } from '@/stores/authStore';
import { Button, Input, Textarea, AnimatedPage, Card, Spinner } from '@/components/ui';
import { UserAvatar } from '@/components/common/userAvatar';
import { useToast } from '@/components/ui/toast';
import { resolveFileUrl } from '@/lib/api';

const ALLOWED_TYPES = ['image/jpeg', 'image/png', 'image/webp'];
const MAX_AVATAR_SIZE = 2 * 1024 * 1024;
const MAX_BANNER_SIZE = 5 * 1024 * 1024;

const formStagger = {
  hidden: { opacity: 0 },
  show: { opacity: 1, transition: { staggerChildren: 0.06 } },
};

const formItem = {
  hidden: { opacity: 0, y: 15 },
  show: { opacity: 1, y: 0, transition: { duration: 0.3 } },
};

function validateImageFile(file: File, maxSize: number): string | null {
  if (!ALLOWED_TYPES.includes(file.type)) {
    return 'Only JPEG, PNG, and WebP images are allowed.';
  }
  if (file.size > maxSize) {
    const sizeMb = Math.round(maxSize / 1024 / 1024);
    return `File must be ${sizeMb} MB or smaller.`;
  }
  return null;
}

export default function EditProfilePage() {
  const navigate = useNavigate();
  const user = useAuthStore((s) => s.user);
  const setUser = useAuthStore((s) => s.setUser);
  const { data: profile, isLoading } = useMyProfile();
  const updateProfile = useUpdateProfile();
  const uploadAvatar = useUploadAvatar();
  const uploadBanner = useUploadBanner();
  const removeBannerMut = useRemoveBanner();
  const { addToast } = useToast();

  const avatarInputRef = useRef<HTMLInputElement>(null);
  const bannerInputRef = useRef<HTMLInputElement>(null);
  const [avatarPreview, setAvatarPreview] = useState<string | null>(null);
  const [bannerPreview, setBannerPreview] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isDirty },
  } = useForm<UpdateProfileFormData>({
    resolver: zodResolver(updateProfileSchema),
  });

  useEffect(() => {
    if (profile) {
      reset({
        displayName: profile.displayName ?? '',
        bio: profile.bio ?? '',
        country: profile.country ?? '',
        timezone: profile.timezone ?? '',
        weekdaysFrom: '',
        weekdaysTo: '',
        weekendsFrom: '',
        weekendsTo: '',
      });
    }
  }, [profile, reset]);

  const handleAvatarChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;
    e.target.value = '';

    const error = validateImageFile(file, MAX_AVATAR_SIZE);
    if (error) {
      addToast({ title: 'Invalid file', message: error, type: 'error' });
      return;
    }

    const previewUrl = URL.createObjectURL(file);
    setAvatarPreview(previewUrl);

    uploadAvatar.mutate(file, {
      onSuccess: (result) => {
        if (user) setUser({ ...user, avatarUrl: result.avatarUrl });
        addToast({ title: 'Avatar updated', message: 'Your avatar has been changed.', type: 'success' });
      },
      onError: () => {
        setAvatarPreview(null);
        addToast({ title: 'Upload failed', message: 'Could not update avatar. Please try again.', type: 'error' });
      },
      onSettled: () => {
        URL.revokeObjectURL(previewUrl);
      },
    });
  };

  const handleBannerChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;
    e.target.value = '';

    const error = validateImageFile(file, MAX_BANNER_SIZE);
    if (error) {
      addToast({ title: 'Invalid file', message: error, type: 'error' });
      return;
    }

    const previewUrl = URL.createObjectURL(file);
    setBannerPreview(previewUrl);

    uploadBanner.mutate(file, {
      onSuccess: () => {
        addToast({ title: 'Banner updated', message: 'Your banner has been changed.', type: 'success' });
      },
      onError: () => {
        setBannerPreview(null);
        addToast({ title: 'Upload failed', message: 'Could not update banner. Please try again.', type: 'error' });
      },
      onSettled: () => {
        URL.revokeObjectURL(previewUrl);
      },
    });
  };

  const handleRemoveBanner = () => {
    removeBannerMut.mutate(undefined, {
      onSuccess: () => {
        setBannerPreview(null);
        addToast({ title: 'Banner removed', message: 'Your profile banner has been removed.', type: 'info' });
      },
      onError: () => {
        addToast({ title: 'Failed', message: 'Could not remove banner.', type: 'error' });
      },
    });
  };

  const onSubmit = (data: UpdateProfileFormData) => {
    updateProfile.mutate(data, {
      onSuccess: (result) => {
        if (user) {
          setUser({ ...user, isProfileComplete: result.isProfileComplete });
        }
        navigate(`/profile/${user?.id}`);
      },
    });
  };

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-32">
        <Spinner size="lg" />
      </div>
    );
  }

  const currentAvatarUrl = avatarPreview ?? resolveFileUrl(profile?.avatarUrl);
  const currentBannerUrl = bannerPreview ?? resolveFileUrl(profile?.bannerUrl);
  const isUploading = uploadAvatar.isPending || uploadBanner.isPending || removeBannerMut.isPending;

  return (
    <AnimatedPage>
      <div className="mx-auto max-w-2xl space-y-6">
        <motion.div initial={{ opacity: 0, x: -20 }} animate={{ opacity: 1, x: 0 }}>
          <h1 className="text-3xl font-bold text-foreground">Edit Profile</h1>
          <p className="mt-1 text-foreground-muted">Update your gaming identity</p>
        </motion.div>

        {/* Banner Upload */}
        <motion.div
          initial={{ opacity: 0, scale: 0.98 }}
          animate={{ opacity: 1, scale: 1 }}
          transition={{ delay: 0.05 }}
        >
          <Card className="overflow-hidden p-0!">
            <div
              className="group relative h-36 cursor-pointer sm:h-44"
              onClick={() => bannerInputRef.current?.click()}
            >
              {currentBannerUrl ? (
                <img
                  src={currentBannerUrl}
                  alt="Profile banner"
                  className="h-full w-full object-cover"
                />
              ) : (
                <div className="h-full w-full bg-linear-to-r from-primary/20 via-primary/10 to-accent/20">
                  <div className="absolute inset-0 bg-[radial-gradient(circle_at_30%_50%,var(--color-primary)_0%,transparent_60%)] opacity-20" />
                </div>
              )}

              <div className="absolute inset-0 flex items-center justify-center bg-black/0 transition-colors group-hover:bg-black/40">
                <div className="flex items-center gap-2 rounded-lg bg-black/60 px-4 py-2 text-sm font-medium text-white opacity-0 transition-opacity group-hover:opacity-100">
                  {uploadBanner.isPending ? (
                    <Spinner size="sm" />
                  ) : (
                    <svg className="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth={1.5}>
                      <path strokeLinecap="round" strokeLinejoin="round" d="M6.827 6.175A2.31 2.31 0 015.186 7.23c-.38.054-.757.112-1.134.175C2.999 7.58 2.25 8.507 2.25 9.574V18a2.25 2.25 0 002.25 2.25h15A2.25 2.25 0 0021.75 18V9.574c0-1.067-.75-1.994-1.802-2.169a47.865 47.865 0 00-1.134-.175 2.31 2.31 0 01-1.64-1.055l-.822-1.316a2.192 2.192 0 00-1.736-1.039 48.774 48.774 0 00-5.232 0 2.192 2.192 0 00-1.736 1.039l-.821 1.316z" />
                      <path strokeLinecap="round" strokeLinejoin="round" d="M16.5 12.75a4.5 4.5 0 11-9 0 4.5 4.5 0 019 0zM18.75 10.5h.008v.008h-.008V10.5z" />
                    </svg>
                  )}
                  {uploadBanner.isPending ? 'Uploading...' : 'Change Banner'}
                </div>
              </div>

              <input
                ref={bannerInputRef}
                type="file"
                accept="image/jpeg,image/png,image/webp"
                className="hidden"
                onChange={handleBannerChange}
              />
            </div>

            {(currentBannerUrl && !bannerPreview) && (
              <div className="flex justify-end px-4 py-2">
                <button
                  type="button"
                  onClick={(e) => {
                    e.stopPropagation();
                    handleRemoveBanner();
                  }}
                  disabled={removeBannerMut.isPending}
                  className="text-xs text-foreground-muted transition-colors hover:text-danger"
                >
                  {removeBannerMut.isPending ? 'Removing...' : 'Remove banner'}
                </button>
              </div>
            )}
          </Card>
        </motion.div>

        {/* Avatar + User Info */}
        <motion.div
          initial={{ opacity: 0, scale: 0.9 }}
          animate={{ opacity: 1, scale: 1 }}
          transition={{ delay: 0.1 }}
        >
          <Card className="flex items-center gap-4">
            <div
              className="group relative cursor-pointer"
              onClick={() => avatarInputRef.current?.click()}
            >
              <UserAvatar
                username={user?.username ?? 'User'}
                avatarUrl={currentAvatarUrl}
                size="lg"
                className="h-20! w-20! text-xl!"
              />

              <div className="absolute inset-0 flex items-center justify-center rounded-full bg-black/0 transition-colors group-hover:bg-black/40">
                {uploadAvatar.isPending ? (
                  <Spinner size="sm" />
                ) : (
                  <svg
                    className="h-5 w-5 text-white opacity-0 transition-opacity group-hover:opacity-100"
                    fill="none"
                    stroke="currentColor"
                    viewBox="0 0 24 24"
                    strokeWidth={1.5}
                  >
                    <path strokeLinecap="round" strokeLinejoin="round" d="M6.827 6.175A2.31 2.31 0 015.186 7.23c-.38.054-.757.112-1.134.175C2.999 7.58 2.25 8.507 2.25 9.574V18a2.25 2.25 0 002.25 2.25h15A2.25 2.25 0 0021.75 18V9.574c0-1.067-.75-1.994-1.802-2.169a47.865 47.865 0 00-1.134-.175 2.31 2.31 0 01-1.64-1.055l-.822-1.316a2.192 2.192 0 00-1.736-1.039 48.774 48.774 0 00-5.232 0 2.192 2.192 0 00-1.736 1.039l-.821 1.316z" />
                    <path strokeLinecap="round" strokeLinejoin="round" d="M16.5 12.75a4.5 4.5 0 11-9 0 4.5 4.5 0 019 0zM18.75 10.5h.008v.008h-.008V10.5z" />
                  </svg>
                )}
              </div>

              <input
                ref={avatarInputRef}
                type="file"
                accept="image/jpeg,image/png,image/webp"
                className="hidden"
                onChange={handleAvatarChange}
              />
            </div>

            <div>
              <p className="font-semibold text-foreground">{user?.username}</p>
              <p className="text-sm text-foreground-muted">{user?.email}</p>
              <p className="mt-1 text-xs text-foreground-subtle">
                Click avatar or banner to change
              </p>
            </div>
          </Card>
        </motion.div>

        <form onSubmit={handleSubmit(onSubmit)}>
          <motion.div
            variants={formStagger}
            initial="hidden"
            animate="show"
            className="space-y-5"
          >
            <motion.div variants={formItem}>
              <Card>
                <h2 className="mb-4 text-lg font-semibold text-foreground">Basic Info</h2>
                <div className="space-y-4">
                  <Input
                    id="displayName"
                    label="Display Name"
                    placeholder="How others will see you"
                    error={errors.displayName?.message}
                    {...register('displayName')}
                  />
                  <Textarea
                    id="bio"
                    label="Bio"
                    placeholder="Tell other gamers about yourself..."
                    rows={4}
                    error={errors.bio?.message}
                    {...register('bio')}
                  />
                  <div className="grid gap-4 sm:grid-cols-2">
                    <Input
                      id="country"
                      label="Country"
                      placeholder="Your country"
                      error={errors.country?.message}
                      {...register('country')}
                    />
                    <Input
                      id="timezone"
                      label="Timezone"
                      placeholder="e.g. UTC+3"
                      error={errors.timezone?.message}
                      {...register('timezone')}
                    />
                  </div>
                </div>
              </Card>
            </motion.div>

            <motion.div variants={formItem}>
              <Card>
                <h2 className="mb-4 text-lg font-semibold text-foreground">Play Schedule</h2>
                <div className="space-y-3">
                  <p className="text-sm text-foreground-muted">
                    Let others know when you're usually available
                  </p>
                  <div className="grid gap-4 sm:grid-cols-2">
                    <Input
                      id="weekdaysFrom"
                      label="Weekdays From"
                      placeholder="e.g. 18:00"
                      {...register('weekdaysFrom')}
                    />
                    <Input
                      id="weekdaysTo"
                      label="Weekdays To"
                      placeholder="e.g. 23:00"
                      {...register('weekdaysTo')}
                    />
                    <Input
                      id="weekendsFrom"
                      label="Weekends From"
                      placeholder="e.g. 10:00"
                      {...register('weekendsFrom')}
                    />
                    <Input
                      id="weekendsTo"
                      label="Weekends To"
                      placeholder="e.g. 02:00"
                      {...register('weekendsTo')}
                    />
                  </div>
                </div>
              </Card>
            </motion.div>

            <motion.div variants={formItem} className="flex items-center justify-end gap-3">
              <Button type="button" variant="ghost" onClick={() => navigate(-1)}>
                Cancel
              </Button>
              <motion.div whileHover={{ scale: 1.02 }} whileTap={{ scale: 0.98 }}>
                <Button
                  type="submit"
                  isLoading={updateProfile.isPending}
                  disabled={!isDirty || isUploading}
                >
                  Save Changes
                </Button>
              </motion.div>
            </motion.div>

            {updateProfile.error && (
              <motion.p
                initial={{ opacity: 0, x: -10 }}
                animate={{ opacity: 1, x: 0 }}
                className="rounded-md bg-danger/10 px-3 py-2 text-sm text-danger"
              >
                Failed to update profile. Please try again.
              </motion.p>
            )}
          </motion.div>
        </form>
      </div>
    </AnimatedPage>
  );
}
