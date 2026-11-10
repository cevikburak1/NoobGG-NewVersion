import { Link } from 'react-router-dom';
import { motion } from 'framer-motion';
import { useTranslation } from 'react-i18next';
import { LanguageToggle } from '@/components/layout/languageToggle';
import { GameArtwork } from '@/components/common/gameArtwork';
import { Brand } from '@/components/common/brand';
import { Icon, type IconName } from '@/components/ui/icon';
import { useGameBrowse } from '@/features/games/hooks';

const paths: { number: string; icon: IconName; title: string; description: string; to: string }[] = [
  { number: '01', icon: 'compass', title: 'Find your squad', description: 'Browse active rooms or use quick match to meet players for your game.', to: '/rooms' },
  { number: '02', icon: 'people', title: 'Build a guild', description: 'Keep your team together with guild chat and shared events.', to: '/guilds' },
  { number: '03', icon: 'trophy', title: 'Compete', description: 'Enter tournaments and follow season standings.', to: '/tournaments' },
];
export default function LandingPage() {
  const { t } = useTranslation();
  const { data } = useGameBrowse({ multiplayer: true, pageSize: 6 });
  const games = data?.items ?? [];
  return <div className="min-h-screen text-foreground">
    <header className="relative z-20 mx-auto flex max-w-[1400px] items-center justify-between gap-3 px-5 py-5 sm:px-10">
      <Brand />
      <nav className="hidden items-center gap-8 text-xs font-medium text-foreground-muted lg:flex"><a href="#how-it-works" className="hover:text-primary">{t('How it works')}</a><Link to="/discover" className="hover:text-primary">{t('Discover')}</Link><Link to="/subscriptions" className="hover:text-primary">{t('Plans')}</Link></nav>
      <div className="flex items-center gap-3 sm:gap-5"><LanguageToggle /><Link to="/login" className="text-xs font-semibold text-foreground-muted hover:text-primary">{t('Sign in')}</Link><Link to="/register" className="hidden rounded-xl bg-primary px-5 py-3 text-xs font-bold text-primary-foreground hover:bg-primary-hover sm:block">{t('Sign up')} ↗︎</Link></div>
    </header>
    <main>
      <section className="landing-grid relative overflow-hidden border-b border-border/60">
        <div className="pointer-events-none absolute right-0 top-0 -z-10 h-[600px] w-[600px] rounded-full bg-primary/5 blur-[100px]" />
        <div className="mx-auto grid max-w-[1400px] items-center gap-10 px-5 pb-14 pt-12 sm:px-10 sm:pb-20 sm:pt-16 lg:min-h-[660px] lg:grid-cols-[1.05fr_1fr] lg:gap-16">
          <motion.div initial={{ opacity: 0, y: 20 }} animate={{ opacity: 1, y: 0 }} transition={{ duration: .55 }}>
            <div className="mb-7 inline-flex items-center gap-2 rounded-full border border-primary/20 bg-primary/5 px-3 py-2 text-[9px] font-bold uppercase tracking-[.16em] text-primary"><span className="h-1.5 w-1.5 rounded-full bg-primary" />{t('Play together. Stay together.')}</div>
            <h1 data-testid="landing-title" className="text-[clamp(3.25rem,6.2vw,6rem)] font-extrabold leading-[.98] tracking-[-.055em]">{t('Good teammates.')}<br /><span className="text-primary">{t('Great games.')}</span></h1>
            <p className="mt-7 max-w-md text-sm leading-7 text-foreground-muted sm:text-base">{t('Find players for your game, build a squad, and compete together.')}</p>
            <div className="mt-8 flex flex-wrap gap-3"><Link to="/register" className="inline-flex min-h-12 items-center gap-5 rounded-xl bg-primary px-6 text-sm font-bold text-primary-foreground hover:bg-primary-hover">{t('Find your people')}<Icon name="arrow" className="h-4 w-4" /></Link><Link to="/rooms" className="inline-flex min-h-12 items-center gap-3 rounded-xl border border-border bg-surface/80 px-5 text-sm font-semibold hover:border-border-hover">{t('Browse Rooms')}<Icon name="rooms" className="h-4 w-4 text-foreground-muted" /></Link></div>
            <div className="mt-8 flex flex-wrap items-center gap-5 text-[10px] font-medium text-foreground-subtle"><span className="flex items-center gap-2"><Icon name="shield" className="h-4 w-4 text-primary" />{t('Free to get started')}</span><span>{t('Built around your games')}</span></div>
          </motion.div>
          <motion.div initial={{ opacity: 0, y: 24 }} animate={{ opacity: 1, y: 0 }} transition={{ duration: .6, delay: .15 }} className="relative mx-auto w-full max-w-[550px] py-3">
            <div className="absolute -inset-4 rounded-[40px] border border-white/5 sm:-rotate-3" />
            <div className="relative rounded-3xl border border-border bg-[#11151df5] p-4 shadow-[0_30px_100px_#00000050] sm:p-5">
              <div className="mb-4 flex items-center justify-between"><div className="flex items-center gap-2"><span className="flex h-7 w-7 items-center justify-center rounded-lg bg-primary/15 text-primary"><Icon name="game" className="h-4 w-4" /></span><span className="text-xs font-bold">{t('Your next session')}</span></div><span className="rounded-full border border-border px-2 py-1 text-[8px] font-bold tracking-widest text-foreground-subtle">NOOBGG</span></div>
              <Link to={games[0] ? `/games/${games[0].id}` : '/discover'} className="game-tile relative block h-52 rounded-2xl sm:h-64">
                <GameArtwork src={games[0]?.backgroundImageUrl} name={games[0]?.name ?? t('Find your squad')} className="h-full w-full" />
                <div className="absolute bottom-5 left-5 right-5 z-10"><p className="mb-2 text-[8px] font-bold uppercase tracking-[.2em] text-primary">{t('Jump into the game')}</p><p className="font-display text-2xl font-extrabold">{games[0]?.name ?? t('Find your squad')}</p><div className="mt-3 flex items-center justify-between text-[10px] text-white/70"><span>{t('Find a room. Make it a team.')}</span><span className="flex h-7 w-7 items-center justify-center rounded-full bg-primary text-primary-foreground"><Icon name="arrow" className="h-3.5 w-3.5" /></span></div></div>
              </Link>
              <div className="mt-3 grid grid-cols-2 gap-3">{[1, 2].map((index) => <Link key={index} to={games[index] ? `/games/${games[index].id}` : '/discover'} className="game-tile relative h-28 rounded-xl sm:h-32"><GameArtwork src={games[index]?.backgroundImageUrl} name={games[index]?.name ?? t('Discover')} className="h-full w-full" /><div className="absolute bottom-3 left-3 right-3 z-10"><span className="text-[8px] uppercase tracking-wider text-white/60">{t('Discover')}</span><p className="truncate text-xs font-semibold">{games[index]?.name ?? t(index === 1 ? 'Build a guild' : 'Compete')}</p></div></Link>)}</div>
            </div>
            <Link to="/guilds" className="absolute -bottom-2 right-3 flex items-center gap-3 rounded-2xl border border-accent/30 bg-[#242032] px-4 py-3 shadow-xl sm:-right-4"><span className="flex h-9 w-9 items-center justify-center rounded-xl bg-accent/15 text-accent"><Icon name="people" /></span><div><p className="text-[8px] uppercase tracking-widest text-accent">{t('Better together')}</p><p className="mt-1 text-xs font-semibold">{t('A place for your team')}</p></div></Link>
          </motion.div>
        </div>
      </section>
      <section id="how-it-works" className="mx-auto max-w-[1400px] px-5 py-16 sm:px-10 sm:py-20">
        <div className="mb-8 flex flex-col justify-between gap-3 sm:flex-row sm:items-end"><div><p className="eyebrow mb-3">{t('Less searching. More playing.')}</p><h2 className="text-3xl font-extrabold sm:text-4xl">{t('This is where you belong.')}</h2></div><p className="max-w-xs text-xs leading-6 text-foreground-muted">{t('Your game. Your pace. Your people.')}</p></div>
        <div className="grid gap-4 md:grid-cols-3">{paths.map((path) => <Link to={path.to} key={path.number} className="group rounded-2xl border border-border bg-surface/85 p-6 transition-colors hover:border-primary/40 sm:p-7"><div className="flex items-center justify-between"><span className="flex h-11 w-11 items-center justify-center rounded-xl border border-border bg-background text-primary"><Icon name={path.icon} /></span><span className="font-display text-3xl font-bold text-foreground-subtle/30">{path.number}</span></div><h3 className="mt-7 text-xl font-bold group-hover:text-primary">{t(path.title)}</h3><p className="mt-3 min-h-16 text-xs leading-6 text-foreground-muted">{t(path.description)}</p><span className="mt-5 flex items-center gap-2 text-xs font-semibold text-primary">{t('Explore')}<Icon name="arrow" className="h-4 w-4" /></span></Link>)}</div>
      </section>
    </main>
    <footer className="border-t border-border px-5 py-7"><div className="mx-auto flex max-w-[1320px] flex-wrap items-center justify-between gap-5"><Brand /><p className="text-[10px] tracking-wide text-foreground-subtle">{t('Made for the way you play.')}</p><Link to="/subscriptions" className="text-xs text-foreground-muted hover:text-primary">{t('Explore plans')} ↗︎</Link></div></footer>
  </div>;
}
