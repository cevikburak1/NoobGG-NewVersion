import { useState } from 'react';

export function usePagination<T>(items: readonly T[], pageSize: number, scope = '') {
  const [selection, setSelection] = useState({ scope, page: 1 });
  const totalPages = Math.max(1, Math.ceil(items.length / pageSize));
  const page = selection.scope === scope ? Math.min(selection.page, totalPages) : 1;

  const setPage = (nextPage: number) => {
    setSelection({ scope, page: Math.max(1, Math.min(nextPage, totalPages)) });
  };

  return {
    page,
    pageSize,
    totalPages,
    totalItems: items.length,
    items: items.slice((page - 1) * pageSize, page * pageSize),
    setPage,
  };
}
