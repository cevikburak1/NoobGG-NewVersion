import { z } from 'zod';

export const createRoomSchema = z.object({
  title: z.string().min(3).max(100),
  gameId: z.string().min(1),
  region: z.string(),
  language: z.string(),
  maxMembers: z.number().min(2).max(10).optional(),
  description: z.string().optional(),
  isPublic: z.boolean().optional(),
  tags: z.array(z.string()).optional(),
});

export type CreateRoomFormData = z.infer<typeof createRoomSchema>;
