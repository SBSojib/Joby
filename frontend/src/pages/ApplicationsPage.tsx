import { useState, useMemo } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import { applicationsApi } from '@/lib/api';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import {
  AlertDialog,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
  AlertDialogTrigger,
} from '@/components/ui/alert-dialog';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { toast } from '@/components/ui/toaster';
import { Loader2, FileText, LayoutGrid, List, Building2, Trash2, ChevronDown } from 'lucide-react';
import { formatRelativeTime, getStatusColor, cn } from '@/lib/utils';
import { ApplicationStatus, ApplicationStatusLabels } from '@/types';
import type { Application } from '@/types';

type ViewMode = 'kanban' | 'list';

/** API JSON may deserialize status as string; pipeline dict keys from .NET are enum names, not numeric keys. */
function normalizeApplicationStatus(raw: Application['status']): ApplicationStatus {
  if (typeof raw === 'number' && raw >= ApplicationStatus.Saved && raw <= ApplicationStatus.Withdrawn) {
    return raw;
  }
  if (typeof raw === 'string') {
    const n = Number(raw);
    if (!Number.isNaN(n) && n >= ApplicationStatus.Saved && n <= ApplicationStatus.Withdrawn) {
      return n as ApplicationStatus;
    }
    const named = ApplicationStatus[raw as keyof typeof ApplicationStatus];
    if (typeof named === 'number') return named;
  }
  return ApplicationStatus.Saved;
}

function groupApplicationsByStatus(
  applications: Application[],
  columns: readonly ApplicationStatus[]
): Record<ApplicationStatus, Application[]> {
  const pipeline = {} as Record<ApplicationStatus, Application[]>;
  for (const s of columns) {
    pipeline[s] = [];
  }
  for (const app of applications) {
    pipeline[normalizeApplicationStatus(app.status)].push(app);
  }
  return pipeline;
}

const statusOrder: ApplicationStatus[] = [
  ApplicationStatus.Saved,
  ApplicationStatus.Applied,
  ApplicationStatus.RecruiterScreen,
  ApplicationStatus.TechInterview,
  ApplicationStatus.Onsite,
  ApplicationStatus.Offer,
  ApplicationStatus.Rejected,
  ApplicationStatus.Withdrawn,
];

export default function ApplicationsPage() {
  const [viewMode, setViewMode] = useState<ViewMode>('kanban');
  const queryClient = useQueryClient();

  const { data: applications, isLoading } = useQuery({
    queryKey: ['applications'],
    queryFn: applicationsApi.getAll,
  });

  const pipeline = useMemo(
    () => groupApplicationsByStatus(applications ?? [], statusOrder),
    [applications]
  );

  const allApplications = useMemo(
    () =>
      [...(applications ?? [])].sort(
        (a, b) => new Date(b.updatedAt).getTime() - new Date(a.updatedAt).getTime()
      ),
    [applications]
  );

  const updateStatusMutation = useMutation({
    mutationFn: ({ id, status }: { id: string; status: ApplicationStatus }) =>
      applicationsApi.updateStatus(id, status),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['applications'] });
      toast({ title: 'Status updated' });
    },
  });

  const deleteApplicationMutation = useMutation({
    mutationFn: (id: string) => applicationsApi.delete(id),
    onSuccess: (_, deletedId) => {
      const cached = queryClient.getQueryData<Application>(['application', deletedId]);
      const jobId = cached?.job.id;
      queryClient.invalidateQueries({ queryKey: ['applications'] });
      queryClient.invalidateQueries({ queryKey: ['jobs'] });
      if (jobId) {
        queryClient.invalidateQueries({ queryKey: ['job', jobId] });
      }
      queryClient.removeQueries({ queryKey: ['application', deletedId] });
      toast({ title: 'Application deleted', description: 'This application has been permanently removed.' });
    },
    onError: () => {
      toast({
        variant: 'destructive',
        title: 'Could not delete application',
        description: 'Something went wrong. Please try again.',
      });
    },
  });

  if (isLoading) {
    return (
      <div className="flex justify-center py-12">
        <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col gap-4 md:flex-row md:items-center md:justify-between">
        <div>
          <h1 className="text-3xl font-bold">Applications</h1>
          <p className="text-muted-foreground">Track your job applications</p>
        </div>

        <div className="flex items-center gap-2">
          <Button
            variant={viewMode === 'kanban' ? 'secondary' : 'ghost'}
            size="sm"
            onClick={() => setViewMode('kanban')}
          >
            <LayoutGrid className="h-4 w-4 mr-2" />
            Kanban
          </Button>
          <Button
            variant={viewMode === 'list' ? 'secondary' : 'ghost'}
            size="sm"
            onClick={() => setViewMode('list')}
          >
            <List className="h-4 w-4 mr-2" />
            List
          </Button>
        </div>
      </div>

      {allApplications.length === 0 ? (
        <Card>
          <CardContent className="flex flex-col items-center justify-center py-12">
            <FileText className="h-12 w-12 text-muted-foreground mb-4" />
            <h3 className="text-lg font-medium">No applications yet</h3>
            <p className="text-muted-foreground text-center mt-1">
              Start by saving jobs you're interested in.
            </p>
            <Button className="mt-4" asChild>
              <Link to="/jobs">Browse Jobs</Link>
            </Button>
          </CardContent>
        </Card>
      ) : viewMode === 'kanban' ? (
        <KanbanView
          pipeline={pipeline}
          onStatusChange={(id, status) => updateStatusMutation.mutate({ id, status })}
          onDelete={(id) => deleteApplicationMutation.mutateAsync(id)}
          deletingId={
            deleteApplicationMutation.isPending ? deleteApplicationMutation.variables : undefined
          }
        />
      ) : (
        <ListView
          applications={allApplications}
          onDelete={(id) => deleteApplicationMutation.mutateAsync(id)}
          deletingId={
            deleteApplicationMutation.isPending ? deleteApplicationMutation.variables : undefined
          }
        />
      )}
    </div>
  );
}

function KanbanView({
  pipeline,
  onStatusChange,
  onDelete,
  deletingId,
}: {
  pipeline: Record<ApplicationStatus, Application[]>;
  onStatusChange: (id: string, status: ApplicationStatus) => void;
  onDelete: (id: string) => Promise<void>;
  deletingId: string | undefined;
}) {
  return (
    <div className="flex justify-center pb-6">
      <div className="flex w-full max-w-xl flex-col items-center">
        {statusOrder.map((status, index) => {
          const nextLabel =
            index < statusOrder.length - 1
              ? ApplicationStatusLabels[statusOrder[index + 1]]
              : null;
          return (
            <div key={status} className="flex w-full flex-col items-center">
              <Card className="w-full border-border/80 shadow-sm">
                <CardHeader className="border-b border-border/60 py-3 text-center">
                  <CardTitle className="flex flex-wrap items-center justify-center gap-2 text-base">
                    <span>{ApplicationStatusLabels[status]}</span>
                    <Badge variant="secondary" className="text-xs font-normal tabular-nums">
                      {pipeline[status]?.length || 0}
                    </Badge>
                  </CardTitle>
                </CardHeader>
                <CardContent className="divide-y divide-border/60 p-0">
                  {pipeline[status]?.map((app) => (
                    <KanbanRow
                      key={app.id}
                      application={app}
                      currentStatus={status}
                      onStatusChange={onStatusChange}
                      onDelete={onDelete}
                      isDeleting={deletingId === app.id}
                    />
                  ))}
                  {(!pipeline[status] || pipeline[status].length === 0) && (
                    <p className="text-sm text-muted-foreground text-center py-4">No applications</p>
                  )}
                </CardContent>
              </Card>
              {nextLabel != null && (
                <div className="flex w-full flex-col items-center gap-1 py-3 text-muted-foreground">
                  <span className="sr-only">Next stage: {nextLabel}</span>
                  <div aria-hidden className="flex flex-col items-center gap-1">
                    <div className="h-4 w-px bg-gradient-to-b from-transparent via-border to-transparent" />
                    <ChevronDown className="h-7 w-7 shrink-0 opacity-80" strokeWidth={2} />
                    <div className="h-4 w-px bg-gradient-to-b from-border via-border to-transparent" />
                  </div>
                </div>
              )}
            </div>
          );
        })}
      </div>
    </div>
  );
}

function KanbanRow({
  application,
  currentStatus,
  onStatusChange,
  onDelete,
  isDeleting,
}: {
  application: Application;
  currentStatus: ApplicationStatus;
  onStatusChange: (id: string, status: ApplicationStatus) => void;
  onDelete: (id: string) => Promise<void>;
  isDeleting: boolean;
}) {
  const [deleteOpen, setDeleteOpen] = useState(false);

  return (
    <div className="flex items-center gap-1.5 px-2 py-1.5 sm:px-3 sm:gap-2">
      <Link
        to={`/applications/${application.id}`}
        className="flex min-w-0 flex-1 items-center gap-2 rounded-md py-1 pl-1 pr-2 text-left text-sm transition-colors hover:bg-accent/60 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
      >
        <span className="min-w-0 flex-1 truncate">
          <span className="font-medium">{application.job.title}</span>
          <span className="font-normal text-muted-foreground">
            {' '}
            · <span className="inline-flex items-center gap-0.5 align-middle">
              <Building2 className="inline h-3 w-3 shrink-0 opacity-70" aria-hidden />
              {application.job.company}
            </span>
          </span>
        </span>
        <span className="shrink-0 whitespace-nowrap text-xs text-muted-foreground tabular-nums">
          {formatRelativeTime(application.updatedAt)}
        </span>
      </Link>
      <select
        aria-label="Change application stage"
        className="h-8 max-w-[9.5rem] shrink-0 cursor-pointer rounded-md border border-input bg-background px-1.5 text-xs shadow-sm focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
        value={currentStatus}
        onChange={(e) => {
          const next = Number(e.target.value) as ApplicationStatus;
          if (next !== currentStatus) onStatusChange(application.id, next);
        }}
      >
        {statusOrder.map((s) => (
          <option key={s} value={s}>
            {ApplicationStatusLabels[s]}
          </option>
        ))}
      </select>
      <AlertDialog open={deleteOpen} onOpenChange={setDeleteOpen}>
        <AlertDialogTrigger asChild>
          <Button
            type="button"
            variant="ghost"
            size="sm"
            className="h-8 w-8 shrink-0 p-0 text-muted-foreground hover:text-destructive"
            aria-label="Delete application"
          >
            <Trash2 className="h-4 w-4" />
          </Button>
        </AlertDialogTrigger>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Delete this application?</AlertDialogTitle>
            <AlertDialogDescription>
              This permanently removes this application and its timeline. The job listing remains in Jobs unless you
              delete it separately. This cannot be undone.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Cancel</AlertDialogCancel>
            <Button
              variant="destructive"
              disabled={isDeleting}
              onClick={async () => {
                try {
                  await onDelete(application.id);
                  setDeleteOpen(false);
                } catch {
                  /* toast from parent */
                }
              }}
            >
              {isDeleting ? (
                <Loader2 className="h-4 w-4 animate-spin mr-2" />
              ) : (
                <Trash2 className="h-4 w-4 mr-2" />
              )}
              Delete permanently
            </Button>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  );
}

function ListRow({
  application: app,
  onDelete,
  isDeleting,
}: {
  application: Application;
  onDelete: (id: string) => Promise<void>;
  isDeleting: boolean;
}) {
  const [deleteOpen, setDeleteOpen] = useState(false);

  return (
    <div className="flex items-center gap-2 p-4 hover:bg-accent/50 transition-colors">
      <Link to={`/applications/${app.id}`} className="flex flex-1 items-center gap-4 min-w-0">
        <div className="flex-1 min-w-0">
          <h3 className="font-medium truncate">{app.job.title}</h3>
          <div className="flex items-center gap-2 text-sm text-muted-foreground">
            <Building2 className="h-3.5 w-3.5" />
            <span className="truncate">{app.job.company}</span>
          </div>
        </div>
        <Badge className={cn(getStatusColor(app.status))}>{ApplicationStatusLabels[app.status]}</Badge>
        <span className="text-sm text-muted-foreground whitespace-nowrap hidden sm:inline">
          {formatRelativeTime(app.updatedAt)}
        </span>
      </Link>
      <AlertDialog open={deleteOpen} onOpenChange={setDeleteOpen}>
        <AlertDialogTrigger asChild>
          <Button
            type="button"
            variant="ghost"
            size="sm"
            className="shrink-0 h-9 w-9 p-0 text-muted-foreground hover:text-destructive"
            aria-label="Delete application"
          >
            <Trash2 className="h-4 w-4" />
          </Button>
        </AlertDialogTrigger>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Delete this application?</AlertDialogTitle>
            <AlertDialogDescription>
              This permanently removes this application and its timeline. The job listing remains in Jobs unless you
              delete it separately. This cannot be undone.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Cancel</AlertDialogCancel>
            <Button
              variant="destructive"
              disabled={isDeleting}
              onClick={async () => {
                try {
                  await onDelete(app.id);
                  setDeleteOpen(false);
                } catch {
                  /* toast from parent */
                }
              }}
            >
              {isDeleting ? (
                <Loader2 className="h-4 w-4 animate-spin mr-2" />
              ) : (
                <Trash2 className="h-4 w-4 mr-2" />
              )}
              Delete permanently
            </Button>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  );
}

function ListView({
  applications,
  onDelete,
  deletingId,
}: {
  applications: Application[];
  onDelete: (id: string) => Promise<void>;
  deletingId: string | undefined;
}) {
  return (
    <Card>
      <CardContent className="p-0">
        <div className="divide-y">
          {applications.map((app) => (
            <ListRow
              key={app.id}
              application={app}
              onDelete={onDelete}
              isDeleting={deletingId === app.id}
            />
          ))}
        </div>
      </CardContent>
    </Card>
  );
}




