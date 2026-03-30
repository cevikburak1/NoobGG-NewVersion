import { z } from 'zod';

export const createGuildSchema = z.object({
  name: z
    .string()
    .min(3, 'Guild name must be at least 3 characters')
    .max(50, 'Guild name must not exceed 50 characters'),
  tag: z
    .string()
    .min(2, 'Tag must be at least 2 characters')
    .max(6, 'Tag must not exceed 6 characters')
    .regex(/^[A-Za-z0-9]+$/, 'Tag must contain only letters and numbers'),
  description: z.string().max(500, 'Description must not exceed 500 characters').optional(),
  isPublic: z.boolean().optional().default(true),
  region: z.string().min(1, 'Region is required'),
  language: z.string().min(1, 'Language is required'),
  gameIds: z.array(z.string()).max(10, 'Maximum 10 games allowed').optional().default([]),
});

export type CreateGuildFormData = z.infer<typeof createGuildSchema>;
