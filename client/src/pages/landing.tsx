import { Link } from 'react-router-dom';
import { motion, useInView, useScroll, useTransform } from 'framer-motion';
import { useRef } from 'react';
import { Button } from '@/components/ui';

const floatingAnimation = {
  y: [0, -10, 0],
  transition: { duration: 3, repeat: Infinity, ease: 'easeInOut' as const },
};

const stats = [
  { value: '50K+', label: 'Active Gamers' },
  { value: '12K+', label: 'Rooms Created' },
  { value: '200+', label: 'Games Supported' },
  { value: '98%', label: 'Match Success' },
];

const features = [
  {
    icon: (
      <svg className="h-8 w-8" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth={1.5}>
        <path strokeLinecap="round" strokeLinejoin="round" d="M18 18.72a9.094 9.094 0 003.741-.479 3 3 0 00-4.682-2.72m.94 3.198l.001.031c0 .225-.012.447-.037.666A11.944 11.944 0 0112 21c-2.17 0-4.207-.576-5.963-1.584A6.062 6.062 0 016 18.719m12 0a5.971 5.971 0 00-.941-3.197m0 0A5.995 5.995 0 0012 12.75a5.995 5.995 0 00-5.058 2.772m0 0a3 3 0 00-4.681 2.72 8.986 8.986 0 003.74.477m.94-3.197a5.971 5.971 0 00-.94 3.197M15 6.75a3 3 0 11-6 0 3 3 0 016 0zm6 3a2.25 2.25 0 11-4.5 0 2.25 2.25 0 014.5 0zm-13.5 0a2.25 2.25 0 11-4.5 0 2.25 2.25 0 014.5 0z" />
      </svg>
    ),
    title: 'Smart Matchmaking',
    description: 'Our algorithm pairs you with gamers who match your skill level, playstyle, and schedule.',
  },
  {
    icon: (
      <svg className="h-8 w-8" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth={1.5}>
        <path strokeLinecap="round" strokeLinejoin="round" d="M20.25 8.511c.884.284 1.5 1.128 1.5 2.097v4.286c0 1.136-.847 2.1-1.98 2.193-.34.027-.68.052-1.02.072v3.091l-3-3c-1.354 0-2.694-.055-4.02-.163a2.115 2.115 0 01-.825-.242m9.345-8.334a2.126 2.126 0 00-.476-.095 48.64 48.64 0 00-8.048 0c-1.131.094-1.976 1.057-1.976 2.192v4.286c0 .837.46 1.58 1.155 1.951m9.345-8.334V6.637c0-1.621-1.152-3.026-2.76-3.235A48.455 48.455 0 0011.25 3c-2.115 0-4.198.137-6.24.402-1.608.209-2.76 1.614-2.76 3.235v6.226c0 1.621 1.152 3.026 2.76 3.235.577.075 1.157.14 1.74.194V21l4.155-4.155" />
      </svg>
    ),
    title: 'Real-Time Chat',
    description: 'Communicate instantly with your team through our built-in real-time messaging system.',
  },
  {
    icon: (
      <svg className="h-8 w-8" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth={1.5}>
        <path strokeLinecap="round" strokeLinejoin="round" d="M14.25 6.087c0-.355.186-.676.401-.959.221-.29.349-.634.349-1.003 0-1.036-1.007-1.875-2.25-1.875s-2.25.84-2.25 1.875c0 .369.128.713.349 1.003.215.283.401.604.401.959v0a.64.64 0 01-.657.643 48.491 48.491 0 01-4.163-.3c.186 1.613.466 3.193.834 4.73.46 1.93 1.075 3.792 1.838 5.567m5.548-11.14a48.674 48.674 0 00-.344 0m.344 0A17.933 17.933 0 0112 6.844c-1.23 0-2.44.098-3.627.285m7.254 0a48.345 48.345 0 013.232.238c.16 1.37.393 2.72.695 4.04.42 1.826.97 3.59 1.644 5.277" />
      </svg>
    ),
    title: 'Game Rooms',
    description: 'Create or join rooms for any game. Set filters for region, rank, and language preferences.',
  },
  {
    icon: (
      <svg className="h-8 w-8" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth={1.5}>
        <path strokeLinecap="round" strokeLinejoin="round" d="M3 13.125C3 12.504 3.504 12 4.125 12h2.25c.621 0 1.125.504 1.125 1.125v6.75C7.5 20.496 6.996 21 6.375 21h-2.25A1.125 1.125 0 013 19.875v-6.75zM9.75 8.625c0-.621.504-1.125 1.125-1.125h2.25c.621 0 1.125.504 1.125 1.125v11.25c0 .621-.504 1.125-1.125 1.125h-2.25a1.125 1.125 0 01-1.125-1.125V8.625zM16.5 4.125c0-.621.504-1.125 1.125-1.125h2.25C20.496 3 21 3.504 21 4.125v15.75c0 .621-.504 1.125-1.125 1.125h-2.25a1.125 1.125 0 01-1.125-1.125V4.125z" />
      </svg>
    ),
    title: 'Track Your Stats',
    description: 'Monitor your gaming journey with detailed profiles, game stats, and progress tracking.',
  },
  {
    icon: (
      <svg className="h-8 w-8" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth={1.5}>
        <path strokeLinecap="round" strokeLinejoin="round" d="M9 12.75L11.25 15 15 9.75m-3-7.036A11.959 11.959 0 013.598 6 11.99 11.99 0 003 9.749c0 5.592 3.824 10.29 9 11.623 5.176-1.332 9-6.03 9-11.622 0-1.31-.21-2.571-.598-3.751h-.152c-3.196 0-6.1-1.248-8.25-3.285z" />
      </svg>
    ),
    title: 'Safe Community',
    description: 'Our moderation system keeps the community clean with reports, blocks, and active moderators.',
  },
  {
    icon: (
      <svg className="h-8 w-8" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth={1.5}>
        <path strokeLinecap="round" strokeLinejoin="round" d="M9.813 15.904L9 18.75l-.813-2.846a4.5 4.5 0 00-3.09-3.09L2.25 12l2.846-.813a4.5 4.5 0 003.09-3.09L9 5.25l.813 2.846a4.5 4.5 0 003.09 3.09L15.75 12l-2.846.813a4.5 4.5 0 00-3.09 3.09zM18.259 8.715L18 9.75l-.259-1.035a3.375 3.375 0 00-2.455-2.456L14.25 6l1.036-.259a3.375 3.375 0 002.455-2.456L18 2.25l.259 1.035a3.375 3.375 0 002.455 2.456L21.75 6l-1.036.259a3.375 3.375 0 00-2.455 2.456zM16.894 20.567L16.5 21.75l-.394-1.183a2.25 2.25 0 00-1.423-1.423L13.5 18.75l1.183-.394a2.25 2.25 0 001.423-1.423l.394-1.183.394 1.183a2.25 2.25 0 001.423 1.423l1.183.394-1.183.394a2.25 2.25 0 00-1.423 1.423z" />
      </svg>
    ),
    title: 'Premium Perks',
    description: 'Unlock enhanced features with our Plus and Pro tiers for the most dedicated gamers.',
  },
];

const steps = [
  { step: '01', title: 'Create Account', description: 'Sign up in seconds with just your email and a username.' },
  { step: '02', title: 'Set Up Profile', description: 'Add your games, rank, playstyle, and availability.' },
  { step: '03', title: 'Find Teammates', description: 'Browse rooms or let our system match you with perfect teammates.' },
  { step: '04', title: 'Play Together', description: 'Join rooms, chat in real-time, and dominate together.' },
];

function AnimatedCounter({ value }: { value: string }) {
  const ref = useRef(null);
  const isInView = useInView(ref, { once: true });

  return (
    <motion.span
      ref={ref}
      initial={{ opacity: 0, scale: 0.5 }}
      animate={isInView ? { opacity: 1, scale: 1 } : {}}
      transition={{ duration: 0.5, type: 'spring', bounce: 0.3 }}
      className="text-3xl font-bold text-foreground sm:text-4xl"
    >
      {value}
    </motion.span>
  );
}

function FeatureCard({ feature, index }: { feature: typeof features[0]; index: number }) {
  const ref = useRef(null);
  const isInView = useInView(ref, { once: true, margin: '-50px' });

  return (
    <motion.div
      ref={ref}
      initial={{ opacity: 0, y: 40, rotateX: 10 }}
      animate={isInView ? { opacity: 1, y: 0, rotateX: 0 } : {}}
      transition={{ duration: 0.5, delay: index * 0.1 }}
      whileHover={{ y: -8, scale: 1.02 }}
      className="group relative overflow-hidden rounded-xl border border-border bg-surface p-6 transition-colors hover:border-primary/30"
    >
      <div className="absolute inset-0 bg-gradient-to-br from-primary/5 via-transparent to-accent/5 opacity-0 transition-opacity group-hover:opacity-100" />
      <div className="relative">
        <div className="mb-4 inline-flex rounded-lg bg-primary/10 p-3 text-primary">
          {feature.icon}
        </div>
        <h3 className="mb-2 text-lg font-semibold text-foreground">{feature.title}</h3>
        <p className="text-sm leading-relaxed text-foreground-muted">{feature.description}</p>
      </div>
    </motion.div>
  );
}

function GlowOrb({ className }: { className: string }) {
  return (
    <motion.div
      animate={floatingAnimation}
      className={`absolute rounded-full blur-3xl ${className}`}
    />
  );
}

export default function LandingPage() {
  const heroRef = useRef(null);
  const { scrollYProgress } = useScroll({ target: heroRef, offset: ['start start', 'end start'] });
  const heroY = useTransform(scrollYProgress, [0, 1], [0, 150]);
  const heroOpacity = useTransform(scrollYProgress, [0, 0.8], [1, 0]);

  return (
    <div className="relative min-h-screen overflow-hidden bg-background">
      <GlowOrb className="left-1/4 top-1/4 h-96 w-96 bg-primary/10" />
      <GlowOrb className="right-1/4 top-1/2 h-64 w-64 bg-accent/10" />
      <GlowOrb className="bottom-1/4 left-1/2 h-80 w-80 bg-primary/5" />

      {/* Navbar */}
      <header className="relative z-20 mx-auto flex max-w-7xl items-center justify-between px-6 py-5">
        <motion.div
          initial={{ opacity: 0, x: -20 }}
          animate={{ opacity: 1, x: 0 }}
          transition={{ duration: 0.5 }}
          className="flex items-center gap-1 text-2xl font-bold"
        >
          <span className="text-primary">Noob</span>
          <span className="text-accent">Gg</span>
        </motion.div>
        <motion.div
          initial={{ opacity: 0, x: 20 }}
          animate={{ opacity: 1, x: 0 }}
          transition={{ duration: 0.5 }}
          className="flex items-center gap-3"
        >
          <Link to="/login">
            <Button variant="ghost" size="sm">Sign in</Button>
          </Link>
          <Link to="/register">
            <Button size="sm">Get Started</Button>
          </Link>
        </motion.div>
      </header>

      {/* Hero */}
      <section ref={heroRef} className="relative z-10 flex flex-col items-center justify-center px-6 pb-20 pt-16 text-center sm:pt-24 md:pt-32">
        <motion.div style={{ y: heroY, opacity: heroOpacity }}>
          <motion.div
            initial={{ opacity: 0, scale: 0.8 }}
            animate={{ opacity: 1, scale: 1 }}
            transition={{ duration: 0.6, type: 'spring', bounce: 0.3 }}
            className="mx-auto mb-6 inline-flex items-center gap-2 rounded-full border border-primary/30 bg-primary/10 px-4 py-1.5 text-sm text-primary"
          >
            <motion.span
              animate={{ rotate: [0, 15, -15, 0] }}
              transition={{ duration: 2, repeat: Infinity }}
            >
              🎮
            </motion.span>
            <span>The #1 Gaming Teammate Finder</span>
          </motion.div>

          <motion.h1
            initial={{ opacity: 0, y: 30 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.6, delay: 0.1 }}
            className="mx-auto max-w-4xl text-4xl font-extrabold leading-tight text-foreground sm:text-5xl md:text-7xl"
          >
            Find Your{' '}
            <span className="relative">
              <span className="relative z-10 bg-gradient-to-r from-primary to-primary-hover bg-clip-text text-transparent">
                Teammates
              </span>
              <motion.span
                initial={{ scaleX: 0 }}
                animate={{ scaleX: 1 }}
                transition={{ duration: 0.8, delay: 0.5 }}
                className="absolute -bottom-2 left-0 h-3 w-full origin-left rounded-full bg-primary/20"
              />
            </span>
            ,<br />
            <span className="bg-gradient-to-r from-accent to-accent-hover bg-clip-text text-transparent">
              Level Up
            </span>{' '}
            Together
          </motion.h1>

          <motion.p
            initial={{ opacity: 0, y: 30 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.6, delay: 0.2 }}
            className="mx-auto mt-6 max-w-2xl text-lg text-foreground-muted sm:text-xl"
          >
            Connect with gamers who match your playstyle. Create rooms, chat in real-time,
            and build the perfect team for any game.
          </motion.p>

          <motion.div
            initial={{ opacity: 0, y: 30 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.6, delay: 0.3 }}
            className="mt-10 flex flex-col items-center gap-4 sm:flex-row sm:justify-center"
          >
            <Link to="/register">
              <motion.div whileHover={{ scale: 1.05 }} whileTap={{ scale: 0.98 }}>
                <Button size="lg" className="px-8 text-base shadow-lg shadow-primary/25">
                  Start Playing — It&apos;s Free
                </Button>
              </motion.div>
            </Link>
            <Link to="/rooms">
              <motion.div whileHover={{ scale: 1.05 }} whileTap={{ scale: 0.98 }}>
                <Button variant="outline" size="lg" className="px-8 text-base">
                  Browse Rooms
                </Button>
              </motion.div>
            </Link>
          </motion.div>
        </motion.div>

        {/* Floating game icons decoration */}
        <div className="pointer-events-none absolute inset-0 overflow-hidden">
          {['🎯', '⚔️', '🏆', '🚀', '💎', '🔥'].map((emoji, i) => (
            <motion.span
              key={i}
              className="absolute text-2xl opacity-20"
              style={{
                left: `${15 + i * 14}%`,
                top: `${20 + (i % 3) * 25}%`,
              }}
              animate={{
                y: [0, -20, 0],
                rotate: [0, 10, -10, 0],
                opacity: [0.1, 0.3, 0.1],
              }}
              transition={{
                duration: 4 + i,
                repeat: Infinity,
                delay: i * 0.5,
              }}
            >
              {emoji}
            </motion.span>
          ))}
        </div>
      </section>

      {/* Stats */}
      <section className="relative z-10 border-y border-border/50 bg-surface/50 backdrop-blur-sm">
        <div className="mx-auto grid max-w-5xl grid-cols-2 gap-8 px-6 py-12 sm:py-16 md:grid-cols-4">
          {stats.map((stat, i) => (
            <motion.div
              key={stat.label}
              initial={{ opacity: 0, y: 20 }}
              whileInView={{ opacity: 1, y: 0 }}
              viewport={{ once: true }}
              transition={{ duration: 0.5, delay: i * 0.1 }}
              className="flex flex-col items-center text-center"
            >
              <AnimatedCounter value={stat.value} />
              <span className="mt-1 text-sm text-foreground-muted">{stat.label}</span>
            </motion.div>
          ))}
        </div>
      </section>

      {/* Features */}
      <section className="relative z-10 px-6 py-20 sm:py-28">
        <div className="mx-auto max-w-6xl">
          <motion.div
            initial={{ opacity: 0, y: 20 }}
            whileInView={{ opacity: 1, y: 0 }}
            viewport={{ once: true }}
            className="mb-16 text-center"
          >
            <span className="mb-3 inline-block rounded-full bg-accent/10 px-3 py-1 text-xs font-semibold uppercase tracking-wider text-accent">
              Features
            </span>
            <h2 className="text-3xl font-bold text-foreground sm:text-4xl">
              Everything You Need to{' '}
              <span className="text-primary">Dominate</span>
            </h2>
            <p className="mx-auto mt-4 max-w-xl text-foreground-muted">
              From finding teammates to tracking your progress, we&apos;ve built the ultimate gaming companion.
            </p>
          </motion.div>

          <div className="grid gap-6 sm:grid-cols-2 lg:grid-cols-3">
            {features.map((feature, i) => (
              <FeatureCard key={feature.title} feature={feature} index={i} />
            ))}
          </div>
        </div>
      </section>

      {/* How it works */}
      <section className="relative z-10 border-y border-border/50 bg-surface/30 px-6 py-20 sm:py-28">
        <div className="mx-auto max-w-4xl">
          <motion.div
            initial={{ opacity: 0, y: 20 }}
            whileInView={{ opacity: 1, y: 0 }}
            viewport={{ once: true }}
            className="mb-16 text-center"
          >
            <span className="mb-3 inline-block rounded-full bg-primary/10 px-3 py-1 text-xs font-semibold uppercase tracking-wider text-primary">
              How it works
            </span>
            <h2 className="text-3xl font-bold text-foreground sm:text-4xl">
              Ready in <span className="text-accent">4 Simple Steps</span>
            </h2>
          </motion.div>

          <div className="space-y-8">
            {steps.map((s, i) => (
              <motion.div
                key={s.step}
                initial={{ opacity: 0, x: i % 2 === 0 ? -40 : 40 }}
                whileInView={{ opacity: 1, x: 0 }}
                viewport={{ once: true, margin: '-50px' }}
                transition={{ duration: 0.6, delay: i * 0.1 }}
                className="flex items-start gap-6"
              >
                <motion.div
                  whileHover={{ scale: 1.1, rotate: 5 }}
                  className="flex h-14 w-14 shrink-0 items-center justify-center rounded-xl bg-gradient-to-br from-primary to-primary-hover text-lg font-bold text-white shadow-lg shadow-primary/25"
                >
                  {s.step}
                </motion.div>
                <div>
                  <h3 className="text-xl font-semibold text-foreground">{s.title}</h3>
                  <p className="mt-1 text-foreground-muted">{s.description}</p>
                </div>
              </motion.div>
            ))}
          </div>
        </div>
      </section>

      {/* Games showcase (animated floating cards) */}
      <section className="relative z-10 overflow-hidden px-6 py-20 sm:py-28">
        <div className="mx-auto max-w-6xl text-center">
          <motion.div
            initial={{ opacity: 0, y: 20 }}
            whileInView={{ opacity: 1, y: 0 }}
            viewport={{ once: true }}
          >
            <span className="mb-3 inline-block rounded-full bg-accent/10 px-3 py-1 text-xs font-semibold uppercase tracking-wider text-accent">
              Popular Games
            </span>
            <h2 className="text-3xl font-bold text-foreground sm:text-4xl">
              Hundreds of <span className="text-primary">Games</span> Supported
            </h2>
            <p className="mx-auto mt-4 max-w-xl text-foreground-muted">
              From competitive shooters to co-op adventures, find teammates for any game you play.
            </p>
          </motion.div>

          <div className="mt-12 flex flex-wrap items-center justify-center gap-4">
            {['Valorant', 'League of Legends', 'CS2', 'Fortnite', 'Apex Legends', 'Overwatch 2', 'Dota 2', 'Rocket League', 'PUBG', 'Rainbow Six', 'Minecraft', 'GTA Online'].map((game, i) => (
              <motion.div
                key={game}
                initial={{ opacity: 0, scale: 0.8 }}
                whileInView={{ opacity: 1, scale: 1 }}
                viewport={{ once: true }}
                transition={{ duration: 0.4, delay: i * 0.05 }}
                whileHover={{ scale: 1.1, y: -4 }}
                className="rounded-xl border border-border bg-surface px-5 py-3 text-sm font-medium text-foreground shadow-lg transition-colors hover:border-primary/30 hover:bg-surface-hover"
              >
                {game}
              </motion.div>
            ))}
          </div>
        </div>
      </section>

      {/* CTA */}
      <section className="relative z-10 px-6 py-20 sm:py-28">
        <motion.div
          initial={{ opacity: 0, scale: 0.95 }}
          whileInView={{ opacity: 1, scale: 1 }}
          viewport={{ once: true }}
          className="relative mx-auto max-w-4xl overflow-hidden rounded-2xl border border-primary/20 bg-gradient-to-br from-primary/10 via-surface to-accent/10 p-12 text-center shadow-2xl sm:p-16"
        >
          <div className="absolute -right-20 -top-20 h-64 w-64 rounded-full bg-primary/10 blur-3xl" />
          <div className="absolute -bottom-20 -left-20 h-64 w-64 rounded-full bg-accent/10 blur-3xl" />

          <div className="relative">
            <motion.h2
              initial={{ opacity: 0, y: 20 }}
              whileInView={{ opacity: 1, y: 0 }}
              viewport={{ once: true }}
              className="text-3xl font-bold text-foreground sm:text-4xl"
            >
              Ready to Find Your{' '}
              <span className="text-primary">Dream Team</span>?
            </motion.h2>
            <motion.p
              initial={{ opacity: 0, y: 20 }}
              whileInView={{ opacity: 1, y: 0 }}
              viewport={{ once: true }}
              transition={{ delay: 0.1 }}
              className="mx-auto mt-4 max-w-lg text-lg text-foreground-muted"
            >
              Join thousands of gamers already using NoobGg to find the perfect teammates.
              It&apos;s free to start.
            </motion.p>
            <motion.div
              initial={{ opacity: 0, y: 20 }}
              whileInView={{ opacity: 1, y: 0 }}
              viewport={{ once: true }}
              transition={{ delay: 0.2 }}
              className="mt-8"
            >
              <Link to="/register">
                <motion.div whileHover={{ scale: 1.05 }} whileTap={{ scale: 0.98 }}>
                  <Button size="lg" className="px-10 text-base shadow-lg shadow-primary/25">
                    Get Started for Free
                  </Button>
                </motion.div>
              </Link>
            </motion.div>
          </div>
        </motion.div>
      </section>

      {/* Footer */}
      <footer className="relative z-10 border-t border-border/50 bg-surface/30">
        <div className="mx-auto flex max-w-6xl flex-col items-center justify-between gap-4 px-6 py-8 sm:flex-row">
          <div className="flex items-center gap-1 text-lg font-bold">
            <span className="text-primary">Noob</span>
            <span className="text-accent">Gg</span>
          </div>
          <div className="flex gap-6 text-sm text-foreground-muted">
            <Link to="/rooms" className="hover:text-foreground transition-colors">Rooms</Link>
            <Link to="/discover" className="hover:text-foreground transition-colors">Discover</Link>
            <Link to="/subscriptions" className="hover:text-foreground transition-colors">Pricing</Link>
          </div>
          <p className="text-xs text-foreground-subtle">&copy; 2026 NoobGg. All rights reserved.</p>
        </div>
      </footer>
    </div>
  );
}
