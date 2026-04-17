import { useCallback, useMemo } from 'react';
import { useSearchParams } from 'react-router-dom';
import { COMPARE_QUERY_LEFT, COMPARE_QUERY_RIGHT, type CompareSlot } from './types';

function normalizeId(v: string | null): string | undefined {
  const t = v?.trim();
  return t ? t : undefined;
}

export function useCompareSearchParams() {
  const [params, setParams] = useSearchParams();

  const leftId = useMemo(() => normalizeId(params.get(COMPARE_QUERY_LEFT)), [params]);
  const rightId = useMemo(() => normalizeId(params.get(COMPARE_QUERY_RIGHT)), [params]);

  const setPair = useCallback(
    (left: string | undefined, right: string | undefined) => {
      const next = new URLSearchParams();
      if (left) next.set(COMPARE_QUERY_LEFT, left);
      if (right) next.set(COMPARE_QUERY_RIGHT, right);
      setParams(next, { replace: true });
    },
    [setParams],
  );

  const setSlot = useCallback(
    (slot: CompareSlot, userId: string | undefined) => {
      const next = new URLSearchParams(params);
      const key = slot === 'left' ? COMPARE_QUERY_LEFT : COMPARE_QUERY_RIGHT;
      if (userId) next.set(key, userId);
      else next.delete(key);
      setParams(next, { replace: true });
    },
    [params, setParams],
  );

  const swap = useCallback(() => {
    const l = normalizeId(params.get(COMPARE_QUERY_LEFT));
    const r = normalizeId(params.get(COMPARE_QUERY_RIGHT));
    const next = new URLSearchParams();
    if (r) next.set(COMPARE_QUERY_LEFT, r);
    if (l) next.set(COMPARE_QUERY_RIGHT, l);
    setParams(next, { replace: true });
  }, [params, setParams]);

  return { leftId, rightId, setPair, setSlot, swap };
}
