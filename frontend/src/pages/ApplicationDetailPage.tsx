import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useParams, useNavigate, Link } from 'react-router-dom';
import { applicationsApi, remindersApi } from '@/lib/api';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { toast } from '@/components/ui/toaster';
import {
  ArrowLeft,
  ExternalLink,
  Loader2,
  Building2,
  MapPin,
  Clock,
  Bell,
  Plus,
  CheckCircle,
  Calendar,
  MessageSquare,
} from 'lucide-react';
import { formatDate, formatRelativeTime, getStatusColor, cn } from '@/lib/utils';
import { ApplicationStatus, ApplicationStatusLabels, ReminderStatus } from '@/types';
import { useState } from 'react';

export default function ApplicationDetailPage() {
  const { applicationId } = useParams<{ applicationId: string }>();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [showAddEvent, setShowAddEvent] = useState(false);
  const [eventNote, setEventNote] = useState('');

  const { data: application, isLoading } = useQuery({
    queryKey: ['application', applicationId],
    queryFn: () => applicationsApi.get(applicationId!),
    enabled: !!applicationId,
  });

  const updateStatusMutation = useMutation({
    mutationFn: (status: ApplicationStatus) =>
      applicationsApi.updateStatus(applicationId!, status),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['application', applicationId] });
      queryClient.invalidateQueries({ queryKey: ['applications'] });
      toast({ title: 'Status updated' });
    },
  });

  const addEventMutation = useMutation({
    mutationFn: (data: { eventType: string; description: string }) =>
      applicationsApi.addEvent(applicationId!, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['application', applicationId] });
      setShowAddEvent(false);
      setEventNote('');
      toast({ title: 'Note added' });
    },
  });

  const completeReminderMutation = useMutation({
    mutationFn: (id: string) => remindersApi.complete(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['application', applicationId] });
      queryClient.invalidateQueries({ queryKey: ['reminders'] });
      toast({ title: 'Reminder completed' });
    },
  });

  if (isLoading) {
    return (
      <div className="flex justify-center py-12">
        <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
      </div>
    );
  }

  if (!application) {
    return (
      <Card>
        <CardContent className="py-12 text-center">
          <p className="text-muted-foreground">Application not found</p>
        </CardContent>
      </Card>
    );
  }

  const { job } = application;
  const activeReminders = application.reminders.filter(
    (r) => r.status === ReminderStatus.Pending || r.status === ReminderStatus.Snoozed
  );

  const handleAddNote = () => {
    if (eventNote.trim()) {
      addEventMutation.mutate({ eventType: 'Note', description: eventNote.trim() });
    }
  };

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
          {/* Job Info Card */}
          <Card>
            <CardHeader>
              <div className="flex items-start justify-between gap-4">
                <div>
                  <CardTitle className="text-2xl">{job.title}</CardTitle>
                  <div className="flex items-center gap-2 mt-2 text-muted-foreground">
                    <Building2 className="h-4 w-4" />
                    {job.company}
                    {job.location && (
                      <>
                        <span className="text-muted-foreground">•</span>
                        <MapPin className="h-4 w-4" />
                        {job.location}
                      </>
                    )}
                  </div>
                </div>
                <Badge className={cn(getStatusColor(application.status), 'text-sm')}>
                  {ApplicationStatusLabels[application.status]}
                </Badge>
              </div>
            </CardHeader>
            <CardContent>
              <div className="flex flex-wrap gap-2">
                {job.sourceUrl && (
                  <Button variant="outline" size="sm" asChild>
                    <a href={job.sourceUrl} target="_blank" rel="noopener noreferrer">
                      <ExternalLink className="h-4 w-4 mr-2" />
                      View Job Posting
                    </a>
                  </Button>
                )}
                <Button variant="outline" size="sm" asChild>
                  <Link to={`/jobs/${job.id}`}>View Job Details</Link>
                </Button>
              </div>
            </CardContent>
          </Card>

          {/* Timeline */}
          <Card>
            <CardHeader>
              <div className="flex items-center justify-between">
                <CardTitle className="text-lg flex items-center gap-2">
                  <Clock className="h-5 w-5" />
                  Timeline
                </CardTitle>
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => setShowAddEvent(!showAddEvent)}
                >
                  <Plus className="h-4 w-4 mr-2" />
                  Add Note
                </Button>
              </div>
            </CardHeader>
            <CardContent>
              {showAddEvent && (
                <div className="mb-6 p-4 border rounded-lg space-y-3">
                  <Label htmlFor="note">Add a note</Label>
                  <Input
                    id="note"
                    placeholder="Interview went well, discussed salary expectations..."
                    value={eventNote}
                    onChange={(e) => setEventNote(e.target.value)}
                  />
                  <div className="flex gap-2">
                    <Button
                      size="sm"
                      onClick={handleAddNote}
                      disabled={addEventMutation.isPending}
                    >
                      {addEventMutation.isPending && (
                        <Loader2 className="h-4 w-4 animate-spin mr-2" />
                      )}
                      Save Note
                    </Button>
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={() => setShowAddEvent(false)}
                    >
                      Cancel
                    </Button>
                  </div>
                </div>
              )}

              <div className="space-y-4">
                {application.events.map((event, index) => (
                  <div key={event.id} className="flex gap-4">
                    <div className="flex flex-col items-center">
                      <div
                        className={cn(
                          'h-8 w-8 rounded-full flex items-center justify-center',
                          event.eventType === 'StatusChange'
                            ? 'bg-primary/20 text-primary'
                            : 'bg-secondary text-muted-foreground'
                        )}
                      >
                        {event.eventType === 'StatusChange' ? (
                          <CheckCircle className="h-4 w-4" />
                        ) : (
                          <MessageSquare className="h-4 w-4" />
                        )}
                      </div>
                      {index < application.events.length - 1 && (
                        <div className="w-px h-full bg-border mt-2" />
                      )}
                    </div>
                    <div className="flex-1 pb-4">
                      <p className="font-medium text-sm">
                        {event.eventType === 'StatusChange' && event.newStatus !== undefined
                          ? `Status changed to ${ApplicationStatusLabels[event.newStatus]}`
                          : event.eventType}
                      </p>
                      {event.description && (
                        <p className="text-sm text-muted-foreground mt-1">{event.description}</p>
                      )}
                      <p className="text-xs text-muted-foreground mt-1">
                        {formatRelativeTime(event.createdAt)}
                      </p>
                    </div>
                  </div>
                ))}
              </div>
            </CardContent>
          </Card>
        </div>

        {/* Sidebar */}
        <div className="space-y-6">
          {/* Status Actions */}
          <Card>
            <CardHeader>
              <CardTitle className="text-lg">Update Status</CardTitle>
            </CardHeader>
            <CardContent className="space-y-2">
              {Object.values(ApplicationStatus)
                .filter((s) => typeof s === 'number')
                .map((status) => (
                  <Button
                    key={status}
                    variant={application.status === status ? 'default' : 'outline'}
                    size="sm"
                    className="w-full justify-start"
                    onClick={() => updateStatusMutation.mutate(status as ApplicationStatus)}
                    disabled={
                      updateStatusMutation.isPending || application.status === status
                    }
                  >
                    {ApplicationStatusLabels[status as ApplicationStatus]}
                  </Button>
                ))}
            </CardContent>
          </Card>

          {/* Reminders */}
          <Card>
            <CardHeader>
              <CardTitle className="text-lg flex items-center gap-2">
                <Bell className="h-5 w-5" />
                Reminders
              </CardTitle>
            </CardHeader>
            <CardContent>
              {activeReminders.length > 0 ? (
                <div className="space-y-3">
                  {activeReminders.map((reminder) => (
                    <div
                      key={reminder.id}
                      className="p-3 rounded-lg border flex items-start justify-between gap-2"
                    >
                      <div>
                        <p className="text-sm font-medium">{reminder.title}</p>
                        <p className="text-xs text-muted-foreground flex items-center gap-1 mt-1">
                          <Calendar className="h-3 w-3" />
                          Due {formatDate(reminder.dueAt)}
                        </p>
                      </div>
                      <Button
                        variant="ghost"
                        size="sm"
                        onClick={() => completeReminderMutation.mutate(reminder.id)}
                      >
                        <CheckCircle className="h-4 w-4" />
                      </Button>
                    </div>
                  ))}
                </div>
              ) : (
                <p className="text-sm text-muted-foreground">No active reminders</p>
              )}
            </CardContent>
          </Card>

          {/* Contact Info */}
          {(application.contactName || application.contactEmail) && (
            <Card>
              <CardHeader>
                <CardTitle className="text-lg">Contact</CardTitle>
              </CardHeader>
              <CardContent className="space-y-2 text-sm">
                {application.contactName && <p>{application.contactName}</p>}
                {application.contactEmail && (
                  <a
                    href={`mailto:${application.contactEmail}`}
                    className="text-primary hover:underline"
                  >
                    {application.contactEmail}
                  </a>
                )}
                {application.contactPhone && <p>{application.contactPhone}</p>}
              </CardContent>
            </Card>
          )}

          {/* Notes */}
          {application.notes && (
            <Card>
              <CardHeader>
                <CardTitle className="text-lg">Notes</CardTitle>
              </CardHeader>
              <CardContent>
                <p className="text-sm text-muted-foreground whitespace-pre-line">
                  {application.notes}
                </p>
              </CardContent>
            </Card>
          )}
        </div>
      </div>
    </div>
  );
}




