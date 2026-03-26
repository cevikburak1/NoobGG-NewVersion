import { useState, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { motion, AnimatePresence } from 'framer-motion';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { Button, Input, Textarea, Select, Spinner } from '@/components/ui';
import { useUpdateProfile } from '@/features/profile/hooks';
import { useAddGameProfile } from '@/features/profile/hooks';
import { useGameSearch } from '@/features/games/hooks';
import { useAuthStore } from '@/stores/authStore';
import type { GameResponse } from '@/features/games/types';

const REGIONS = ['EU', 'NA', 'SA', 'AS', 'OCE', 'ME', 'AF', 'TR', 'CIS', 'SEA'];
const EXPERIENCE_LEVELS = ['Beginner', 'Intermediate', 'Advanced', 'Expert'];
const COMMUNICATION_PREFS = ['Text', 'Voice', 'Both', 'None'];

const profileSchema = z.object({
  displayName: z.string().min(2, 'At least 2 characters').max(50),
  bio: z.string().max(500).optional(),
  country: z.string().optional(),
});

type ProfileFormData = z.infer<typeof profileSchema>;

interface SelectedGame {
  game: GameResponse;
  rank: string;
  region: string;
  experienceLevel: string;
  communicationPreference: string;
  lookingForTeam: boolean;
  inGameName: string;
}

const steps = ['Profile', 'Games', 'Review'];

export default function OnboardingPage() {
  const navigate = useNavigate();
  const updateProfile = useUpdateProfile();
  const addGameProfile = useAddGameProfile();
  const setUser = useAuthStore((s) => s.setUser);
  const user = useAuthStore((s) => s.user);

  const [currentStep, setCurrentStep] = useState(0);
  const [selectedGames, setSelectedGames] = useState<SelectedGame[]>([]);
  const [gameQuery, setGameQuery] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);

  const { data: searchResults, isLoading: isSearching } = useGameSearch(gameQuery);

  const {
    register,
    handleSubmit,
    formState: { errors },
    getValues,
  } = useForm<ProfileFormData>({
    resolver: zodResolver(profileSchema),
    defaultValues: {
      displayName: user?.username ?? '',
    },
  });

  const handleAddGame = useCallback((game: GameResponse) => {
    setSelectedGames((prev) => {
      if (prev.find((g) => g.game.id === game.id)) return prev;
      return [
        ...prev,
        {
          game,
          rank: '',
          region: 'EU',
          experienceLevel: 'Beginner',
          communicationPreference: 'Both',
          lookingForTeam: true,
          inGameName: '',
        },
      ];
    });
    setGameQuery('');
  }, []);

  const handleRemoveGame = useCallback((gameId: string) => {
    setSelectedGames((prev) => prev.filter((g) => g.game.id !== gameId));
  }, []);

  const handleGameFieldChange = useCallback(
    (gameId: string, field: keyof SelectedGame, value: string | boolean) => {
      setSelectedGames((prev) =>
        prev.map((g) =>
          g.game.id === gameId ? { ...g, [field]: value } : g,
        ),
      );
    },
    [],
  );

  const handleNext = handleSubmit(() => {
    setCurrentStep((s) => Math.min(s + 1, 2));
  });

  const handleBack = () => setCurrentStep((s) => Math.max(s - 1, 0));

  const handleFinish = async () => {
    setIsSubmitting(true);
    try {
      const values = getValues();
      const profileResult = await updateProfile.mutateAsync({
        displayName: values.displayName,
        bio: values.bio,
        country: values.country,
      });

      for (const sg of selectedGames) {
        await addGameProfile.mutateAsync({
          gameId: sg.game.id,
          rank: sg.rank || 'Unranked',
          region: sg.region,
          experienceLevel: sg.experienceLevel,
          communicationPreference: sg.communicationPreference,
          lookingForTeam: sg.lookingForTeam,
          inGameName: sg.inGameName || undefined,
        });
      }

      if (user) {
        setUser({ ...user, isProfileComplete: profileResult.isProfileComplete });
      }

      navigate('/rooms', { replace: true });
    } catch {
      // errors handled by mutation hooks
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="min-h-screen bg-background flex flex-col items-center justify-center p-4">
      <div className="w-full max-w-2xl">
        <div className="text-center mb-8">
          <h1 className="text-3xl font-bold text-foreground mb-2">Welcome to NoobGg!</h1>
          <p className="text-foreground-muted">Let's set up your profile so others can find you</p>
        </div>

        <div className="flex items-center justify-center gap-2 mb-8">
          {steps.map((step, i) => (
            <div key={step} className="flex items-center gap-2">
              <div
                className={`w-8 h-8 rounded-full flex items-center justify-center text-sm font-medium transition-colors ${
                  i <= currentStep
                    ? 'bg-primary text-primary-foreground'
                    : 'bg-surface text-foreground-muted border border-border'
                }`}
              >
                {i + 1}
              </div>
              <span
                className={`text-sm hidden sm:block ${
                  i <= currentStep ? 'text-foreground' : 'text-foreground-muted'
                }`}
              >
                {step}
              </span>
              {i < steps.length - 1 && (
                <div
                  className={`w-12 h-0.5 ${
                    i < currentStep ? 'bg-primary' : 'bg-border'
                  }`}
                />
              )}
            </div>
          ))}
        </div>

        <div className="bg-surface border border-border rounded-xl p-6">
          <AnimatePresence mode="wait">
            {currentStep === 0 && (
              <motion.div
                key="step-0"
                initial={{ opacity: 0, x: 20 }}
                animate={{ opacity: 1, x: 0 }}
                exit={{ opacity: 0, x: -20 }}
                transition={{ duration: 0.2 }}
              >
                <h2 className="text-xl font-semibold text-foreground mb-4">
                  Basic Info
                </h2>
                <div className="space-y-4">
                  <div>
                    <label className="block text-sm font-medium text-foreground mb-1">
                      Display Name *
                    </label>
                    <Input
                      {...register('displayName')}
                      placeholder="How others will see you"
                    />
                    {errors.displayName && (
                      <p className="text-sm text-danger mt-1">{errors.displayName.message}</p>
                    )}
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-foreground mb-1">
                      Bio
                    </label>
                    <Textarea
                      {...register('bio')}
                      placeholder="Tell others about yourself..."
                      rows={3}
                    />
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-foreground mb-1">
                      Country
                    </label>
                    <Input
                      {...register('country')}
                      placeholder="Your country"
                    />
                  </div>
                </div>
                <div className="flex justify-end mt-6">
                  <Button onClick={handleNext}>Continue</Button>
                </div>
              </motion.div>
            )}

            {currentStep === 1 && (
              <motion.div
                key="step-1"
                initial={{ opacity: 0, x: 20 }}
                animate={{ opacity: 1, x: 0 }}
                exit={{ opacity: 0, x: -20 }}
                transition={{ duration: 0.2 }}
              >
                <h2 className="text-xl font-semibold text-foreground mb-2">
                  Games You Play
                </h2>
                <p className="text-sm text-foreground-muted mb-4">
                  Add at least one game so teammates can find you
                </p>

                <div className="relative mb-4">
                  <Input
                    value={gameQuery}
                    onChange={(e) => setGameQuery(e.target.value)}
                    placeholder="Search for a game..."
                  />
                  {gameQuery.length >= 2 && (
                    <div className="absolute z-20 top-full left-0 right-0 mt-1 bg-surface border border-border rounded-lg shadow-lg max-h-60 overflow-y-auto">
                      {isSearching ? (
                        <div className="flex justify-center p-4">
                          <Spinner size="sm" />
                        </div>
                      ) : searchResults && searchResults.length > 0 ? (
                        searchResults.map((game) => (
                          <button
                            key={game.id}
                            className="w-full flex items-center gap-3 p-3 hover:bg-surface-hover transition-colors text-left"
                            onClick={() => handleAddGame(game)}
                          >
                            {game.backgroundImageUrl ? (
                              <img
                                src={game.backgroundImageUrl}
                                alt={game.name}
                                className="w-10 h-10 rounded object-cover"
                              />
                            ) : (
                              <div className="w-10 h-10 rounded bg-border flex items-center justify-center text-foreground-muted text-xs">
                                ?
                              </div>
                            )}
                            <div>
                              <p className="text-sm font-medium text-foreground">{game.name}</p>
                              <p className="text-xs text-foreground-muted">
                                {game.genres.slice(0, 3).join(', ')}
                              </p>
                            </div>
                          </button>
                        ))
                      ) : (
                        <p className="p-4 text-sm text-foreground-muted text-center">
                          No games found
                        </p>
                      )}
                    </div>
                  )}
                </div>

                <div className="space-y-4">
                  {selectedGames.map((sg) => (
                    <div
                      key={sg.game.id}
                      className="border border-border rounded-lg p-4 bg-background"
                    >
                      <div className="flex items-start justify-between mb-3">
                        <div className="flex items-center gap-3">
                          {sg.game.backgroundImageUrl ? (
                            <img
                              src={sg.game.backgroundImageUrl}
                              alt={sg.game.name}
                              className="w-12 h-12 rounded object-cover"
                            />
                          ) : (
                            <div className="w-12 h-12 rounded bg-border" />
                          )}
                          <div>
                            <p className="font-medium text-foreground">{sg.game.name}</p>
                            <p className="text-xs text-foreground-muted">
                              {sg.game.genres.slice(0, 2).join(', ')}
                            </p>
                          </div>
                        </div>
                        <Button
                          variant="ghost"
                          size="sm"
                          onClick={() => handleRemoveGame(sg.game.id)}
                        >
                          Remove
                        </Button>
                      </div>
                      <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                        <div>
                          <label className="block text-xs text-foreground-muted mb-1">
                            In-Game Name
                          </label>
                          <Input
                            value={sg.inGameName}
                            onChange={(e) =>
                              handleGameFieldChange(sg.game.id, 'inGameName', e.target.value)
                            }
                            placeholder="Your IGN"
                          />
                        </div>
                        <div>
                          <label className="block text-xs text-foreground-muted mb-1">
                            Rank
                          </label>
                          <Input
                            value={sg.rank}
                            onChange={(e) =>
                              handleGameFieldChange(sg.game.id, 'rank', e.target.value)
                            }
                            placeholder="e.g. Gold, Diamond, GN3..."
                          />
                        </div>
                        <div>
                          <label className="block text-xs text-foreground-muted mb-1">
                            Region
                          </label>
                          <Select
                            value={sg.region}
                            onChange={(e) =>
                              handleGameFieldChange(sg.game.id, 'region', e.target.value)
                            }
                            options={REGIONS.map((r) => ({ value: r, label: r }))}
                          />
                        </div>
                        <div>
                          <label className="block text-xs text-foreground-muted mb-1">
                            Experience Level
                          </label>
                          <Select
                            value={sg.experienceLevel}
                            onChange={(e) =>
                              handleGameFieldChange(sg.game.id, 'experienceLevel', e.target.value)
                            }
                            options={EXPERIENCE_LEVELS.map((l) => ({ value: l, label: l }))}
                          />
                        </div>
                        <div>
                          <label className="block text-xs text-foreground-muted mb-1">
                            Communication
                          </label>
                          <Select
                            value={sg.communicationPreference}
                            onChange={(e) =>
                              handleGameFieldChange(
                                sg.game.id,
                                'communicationPreference',
                                e.target.value,
                              )
                            }
                            options={COMMUNICATION_PREFS.map((c) => ({ value: c, label: c }))}
                          />
                        </div>
                        <div className="flex items-end">
                          <label className="flex items-center gap-2 cursor-pointer">
                            <input
                              type="checkbox"
                              checked={sg.lookingForTeam}
                              onChange={(e) =>
                                handleGameFieldChange(
                                  sg.game.id,
                                  'lookingForTeam',
                                  e.target.checked,
                                )
                              }
                              className="rounded border-border"
                            />
                            <span className="text-sm text-foreground">Looking for team</span>
                          </label>
                        </div>
                      </div>
                    </div>
                  ))}
                </div>

                {selectedGames.length === 0 && (
                  <p className="text-center text-foreground-muted py-8">
                    Search and add the games you play above
                  </p>
                )}

                <div className="flex justify-between mt-6">
                  <Button variant="ghost" onClick={handleBack}>
                    Back
                  </Button>
                  <Button
                    onClick={() => setCurrentStep(2)}
                    disabled={selectedGames.length === 0}
                  >
                    Continue
                  </Button>
                </div>
              </motion.div>
            )}

            {currentStep === 2 && (
              <motion.div
                key="step-2"
                initial={{ opacity: 0, x: 20 }}
                animate={{ opacity: 1, x: 0 }}
                exit={{ opacity: 0, x: -20 }}
                transition={{ duration: 0.2 }}
              >
                <h2 className="text-xl font-semibold text-foreground mb-4">
                  Review Your Profile
                </h2>

                <div className="space-y-4">
                  <div className="bg-background border border-border rounded-lg p-4">
                    <h3 className="text-sm font-medium text-foreground-muted mb-2">
                      Basic Info
                    </h3>
                    <p className="text-foreground font-medium">{getValues('displayName')}</p>
                    {getValues('bio') && (
                      <p className="text-sm text-foreground-muted mt-1">{getValues('bio')}</p>
                    )}
                    {getValues('country') && (
                      <p className="text-sm text-foreground-muted mt-1">
                        {getValues('country')}
                      </p>
                    )}
                  </div>

                  <div className="bg-background border border-border rounded-lg p-4">
                    <h3 className="text-sm font-medium text-foreground-muted mb-2">
                      Games ({selectedGames.length})
                    </h3>
                    <div className="space-y-2">
                      {selectedGames.map((sg) => (
                        <div key={sg.game.id} className="flex items-center gap-3">
                          {sg.game.backgroundImageUrl ? (
                            <img
                              src={sg.game.backgroundImageUrl}
                              alt={sg.game.name}
                              className="w-8 h-8 rounded object-cover"
                            />
                          ) : (
                            <div className="w-8 h-8 rounded bg-border" />
                          )}
                          <div className="flex-1 min-w-0">
                            <p className="text-sm font-medium text-foreground truncate">
                              {sg.game.name}
                            </p>
                            <p className="text-xs text-foreground-muted">
                              {sg.experienceLevel} · {sg.region} · {sg.rank || 'Unranked'}
                            </p>
                          </div>
                        </div>
                      ))}
                    </div>
                  </div>
                </div>

                <div className="flex justify-between mt-6">
                  <Button variant="ghost" onClick={handleBack}>
                    Back
                  </Button>
                  <Button
                    onClick={handleFinish}
                    isLoading={isSubmitting}
                  >
                    Complete Setup
                  </Button>
                </div>
              </motion.div>
            )}
          </AnimatePresence>
        </div>
      </div>
    </div>
  );
}
