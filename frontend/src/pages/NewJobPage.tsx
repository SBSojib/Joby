import { useForm } from 'react-hook-form';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { Link, useNavigate } from 'react-router-dom';
import { jobsApi } from '@/lib/api';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { toast } from '@/components/ui/toaster';
import { ArrowLeft, Loader2 } from 'lucide-react';
import type { CreateJobRequest } from '@/types';

export default function NewJobPage() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { register, handleSubmit } = useForm<CreateJobRequest>({
    defaultValues: {
      title: '',
      company: '',
      location: '',
      description: '',
      requirements: '',
      salary: '',
      jobType: '',
      sourceUrl: '',
    },
  });

  const createMutation = useMutation({
    mutationFn: (data: CreateJobRequest) => jobsApi.create(data),
    onSuccess: (job) => {
      queryClient.invalidateQueries({ queryKey: ['jobs'] });
      queryClient.invalidateQueries({ queryKey: ['applications'] });
      toast({ title: 'Job added!', description: 'The job has been saved.' });
      navigate(`/jobs/${job.id}`);
    },
    onError: () => {
      toast({
        variant: 'destructive',
        title: 'Failed to add job',
        description: 'Check required fields and try again.',
      });
    },
  });

  const onSubmit = (data: CreateJobRequest) => {
    const payload: CreateJobRequest = {
      title: data.title.trim(),
      company: data.company.trim(),
      location: data.location?.trim() || undefined,
      description: data.description?.trim() || undefined,
      requirements: data.requirements?.trim() || undefined,
      salary: data.salary?.trim() || undefined,
      jobType: data.jobType?.trim() || undefined,
      sourceUrl: data.sourceUrl?.trim() || undefined,
    };
    createMutation.mutate(payload);
  };

  return (
    <div className="space-y-6 max-w-2xl">
      <Button variant="ghost" size="sm" asChild>
        <Link to="/jobs">
          <ArrowLeft className="h-4 w-4 mr-2" />
          Back to jobs
        </Link>
      </Button>

      <Card>
        <CardHeader>
          <CardTitle>Add job manually</CardTitle>
          <CardDescription>Enter the role details yourself when you do not have a listing URL.</CardDescription>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
            <div className="space-y-2">
              <Label htmlFor="title">Title</Label>
              <Input id="title" {...register('title', { required: true })} placeholder="e.g. Senior Software Engineer" />
            </div>
            <div className="space-y-2">
              <Label htmlFor="company">Company</Label>
              <Input id="company" {...register('company', { required: true })} placeholder="Company name" />
            </div>
            <div className="space-y-2">
              <Label htmlFor="location">Location</Label>
              <Input id="location" {...register('location')} placeholder="City or remote" />
            </div>
            <div className="space-y-2">
              <Label htmlFor="jobType">Job type</Label>
              <Input id="jobType" {...register('jobType')} placeholder="Full-time, contract…" />
            </div>
            <div className="space-y-2">
              <Label htmlFor="salary">Salary</Label>
              <Input id="salary" {...register('salary')} placeholder="Optional" />
            </div>
            <div className="space-y-2">
              <Label htmlFor="sourceUrl">Source URL</Label>
              <Input id="sourceUrl" type="url" {...register('sourceUrl')} placeholder="https://…" />
            </div>
            <div className="space-y-2">
              <Label htmlFor="description">Description</Label>
              <textarea
                id="description"
                className="flex min-h-[120px] w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
                {...register('description')}
                placeholder="Role summary"
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="requirements">Requirements</Label>
              <textarea
                id="requirements"
                className="flex min-h-[100px] w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
                {...register('requirements')}
                placeholder="Skills, experience…"
              />
            </div>
            <div className="flex gap-2 pt-2">
              <Button type="submit" disabled={createMutation.isPending}>
                {createMutation.isPending ? (
                  <Loader2 className="h-4 w-4 animate-spin mr-2" />
                ) : null}
                Save job
              </Button>
              <Button type="button" variant="outline" asChild>
                <Link to="/jobs">Cancel</Link>
              </Button>
            </div>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}
