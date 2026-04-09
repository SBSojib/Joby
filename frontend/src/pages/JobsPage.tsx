import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Link, useNavigate } from 'react-router-dom';
import { jobsApi, applicationsApi } from '@/lib/api';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Badge } from '@/components/ui/badge';
import { toast } from '@/components/ui/toaster';
import {
  Plus,
  Search,
  ExternalLink,
  Loader2,
  Briefcase,
  MapPin,
  Building2,
  Sparkles,
} from 'lucide-react';
import { formatDate, truncate } from '@/lib/utils';
import { ApplicationStatus } from '@/types';
import type { JobWithRecommendation } from '@/types';

export default function JobsPage() {
  const [jobUrl, setJobUrl] = useState('');
  const [searchQuery, setSearchQuery] = useState('');
  const [isAddingJob, setIsAddingJob] = useState(false);
  const queryClient = useQueryClient();
  const navigate = useNavigate();

  const { data: recommendedJobs, isLoading } = useQuery({
    queryKey: ['jobs', 'recommended'],
    queryFn: () => jobsApi.getRecommended(1, 50),
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

  const handleAddJob = (e: React.FormEvent) => {
    e.preventDefault();
    if (jobUrl.trim()) {
      addJobMutation.mutate(jobUrl.trim());
    }
  };

  const jobs = recommendedJobs?.items || [];
  const filteredJobs = searchQuery
    ? jobs.filter(
        (job) =>
          job.title.toLowerCase().includes(searchQuery.toLowerCase()) ||
          job.company.toLowerCase().includes(searchQuery.toLowerCase())
      )
    : jobs;

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

      {/* Search */}
      <div className="relative">
        <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
        <Input
          placeholder="Search jobs..."
          value={searchQuery}
          onChange={(e) => setSearchQuery(e.target.value)}
          className="pl-10"
        />
      </div>

      {/* Jobs List */}
      {isLoading ? (
        <div className="flex justify-center py-12">
          <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
        </div>
      ) : filteredJobs.length ? (
        <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
          {filteredJobs.map((job: JobWithRecommendation) => (
            <JobCard
              key={job.id}
              job={job}
              onSave={() => saveJobMutation.mutate(job.id)}
              isSaving={saveJobMutation.isPending}
            />
          ))}
        </div>
      ) : (
        <Card>
          <CardContent className="flex flex-col items-center justify-center py-12">
            <Briefcase className="h-12 w-12 text-muted-foreground mb-4" />
            <h3 className="text-lg font-medium">No jobs found</h3>
            <p className="text-muted-foreground text-center mt-1">
              Add jobs by URL or create a manual entry to get started.
            </p>
          </CardContent>
        </Card>
      )}
    </div>
  );
}

function JobCard({
  job,
  onSave,
  isSaving,
}: {
  job: JobWithRecommendation;
  onSave: () => void;
  isSaving: boolean;
}) {
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

        {/* Recommendation Score */}
        {job.recommendationScore !== undefined && job.recommendationScore > 0 && (
          <div className="flex items-center gap-2">
            <Sparkles className="h-4 w-4 text-primary" />
            <Badge variant="secondary" className="bg-primary/20 text-primary">
              {Math.round(job.recommendationScore)}% match
            </Badge>
          </div>
        )}

        {/* Matched Skills */}
        {job.matchedSkills.length > 0 && (
          <div className="flex flex-wrap gap-1">
            {job.matchedSkills.slice(0, 3).map((skill) => (
              <Badge key={skill} variant="outline" className="text-xs bg-green-500/10 text-green-400 border-green-500/30">
                {skill}
              </Badge>
            ))}
            {job.matchedSkills.length > 3 && (
              <Badge variant="outline" className="text-xs">
                +{job.matchedSkills.length - 3}
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
            <Button variant="secondary" size="sm" className="w-full" asChild>
              <Link to={`/applications/${job.applicationId}`}>View Application</Link>
            </Button>
          ) : (
            <Button size="sm" className="w-full" onClick={onSave} disabled={isSaving}>
              {isSaving ? <Loader2 className="h-4 w-4 animate-spin" /> : 'Save Job'}
            </Button>
          )}
        </div>
      </CardContent>
    </Card>
  );
}




