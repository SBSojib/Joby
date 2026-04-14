import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Link, useNavigate } from 'react-router-dom';
import { jobsApi, applicationsApi } from '@/lib/api';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
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
import { Input } from '@/components/ui/input';
import { Badge } from '@/components/ui/badge';
import { toast } from '@/components/ui/toaster';
import {
  Plus,
  ExternalLink,
  Loader2,
  Briefcase,
  MapPin,
  Building2,
  Sparkles,
  Trash2,
} from 'lucide-react';
import { formatDate, truncate } from '@/lib/utils';
import { ApplicationStatus } from '@/types';
import type { Job, JobWithRecommendation } from '@/types';

const SAVED_JOBS_PAGE_SIZE = 200;

export default function JobsPage() {
  const [jobUrl, setJobUrl] = useState('');
  const [isAddingJob, setIsAddingJob] = useState(false);
  const queryClient = useQueryClient();
  const navigate = useNavigate();

  const { data: savedJobsPage, isLoading } = useQuery({
    queryKey: ['jobs', 'saved'],
    queryFn: () => jobsApi.search({ page: 1, pageSize: SAVED_JOBS_PAGE_SIZE }),
  });
  const { data: applications = [] } = useQuery({
    queryKey: ['applications'],
    queryFn: applicationsApi.getAll,
  });

  const addJobMutation = useMutation({
    mutationFn: (url: string) => jobsApi.createByUrl(url),
    onSuccess: (job) => {
      queryClient.invalidateQueries({ queryKey: ['jobs'] });
      queryClient.invalidateQueries({ queryKey: ['applications'] });
      setJobUrl('');
      setIsAddingJob(false);
      toast({ title: 'Job added!', description: 'The job has been saved.' });
      navigate(`/jobs/${job.id}`);
    },
    onError: () => {
      toast({
        variant: 'destructive',
        title: 'Failed to add job',
        description: 'Could not extract job details from URL.',
      });
    },
  });

  const saveJobMutation = useMutation({
    mutationFn: (jobId: string) =>
      applicationsApi.create({ jobId, status: ApplicationStatus.Saved }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['jobs'] });
      queryClient.invalidateQueries({ queryKey: ['applications'] });
      toast({ title: 'Job saved!', description: 'Added to your applications.' });
    },
  });

  const deleteJobMutation = useMutation({
    mutationFn: (id: string) => jobsApi.delete(id),
    onSuccess: (_, deletedId) => {
      queryClient.invalidateQueries({ queryKey: ['jobs'] });
      queryClient.invalidateQueries({ queryKey: ['applications'] });
      queryClient.removeQueries({ queryKey: ['job', deletedId] });
      toast({ title: 'Job deleted', description: 'This job has been permanently removed.' });
    },
    onError: () => {
      toast({
        variant: 'destructive',
        title: 'Could not delete job',
        description: 'Something went wrong. Please try again.',
      });
    },
  });

  const handleAddJob = (e: React.FormEvent) => {
    e.preventDefault();
    if (jobUrl.trim()) {
      addJobMutation.mutate(jobUrl.trim());
    }
  };

  const savedJobs = savedJobsPage?.items ?? [];
  const fallbackJobs = Array.from(
    new Map(applications.map((app) => [app.job.id, app.job])).values()
  );
  const effectiveSavedJobs = savedJobs.length > 0 ? savedJobs : fallbackJobs;
  const totalSaved = savedJobsPage?.totalCount ?? effectiveSavedJobs.length;
  const showingPartial = savedJobsPage != null && totalSaved > effectiveSavedJobs.length;

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col gap-4 md:flex-row md:items-center md:justify-between">
        <div>
          <h1 className="text-3xl font-bold">Jobs</h1>
          <p className="text-muted-foreground">Discover and track job opportunities</p>
        </div>

        {isAddingJob ? (
          <form onSubmit={handleAddJob} className="flex gap-2 w-full md:w-auto">
            <Input
              placeholder="Paste job URL..."
              value={jobUrl}
              onChange={(e) => setJobUrl(e.target.value)}
              className="w-full md:w-80"
            />
            <Button type="submit" disabled={addJobMutation.isPending}>
              {addJobMutation.isPending ? (
                <Loader2 className="h-4 w-4 animate-spin" />
              ) : (
                'Add'
              )}
            </Button>
            <Button type="button" variant="ghost" onClick={() => setIsAddingJob(false)}>
              Cancel
            </Button>
          </form>
        ) : (
          <div className="flex gap-2">
            <Button onClick={() => setIsAddingJob(true)}>
              <Plus className="h-4 w-4 mr-2" />
              Add Job by URL
            </Button>
            <Button variant="outline" asChild>
              <Link to="/jobs/new">Manual Entry</Link>
            </Button>
          </div>
        )}
      </div>

      {/* Saved jobs */}
      <section className="space-y-4">
        <div>
          <h2 className="text-xl font-semibold tracking-tight">Saved jobs</h2>
          <p className="text-sm text-muted-foreground">
            Every job you have added to Joby, newest first.
            {totalSaved > 0 && (
              <span className="text-foreground"> ({totalSaved} total)</span>
            )}
          </p>
        </div>

        {isLoading ? (
          <div className="flex justify-center py-12">
            <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
          </div>
        ) : effectiveSavedJobs.length ? (
          <>
            {showingPartial && (
              <p className="text-sm text-muted-foreground">
                Showing the {effectiveSavedJobs.length} most recent of {totalSaved} saved jobs.
              </p>
            )}
            <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
              {effectiveSavedJobs.map((job) => (
                <JobCard
                  key={job.id}
                  job={job}
                  onSave={() => saveJobMutation.mutate(job.id)}
                  isSaving={saveJobMutation.isPending}
                  onDelete={() => deleteJobMutation.mutateAsync(job.id)}
                  isDeleting={deleteJobMutation.isPending && deleteJobMutation.variables === job.id}
                />
              ))}
            </div>
          </>
        ) : (
          <Card>
            <CardHeader>
              <CardTitle className="text-lg">No saved jobs yet</CardTitle>
              <CardDescription>
                Add a job by URL or use manual entry. Everything you save will show up here.
              </CardDescription>
            </CardHeader>
            <CardContent className="flex flex-col items-center justify-center pb-10 pt-2">
              <Briefcase className="h-12 w-12 text-muted-foreground mb-4 opacity-60" />
            </CardContent>
          </Card>
        )}
      </section>
    </div>
  );
}

function JobCard({
  job,
  onSave,
  isSaving,
  onDelete,
  isDeleting,
}: {
  job: Job;
  onSave: () => void;
  isSaving: boolean;
  onDelete: () => Promise<void>;
  isDeleting: boolean;
}) {
  const [deleteOpen, setDeleteOpen] = useState(false);
  const rec = job as JobWithRecommendation;
  const matchedSkills = rec.matchedSkills ?? [];
  const showMatchScore = rec.recommendationScore != null && rec.recommendationScore > 0;

  return (
    <Card className="hover:border-primary/50 transition-colors">
      <CardHeader className="pb-3">
        <div className="flex items-start justify-between gap-2">
          <div className="flex-1 min-w-0">
            <CardTitle className="text-lg truncate">
              <Link to={`/jobs/${job.id}`} className="hover:text-primary">
                {job.title}
              </Link>
            </CardTitle>
            <div className="flex items-center gap-1 text-sm text-muted-foreground mt-1">
              <Building2 className="h-3.5 w-3.5" />
              <span className="truncate">{job.company}</span>
            </div>
          </div>
          {job.sourceUrl && (
            <a
              href={job.sourceUrl}
              target="_blank"
              rel="noopener noreferrer"
              className="text-muted-foreground hover:text-foreground"
            >
              <ExternalLink className="h-4 w-4" />
            </a>
          )}
        </div>
      </CardHeader>
      <CardContent className="space-y-3">
        {job.location && (
          <div className="flex items-center gap-1 text-sm text-muted-foreground">
            <MapPin className="h-3.5 w-3.5" />
            {job.location}
          </div>
        )}

        {job.description && (
          <p className="text-sm text-muted-foreground line-clamp-2">
            {truncate(job.description, 120)}
          </p>
        )}

        {/* Recommendation extras (only when present, e.g. from recommendation feeds) */}
        {showMatchScore && (
          <div className="flex items-center gap-2">
            <Sparkles className="h-4 w-4 text-primary" />
            <Badge variant="secondary" className="bg-primary/20 text-primary">
              {Math.round(rec.recommendationScore!)}% match
            </Badge>
          </div>
        )}

        {matchedSkills.length > 0 && (
          <div className="flex flex-wrap gap-1">
            {matchedSkills.slice(0, 3).map((skill) => (
              <Badge key={skill} variant="outline" className="text-xs bg-green-500/10 text-green-400 border-green-500/30">
                {skill}
              </Badge>
            ))}
            {matchedSkills.length > 3 && (
              <Badge variant="outline" className="text-xs">
                +{matchedSkills.length - 3}
              </Badge>
            )}
          </div>
        )}

        {job.postedDate && (
          <p className="text-xs text-muted-foreground">Posted {formatDate(job.postedDate)}</p>
        )}

        {/* Actions */}
        <div className="flex gap-2 pt-2">
          {job.hasApplication ? (
            <Button variant="secondary" size="sm" className="flex-1" asChild>
              <Link to={`/applications/${job.applicationId}`}>View Application</Link>
            </Button>
          ) : (
            <Button size="sm" className="flex-1" onClick={onSave} disabled={isSaving}>
              {isSaving ? <Loader2 className="h-4 w-4 animate-spin" /> : 'Save Job'}
            </Button>
          )}
          <AlertDialog open={deleteOpen} onOpenChange={setDeleteOpen}>
            <AlertDialogTrigger asChild>
              <Button
                type="button"
                variant="outline"
                size="sm"
                className="shrink-0 border-destructive/50 text-destructive hover:bg-destructive/10 hover:text-destructive"
                aria-label="Delete job"
              >
                <Trash2 className="h-4 w-4" />
              </Button>
            </AlertDialogTrigger>
            <AlertDialogContent>
              <AlertDialogHeader>
                <AlertDialogTitle>Delete this job?</AlertDialogTitle>
                <AlertDialogDescription>
                  This permanently removes this job from your account
                  {job.hasApplication ? ', including the linked application and its history' : ''}. This cannot be
                  undone.
                </AlertDialogDescription>
              </AlertDialogHeader>
              <AlertDialogFooter>
                <AlertDialogCancel>Cancel</AlertDialogCancel>
                <Button
                  variant="destructive"
                  disabled={isDeleting}
                  onClick={async () => {
                    try {
                      await onDelete();
                      setDeleteOpen(false);
                    } catch {
                      /* toast handled by mutation onError */
                    }
                  }}
                >
                  {isDeleting ? <Loader2 className="h-4 w-4 animate-spin mr-2" /> : <Trash2 className="h-4 w-4 mr-2" />}
                  Delete permanently
                </Button>
              </AlertDialogFooter>
            </AlertDialogContent>
          </AlertDialog>
        </div>
      </CardContent>
    </Card>
  );
}




