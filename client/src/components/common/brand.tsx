import { Link } from 'react-router-dom';
export function Brand({ className = '' }: { className?: string }) {
  return <Link to="/" aria-label="NoobGG home" className={`inline-flex shrink-0 items-center gap-2.5 ${className}`}>
    <span className="flex h-9 w-9 items-center justify-center rounded-xl bg-primary text-primary-foreground">
      <svg viewBox="0 0 28 28" className="h-7 w-7" fill="none" aria-hidden="true"><path d="M7 19V9l7 10V9m7 1h-4v8h4v-4h-2" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round" /></svg>
    </span>
    <span className="font-display text-[23px] font-extrabold tracking-[-1px] text-foreground">noob<span className="text-primary">gg.</span></span>
  </Link>;
}
