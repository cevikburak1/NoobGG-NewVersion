import { useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { motion } from 'framer-motion';
import { updateProfileSchema, type UpdateProfileFormData } from '@/features/profile/schemas';
import { useMyProfile, useUpdateProfile } from '@/features/profile/hooks';
import { useAuthStore } from '@/stores/authStore';
import { Button, Input, Textarea, AnimatedPage, Card, Spinner } from '@/components/ui';
import { UserAvatar } from '@/components/common/userAvatar';

const formStagger = {
  hidden: { opacity: 0 },
  show: { opacity: 1, transition: { staggerChildren: 0.06 } },
};

const formItem = {
  hidden: { opacity: 0, y: 15 },
  show: { opacity: 1, y: 0, transition: { duration: 0.3 } },
};

export default function EditProfilePage() {
  const navigate = useNavigate();
  const user = useAuthStore((s) => s.user);
  const setUser = useAuthStore((s) => s.setUser);
  const { data: profile, isLoading } = useMyProfile();
  const updateProfile = useUpdateProfile();

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

  return (
    <AnimatedPage>
      <div className="mx-auto max-w-2xl space-y-6">
        <motion.div initial={{ opacity: 0, x: -20 }} animate={{ opacity: 1, x: 0 }}>
          <h1 className="text-3xl font-bold text-foreground">Edit Profile</h1>
          <p className="mt-1 text-foreground-muted">Update your gaming identity</p>
        </motion.div>

        <motion.div
          initial={{ opacity: 0, scale: 0.9 }}
          animate={{ opacity: 1, scale: 1 }}
          transition={{ delay: 0.1 }}
        >
          <Card className="flex items-center gap-4">
            <UserAvatar
              username={user?.username ?? 'User'}
              avatarUrl={profile?.avatarUrl}
              size="lg"
            />
            <div>
              <p className="font-semibold text-foreground">{user?.username}</p>
              <p className="text-sm text-foreground-muted">{user?.email}</p>
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
                <Button type="submit" isLoading={updateProfile.isPending} disabled={!isDirty}>
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
