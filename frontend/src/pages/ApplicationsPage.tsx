import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import { applicationsApi } from '@/lib/api';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { toast } from '@/components/ui/toaster';
import { Loader2, FileText, LayoutGrid, List, Building2 } from 'lucide-react';
import { formatRelativeTime, getStatusColor, cn } from '@/lib/utils';
import { ApplicationStatus, ApplicationStatusLabels } from '@/types';
import type { Application } from '@/types';

type ViewMode = 'kanban' | 'list';

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

  const { data: pipeline, isLoading } = useQuery({
    queryKey: ['applications', 'pipeline'],
    queryFn: applicationsApi.getPipeline,
  });

  const updateStatusMutation = useMutation({
    mutationFn: ({ id, status }: { id: string; status: ApplicationStatus }) =>
      applicationsApi.updateStatus(id, status),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['applications'] });
      toast({ title: 'Status updated' });
    },
  });

  if (isLoading) {
    return (
      <div className="flex justify-center py-12">
        <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
      </div>
    );
  }

  const allApplications = pipeline
    ? Object.values(pipeline).flat().sort((a, b) => 
        new Date(b.updatedAt).getTime() - new Date(a.updatedAt).getTime()
      )
    : [];

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
          pipeline={pipeline!}
          onStatusChange={(id, status) => updateStatusMutation.mutate({ id, status })}
        />
      ) : (
        <ListView applications={allApplications} />
      )}
    </div>
  );
}

function KanbanView({
  pipeline,
  onStatusChange,
}: {
  pipeline: Record<ApplicationStatus, Application[]>;
  onStatusChange: (id: string, status: ApplicationStatus) => void;
}) {
  return (
    <div className="flex gap-4 overflow-x-auto pb-4">
      {statusOrder.map((status) => (
        <div key={status} className="flex-shrink-0 w-72">
          <Card className="h-full">
            <CardHeader className="py-3">
              <CardTitle className="text-sm flex items-center justify-between">
                <span>{ApplicationStatusLabels[status]}</span>
                <Badge variant="secondary" className="text-xs">
                  {pipeline[status]?.length || 0}
                </Badge>
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-3 max-h-[calc(100vh-300px)] overflow-y-auto">
              {pipeline[status]?.map((app) => (
                <KanbanCard
                  key={app.id}
                  application={app}
                  currentStatus={status}
                  onStatusChange={onStatusChange}
                />
              ))}
              {(!pipeline[status] || pipeline[status].length === 0) && (
                <p className="text-sm text-muted-foreground text-center py-4">
                  No applications
                </p>
              )}
            </CardContent>
          </Card>
        </div>
      ))}
    </div>
  );
}

function KanbanCard({
  application,
  currentStatus,
  onStatusChange,
}: {
  application: Application;
  currentStatus: ApplicationStatus;
  onStatusChange: (id: string, status: ApplicationStatus) => void;
}) {
  const [showActions, setShowActions] = useState(false);

  const nextStatuses = statusOrder.filter((s) => s !== currentStatus && s < 6);

  return (
    <div
      className="p-3 rounded-lg border bg-card hover:border-primary/50 transition-colors cursor-pointer"
      onClick={() => setShowActions(!showActions)}
    >
      <Link to={`/applications/${application.id}`} onClick={(e) => e.stopPropagation()}>
        <h4 className="font-medium text-sm truncate hover:text-primary">
          {application.job.title}
        </h4>
      </Link>
      <div className="flex items-center gap-1 text-xs text-muted-foreground mt-1">
        <Building2 className="h-3 w-3" />
        <span className="truncate">{application.job.company}</span>
      </div>
      <p className="text-xs text-muted-foreground mt-2">
        {formatRelativeTime(application.updatedAt)}
      </p>

      {showActions && nextStatuses.length > 0 && (
        <div className="mt-3 flex flex-wrap gap-1" onClick={(e) => e.stopPropagation()}>
          {nextStatuses.slice(0, 3).map((status) => (
            <Button
              key={status}
              variant="outline"
              size="sm"
              className="text-xs h-7"
              onClick={() => onStatusChange(application.id, status)}
            >
              → {ApplicationStatusLabels[status]}
            </Button>
          ))}
        </div>
      )}
    </div>
  );
}

function ListView({ applications }: { applications: Application[] }) {
  return (
    <Card>
      <CardContent className="p-0">
        <div className="divide-y">
          {applications.map((app) => (
            <Link
              key={app.id}
              to={`/applications/${app.id}`}
              className="flex items-center gap-4 p-4 hover:bg-accent/50 transition-colors"
            >
              <div className="flex-1 min-w-0">
                <h3 className="font-medium truncate">{app.job.title}</h3>
                <div className="flex items-center gap-2 text-sm text-muted-foreground">
                  <Building2 className="h-3.5 w-3.5" />
                  <span className="truncate">{app.job.company}</span>
                </div>
              </div>
              <Badge className={cn(getStatusColor(app.status))}>
                {ApplicationStatusLabels[app.status]}
              </Badge>
              <span className="text-sm text-muted-foreground whitespace-nowrap">
                {formatRelativeTime(app.updatedAt)}
              </span>
            </Link>
          ))}
        </div>
      </CardContent>
    </Card>
  );
}




