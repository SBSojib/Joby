import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { remindersApi } from '@/lib/api';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { toast } from '@/components/ui/toaster';
import {
  Loader2,
  Bell,
  CheckCircle,
  Clock,
  Calendar,
  XCircle,
} from 'lucide-react';
import { formatDate, formatRelativeTime } from '@/lib/utils';
import { ReminderStatus } from '@/types';
import type { Reminder } from '@/types';
import { Link } from 'react-router-dom';

export default function RemindersPage() {
  const queryClient = useQueryClient();

  const { data: reminders, isLoading } = useQuery({
    queryKey: ['reminders'],
    queryFn: () => remindersApi.getAll(true),
  });

  const completeMutation = useMutation({
    mutationFn: (id: string) => remindersApi.complete(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['reminders'] });
      toast({ title: 'Reminder completed' });
    },
  });

  const snoozeMutation = useMutation({
    mutationFn: ({ id, date }: { id: string; date: string }) => remindersApi.snooze(id, date),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['reminders'] });
      toast({ title: 'Reminder snoozed' });
    },
  });

  const dismissMutation = useMutation({
    mutationFn: (id: string) => remindersApi.dismiss(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['reminders'] });
      toast({ title: 'Reminder dismissed' });
    },
  });

  if (isLoading) {
    return (
      <div className="flex justify-center py-12">
        <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
      </div>
    );
  }

  const activeReminders =
    reminders?.filter(
      (r) => r.status === ReminderStatus.Pending || r.status === ReminderStatus.Snoozed
    ) || [];
  const completedReminders =
    reminders?.filter(
      (r) => r.status === ReminderStatus.Completed || r.status === ReminderStatus.Dismissed
    ) || [];

  const handleSnooze = (id: string, days: number) => {
    const date = new Date();
    date.setDate(date.getDate() + days);
    snoozeMutation.mutate({ id, date: date.toISOString() });
  };

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold">Reminders</h1>
        <p className="text-muted-foreground">Stay on top of your job search</p>
      </div>

      {/* Active Reminders */}
      <div className="space-y-4">
        <h2 className="text-lg font-semibold flex items-center gap-2">
          <Bell className="h-5 w-5 text-primary" />
          Active Reminders
          <Badge variant="secondary">{activeReminders.length}</Badge>
        </h2>

        {activeReminders.length > 0 ? (
          <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
            {activeReminders.map((reminder) => (
              <ReminderCard
                key={reminder.id}
                reminder={reminder}
                onComplete={() => completeMutation.mutate(reminder.id)}
                onSnooze={(days) => handleSnooze(reminder.id, days)}
                onDismiss={() => dismissMutation.mutate(reminder.id)}
              />
            ))}
          </div>
        ) : (
          <Card>
            <CardContent className="flex flex-col items-center justify-center py-12">
              <CheckCircle className="h-12 w-12 text-green-400 mb-4" />
              <h3 className="text-lg font-medium">All caught up!</h3>
              <p className="text-muted-foreground text-center mt-1">
                No active reminders at the moment.
              </p>
            </CardContent>
          </Card>
        )}
      </div>

      {/* Completed Reminders */}
      {completedReminders.length > 0 && (
        <div className="space-y-4">
          <h2 className="text-lg font-semibold flex items-center gap-2 text-muted-foreground">
            <CheckCircle className="h-5 w-5" />
            Completed
            <Badge variant="outline">{completedReminders.length}</Badge>
          </h2>

          <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
            {completedReminders.slice(0, 6).map((reminder) => (
              <Card key={reminder.id} className="opacity-60">
                <CardContent className="pt-6">
                  <div className="flex items-start gap-3">
                    <CheckCircle className="h-5 w-5 text-green-400 mt-0.5" />
                    <div>
                      <h3 className="font-medium line-through">{reminder.title}</h3>
                      {reminder.completedAt && (
                        <p className="text-sm text-muted-foreground mt-1">
                          Completed {formatRelativeTime(reminder.completedAt)}
                        </p>
                      )}
                    </div>
                  </div>
                </CardContent>
              </Card>
            ))}
          </div>
        </div>
      )}
    </div>
  );
}

function ReminderCard({
  reminder,
  onComplete,
  onSnooze,
  onDismiss,
}: {
  reminder: Reminder;
  onComplete: () => void;
  onSnooze: (days: number) => void;
  onDismiss: () => void;
}) {
  const isOverdue = new Date(reminder.dueAt) < new Date();

  return (
    <Card className={isOverdue ? 'border-destructive/50' : ''}>
      <CardHeader className="pb-2">
        <div className="flex items-start justify-between gap-2">
          <CardTitle className="text-base">{reminder.title}</CardTitle>
          {reminder.isAutoGenerated && (
            <Badge variant="outline" className="text-xs shrink-0">
              Auto
            </Badge>
          )}
        </div>
      </CardHeader>
      <CardContent className="space-y-4">
        {reminder.description && (
          <p className="text-sm text-muted-foreground">{reminder.description}</p>
        )}

        <div className="flex items-center gap-2 text-sm">
          {isOverdue ? (
            <Badge variant="destructive" className="flex items-center gap-1">
              <Clock className="h-3 w-3" />
              Overdue
            </Badge>
          ) : (
            <div className="flex items-center gap-1 text-muted-foreground">
              <Calendar className="h-4 w-4" />
              Due {formatDate(reminder.dueAt)}
            </div>
          )}
        </div>

        {reminder.applicationId && (
          <Link
            to={`/applications/${reminder.applicationId}`}
            className="text-sm text-primary hover:underline"
          >
            View Application →
          </Link>
        )}

        <div className="flex flex-wrap gap-2 pt-2">
          <Button size="sm" onClick={onComplete}>
            <CheckCircle className="h-4 w-4 mr-1" />
            Done
          </Button>
          <Button variant="outline" size="sm" onClick={() => onSnooze(1)}>
            +1 day
          </Button>
          <Button variant="outline" size="sm" onClick={() => onSnooze(3)}>
            +3 days
          </Button>
          <Button variant="ghost" size="sm" onClick={onDismiss}>
            <XCircle className="h-4 w-4" />
          </Button>
        </div>
      </CardContent>
    </Card>
  );
}




