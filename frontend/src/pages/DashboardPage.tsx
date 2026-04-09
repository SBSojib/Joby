import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import { jobsApi, applicationsApi, remindersApi } from '@/lib/api';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Badge } from '@/components/ui/badge';
import { toast } from '@/components/ui/toaster';
import {
  Briefcase,
  Plus,
  ExternalLink,
  Sparkles,
  Bell,
  FileText,
  Loader2,
  TrendingUp,
} from 'lucide-react';
import { formatRelativeTime, getStatusColor } from '@/lib/utils';
import { ApplicationStatusLabels } from '@/types';
import type { JobWithRecommendation, Application, Reminder } from '@/types';

export default function DashboardPage() {
  const [jobUrl, setJobUrl] = useState('');
  const [isAddingJob, setIsAddingJob] = useState(false);
  const queryClient = useQueryClient();

  const { data: recommendations, isLoading: loadingRecs } = useQuery({
    queryKey: ['recommendations'],
    queryFn: () => jobsApi.getTopRecommendations(5),
  });

  const { data: applications, isLoading: loadingApps } = useQuery({
    queryKey: ['applications'],
    queryFn: applicationsApi.getAll,
  });

  const { data: reminders, isLoading: loadingReminders } = useQuery({
    queryKey: ['reminders', 'upcoming'],
    queryFn: () => remindersApi.getUpcoming(7),
  });

  const addJobMutation = useMutation({
    mutationFn: (url: string) => jobsApi.createByUrl(url),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['recommendations'] });
      queryClient.invalidateQueries({ queryKey: ['jobs'] });
      setJobUrl('');
      setIsAddingJob(false);
      toast({ title: 'Job added!', description: 'The job has been saved to your list.' });
    },
    onError: () => {
      toast({
        variant: 'destructive',
        title: 'Failed to add job',
        description: 'Could not extract job details. Please try manual entry.',
      });
    },
  });

  const handleAddJob = (e: React.FormEvent) => {
    e.preventDefault();
    if (jobUrl.trim()) {
      addJobMutation.mutate(jobUrl.trim());
    }
  };

  // Stats
  const recentApps = applications?.slice(0, 5) || [];
  const totalApps = applications?.length || 0;
  const activeApps = applications?.filter((a) => a.status < 5).length || 0;
  const offerCount = applications?.filter((a) => a.status === 5).length || 0;

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col gap-4 md:flex-row md:items-center md:justify-between">
        <div>
          <h1 className="text-3xl font-bold">Dashboard</h1>
          <p className="text-muted-foreground">Track your job search progress</p>
        </div>

        {/* Quick Add Job */}
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
          <Button onClick={() => setIsAddingJob(true)}>
            <Plus className="h-4 w-4 mr-2" />
            Add Job by URL
          </Button>
        )}
      </div>

      {/* Stats Cards */}
      <div className="grid gap-4 md:grid-cols-4">
        <Card>
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">
              Total Applications
            </CardTitle>
            <FileText className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-3xl font-bold">{totalApps}</div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">
              Active Applications
            </CardTitle>
            <TrendingUp className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-3xl font-bold">{activeApps}</div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">Offers</CardTitle>
            <Sparkles className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-3xl font-bold text-green-400">{offerCount}</div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">
              Upcoming Reminders
            </CardTitle>
            <Bell className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-3xl font-bold">{reminders?.length || 0}</div>
          </CardContent>
        </Card>
      </div>

      <div className="grid gap-6 lg:grid-cols-2">
        {/* Recommended Jobs */}
        <Card>
          <CardHeader>
            <div className="flex items-center justify-between">
              <div>
                <CardTitle className="flex items-center gap-2">
                  <Sparkles className="h-5 w-5 text-primary" />
                  Recommended Jobs
                </CardTitle>
                <CardDescription>Based on your profile and skills</CardDescription>
              </div>
              <Button variant="ghost" size="sm" asChild>
                <Link to="/jobs">View all</Link>
              </Button>
            </div>
          </CardHeader>
          <CardContent>
            {loadingRecs ? (
              <div className="flex justify-center py-8">
                <Loader2 className="h-6 w-6 animate-spin text-muted-foreground" />
              </div>
            ) : recommendations?.length ? (
              <div className="space-y-4">
                {recommendations.map((job: JobWithRecommendation) => (
                  <JobRecommendationCard key={job.id} job={job} />
                ))}
              </div>
            ) : (
              <div className="text-center py-8 text-muted-foreground">
                <Briefcase className="h-8 w-8 mx-auto mb-2 opacity-50" />
                <p>No recommendations yet</p>
                <p className="text-sm">Add jobs to see personalized suggestions</p>
              </div>
            )}
          </CardContent>
        </Card>

        {/* Recent Applications */}
        <Card>
          <CardHeader>
            <div className="flex items-center justify-between">
              <div>
                <CardTitle className="flex items-center gap-2">
                  <FileText className="h-5 w-5 text-primary" />
                  Recent Applications
                </CardTitle>
                <CardDescription>Your latest job applications</CardDescription>
              </div>
              <Button variant="ghost" size="sm" asChild>
                <Link to="/applications">View all</Link>
              </Button>
            </div>
          </CardHeader>
          <CardContent>
            {loadingApps ? (
              <div className="flex justify-center py-8">
                <Loader2 className="h-6 w-6 animate-spin text-muted-foreground" />
              </div>
            ) : recentApps.length ? (
              <div className="space-y-4">
                {recentApps.map((app: Application) => (
                  <ApplicationCard key={app.id} application={app} />
                ))}
              </div>
            ) : (
              <div className="text-center py-8 text-muted-foreground">
                <FileText className="h-8 w-8 mx-auto mb-2 opacity-50" />
                <p>No applications yet</p>
                <p className="text-sm">Start by saving jobs you're interested in</p>
              </div>
            )}
          </CardContent>
        </Card>
      </div>

      {/* Upcoming Reminders */}
      {!loadingReminders && reminders && reminders.length > 0 && (
        <Card>
          <CardHeader>
            <div className="flex items-center justify-between">
              <div>
                <CardTitle className="flex items-center gap-2">
                  <Bell className="h-5 w-5 text-primary" />
                  Upcoming Reminders
                </CardTitle>
                <CardDescription>Don't forget to follow up</CardDescription>
              </div>
              <Button variant="ghost" size="sm" asChild>
                <Link to="/reminders">View all</Link>
              </Button>
            </div>
          </CardHeader>
          <CardContent>
            <div className="space-y-3">
              {reminders.slice(0, 3).map((reminder: Reminder) => (
                <ReminderCard key={reminder.id} reminder={reminder} />
              ))}
            </div>
          </CardContent>
        </Card>
      )}
    </div>
  );
}

function JobRecommendationCard({ job }: { job: JobWithRecommendation }) {
  return (
    <Link
      to={`/jobs/${job.id}`}
      className="block p-4 rounded-lg border bg-card hover:bg-accent/50 transition-colors"
    >
      <div className="flex items-start justify-between gap-4">
        <div className="flex-1 min-w-0">
          <h3 className="font-medium truncate">{job.title}</h3>
          <p className="text-sm text-muted-foreground truncate">{job.company}</p>
          {job.location && (
            <p className="text-sm text-muted-foreground">{job.location}</p>
          )}
        </div>
        <div className="flex flex-col items-end gap-2">
          {job.recommendationScore && (
            <Badge variant="secondary" className="bg-primary/20 text-primary">
              {Math.round(job.recommendationScore)}% match
            </Badge>
          )}
          {job.sourceUrl && (
            <ExternalLink className="h-4 w-4 text-muted-foreground" />
          )}
        </div>
      </div>
      {job.matchedSkills.length > 0 && (
        <div className="mt-3 flex flex-wrap gap-1">
          {job.matchedSkills.slice(0, 4).map((skill) => (
            <Badge key={skill} variant="outline" className="text-xs">
              {skill}
            </Badge>
          ))}
          {job.matchedSkills.length > 4 && (
            <Badge variant="outline" className="text-xs">
              +{job.matchedSkills.length - 4}
            </Badge>
          )}
        </div>
      )}
    </Link>
  );
}

function ApplicationCard({ application }: { application: Application }) {
  return (
    <Link
      to={`/applications/${application.id}`}
      className="block p-4 rounded-lg border bg-card hover:bg-accent/50 transition-colors"
    >
      <div className="flex items-start justify-between gap-4">
        <div className="flex-1 min-w-0">
          <h3 className="font-medium truncate">{application.job.title}</h3>
          <p className="text-sm text-muted-foreground truncate">{application.job.company}</p>
        </div>
        <Badge className={getStatusColor(application.status)}>
          {ApplicationStatusLabels[application.status]}
        </Badge>
      </div>
      <p className="mt-2 text-xs text-muted-foreground">
        Updated {formatRelativeTime(application.updatedAt)}
      </p>
    </Link>
  );
}

function ReminderCard({ reminder }: { reminder: Reminder }) {
  return (
    <Link
      to="/reminders"
      className="block p-4 rounded-lg border bg-card hover:bg-accent/50 transition-colors"
    >
      <div className="flex items-center gap-3">
        <Bell className="h-5 w-5 text-primary shrink-0" />
        <div className="flex-1 min-w-0">
          <h4 className="font-medium truncate">{reminder.title}</h4>
          <p className="text-sm text-muted-foreground">
            Due {formatRelativeTime(reminder.dueAt)}
          </p>
        </div>
        {reminder.isAutoGenerated && (
          <Badge variant="outline" className="text-xs shrink-0">
            Auto
          </Badge>
        )}
      </div>
    </Link>
  );
}




