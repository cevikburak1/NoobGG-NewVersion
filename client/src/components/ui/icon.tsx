import type { SVGProps } from 'react';
const paths = {
  arrow: 'M5 12h14m-6-6 6 6-6 6',
  search: 'm21 21-4.4-4.4M19 10.5a8.5 8.5 0 1 1-17 0 8.5 8.5 0 0 1 17 0Z',
  game: 'M7 7h10a4 4 0 0 1 3.9 3.1l1 6A3 3 0 0 1 17 19l-2-2H9l-2 2a3 3 0 0 1-4.9-2.9l1-6A4 4 0 0 1 7 7ZM6 11v4m-2-2h4m8-1h.01M19 14h.01',
  rooms: 'M3 4h7v7H3zm11 0h7v7h-7zM3 15h7v6H3zm11 0h7v6h-7z',
  people: 'M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2m20 0v-2a4 4 0 0 0-3-3.9M13 3.1a4 4 0 0 1 0 7.8M13 7a4 4 0 1 1-8 0 4 4 0 0 1 8 0Z',
  chat: 'M21 11.5a8.5 8.5 0 0 1-8.5 8.5H4l-3 3v-11A8.5 8.5 0 0 1 9.5 3h3a8.5 8.5 0 0 1 8.5 8.5ZM7 10h8m-8 4h5',
  compass: 'm16 8-3 5-5 3 3-5 5-3ZM22 12a10 10 0 1 1-20 0 10 10 0 0 1 20 0Z',
  spark: 'm12 2 2.5 7.5L22 12l-7.5 2.5L12 22l-2.5-7.5L2 12l7.5-2.5L12 2Z',
  bolt: 'm13 2-9 12h7l-1 8 10-12h-7l1-8Z',
  shield: 'm12 3 9 4v5c0 5-9 10-9 10S3 17 3 12V7l9-4Zm-4 9 3 3 5-6',
  trophy: 'M8 3h8v7a4 4 0 0 1-8 0V3Zm0 2H4v3a4 4 0 0 0 4 4m8-7h4v3a4 4 0 0 1-4 4m-4 2v6m-5 1h10',
  more: 'M5 12h.01M12 12h.01M19 12h.01',
  close: 'm6 6 12 12M6 18 18 6',
  plus: 'M12 5v14M5 12h14',
} as const;
export type IconName = keyof typeof paths;
export function Icon({ name, className = 'h-5 w-5', ...props }: SVGProps<SVGSVGElement> & { name: IconName }) {
  return <svg className={className} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={1.7} strokeLinecap="round" strokeLinejoin="round" aria-hidden="true" {...props}><path d={paths[name]} /></svg>;
}
