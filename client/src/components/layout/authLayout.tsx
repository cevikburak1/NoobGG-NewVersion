import { Outlet, Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { LanguageToggle } from './languageToggle';
import { Brand } from '@/components/common/brand';
import { Icon } from '@/components/ui/icon';

export function AuthLayout() {
  const { t } = useTranslation();
  return <div className="grid min-h-dvh lg:grid-cols-2">
    <aside className="landing-grid relative hidden flex-col overflow-hidden border-r border-border bg-surface/70 p-12 lg:flex xl:p-16">
      <Brand />
      <div className="relative z-10 my-auto max-w-lg py-16">
        <p className="eyebrow mb-6">{t('Your next chapter starts here')}</p>
        <h2 className="text-6xl font-extrabold leading-[1.03]">{t('Good teammates.')}<br /><span className="text-primary">{t('Great games.')}</span></h2>
        <p className="mt-6 max-w-sm text-sm leading-7 text-foreground-muted">{t('Find players for your game, build a squad, and compete together.')}</p>
        <div className="relative mt-12 space-y-3">
          {([{ icon: 'people', text: 'Find your squad', color: 'text-primary' }, { icon: 'chat', text: 'Stay connected', color: 'text-accent' }, { icon: 'trophy', text: 'Play to your potential', color: 'text-warning' }] as const).map((item, i) => <div key={item.text} className="flex items-center gap-4 rounded-2xl border border-border bg-background/70 p-4" style={{ marginLeft: `${i * 20}px` }}><span className={`flex h-10 w-10 items-center justify-center rounded-xl bg-surface-active ${item.color}`}><Icon name={item.icon} /></span><p className="text-sm font-semibold">{t(item.text)}</p><Icon name="arrow" className="ml-auto h-4 w-4 text-foreground-subtle" /></div>)}
        </div>
      </div>
      <p className="text-[10px] font-medium uppercase tracking-[.2em] text-foreground-subtle">PLAY TOGETHER. STAY TOGETHER.</p>
      <div className="pointer-events-none absolute -bottom-40 -left-40 h-96 w-96 rounded-full bg-primary/5 blur-3xl" />
    </aside>
    <main className="relative flex flex-col px-6 pb-10 pt-7 sm:px-12">
      <div className="flex items-center justify-between"><div className="lg:hidden"><Brand /></div><Link to="/" className="hidden text-xs text-foreground-muted hover:text-primary lg:block">← {t('Back to home')}</Link><LanguageToggle /></div>
      <div className="mx-auto flex w-full max-w-sm flex-1 flex-col justify-center py-12"><div className="mb-7 flex h-12 w-12 items-center justify-center rounded-2xl border border-primary/20 bg-primary/10 text-primary"><Icon name="game" className="h-6 w-6" /></div><Outlet /><p className="mt-8 border-t border-border pt-5 text-[10px] leading-5 text-foreground-subtle">{t('Find your teammates, level up together')}</p></div>
    </main>
  </div>;
}
