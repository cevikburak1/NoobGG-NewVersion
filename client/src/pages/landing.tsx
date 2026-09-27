import { Link } from 'react-router-dom';
import { motion } from 'framer-motion';
import { useTranslation } from 'react-i18next';
import { LanguageToggle } from '@/components/layout/languageToggle';

const paths = [
  { number: '01', title: 'Find your squad', description: 'Browse active rooms or use quick match to meet players for your game.', to: '/rooms' },
  { number: '02', title: 'Build a guild', description: 'Keep your team together with guild chat and shared events.', to: '/guilds' },
  { number: '03', title: 'Compete', description: 'Enter tournaments and follow season standings.', to: '/tournaments' },
];

export default function LandingPage() {
  const { t } = useTranslation();
  return (
    <div className="min-h-screen text-foreground">
      <header className="relative z-20 mx-auto flex max-w-7xl items-center justify-between px-5 py-5">
        <Link to="/" className="text-xl font-black text-primary">Noob<span className="text-accent">Gg</span></Link>
        <nav className="flex items-center gap-4 text-sm">
          <LanguageToggle />
          <Link to="/login" className="text-foreground-muted hover:text-foreground">{t('Sign in')}</Link>
          <Link to="/register" className="border border-primary bg-primary px-4 py-2 font-semibold text-primary-foreground hover:bg-primary-hover">{t('Sign up')}</Link>
        </nav>
      </header>

      <main>
        <section className="relative flex min-h-[min(78vh,720px)] items-end px-5 pb-16 pt-12 sm:items-center sm:pb-12">
          <div className="mx-auto w-full max-w-7xl">
            <motion.div initial={{ opacity: 0, y: 24 }} animate={{ opacity: 1, y: 0 }} transition={{ duration: 0.7 }} className="max-w-3xl">
              <p className="mb-4 text-xs font-bold uppercase text-accent">{t('Play together. Stay together.')}</p>
              <h1 className="text-6xl font-black leading-none sm:text-8xl">NoobGg</h1>
              <p className="mt-5 max-w-xl text-lg leading-relaxed text-foreground-muted sm:text-xl">
                {t('Find players for your game, build a squad, and compete together.')}
              </p>
              <div className="mt-8 flex flex-wrap gap-3">
                <Link to="/register" className="border border-primary bg-primary px-6 py-3 text-sm font-bold text-primary-foreground hover:bg-primary-hover">
                  {t('Get Started')}
                </Link>
                <Link to="/rooms" className="border border-border bg-surface/85 px-6 py-3 text-sm font-bold text-foreground hover:border-primary">
                  {t('Browse Rooms')}
                </Link>
              </div>
            </motion.div>
          </div>
        </section>

        <section className="relative border-t border-border bg-background/90 px-5 py-14">
          <div className="mx-auto grid max-w-7xl gap-8 md:grid-cols-3">
            {paths.map((path) => (
              <Link to={path.to} key={path.number} className="group border-l-2 border-border pl-5 transition-colors hover:border-primary">
                <span className="text-xs font-bold text-accent">{path.number}</span>
                <h2 className="mt-3 text-xl font-semibold text-foreground group-hover:text-primary">{t(path.title)}</h2>
                <p className="mt-2 text-sm leading-relaxed text-foreground-muted">{t(path.description)}</p>
                <span className="mt-4 inline-block text-lg text-primary" aria-hidden="true">→</span>
              </Link>
            ))}
          </div>
        </section>
      </main>
    </div>
  );
}
