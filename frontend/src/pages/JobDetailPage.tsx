import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useParams, Link, useNavigate } from 'react-router-dom';
import { jobsApi, applicationsApi } from '@/lib/api';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
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
} from 'lucide-react';
import { formatDate } from '@/lib/utils';
import { ApplicationStatus } from '@/types';

export default function JobDetailPage() {
  const { jobId } = useParams<{ jobId: string }>();
  const navigate = useNavigate();
  const queryClient = useQueryClient();

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

              {/* Description */}
              {job.description && (
                <div>
                  <h3 className="font-semibold mb-2">Description</h3>
                  <div className="prose prose-sm prose-invert max-w-none">
                    <p className="whitespace-pre-line text-muted-foreground">{job.description}</p>
                  </div>
                </div>
              )}

              {/* Requirements */}
              {job.requirements && (
                <div>
                  <h3 className="font-semibold mb-2">Requirements</h3>
                  <div className="prose prose-sm prose-invert max-w-none">
                    <p className="whitespace-pre-line text-muted-foreground">{job.requirements}</p>
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




