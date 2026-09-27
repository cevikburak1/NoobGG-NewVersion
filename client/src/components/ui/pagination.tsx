import { useTranslation } from 'react-i18next';
import { Button } from './button';

interface PaginationProps {
  page: number;
  pageSize: number;
  totalItems: number;
  onPageChange: (page: number) => void;
  className?: string;
}

export function Pagination({ page, pageSize, totalItems, onPageChange, className = '' }: PaginationProps) {
  const { t } = useTranslation();
  const totalPages = Math.ceil(totalItems / pageSize);
  if (totalPages <= 1) return null;

  const first = Math.max(1, Math.min(page - 2, totalPages - 4));
  const pages = Array.from({ length: Math.min(5, totalPages) }, (_, index) => first + index);
  const from = (page - 1) * pageSize + 1;
  const to = Math.min(page * pageSize, totalItems);

  return (
    <nav aria-label={t('Pagination')} className={`flex flex-wrap items-center justify-between gap-3 border-t border-border/70 pt-3 ${className}`}>
      <span className="text-xs text-foreground-muted">{from}-{to} / {totalItems}</span>
      <div className="flex items-center gap-1">
        <Button type="button" size="sm" variant="outline" disabled={page <= 1}
          onClick={() => onPageChange(page - 1)} aria-label={t('Previous')}>
          {t('Previous')}
        </Button>
        {pages.map((number) => (
          <button key={number} type="button" onClick={() => onPageChange(number)}
            aria-label={`${t('Page')} ${number}`} aria-current={number === page ? 'page' : undefined}
            className={`hidden h-8 min-w-8 border px-2 text-xs font-medium transition-colors sm:inline-flex sm:items-center sm:justify-center ${
              number === page ? 'border-primary bg-primary/15 text-primary' : 'border-border text-foreground-muted hover:text-foreground'
            }`}>
            {number}
          </button>
        ))}
        <span className="min-w-12 text-center text-xs text-foreground-muted sm:hidden">{page}/{totalPages}</span>
        <Button type="button" size="sm" variant="outline" disabled={page >= totalPages}
          onClick={() => onPageChange(page + 1)} aria-label={t('Next')}>
          {t('Next')}
        </Button>
      </div>
    </nav>
  );
}
