import { Link, useNavigate } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { motion } from 'framer-motion';
import { registerSchema } from '@/features/auth/schemas';
import type { RegisterFormData } from '@/features/auth/schemas';
import { useRegister } from '@/features/auth/hooks';
import { Button, Input } from '@/components/ui';

const formAnimation = {
  hidden: { opacity: 0 },
  show: {
    opacity: 1,
    transition: { staggerChildren: 0.08 },
  },
};

const itemAnimation = {
  hidden: { opacity: 0, y: 15 },
  show: { opacity: 1, y: 0, transition: { duration: 0.3 } },
};

export default function RegisterPage() {
  const navigate = useNavigate();
  const registerMutation = useRegister();

  const { register, handleSubmit, formState: { errors }, watch } = useForm<RegisterFormData>({
    resolver: zodResolver(registerSchema),
  });

  const password = watch('password', '');
  const passwordStrength = getPasswordStrength(password);

  const onSubmit = (data: RegisterFormData) => {
    registerMutation.mutate(data, {
      onSuccess: (res) => navigate('/verify-email', { state: { email: res.email } }),
    });
  };

  return (
    <motion.form
      onSubmit={handleSubmit(onSubmit)}
      variants={formAnimation}
      initial="hidden"
      animate="show"
      className="flex flex-col gap-5"
    >
      <motion.div variants={itemAnimation}>
        <h1 className="text-2xl font-bold text-foreground">Create your account</h1>
        <p className="mt-1 text-sm text-foreground-muted">Join the gaming community in seconds</p>
      </motion.div>

      <motion.div variants={itemAnimation}>
        <Input
          id="email"
          type="email"
          label="Email"
          placeholder="your@email.com"
          error={errors.email?.message}
          {...register('email')}
        />
      </motion.div>

      <motion.div variants={itemAnimation}>
        <Input
          id="username"
          label="Username"
          placeholder="Choose a cool username"
          error={errors.username?.message}
          {...register('username')}
        />
      </motion.div>

      <motion.div variants={itemAnimation}>
        <Input
          id="password"
          type="password"
          label="Password"
          placeholder="Min 6 characters"
          error={errors.password?.message}
          {...register('password')}
        />
        {password.length > 0 && (
          <motion.div
            initial={{ opacity: 0, height: 0 }}
            animate={{ opacity: 1, height: 'auto' }}
            className="mt-2"
          >
            <div className="flex gap-1">
              {[1, 2, 3, 4].map((level) => (
                <motion.div
                  key={level}
                  initial={{ scaleX: 0 }}
                  animate={{ scaleX: 1 }}
                  transition={{ delay: level * 0.05 }}
                  className={`h-1 flex-1 rounded-full transition-colors ${
                    level <= passwordStrength.level
                      ? passwordStrength.color
                      : 'bg-border'
                  }`}
                />
              ))}
            </div>
            <p className={`mt-1 text-xs ${passwordStrength.textColor}`}>
              {passwordStrength.label}
            </p>
          </motion.div>
        )}
      </motion.div>

      {registerMutation.error && (
        <motion.p
          initial={{ opacity: 0, x: -10 }}
          animate={{ opacity: 1, x: 0 }}
          className="rounded-md bg-danger/10 px-3 py-2 text-sm text-danger"
        >
          {(registerMutation.error as { response?: { data?: { title?: string } } })?.response?.data?.title ?? 'Registration failed. Please try again.'}
        </motion.p>
      )}

      <motion.div variants={itemAnimation}>
        <motion.div whileHover={{ scale: 1.01 }} whileTap={{ scale: 0.99 }}>
          <Button type="submit" isLoading={registerMutation.isPending} className="w-full">
            Create account
          </Button>
        </motion.div>
      </motion.div>

      <motion.p variants={itemAnimation} className="text-center text-xs text-foreground-subtle">
        By signing up, you agree to our Terms of Service and Privacy Policy
      </motion.p>

      <motion.div variants={itemAnimation} className="flex items-center gap-3">
        <div className="h-px flex-1 bg-border" />
        <span className="text-xs text-foreground-subtle">or</span>
        <div className="h-px flex-1 bg-border" />
      </motion.div>

      <motion.p variants={itemAnimation} className="text-center text-sm text-foreground-muted">
        Already have an account?{' '}
        <Link to="/login" className="font-medium text-primary hover:text-primary-hover transition-colors">
          Sign in
        </Link>
      </motion.p>
    </motion.form>
  );
}

function getPasswordStrength(password: string) {
  let score = 0;
  if (password.length >= 6) score++;
  if (password.length >= 10) score++;
  if (/[A-Z]/.test(password) && /[a-z]/.test(password)) score++;
  if (/[0-9]/.test(password) || /[^A-Za-z0-9]/.test(password)) score++;

  const levels = [
    { level: 0, label: '', color: 'bg-border', textColor: 'text-foreground-subtle' },
    { level: 1, label: 'Weak', color: 'bg-danger', textColor: 'text-danger' },
    { level: 2, label: 'Fair', color: 'bg-warning', textColor: 'text-warning' },
    { level: 3, label: 'Good', color: 'bg-info', textColor: 'text-info' },
    { level: 4, label: 'Strong', color: 'bg-success', textColor: 'text-success' },
  ];

  return levels[score] ?? levels[0];
}
