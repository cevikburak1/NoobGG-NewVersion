import { useMemo, useState, type ReactNode } from 'react';
import { Link } from 'react-router-dom';
import { AnimatePresence, motion } from 'framer-motion';
import {
  useReportDetail,
  useReports,
  useReviewReport,
} from '@/features/moderation/hooks';
import type {
  ReportDetailResponse,
  ReportResponse,
  ReviewAction,
  ReviewReportRequest,
  SuspensionDuration,
} from '@/features/moderation/types';
import type {
  ReportReason,
  ReportStatus,
  ReportTargetType,
} from '@/types/enums';
import type { ReportFilters } from '@/types/api';
import {
  AnimatedPage,
  Badge,
  Button,
  Modal,
  Select,
  Spinner,
  Textarea,
  staggerContainer,
  staggerItem,
} from '@/components/ui';
import { useToast } from '@/components/ui/toast';
import { cn } from '@/lib/cn';

const DEFAULT_PAGE_SIZE = 20;

type StatusFilter = ReportStatus | '';
type ReasonFilter = ReportReason | '';
type TargetFilter = ReportTargetType | '';

const STATUS_OPTIONS: { value: StatusFilter; label: string }[] = [
  { value: '', label: 'All statuses' },
  { value: 'Pending', label: 'Pending' },
  { value: 'Reviewed', label: 'Reviewed' },
  { value: 'Resolved', label: 'Resolved' },
  { value: 'Dismissed', label: 'Dismissed' },
];

const REASON_OPTIONS: { value: ReasonFilter; label: string }[] = [
  { value: '', label: 'All reasons' },
  { value: 'Harassment', label: 'Harassment' },
  { value: 'Spam', label: 'Spam' },
  { value: 'Cheating', label: 'Cheating' },
  { value: 'Inappropriate', label: 'Inappropriate' },
  { value: 'Other', label: 'Other' },
];

const TARGET_OPTIONS: { value: TargetFilter; label: string }[] = [
  { value: '', label: 'All targets' },
  { value: 'User', label: 'User' },
  { value: 'Room', label: 'Room' },
];

const SUSPENSION_OPTIONS: { value: SuspensionDuration; label: string }[] = [
  { value: '1d', label: '24 hours' },
  { value: '7d', label: '7 days' },
  { value: '30d', label: '30 days' },
];

const STATUS_BADGE: Record<ReportStatus, { variant: BadgeVariant; accent: string }> = {
  Pending: { variant: 'warning', accent: 'border-l-warning' },
  Reviewed: { variant: 'primary', accent: 'border-l-primary' },
  Resolved: { variant: 'success', accent: 'border-l-success' },
  Dismissed: { variant: 'default', accent: 'border-l-border' },
};

const REASON_VARIANT: Record<ReportReason, BadgeVariant> = {
  Harassment: 'danger',
  Cheating: 'danger',
  Inappropriate: 'danger',
  Spam: 'warning',
  Other: 'default',
};

const ACTION_META: Record<
  ReviewAction,
  {
    label: string;
    tone: 'warn' | 'suspend' | 'ban' | 'dismiss';
    description: string;
    resultingStatus: ReportStatus;
  }
> = {
  warn: {
    label: 'Warn',
    tone: 'warn',
    description: 'Issue a formal warning. Report stays Reviewed for follow-up.',
    resultingStatus: 'Reviewed',
  },
  suspend: {
    label: 'Suspend',
    tone: 'suspend',
    description: 'Temporarily restrict the reported user. Closes report as Resolved.',
    resultingStatus: 'Resolved',
  },
  ban: {
    label: 'Ban',
    tone: 'ban',
    description: 'Permanently remove the reported user. Closes report as Resolved.',
    resultingStatus: 'Resolved',
  },
  dismiss: {
    label: 'Dismiss',
    tone: 'dismiss',
    description: 'Report is not actionable. Reporter is notified it was dismissed.',
    resultingStatus: 'Dismissed',
  },
};

type BadgeVariant = 'default' | 'primary' | 'accent' | 'danger' | 'warning' | 'success';

export default function ModerationPage() {
  const [statusFilter, setStatusFilter] = useState<StatusFilter>('Pending');
  const [reasonFilter, setReasonFilter] = useState<ReasonFilter>('');
  const [targetFilter, setTargetFilter] = useState<TargetFilter>('');
  const [page, setPage] = useState(1);
  const [selectedReportId, setSelectedReportId] = useState<string | null>(null);

  const filters: ReportFilters = useMemo(
    () => ({
      status: statusFilter || undefined,
      reason: reasonFilter || undefined,
      targetType: targetFilter || undefined,
      page,
      pageSize: DEFAULT_PAGE_SIZE,
    }),
    [statusFilter, reasonFilter, targetFilter, page],
  );

  const hasActiveFilters =
    Boolean(statusFilter) || Boolean(reasonFilter) || Boolean(targetFilter);

  const reports = useReports(filters);

  const handleFilterChange = <T extends string>(
    setter: (v: T) => void,
    value: T,
  ) => {
    setter(value);
    setPage(1);
  };

  const handleReset = () => {
    setStatusFilter('');
    setReasonFilter('');
    setTargetFilter('');
    setPage(1);
  };

  const totalPages = reports.data
    ? Math.max(1, Math.ceil(reports.data.totalCount / DEFAULT_PAGE_SIZE))
    : 1;

  return (
    <AnimatedPage>
      <div className="relative space-y-8">
        <DecorativeBackdrop />

        <Header totalCount={reports.data?.totalCount} isLoading={reports.isLoading} />

        <FiltersBar
          status={statusFilter}
          reason={reasonFilter}
          target={targetFilter}
          hasActiveFilters={hasActiveFilters}
          onStatusChange={(v) => handleFilterChange(setStatusFilter, v)}
          onReasonChange={(v) => handleFilterChange(setReasonFilter, v)}
          onTargetChange={(v) => handleFilterChange(setTargetFilter, v)}
          onReset={handleReset}
        />

        {reports.isLoading && <LoadingSkeleton />}

        {reports.isError && !reports.isLoading && (
          <ErrorState onRetry={() => reports.refetch()} />
        )}

        {!reports.isLoading &&
          !reports.isError &&
          reports.data &&
          reports.data.items.length === 0 && (
            <EmptyState
              hasActiveFilters={hasActiveFilters}
              onReset={handleReset}
            />
          )}

        {!reports.isLoading &&
          !reports.isError &&
          reports.data &&
          reports.data.items.length > 0 && (
            <>
              <motion.ul
                variants={staggerContainer}
                initial="hidden"
                animate="show"
                className="flex flex-col gap-3"
              >
                {reports.data.items.map((report) => (
                  <motion.li key={report.id} variants={staggerItem}>
                    <ReportListItem
                      report={report}
                      onReview={() => setSelectedReportId(report.id)}
                    />
                  </motion.li>
                ))}
              </motion.ul>

              {(reports.data.hasNextPage || reports.data.hasPreviousPage) && (
                <PaginationBar
                  page={reports.data.page}
                  totalPages={totalPages}
                  totalCount={reports.data.totalCount}
                  pageSize={reports.data.pageSize}
                  hasNext={reports.data.hasNextPage}
                  hasPrev={reports.data.hasPreviousPage}
                  onPrev={() => setPage((p) => Math.max(1, p - 1))}
                  onNext={() => setPage((p) => p + 1)}
                />
              )}
            </>
          )}

        <ReviewReportModal
          reportId={selectedReportId}
          onClose={() => setSelectedReportId(null)}
        />
      </div>
    </AnimatedPage>
  );
}

function Header({
  totalCount,
  isLoading,
}: {
  totalCount: number | undefined;
  isLoading: boolean;
}) {
  return (
    <header className="relative flex flex-col gap-3 border-b border-border/60 pb-6">
      <div className="flex items-center gap-3">
        <span className="flex h-9 w-9 items-center justify-center rounded-lg border border-border bg-surface">
          <ShieldIcon className="h-4 w-4 text-primary" />
        </span>
        <span className="text-[11px] font-semibold uppercase tracking-[0.2em] text-foreground-subtle">
          Staff · Moderation
        </span>
      </div>

      <div className="flex flex-wrap items-end justify-between gap-4">
        <div>
          <h1 className="text-3xl font-bold tracking-tight text-foreground sm:text-4xl">
            Reports Queue
          </h1>
          <p className="mt-1.5 max-w-2xl text-sm text-foreground-muted">
            Review community reports, issue warnings, or close cases. Every
            action is audited and the reporter is notified of the outcome.
          </p>
        </div>

        <div className="rounded-lg border border-border bg-surface px-4 py-3">
          <div className="text-[10px] font-semibold uppercase tracking-wider text-foreground-subtle">
            Matching reports
          </div>
          <div className="mt-0.5 flex items-baseline gap-2">
            {isLoading ? (
              <div className="h-7 w-14 animate-pulse rounded bg-surface-hover" />
            ) : (
              <span className="font-mono text-2xl font-bold tabular-nums text-foreground">
                {totalCount ?? 0}
              </span>
            )}
            <span className="text-xs text-foreground-muted">total</span>
          </div>
        </div>
      </div>
    </header>
  );
}

function DecorativeBackdrop() {
  return (
    <div
      aria-hidden
      className="pointer-events-none absolute inset-x-0 -top-4 -z-10 h-40 overflow-hidden opacity-60"
    >
      <div
        className="absolute inset-0 opacity-[0.04]"
        style={{
          backgroundImage:
            'linear-gradient(to right, currentColor 1px, transparent 1px), linear-gradient(to bottom, currentColor 1px, transparent 1px)',
          backgroundSize: '28px 28px',
          color: 'var(--color-foreground, #fff)',
        }}
      />
    </div>
  );
}

function FiltersBar({
  status,
  reason,
  target,
  hasActiveFilters,
  onStatusChange,
  onReasonChange,
  onTargetChange,
  onReset,
}: {
  status: StatusFilter;
  reason: ReasonFilter;
  target: TargetFilter;
  hasActiveFilters: boolean;
  onStatusChange: (v: StatusFilter) => void;
  onReasonChange: (v: ReasonFilter) => void;
  onTargetChange: (v: TargetFilter) => void;
  onReset: () => void;
}) {
  return (
    <div className="sticky top-16 z-10 -mx-1 rounded-xl border border-border bg-surface/80 p-3 backdrop-blur-sm">
      <div className="flex flex-wrap items-end gap-3">
        <div className="flex items-center gap-2 pl-1 pr-2">
          <FilterIcon className="h-4 w-4 text-foreground-muted" />
          <span className="text-xs font-semibold uppercase tracking-wider text-foreground-muted">
            Filters
          </span>
        </div>

        <div className="min-w-[160px] flex-1">
          <Select
            aria-label="Status"
            value={status}
            onChange={(e) => onStatusChange(e.target.value as StatusFilter)}
            options={STATUS_OPTIONS}
          />
        </div>
        <div className="min-w-[160px] flex-1">
          <Select
            aria-label="Reason"
            value={reason}
            onChange={(e) => onReasonChange(e.target.value as ReasonFilter)}
            options={REASON_OPTIONS}
          />
        </div>
        <div className="min-w-[160px] flex-1">
          <Select
            aria-label="Target"
            value={target}
            onChange={(e) => onTargetChange(e.target.value as TargetFilter)}
            options={TARGET_OPTIONS}
          />
        </div>

        <Button
          variant="ghost"
          size="sm"
          onClick={onReset}
          disabled={!hasActiveFilters}
          className="shrink-0"
        >
          Reset
        </Button>
      </div>
    </div>
  );
}

function ReportListItem({
  report,
  onReview,
}: {
  report: ReportResponse;
  onReview: () => void;
}) {
  const statusMeta = STATUS_BADGE[report.status];

  return (
    <button
      type="button"
      onClick={onReview}
      className={cn(
        'group w-full rounded-xl border border-border bg-surface p-0 text-left transition-all',
        'hover:border-primary/40 hover:bg-surface-hover/40 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary/40',
      )}
    >
      <div className={cn('border-l-2 p-4 pl-5', statusMeta.accent)}>
        <div className="flex flex-wrap items-start justify-between gap-3">
          <div className="flex flex-wrap items-center gap-2">
            <Badge variant={REASON_VARIANT[report.reason]}>
              <span className="uppercase tracking-wider">{report.reason}</span>
            </Badge>
            <Badge variant={statusMeta.variant}>
              <span className="uppercase tracking-wider">{report.status}</span>
            </Badge>
            <span className="rounded-md border border-border/60 bg-surface-hover/40 px-1.5 py-0.5 text-[10px] font-semibold uppercase tracking-wider text-foreground-muted">
              {report.targetType}
            </span>
          </div>

          <div className="flex shrink-0 items-center gap-3">
            <RelativeTime value={report.createdAt} />
            <Button
              size="sm"
              variant="outline"
              onClick={(e) => {
                e.stopPropagation();
                onReview();
              }}
              className="shrink-0"
            >
              Review
            </Button>
          </div>
        </div>

        <div className="mt-3 flex flex-wrap items-baseline gap-x-4 gap-y-1 text-sm">
          <span className="text-foreground-subtle">Reported</span>
          <ReportedTarget
            username={report.reportedUsername}
            userId={report.reportedUserId}
            roomTitle={report.roomTitle}
            roomId={report.roomId}
            targetType={report.targetType}
          />
        </div>

        {report.description && (
          <p className="mt-2 line-clamp-2 text-sm text-foreground-muted">
            {report.description}
          </p>
        )}

        <div className="mt-3 flex items-center gap-2 text-[11px] text-foreground-subtle">
          <span className="font-mono">
            #{report.id.slice(0, 8).toUpperCase()}
          </span>
        </div>
      </div>
    </button>
  );
}

function ReportedTarget({
  username,
  userId,
  roomTitle,
  roomId,
  targetType,
}: {
  username: string | null;
  userId: string;
  roomTitle: string | null;
  roomId: string | null;
  targetType: ReportTargetType;
}) {
  if (targetType === 'Room' && roomId) {
    return (
      <span className="flex items-center gap-1.5">
        <Link
          to={`/rooms/${roomId}`}
          onClick={(e) => e.stopPropagation()}
          className="font-semibold text-foreground hover:text-primary"
        >
          {roomTitle ?? 'Unknown room'}
        </Link>
        {username && (
          <span className="text-xs text-foreground-muted">
            · owner {username}
          </span>
        )}
      </span>
    );
  }

  return (
    <Link
      to={`/profile/${userId}`}
      onClick={(e) => e.stopPropagation()}
      className="font-semibold text-foreground hover:text-primary"
    >
      {username ?? 'Unknown user'}
    </Link>
  );
}

function RelativeTime({ value }: { value: string }) {
  return (
    <time
      dateTime={value}
      title={new Date(value).toLocaleString()}
      className="text-xs text-foreground-muted"
    >
      {formatRelativeTime(value)}
    </time>
  );
}

function LoadingSkeleton() {
  return (
    <div className="flex flex-col gap-3">
      {Array.from({ length: 5 }).map((_, i) => (
        <div
          key={i}
          className="flex flex-col gap-3 rounded-xl border border-border bg-surface p-4"
        >
          <div className="flex gap-2">
            <div className="h-5 w-20 animate-pulse rounded-full bg-surface-hover" />
            <div className="h-5 w-20 animate-pulse rounded-full bg-surface-hover" />
          </div>
          <div className="h-4 w-2/3 animate-pulse rounded bg-surface-hover" />
          <div className="h-3 w-full animate-pulse rounded bg-surface-hover" />
        </div>
      ))}
    </div>
  );
}

function ErrorState({ onRetry }: { onRetry: () => void }) {
  return (
    <div className="flex flex-col items-center gap-3 rounded-xl border border-danger/30 bg-danger/5 p-10 text-center">
      <ShieldAlertIcon className="h-8 w-8 text-danger" />
      <div>
        <p className="text-base font-semibold text-foreground">
          Failed to load reports
        </p>
        <p className="mt-1 text-sm text-foreground-muted">
          Check your connection or permissions and try again.
        </p>
      </div>
      <Button variant="outline" size="sm" onClick={onRetry}>
        Retry
      </Button>
    </div>
  );
}

function EmptyState({
  hasActiveFilters,
  onReset,
}: {
  hasActiveFilters: boolean;
  onReset: () => void;
}) {
  return (
    <motion.div
      initial={{ opacity: 0, scale: 0.98 }}
      animate={{ opacity: 1, scale: 1 }}
      className="flex flex-col items-center gap-3 rounded-xl border border-border bg-surface p-14 text-center"
    >
      <div className="flex h-16 w-16 items-center justify-center rounded-full border border-border bg-surface-hover">
        <ShieldCheckIcon className="h-7 w-7 text-success" />
      </div>
      <div>
        <p className="text-base font-semibold text-foreground">
          {hasActiveFilters
            ? 'No reports match these filters'
            : 'Queue is clear'}
        </p>
        <p className="mt-1 max-w-sm text-sm text-foreground-muted">
          {hasActiveFilters
            ? 'Try loosening the filters to see more reports.'
            : 'There are no reports to review right now. Nice work.'}
        </p>
      </div>
      {hasActiveFilters && (
        <Button variant="outline" size="sm" onClick={onReset}>
          Clear filters
        </Button>
      )}
    </motion.div>
  );
}

function PaginationBar({
  page,
  totalPages,
  totalCount,
  pageSize,
  hasNext,
  hasPrev,
  onPrev,
  onNext,
}: {
  page: number;
  totalPages: number;
  totalCount: number;
  pageSize: number;
  hasNext: boolean;
  hasPrev: boolean;
  onPrev: () => void;
  onNext: () => void;
}) {
  const from = (page - 1) * pageSize + 1;
  const to = Math.min(page * pageSize, totalCount);
  return (
    <div className="flex items-center justify-between border-t border-border/60 pt-4">
      <span className="text-xs text-foreground-muted">
        Showing{' '}
        <span className="font-mono font-semibold tabular-nums text-foreground">
          {from}–{to}
        </span>{' '}
        of{' '}
        <span className="font-mono font-semibold tabular-nums text-foreground">
          {totalCount}
        </span>
      </span>
      <div className="flex items-center gap-2">
        <Button variant="ghost" size="sm" disabled={!hasPrev} onClick={onPrev}>
          Previous
        </Button>
        <span className="font-mono text-xs tabular-nums text-foreground-muted">
          {page} / {totalPages}
        </span>
        <Button variant="ghost" size="sm" disabled={!hasNext} onClick={onNext}>
          Next
        </Button>
      </div>
    </div>
  );
}

function ReviewReportModal({
  reportId,
  onClose,
}: {
  reportId: string | null;
  onClose: () => void;
}) {
  const isOpen = Boolean(reportId);
  const detail = useReportDetail(reportId ?? undefined);
  const review = useReviewReport();
  const { addToast } = useToast();

  const [view, setView] = useState<'detail' | 'confirm'>('detail');
  const [pendingAction, setPendingAction] = useState<ReviewAction | null>(null);
  const [reviewNote, setReviewNote] = useState('');
  const [suspendDuration, setSuspendDuration] =
    useState<SuspensionDuration>('7d');

  const resetInternalState = () => {
    setView('detail');
    setPendingAction(null);
    setReviewNote('');
    setSuspendDuration('7d');
  };

  const handleClose = () => {
    if (review.isPending) return;
    resetInternalState();
    onClose();
  };

  const handleSelectAction = (action: ReviewAction) => {
    setPendingAction(action);
    setReviewNote('');
    setView('confirm');
  };

  const handleBack = () => {
    if (review.isPending) return;
    setView('detail');
    setPendingAction(null);
  };

  const handleConfirm = async () => {
    if (!reportId || !pendingAction) return;
    const payload = mapActionToReviewRequest(
      pendingAction,
      reviewNote,
      suspendDuration,
    );
    try {
      await review.mutateAsync({ id: reportId, data: payload });
      addToast({
        title: `Report ${ACTION_META[pendingAction].resultingStatus.toLowerCase()}`,
        message: `Action "${ACTION_META[pendingAction].label}" applied successfully.`,
        type: 'success',
      });
      resetInternalState();
      onClose();
    } catch (error) {
      addToast({
        title: 'Review failed',
        message:
          error instanceof Error
            ? error.message
            : 'Could not submit review. Please try again.',
        type: 'error',
      });
    }
  };

  return (
    <Modal
      isOpen={isOpen}
      onClose={handleClose}
      className="max-w-2xl"
    >
      {detail.isLoading && (
        <div className="flex items-center justify-center py-10">
          <Spinner size="lg" />
        </div>
      )}

      {detail.isError && !detail.isLoading && (
        <div className="flex flex-col items-center gap-3 py-10 text-center">
          <ShieldAlertIcon className="h-8 w-8 text-danger" />
          <p className="text-sm text-foreground-muted">
            Failed to load report details.
          </p>
          <div className="flex gap-2">
            <Button variant="outline" size="sm" onClick={() => detail.refetch()}>
              Retry
            </Button>
            <Button variant="ghost" size="sm" onClick={handleClose}>
              Close
            </Button>
          </div>
        </div>
      )}

      {detail.data && (
        <AnimatePresence mode="wait">
          {view === 'detail' ? (
            <motion.div
              key="detail"
              initial={{ opacity: 0, x: -8 }}
              animate={{ opacity: 1, x: 0 }}
              exit={{ opacity: 0, x: -8 }}
              transition={{ duration: 0.15 }}
            >
              <ReportDetailView report={detail.data} onClose={handleClose} />
              <ReviewActionsRow
                reportStatus={detail.data.status}
                onSelect={handleSelectAction}
              />
            </motion.div>
          ) : (
            <motion.div
              key="confirm"
              initial={{ opacity: 0, x: 8 }}
              animate={{ opacity: 1, x: 0 }}
              exit={{ opacity: 0, x: 8 }}
              transition={{ duration: 0.15 }}
            >
              <ConfirmActionView
                action={pendingAction!}
                report={detail.data}
                reviewNote={reviewNote}
                onNoteChange={setReviewNote}
                suspendDuration={suspendDuration}
                onSuspendDurationChange={setSuspendDuration}
                isSubmitting={review.isPending}
                onBack={handleBack}
                onConfirm={handleConfirm}
              />
            </motion.div>
          )}
        </AnimatePresence>
      )}
    </Modal>
  );
}

function ReportDetailView({
  report,
  onClose,
}: {
  report: ReportDetailResponse;
  onClose: () => void;
}) {
  const statusMeta = STATUS_BADGE[report.status];
  const alreadyResolved =
    report.status === 'Resolved' || report.status === 'Dismissed';

  return (
    <div>
      <div className="mb-5 flex items-start justify-between gap-3">
        <div className="space-y-1.5">
          <div className="flex flex-wrap items-center gap-2">
            <Badge variant={REASON_VARIANT[report.reason]}>
              <span className="uppercase tracking-wider">{report.reason}</span>
            </Badge>
            <Badge variant={statusMeta.variant}>
              <span className="uppercase tracking-wider">{report.status}</span>
            </Badge>
            <span className="rounded-md border border-border/60 bg-surface-hover/40 px-1.5 py-0.5 text-[10px] font-semibold uppercase tracking-wider text-foreground-muted">
              {report.targetType}
            </span>
          </div>
          <h2 className="text-lg font-bold text-foreground">Report details</h2>
          <p className="font-mono text-[11px] text-foreground-subtle">
            #{report.id.slice(0, 8).toUpperCase()}
          </p>
        </div>
        <button
          onClick={onClose}
          className="text-foreground-subtle transition-colors hover:text-foreground"
          aria-label="Close"
        >
          <CloseIcon className="h-5 w-5" />
        </button>
      </div>

      <div className="grid gap-4 sm:grid-cols-2">
        <DetailBlock label="Reporter">
          <Link
            to={`/profile/${report.reporterId}`}
            className="font-semibold text-foreground hover:text-primary"
          >
            {report.reporterUsername ?? 'Unknown user'}
          </Link>
        </DetailBlock>

        <DetailBlock label="Reported user">
          <Link
            to={`/profile/${report.reportedUserId}`}
            className="font-semibold text-foreground hover:text-primary"
          >
            {report.reportedUsername ?? 'Unknown user'}
          </Link>
        </DetailBlock>

        {report.roomId && (
          <DetailBlock label="Room">
            <Link
              to={`/rooms/${report.roomId}`}
              className="font-semibold text-foreground hover:text-primary"
            >
              {report.roomTitle ?? 'Untitled room'}
            </Link>
          </DetailBlock>
        )}

        <DetailBlock label="Filed">
          <span className="text-foreground-muted">
            {new Date(report.createdAt).toLocaleString()}
          </span>
        </DetailBlock>
      </div>

      {report.description && (
        <div className="mt-5 rounded-lg border border-border/70 bg-surface-hover/30 p-4">
          <div className="mb-1.5 text-[10px] font-semibold uppercase tracking-wider text-foreground-subtle">
            Description
          </div>
          <p className="whitespace-pre-wrap text-sm text-foreground">
            {report.description}
          </p>
        </div>
      )}

      {alreadyResolved && report.reviewedBy && (
        <div className="mt-5 rounded-lg border border-border/70 bg-surface-hover/30 p-4">
          <div className="mb-1.5 flex items-center gap-1.5 text-[10px] font-semibold uppercase tracking-wider text-foreground-subtle">
            <ShieldCheckIcon className="h-3 w-3" />
            Previously reviewed
          </div>
          <div className="flex flex-wrap items-baseline gap-x-3 gap-y-1 text-sm">
            <span className="font-semibold text-foreground">
              {report.reviewerUsername ?? 'Unknown moderator'}
            </span>
            {report.reviewedAt && (
              <span className="text-foreground-muted">
                on {new Date(report.reviewedAt).toLocaleString()}
              </span>
            )}
          </div>
          {report.reviewNote && (
            <p className="mt-2 whitespace-pre-wrap text-sm text-foreground-muted">
              {report.reviewNote}
            </p>
          )}
        </div>
      )}
    </div>
  );
}

function DetailBlock({
  label,
  children,
}: {
  label: string;
  children: ReactNode;
}) {
  return (
    <div className="rounded-lg border border-border/70 bg-surface-hover/20 p-3">
      <div className="mb-1 text-[10px] font-semibold uppercase tracking-wider text-foreground-subtle">
        {label}
      </div>
      <div className="text-sm">{children}</div>
    </div>
  );
}

function ReviewActionsRow({
  reportStatus,
  onSelect,
}: {
  reportStatus: ReportStatus;
  onSelect: (action: ReviewAction) => void;
}) {
  const isClosed = reportStatus === 'Resolved' || reportStatus === 'Dismissed';

  return (
    <div className="mt-6 border-t border-border pt-5">
      <div className="mb-3 flex items-center justify-between">
        <span className="text-[11px] font-semibold uppercase tracking-[0.18em] text-foreground-subtle">
          Moderator actions
        </span>
        {isClosed && (
          <span className="text-[11px] text-foreground-muted">
            Re-reviewing this report will overwrite its note and status.
          </span>
        )}
      </div>
      <div className="grid grid-cols-2 gap-2 sm:grid-cols-4">
        <ActionButton tone="warn" onClick={() => onSelect('warn')}>
          Warn
        </ActionButton>
        <ActionButton tone="suspend" onClick={() => onSelect('suspend')}>
          Suspend
        </ActionButton>
        <ActionButton tone="ban" onClick={() => onSelect('ban')}>
          Ban
        </ActionButton>
        <ActionButton tone="dismiss" onClick={() => onSelect('dismiss')}>
          Dismiss
        </ActionButton>
      </div>
    </div>
  );
}

const ACTION_TONE_STYLES: Record<ReviewAction, string> = {
  warn: 'border-warning/40 text-warning hover:bg-warning/10',
  suspend: 'border-orange-500/40 text-orange-400 hover:bg-orange-500/10',
  ban: 'border-danger/40 text-danger hover:bg-danger/10',
  dismiss: 'border-border text-foreground-muted hover:bg-surface-hover',
};

function ActionButton({
  tone,
  onClick,
  children,
}: {
  tone: ReviewAction;
  onClick: () => void;
  children: ReactNode;
}) {
  const toneIcons: Record<ReviewAction, ReactNode> = {
    warn: <WarningIcon className="h-3.5 w-3.5" />,
    suspend: <ClockIcon className="h-3.5 w-3.5" />,
    ban: <BanIcon className="h-3.5 w-3.5" />,
    dismiss: <DismissIcon className="h-3.5 w-3.5" />,
  };

  return (
    <button
      type="button"
      onClick={onClick}
      className={cn(
        'flex items-center justify-center gap-2 rounded-lg border bg-surface px-3 py-2.5 text-sm font-semibold uppercase tracking-wider transition-all',
        'focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary/40',
        ACTION_TONE_STYLES[tone],
      )}
    >
      {toneIcons[tone]}
      <span>{children}</span>
    </button>
  );
}

function ConfirmActionView({
  action,
  report,
  reviewNote,
  onNoteChange,
  suspendDuration,
  onSuspendDurationChange,
  isSubmitting,
  onBack,
  onConfirm,
}: {
  action: ReviewAction;
  report: ReportDetailResponse;
  reviewNote: string;
  onNoteChange: (v: string) => void;
  suspendDuration: SuspensionDuration;
  onSuspendDurationChange: (v: SuspensionDuration) => void;
  isSubmitting: boolean;
  onBack: () => void;
  onConfirm: () => void;
}) {
  const meta = ACTION_META[action];
  const needsDuration = action === 'suspend';
  const noteLimit = 2000;
  const composedPayload = mapActionToReviewRequest(
    action,
    reviewNote,
    suspendDuration,
  );
  const composedNoteLength = composedPayload.reviewNote?.length ?? 0;
  const isNoteTooLong = composedNoteLength > noteLimit;
  const requiresNote =
    action === 'warn' || action === 'suspend' || action === 'ban';
  const isNoteRequired = requiresNote && reviewNote.trim().length === 0;

  return (
    <div>
      <div className="mb-4">
        <button
          type="button"
          onClick={onBack}
          disabled={isSubmitting}
          className="group mb-3 inline-flex items-center gap-1.5 text-xs font-medium text-foreground-muted transition-colors hover:text-foreground disabled:opacity-50"
        >
          <ChevronLeftIcon className="h-3.5 w-3.5 transition-transform group-hover:-translate-x-0.5" />
          Back to details
        </button>
        <div className="space-y-1">
          <div className="flex items-center gap-2">
            <span className="text-[11px] font-semibold uppercase tracking-[0.2em] text-foreground-subtle">
              Confirm action
            </span>
          </div>
          <h2 className="text-xl font-bold tracking-tight text-foreground">
            {meta.label}{' '}
            <span className="text-foreground-muted">
              {report.reportedUsername ?? 'user'}
            </span>
          </h2>
          <p className="text-sm text-foreground-muted">{meta.description}</p>
        </div>
      </div>

      <div className="space-y-4">
        <div className="flex flex-wrap items-center gap-2 rounded-lg border border-border/70 bg-surface-hover/20 p-3 text-xs">
          <span className="text-foreground-subtle">Resulting status</span>
          <Badge variant={STATUS_BADGE[meta.resultingStatus].variant}>
            <span className="uppercase tracking-wider">
              {meta.resultingStatus}
            </span>
          </Badge>
        </div>

        {needsDuration && (
          <Select
            label="Suspension duration"
            value={suspendDuration}
            onChange={(e) =>
              onSuspendDurationChange(e.target.value as SuspensionDuration)
            }
            options={SUSPENSION_OPTIONS}
          />
        )}

        <div>
          <Textarea
            id="review-note"
            label={
              requiresNote
                ? 'Note to attach (required)'
                : 'Note to attach (optional)'
            }
            rows={4}
            placeholder={
              action === 'dismiss'
                ? 'Optional: why is this report not actionable?'
                : 'Explain the context and the action taken...'
            }
            value={reviewNote}
            onChange={(e) => onNoteChange(e.target.value)}
            error={
              isNoteTooLong
                ? `Note plus action prefix exceeds limit (${composedNoteLength} / ${noteLimit}).`
                : undefined
            }
          />
          <div className="mt-1 flex items-center justify-between text-[11px] text-foreground-subtle">
            <span>
              Stored verbatim in the audit log. Visible to the reporter.
            </span>
            <span className="font-mono tabular-nums">
              {composedNoteLength} / {noteLimit}
            </span>
          </div>
        </div>
      </div>

      <div className="mt-6 flex items-center justify-end gap-2 border-t border-border pt-4">
        <Button
          variant="ghost"
          size="md"
          onClick={onBack}
          disabled={isSubmitting}
        >
          Cancel
        </Button>
        <Button
          variant={action === 'dismiss' ? 'outline' : 'primary'}
          size="md"
          onClick={onConfirm}
          isLoading={isSubmitting}
          disabled={isSubmitting || isNoteRequired || isNoteTooLong}
        >
          {isSubmitting ? 'Submitting...' : `Confirm ${meta.label.toLowerCase()}`}
        </Button>
      </div>
    </div>
  );
}

function mapActionToReviewRequest(
  action: ReviewAction,
  note: string,
  duration?: SuspensionDuration,
): ReviewReportRequest {
  const trimmed = note.trim();
  switch (action) {
    case 'warn':
      return {
        status: 'Reviewed',
        reviewNote: `[Warning] ${trimmed}`.trim(),
      };
    case 'suspend':
      return {
        status: 'Resolved',
        reviewNote: `[Suspended: ${duration ?? '7d'}] ${trimmed}`.trim(),
      };
    case 'ban':
      return {
        status: 'Resolved',
        reviewNote: `[Banned] ${trimmed}`.trim(),
      };
    case 'dismiss':
      return {
        status: 'Dismissed',
        reviewNote: trimmed.length > 0 ? trimmed : undefined,
      };
  }
}

function formatRelativeTime(dateStr: string): string {
  const now = Date.now();
  const date = new Date(dateStr).getTime();
  const diffSec = Math.floor((now - date) / 1000);

  if (diffSec < 60) return 'Just now';
  const diffMin = Math.floor(diffSec / 60);
  if (diffMin < 60) return `${diffMin}m ago`;
  const diffHr = Math.floor(diffMin / 60);
  if (diffHr < 24) return `${diffHr}h ago`;
  const diffDay = Math.floor(diffHr / 24);
  if (diffDay < 7) return `${diffDay}d ago`;
  return new Date(dateStr).toLocaleDateString();
}

function ShieldIcon({ className }: { className?: string }) {
  return (
    <svg
      className={className}
      fill="none"
      stroke="currentColor"
      viewBox="0 0 24 24"
      strokeWidth={1.8}
    >
      <path
        strokeLinecap="round"
        strokeLinejoin="round"
        d="M12 2.714a11.959 11.959 0 01-8.402 3.285A12 12 0 003 9.75c0 5.592 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.31-.21-2.572-.598-3.751A11.959 11.959 0 0112 2.714z"
      />
    </svg>
  );
}

function ShieldCheckIcon({ className }: { className?: string }) {
  return (
    <svg
      className={className}
      fill="none"
      stroke="currentColor"
      viewBox="0 0 24 24"
      strokeWidth={1.8}
    >
      <path
        strokeLinecap="round"
        strokeLinejoin="round"
        d="M9 12.75L11.25 15 15 9.75m-3-7.036A11.959 11.959 0 013.598 6 11.99 11.99 0 003 9.749c0 5.592 3.824 10.29 9 11.623 5.176-1.332 9-6.03 9-11.622 0-1.31-.21-2.571-.598-3.751h-.152c-3.196 0-6.1-1.248-8.25-3.285z"
      />
    </svg>
  );
}

function ShieldAlertIcon({ className }: { className?: string }) {
  return (
    <svg
      className={className}
      fill="none"
      stroke="currentColor"
      viewBox="0 0 24 24"
      strokeWidth={1.8}
    >
      <path
        strokeLinecap="round"
        strokeLinejoin="round"
        d="M12 9v3.75m0-10.036A11.959 11.959 0 013.598 6 11.99 11.99 0 003 9.749c0 5.592 3.824 10.29 9 11.623 5.176-1.332 9-6.03 9-11.622 0-1.31-.21-2.572-.598-3.751A11.959 11.959 0 0112 2.714zm0 13.536h.007v.007H12v-.008z"
      />
    </svg>
  );
}

function FilterIcon({ className }: { className?: string }) {
  return (
    <svg
      className={className}
      fill="none"
      stroke="currentColor"
      viewBox="0 0 24 24"
      strokeWidth={1.8}
    >
      <path
        strokeLinecap="round"
        strokeLinejoin="round"
        d="M3 4.5h18M6 10.5h12m-9 6h6"
      />
    </svg>
  );
}

function WarningIcon({ className }: { className?: string }) {
  return (
    <svg
      className={className}
      fill="none"
      stroke="currentColor"
      viewBox="0 0 24 24"
      strokeWidth={2}
    >
      <path
        strokeLinecap="round"
        strokeLinejoin="round"
        d="M12 9v3.75m-9.303 3.376c-.866 1.5.217 3.374 1.948 3.374h14.71c1.73 0 2.813-1.874 1.948-3.374L13.949 3.378c-.866-1.5-3.032-1.5-3.898 0L2.697 16.126zM12 15.75h.007v.008H12v-.008z"
      />
    </svg>
  );
}

function ClockIcon({ className }: { className?: string }) {
  return (
    <svg
      className={className}
      fill="none"
      stroke="currentColor"
      viewBox="0 0 24 24"
      strokeWidth={2}
    >
      <path
        strokeLinecap="round"
        strokeLinejoin="round"
        d="M12 6v6h4.5m4.5 0a9 9 0 11-18 0 9 9 0 0118 0z"
      />
    </svg>
  );
}

function BanIcon({ className }: { className?: string }) {
  return (
    <svg
      className={className}
      fill="none"
      stroke="currentColor"
      viewBox="0 0 24 24"
      strokeWidth={2}
    >
      <path
        strokeLinecap="round"
        strokeLinejoin="round"
        d="M18.364 18.364A9 9 0 005.636 5.636m12.728 12.728A9 9 0 015.636 5.636m12.728 12.728L5.636 5.636"
      />
    </svg>
  );
}

function DismissIcon({ className }: { className?: string }) {
  return (
    <svg
      className={className}
      fill="none"
      stroke="currentColor"
      viewBox="0 0 24 24"
      strokeWidth={2}
    >
      <path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" />
    </svg>
  );
}

function ChevronLeftIcon({ className }: { className?: string }) {
  return (
    <svg
      className={className}
      fill="none"
      stroke="currentColor"
      viewBox="0 0 24 24"
      strokeWidth={2}
    >
      <path strokeLinecap="round" strokeLinejoin="round" d="M15.75 19.5L8.25 12l7.5-7.5" />
    </svg>
  );
}

function CloseIcon({ className }: { className?: string }) {
  return (
    <svg
      className={className}
      fill="none"
      stroke="currentColor"
      viewBox="0 0 24 24"
      strokeWidth={1.8}
    >
      <path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" />
    </svg>
  );
}
