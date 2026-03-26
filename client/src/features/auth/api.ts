import { api } from '@/lib/api';
import type {
  AuthResponse,
  LoginRequest,
  RegisterRequest,
  RegisterResponse,
  UserResponse,
  VerifyEmailRequest,
} from '@/features/auth/types';

export async function login(data: LoginRequest): Promise<AuthResponse> {
  const { data: body } = await api.post<AuthResponse>('/api/auth/login', data);
  return body;
}

export async function register(data: RegisterRequest): Promise<RegisterResponse> {
  const { data: body } = await api.post<RegisterResponse>('/api/auth/register', data);
  return body;
}

export async function verifyEmail(data: VerifyEmailRequest): Promise<AuthResponse> {
  const { data: body } = await api.post<AuthResponse>('/api/auth/verify-email', data);
  return body;
}

export async function resendVerificationEmail(email: string): Promise<void> {
  await api.post('/api/auth/resend-verification', { email });
}

export async function getMe(): Promise<UserResponse> {
  const { data } = await api.get<UserResponse>('/api/auth/me');
  return data;
}

export async function logout(): Promise<void> {
  await api.post('/api/auth/logout');
}
