import { z } from 'zod';

export const updateProfileSchema = z.object({
  displayName: z.string().min(2).max(50).optional(),
  bio: z.string().max(500).optional(),
  country: z.string().max(100).optional(),
  timezone: z.string().optional(),
  weekdaysFrom: z.string().optional(),
  weekdaysTo: z.string().optional(),
  weekendsFrom: z.string().optional(),
  weekendsTo: z.string().optional(),
});

export type UpdateProfileFormData = z.infer<typeof updateProfileSchema>;

export const addGameProfileSchema = z.object({
  gameId: z.string().min(1),
  rank: z.string().min(1).max(50),
  role: z.string().max(50).optional(),
  region: z.string().min(1),
  experienceLevel: z.string().min(1),
  communicationPreference: z.string().min(1),
  hoursPlayed: z.number().optional(),
  lookingForTeam: z.boolean(),
  note: z.string().max(300).optional(),
  inGameName: z.string().max(100).optional(),
});

export type AddGameProfileFormData = z.infer<typeof addGameProfileSchema>;
