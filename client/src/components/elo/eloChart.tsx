import {
  Area,
  AreaChart,
  CartesianGrid,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts';
import type { EloSnapshot } from '@/features/elo/types';

interface ChartDataPoint {
  date: string;
  elo: number;
}

interface CustomTooltipProps {
  active?: boolean;
  payload?: Array<{ value: number; payload: ChartDataPoint }>;
}

function CustomTooltip({ active, payload }: CustomTooltipProps) {
  if (!active || !payload?.length) return null;
  const data = payload[0].payload;
  return (
    <div className="rounded-lg border border-gray-700 bg-gray-800 px-3 py-2 shadow-lg">
      <p className="text-sm font-bold text-white">{data.elo} Elo</p>
      <p className="text-xs text-gray-400 mt-0.5">{data.date}</p>
    </div>
  );
}

interface EloChartProps {
  history: EloSnapshot[];
  tierColor?: string;
  height?: number;
}

export function EloChart({ history, tierColor = '#6366f1', height = 200 }: EloChartProps) {
  const chartData: ChartDataPoint[] = history.map(s => ({
    date: new Date(s.recordedAt).toLocaleDateString('en-US', { month: 'short', day: 'numeric' }),
    elo: s.points,
  }));

  if (chartData.length === 0) return null;

  const elos = chartData.map(d => d.elo);
  const minElo = Math.min(...elos);
  const maxElo = Math.max(...elos);
  const padding = Math.max((maxElo - minElo) * 0.15, 50);

  return (
    <ResponsiveContainer width="100%" height={height}>
      <AreaChart data={chartData} margin={{ top: 5, right: 10, left: -10, bottom: 0 }}>
        <defs>
          <linearGradient id="eloGrad" x1="0" y1="0" x2="0" y2="1">
            <stop offset="5%" stopColor={tierColor} stopOpacity={0.3} />
            <stop offset="95%" stopColor={tierColor} stopOpacity={0} />
          </linearGradient>
        </defs>
        <CartesianGrid strokeDasharray="3 3" stroke="#374151" opacity={0.3} />
        <XAxis
          dataKey="date"
          tick={{ fill: '#9ca3af', fontSize: 11 }}
          tickLine={false}
          axisLine={{ stroke: '#374151' }}
          interval="preserveStartEnd"
        />
        <YAxis
          domain={[minElo - padding, maxElo + padding]}
          tick={{ fill: '#9ca3af', fontSize: 11 }}
          tickLine={false}
          axisLine={{ stroke: '#374151' }}
          width={45}
        />
        <Tooltip content={<CustomTooltip />} cursor={{ stroke: tierColor, strokeDasharray: '5 5' }} />
        <Area
          type="monotone"
          dataKey="elo"
          stroke={tierColor}
          strokeWidth={2}
          fill="url(#eloGrad)"
          activeDot={{ r: 5, fill: tierColor, stroke: '#1f2937', strokeWidth: 2 }}
          dot={false}
        />
      </AreaChart>
    </ResponsiveContainer>
  );
}
