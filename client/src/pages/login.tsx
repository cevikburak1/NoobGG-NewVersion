import { Link, useNavigate } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { motion } from 'framer-motion';
import { loginSchema } from '@/features/auth/schemas';
import type { LoginFormData } from '@/features/auth/schemas';
import { useLogin } from '@/features/auth/hooks';
import { Button, Input } from '@/components/ui';
import { useTranslation } from 'react-i18next';

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

export default function LoginPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const login = useLogin();

  const { register, handleSubmit, formState: { errors } } = useForm<LoginFormData>({
    resolver: zodResolver(loginSchema),
  });

  const onSubmit = (data: LoginFormData) => {
    login.mutate(data, {
      onSuccess: () => navigate('/rooms'),
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
        <h1 className="text-2xl font-bold text-foreground">{t('Welcome back')}</h1>
        <p className="mt-1 text-sm text-foreground-muted">{t('Sign in to continue your gaming journey')}</p>
      </motion.div>

      <motion.div variants={itemAnimation}>
        <Input
          id="emailOrUsername"
          label={t('Email or Username')}
          placeholder="your@email.com"
          error={errors.emailOrUsername?.message}
          {...register('emailOrUsername')}
        />
      </motion.div>

      <motion.div variants={itemAnimation}>
        <Input
          id="password"
          type="password"
          label={t('Password')}
          placeholder={t('Enter your password')}
          error={errors.password?.message}
          {...register('password')}
        />
      </motion.div>

      {login.error && (
        <motion.p
          initial={{ opacity: 0, x: -10 }}
          animate={{ opacity: 1, x: 0 }}
          className="rounded-md bg-danger/10 px-3 py-2 text-sm text-danger"
        >
          {(login.error as { response?: { data?: { title?: string } } })?.response?.data?.title ?? t('Login failed. Please check your credentials.')}
        </motion.p>
      )}

      <motion.div variants={itemAnimation}>
        <motion.div whileHover={{ scale: 1.01 }} whileTap={{ scale: 0.99 }}>
          <Button type="submit" isLoading={login.isPending} className="w-full">
            {t('Sign in')}
          </Button>
        </motion.div>
      </motion.div>

      <motion.div variants={itemAnimation} className="flex items-center gap-3">
        <div className="h-px flex-1 bg-border" />
        <span className="text-xs text-foreground-subtle">{t('or')}</span>
        <div className="h-px flex-1 bg-border" />
      </motion.div>

      <motion.p variants={itemAnimation} className="text-center text-sm text-foreground-muted">
        {t("Don't have an account?")}{' '}
        <Link to="/register" className="font-medium text-primary hover:text-primary-hover transition-colors">
          {t('Sign up')}
        </Link>
      </motion.p>
    </motion.form>
  );
}
