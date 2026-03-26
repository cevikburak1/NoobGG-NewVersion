import { useState, useRef, useEffect } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import { motion } from 'framer-motion';
import { useVerifyEmail, useResendVerification } from '@/features/auth/hooks';
import { Button } from '@/components/ui/button';
import { AnimatedPage } from '@/components/ui/animatedPage';

export default function VerifyEmailPage() {
  const location = useLocation();
  const navigate = useNavigate();
  const email = (location.state as { email?: string })?.email ?? '';

  const [code, setCode] = useState<string[]>(Array(6).fill(''));
  const inputRefs = useRef<(HTMLInputElement | null)[]>([]);

  const verifyMutation = useVerifyEmail();
  const resendMutation = useResendVerification();

  const [cooldown, setCooldown] = useState(0);

  useEffect(() => {
    if (!email) {
      navigate('/register', { replace: true });
    }
  }, [email, navigate]);

  useEffect(() => {
    if (cooldown <= 0) return;
    const timer = setTimeout(() => setCooldown((c) => c - 1), 1000);
    return () => clearTimeout(timer);
  }, [cooldown]);

  useEffect(() => {
    inputRefs.current[0]?.focus();
  }, []);

  const handleChange = (index: number, value: string) => {
    if (!/^\d*$/.test(value)) return;

    const digit = value.slice(-1);
    const newCode = [...code];
    newCode[index] = digit;
    setCode(newCode);

    if (digit && index < 5) {
      inputRefs.current[index + 1]?.focus();
    }

    const fullCode = newCode.join('');
    if (fullCode.length === 6 && newCode.every((d) => d !== '')) {
      verifyMutation.mutate(
        { email, code: fullCode },
        { onSuccess: () => navigate('/rooms', { replace: true }) },
      );
    }
  };

  const handleKeyDown = (index: number, e: React.KeyboardEvent) => {
    if (e.key === 'Backspace' && !code[index] && index > 0) {
      inputRefs.current[index - 1]?.focus();
      const newCode = [...code];
      newCode[index - 1] = '';
      setCode(newCode);
    }
  };

  const handlePaste = (e: React.ClipboardEvent) => {
    e.preventDefault();
    const pasted = e.clipboardData.getData('text').replace(/\D/g, '').slice(0, 6);
    if (!pasted) return;

    const newCode = Array(6).fill('');
    for (let i = 0; i < pasted.length; i++) {
      newCode[i] = pasted[i];
    }
    setCode(newCode);

    const nextIndex = Math.min(pasted.length, 5);
    inputRefs.current[nextIndex]?.focus();

    if (pasted.length === 6) {
      verifyMutation.mutate(
        { email, code: pasted },
        { onSuccess: () => navigate('/rooms', { replace: true }) },
      );
    }
  };

  const handleResend = () => {
    resendMutation.mutate(email, {
      onSuccess: () => setCooldown(60),
    });
  };

  const handleSubmit = () => {
    const fullCode = code.join('');
    if (fullCode.length !== 6) return;
    verifyMutation.mutate(
      { email, code: fullCode },
      { onSuccess: () => navigate('/rooms', { replace: true }) },
    );
  };

  if (!email) return null;

  return (
    <AnimatedPage>
      <div className="min-h-screen bg-[#0f0f23] flex items-center justify-center p-4">
        <motion.div
          initial={{ opacity: 0, y: 20 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.5 }}
          className="w-full max-w-md"
        >
          <div className="bg-[#1a1a2e] rounded-2xl p-8 border border-white/5">
            <div className="text-center mb-8">
              <motion.div
                initial={{ scale: 0 }}
                animate={{ scale: 1 }}
                transition={{ type: 'spring', stiffness: 200, delay: 0.2 }}
                className="w-16 h-16 bg-purple-500/20 rounded-full flex items-center justify-center mx-auto mb-4"
              >
                <svg className="w-8 h-8 text-purple-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z" />
                </svg>
              </motion.div>
              <h1 className="text-2xl font-bold text-white mb-2">Check your email</h1>
              <p className="text-gray-400 text-sm">
                We sent a 6-digit verification code to
              </p>
              <p className="text-purple-400 font-medium text-sm mt-1">{email}</p>
            </div>

            <div className="flex justify-center gap-3 mb-6" onPaste={handlePaste}>
              {code.map((digit, index) => (
                <motion.input
                  key={index}
                  ref={(el) => { inputRefs.current[index] = el; }}
                  type="text"
                  inputMode="numeric"
                  maxLength={1}
                  value={digit}
                  onChange={(e) => handleChange(index, e.target.value)}
                  onKeyDown={(e) => handleKeyDown(index, e)}
                  initial={{ opacity: 0, y: 10 }}
                  animate={{ opacity: 1, y: 0 }}
                  transition={{ delay: index * 0.05 }}
                  className={`w-12 h-14 text-center text-xl font-bold rounded-lg border-2 bg-white/5 text-white outline-none transition-all ${
                    digit
                      ? 'border-purple-500 bg-purple-500/10'
                      : 'border-white/10 focus:border-purple-500/50'
                  }`}
                />
              ))}
            </div>

            {verifyMutation.error && (
              <motion.p
                initial={{ opacity: 0, x: -10 }}
                animate={{ opacity: 1, x: 0 }}
                className="rounded-md bg-red-500/10 px-3 py-2 text-sm text-red-400 text-center mb-4"
              >
                {(verifyMutation.error as { response?: { data?: { title?: string } } })?.response?.data?.title ?? 'Invalid verification code. Please try again.'}
              </motion.p>
            )}

            <Button
              onClick={handleSubmit}
              isLoading={verifyMutation.isPending}
              disabled={code.join('').length !== 6}
              className="w-full mb-4"
            >
              Verify Email
            </Button>

            <div className="text-center">
              <p className="text-gray-500 text-sm mb-2">Didn't receive the code?</p>
              <button
                onClick={handleResend}
                disabled={cooldown > 0 || resendMutation.isPending}
                className="text-purple-400 hover:text-purple-300 text-sm font-medium transition-colors disabled:text-gray-600 disabled:cursor-not-allowed"
              >
                {resendMutation.isPending
                  ? 'Sending...'
                  : cooldown > 0
                    ? `Resend in ${cooldown}s`
                    : 'Resend code'}
              </button>
              {resendMutation.isSuccess && cooldown > 0 && (
                <motion.p
                  initial={{ opacity: 0 }}
                  animate={{ opacity: 1 }}
                  className="text-green-400 text-xs mt-2"
                >
                  New code sent!
                </motion.p>
              )}
            </div>
          </div>
        </motion.div>
      </div>
    </AnimatedPage>
  );
}
