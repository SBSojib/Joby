import { useState, useEffect } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useParams, Link, useNavigate } from 'react-router-dom';
import { jobsApi, applicationsApi, remindersApi } from '@/lib/api';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { ReminderDateTimeFields } from '@/components/ReminderDateTimeFields';
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
import { toast } from '@/components/ui/toaster';
import {
  ArrowLeft,
  ExternalLink,
  Loader2,
  Building2,
  MapPin,
  Calendar,
  DollarSign,
  Briefcase,
  CheckCircle,
  XCircle,
  Sparkles,
  Trash2,
  Bell,
  Plus,
} from 'lucide-react';
import {
  escapeHtmlAttribute,
  formatDate,
  listingBodyFromStoredDescription,
  plainTextFromMaybeEncodedHtml,
  sourceListingBaseHref,
  cn,
  getDefaultReminderDueParts,
  localDateTimePartsToIso,
} from '@/lib/utils';
import { ApplicationStatus, ReminderStatus } from '@/types';

export default function JobDetailPage() {
  const { jobId } = useParams<{ jobId: string }>();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
  const [showAddReminder, setShowAddReminder] = useState(false);
  const [reminderTitle, setReminderTitle] = useState('');
  const [reminderDescription, setReminderDescription] = useState('');
  const [reminderDueDate, setReminderDueDate] = useState('');
  const [reminderDueTime, setReminderDueTime] = useState('');

  useEffect(() => {
    const { date, time } = getDefaultReminderDueParts();
    setReminderDueDate(date);
    setReminderDueTime(time);
  }, [jobId]);

  const { data: job, isLoading } = useQuery({
    queryKey: ['job', jobId],
    queryFn: () => jobsApi.get(jobId!),
    enabled: !!jobId,
  });

  const { data: recommendations } = useQuery({
    queryKey: ['jobs', 'recommended'],
    queryFn: () => jobsApi.getRecommended(1, 100),
  });

  const saveJobMutation = useMutation({
    mutationFn: () => applicationsApi.create({ jobId: jobId!, status: ApplicationStatus.Saved }),
    onSuccess: (app) => {
      queryClient.invalidateQueries({ queryKey: ['job', jobId] });
      queryClient.invalidateQueries({ queryKey: ['applications'] });
      toast({ title: 'Job saved!', description: 'Added to your applications.' });
      navigate(`/applications/${app.id}`);
    },
  });

  const applyJobMutation = useMutation({
    mutationFn: () => applicationsApi.create({ jobId: jobId!, status: ApplicationStatus.Applied }),
    onSuccess: (app) => {
      queryClient.invalidateQueries({ queryKey: ['job', jobId] });
      queryClient.invalidateQueries({ queryKey: ['applications'] });
      toast({ title: 'Application tracked!', description: 'Remember to apply on the company site.' });
      navigate(`/applications/${app.id}`);
    },
  });

  const createReminderMutation = useMutation({
    mutationFn: async (vars: {
      title: string;
      description: string;
      dueAt: string;
      applicationId?: string;
      jobPk: string;
      hasApplication: boolean;
    }) => {
      const base = {
        title: vars.title,
        description: vars.description || undefined,
        dueAt: vars.dueAt,
      };
      if (vars.hasApplication && vars.applicationId) {
        return remindersApi.create({ ...base, applicationId: vars.applicationId });
      }
      return remindersApi.create({ ...base, jobId: vars.jobPk });
    },
    onSuccess: (_, vars) => {
      queryClient.invalidateQueries({ queryKey: ['job', jobId] });
      if (vars.applicationId) {
        queryClient.invalidateQueries({ queryKey: ['application', vars.applicationId] });
      }
      queryClient.invalidateQueries({ queryKey: ['reminders'] });
      setShowAddReminder(false);
      setReminderTitle('');
      setReminderDescription('');
      const { date, time } = getDefaultReminderDueParts();
      setReminderDueDate(date);
      setReminderDueTime(time);
      toast({ title: 'Reminder scheduled', description: 'You will get an email when it is due (if email is configured).' });
    },
    onError: () => {
      toast({
        variant: 'destructive',
        title: 'Could not save reminder',
        description: 'Check the due time is in the future and try again.',
      });
    },
  });

  const completeReminderMutation = useMutation({
    mutationFn: (id: string) => remindersApi.complete(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['job', jobId] });
      const j = queryClient.getQueryData<{ applicationId?: string }>(['job', jobId]);
      if (j?.applicationId) {
        queryClient.invalidateQueries({ queryKey: ['application', j.applicationId] });
      }
      queryClient.invalidateQueries({ queryKey: ['reminders'] });
      toast({ title: 'Reminder completed' });
    },
  });

  const deleteJobMutation = useMutation({
    mutationFn: () => jobsApi.delete(jobId!),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['jobs'] });
      queryClient.invalidateQueries({ queryKey: ['applications'] });
      queryClient.removeQueries({ queryKey: ['job', jobId] });
      setDeleteDialogOpen(false);
      toast({ title: 'Job deleted', description: 'This job has been permanently removed.' });
      navigate('/jobs');
    },
    onError: () => {
      toast({
        variant: 'destructive',
        title: 'Could not delete job',
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

  if (!job) {
    return (
      <Card>
        <CardContent className="py-12 text-center">
          <p className="text-muted-foreground">Job not found</p>
        </CardContent>
      </Card>
    );
  }

  // Find recommendation data for this job
  const recommendation = recommendations?.items?.find((r) => r.id === job.id);

  const jobReminders = job.reminders ?? [];
  const activeJobReminders = jobReminders.filter(
    (r) => r.status === ReminderStatus.Pending || r.status === ReminderStatus.Snoozed
  );

  const handleSaveReminder = () => {
    if (!reminderTitle.trim() || !reminderDueDate || !reminderDueTime) return;
    const dueAt = localDateTimePartsToIso(reminderDueDate, reminderDueTime);
    if (Number.isNaN(new Date(dueAt).getTime())) {
      toast({
        variant: 'destructive',
        title: 'Invalid date or time',
        description: 'Please choose a valid date and time.',
      });
      return;
    }
    createReminderMutation.mutate({
      title: reminderTitle.trim(),
      description: reminderDescription.trim(),
      dueAt,
      applicationId: job.applicationId,
      jobPk: job.id,
      hasApplication: job.hasApplication,
    });
  };

  const fallbackListingHtml = listingBodyFromStoredDescription(job.description);
  const listingHtml = job.sourcePageHtml ?? fallbackListingHtml ?? undefined;
  const listingBaseHref = sourceListingBaseHref(job.sourceUrl);

  return (
    <div className="space-y-6">
      {/* Back button */}
      <Button variant="ghost" size="sm" onClick={() => navigate(-1)}>
        <ArrowLeft className="h-4 w-4 mr-2" />
        Back
      </Button>

      <div className="grid gap-6 lg:grid-cols-3">
        {/* Main content */}
        <div className="lg:col-span-2 space-y-6">
          <Card>
            <CardHeader>
              <div className="flex items-start justify-between gap-4">
                <div>
                  <CardTitle className="text-2xl">{job.title}</CardTitle>
                  <CardDescription className="flex items-center gap-2 mt-2">
                    <Building2 className="h-4 w-4" />
                    {job.company}
                  </CardDescription>
                </div>
                {job.sourceUrl && (
                  <Button variant="outline" size="sm" asChild>
                    <a href={job.sourceUrl} target="_blank" rel="noopener noreferrer">
                      <ExternalLink className="h-4 w-4 mr-2" />
                      View Original
                    </a>
                  </Button>
                )}
              </div>
            </CardHeader>
            <CardContent className="space-y-4">
              {/* Job meta */}
              <div className="flex flex-wrap gap-4 text-sm text-muted-foreground">
                {job.location && (
                  <div className="flex items-center gap-1">
                    <MapPin className="h-4 w-4" />
                    {job.location}
                  </div>
                )}
                {job.jobType && (
                  <div className="flex items-center gap-1">
                    <Briefcase className="h-4 w-4" />
                    {job.jobType}
                  </div>
                )}
                {job.salary && (
                  <div className="flex items-center gap-1">
                    <DollarSign className="h-4 w-4" />
                    {job.salary}
                  </div>
                )}
                {job.postedDate && (
                  <div className="flex items-center gap-1">
                    <Calendar className="h-4 w-4" />
                    Posted {formatDate(job.postedDate)}
                  </div>
                )}
              </div>

              {/* Saved page HTML, or description that is actually HTML (e.g. legacy JSON \\u003c escapes) */}
              {listingHtml ? (
                <div>
                  <h3 className="font-semibold mb-2">{job.sourcePageHtml ? 'Listing' : 'Description'}</h3>
                  <iframe
                    title="Job listing content"
                    className="w-full min-h-[70vh] rounded-md border border-border bg-background"
                    sandbox="allow-popups allow-popups-to-escape-sandbox"
                    srcDoc={`<base href="${escapeHtmlAttribute(listingBaseHref)}" target="_blank">${listingHtml}`}
                  />
                </div>
              ) : (
                job.description && (
                  <div>
                    <h3 className="font-semibold mb-2">Description</h3>
                    <div className="prose prose-sm prose-invert max-w-none">
                      <p className="whitespace-pre-line text-muted-foreground">
                        {plainTextFromMaybeEncodedHtml(job.description)}
                      </p>
                    </div>
                  </div>
                )
              )}

              {/* Requirements */}
              {job.requirements && (
                <div>
                  <h3 className="font-semibold mb-2">Requirements</h3>
                  <div className="prose prose-sm prose-invert max-w-none">
                    <p className="whitespace-pre-line text-muted-foreground">
                      {plainTextFromMaybeEncodedHtml(job.requirements)}
                    </p>
                  </div>
                </div>
              )}
            </CardContent>
          </Card>
        </div>

        {/* Sidebar */}
        <div className="space-y-6">
          {/* Actions */}
          <Card>
            <CardHeader>
              <CardTitle className="text-lg">Actions</CardTitle>
            </CardHeader>
            <CardContent className="space-y-3">
              {job.hasApplication ? (
                <Button className="w-full" asChild>
                  <Link to={`/applications/${job.applicationId}`}>View Application</Link>
                </Button>
              ) : (
                <>
                  <Button
                    className="w-full"
                    onClick={() => applyJobMutation.mutate()}
                    disabled={applyJobMutation.isPending}
                  >
                    {applyJobMutation.isPending ? (
                      <Loader2 className="h-4 w-4 animate-spin mr-2" />
                    ) : null}
                    Mark as Applied
                  </Button>
                  <Button
                    variant="secondary"
                    className="w-full"
                    onClick={() => saveJobMutation.mutate()}
                    disabled={saveJobMutation.isPending}
                  >
                    {saveJobMutation.isPending ? (
                      <Loader2 className="h-4 w-4 animate-spin mr-2" />
                    ) : null}
                    Save for Later
                  </Button>
                </>
              )}

              <AlertDialog open={deleteDialogOpen} onOpenChange={setDeleteDialogOpen}>
                <AlertDialogTrigger asChild>
                  <Button variant="outline" className="w-full border-destructive/50 text-destructive hover:bg-destructive/10 hover:text-destructive">
                    <Trash2 className="h-4 w-4 mr-2" />
                    Delete job
                  </Button>
                </AlertDialogTrigger>
                <AlertDialogContent>
                  <AlertDialogHeader>
                    <AlertDialogTitle>Delete this job?</AlertDialogTitle>
                    <AlertDialogDescription>
                      This permanently removes this job listing from your account
                      {job.hasApplication ? ', including the linked application and its history' : ''}. This action
                      cannot be undone.
                    </AlertDialogDescription>
                  </AlertDialogHeader>
                  <AlertDialogFooter>
                    <AlertDialogCancel>Cancel</AlertDialogCancel>
                    <Button
                      variant="destructive"
                      disabled={deleteJobMutation.isPending}
                      onClick={() => deleteJobMutation.mutate()}
                    >
                      {deleteJobMutation.isPending ? (
                        <Loader2 className="h-4 w-4 animate-spin mr-2" />
                      ) : (
                        <Trash2 className="h-4 w-4 mr-2" />
                      )}
                      Delete permanently
                    </Button>
                  </AlertDialogFooter>
                </AlertDialogContent>
              </AlertDialog>
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <div className="flex items-center justify-between gap-2">
                <CardTitle className="text-lg flex items-center gap-2">
                  <Bell className="h-5 w-5" />
                  Reminders
                </CardTitle>
                <Button variant="outline" size="sm" onClick={() => setShowAddReminder(!showAddReminder)}>
                  <Plus className="h-4 w-4 mr-2" />
                  Add
                </Button>
              </div>
              <CardDescription>
                Emails are sent to your account address when a reminder is due (SMTP must be configured on the server).
              </CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              {showAddReminder && (
                <div className="p-4 border rounded-lg space-y-3">
                  <div>
                    <Label htmlFor="jr-title">Title</Label>
                    <Input
                      id="jr-title"
                      placeholder="Follow up with recruiter"
                      value={reminderTitle}
                      onChange={(e) => setReminderTitle(e.target.value)}
                    />
                  </div>
                  <div>
                    <Label htmlFor="jr-desc">Details (optional)</Label>
                    <textarea
                      id="jr-desc"
                      className={cn(
                        'flex min-h-[72px] w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50'
                      )}
                      placeholder="What should you do when this fires?"
                      value={reminderDescription}
                      onChange={(e) => setReminderDescription(e.target.value)}
                    />
                  </div>
                  <ReminderDateTimeFields
                    idPrefix="jr"
                    dateValue={reminderDueDate}
                    timeValue={reminderDueTime}
                    onDateChange={setReminderDueDate}
                    onTimeChange={setReminderDueTime}
                  />
                  <div className="flex gap-2">
                    <Button size="sm" onClick={handleSaveReminder} disabled={createReminderMutation.isPending}>
                      {createReminderMutation.isPending ? (
                        <Loader2 className="h-4 w-4 animate-spin mr-2" />
                      ) : null}
                      Save reminder
                    </Button>
                    <Button variant="ghost" size="sm" onClick={() => setShowAddReminder(false)}>
                      Cancel
                    </Button>
                  </div>
                </div>
              )}

              {activeJobReminders.length > 0 ? (
                <div className="space-y-3">
                  {activeJobReminders.map((reminder) => (
                    <div
                      key={reminder.id}
                      className="p-3 rounded-lg border flex items-start justify-between gap-2"
                    >
                      <div>
                        <p className="text-sm font-medium">{reminder.title}</p>
                        {reminder.description && (
                          <p className="text-xs text-muted-foreground mt-1">{reminder.description}</p>
                        )}
                        <p className="text-xs text-muted-foreground flex items-center gap-1 mt-1">
                          <Calendar className="h-3 w-3" />
                          Due {formatDate(reminder.dueAt)}
                        </p>
                      </div>
                      <Button
                        variant="ghost"
                        size="sm"
                        onClick={() => completeReminderMutation.mutate(reminder.id)}
                        aria-label="Mark reminder complete"
                      >
                        <CheckCircle className="h-4 w-4" />
                      </Button>
                    </div>
                  ))}
                </div>
              ) : (
                !showAddReminder && <p className="text-sm text-muted-foreground">No active reminders for this job.</p>
              )}
            </CardContent>
          </Card>

          {/* Match Analysis */}
          {recommendation && (recommendation.matchedSkills?.length > 0 || recommendation.missingSkills?.length > 0) && (
            <Card>
              <CardHeader>
                <CardTitle className="text-lg flex items-center gap-2">
                  <Sparkles className="h-5 w-5 text-primary" />
                  Match Analysis
                </CardTitle>
                {recommendation.recommendationScore !== undefined && (
                  <Badge variant="secondary" className="bg-primary/20 text-primary w-fit">
                    {Math.round(recommendation.recommendationScore)}% match
                  </Badge>
                )}
              </CardHeader>
              <CardContent className="space-y-4">
                {/* Matched Skills */}
                {recommendation.matchedSkills?.length > 0 && (
                  <div>
                    <h4 className="text-sm font-medium flex items-center gap-2 mb-2">
                      <CheckCircle className="h-4 w-4 text-green-400" />
                      Skills You Have
                    </h4>
                    <div className="flex flex-wrap gap-1">
                      {recommendation.matchedSkills.map((skill) => (
                        <Badge key={skill} variant="outline" className="text-xs bg-green-500/10 text-green-400 border-green-500/30">
                          {skill}
                        </Badge>
                      ))}
                    </div>
                  </div>
                )}

                {/* Missing Skills */}
                {recommendation.missingSkills?.length > 0 && (
                  <div>
                    <h4 className="text-sm font-medium flex items-center gap-2 mb-2">
                      <XCircle className="h-4 w-4 text-amber-400" />
                      Skills to Learn
                    </h4>
                    <div className="flex flex-wrap gap-1">
                      {recommendation.missingSkills.map((skill) => (
                        <Badge key={skill} variant="outline" className="text-xs bg-amber-500/10 text-amber-400 border-amber-500/30">
                          {skill}
                        </Badge>
                      ))}
                    </div>
                  </div>
                )}
              </CardContent>
            </Card>
          )}

          {/* Source info */}
          {job.sourcePlatform && (
            <Card>
              <CardHeader>
                <CardTitle className="text-lg">Source</CardTitle>
              </CardHeader>
              <CardContent>
                <Badge variant="secondary">{job.sourcePlatform}</Badge>
              </CardContent>
            </Card>
          )}
        </div>
      </div>
    </div>
  );
}




