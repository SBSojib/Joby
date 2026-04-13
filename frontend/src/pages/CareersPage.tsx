import { useRef, useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { toast } from '@/components/ui/toaster';
import { careersApi } from '@/lib/api';
import { CONTACT_EMAIL } from '@/constants/marketing';
import { Loader2 } from 'lucide-react';
import { AxiosError } from 'axios';

const maxFileBytes = 10 * 1024 * 1024;
const allowedExt = new Set(['pdf', 'doc', 'docx']);

const careerSchema = z.object({
  fullName: z.string().min(1, 'Name is required').max(200),
  email: z.string().email('Valid email required'),
  roleInterest: z.string().max(200).optional(),
  message: z.string().max(5000).optional(),
});

type CareerForm = z.infer<typeof careerSchema>;

export default function CareersPage() {
  const [submitting, setSubmitting] = useState(false);
  const resumeRef = useRef<HTMLInputElement>(null);
  const {
    register,
    handleSubmit,
    formState: { errors },
    reset,
  } = useForm<CareerForm>({
    resolver: zodResolver(careerSchema),
  });

  const onSubmit = async (data: CareerForm) => {
    const file = resumeRef.current?.files?.[0];
    if (!file) {
      toast({
        variant: 'destructive',
        title: 'Resume required',
        description: 'Please attach a PDF or Word file.',
      });
      return;
    }
    if (file.size > maxFileBytes) {
      toast({
        variant: 'destructive',
        title: 'File too large',
        description: 'Resume must be 10 MB or smaller.',
      });
      return;
    }
    const ext = file.name.split('.').pop()?.toLowerCase() ?? '';
    if (!allowedExt.has(ext)) {
      toast({
        variant: 'destructive',
        title: 'Invalid file type',
        description: 'Use PDF or Word (.pdf, .doc, .docx).',
      });
      return;
    }

    setSubmitting(true);
    try {
      const fd = new FormData();
      fd.append('fullName', data.fullName);
      fd.append('email', data.email);
      fd.append('roleInterest', data.roleInterest ?? '');
      fd.append('message', data.message ?? '');
      fd.append('resume', file);

      await careersApi.submitApplication(fd);
      toast({
        title: 'Application sent',
        description: 'Thank you — we will review your resume and get back to you by email.',
      });
      reset();
      if (resumeRef.current) resumeRef.current.value = '';
    } catch (err) {
      const ax = err as AxiosError<{ message?: string }>;
      const msg =
        ax.response?.data?.message ||
        'Could not submit right now. Try again later or email your resume directly.';
      toast({ variant: 'destructive', title: 'Submission failed', description: msg });
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div className="mx-auto max-w-2xl px-4 sm:px-6 lg:px-8 py-12 sm:py-16">
      <h1 className="text-3xl sm:text-4xl font-bold tracking-tight mb-4">Careers</h1>
      <p className="text-muted-foreground leading-relaxed mb-10">
        We are a small team building Joby for job seekers. If you want to contribute — engineering,
        design, or operations — send your resume using the form below. You can also reach us
        directly at{' '}
        <a className="text-primary hover:underline" href={`mailto:${CONTACT_EMAIL}`}>
          {CONTACT_EMAIL}
        </a>
        .
      </p>

      <Card className="border-border/50 bg-card/50">
        <CardHeader>
          <CardTitle>Apply</CardTitle>
          <CardDescription>
            PDF or Word, up to 10 MB. Your application is emailed to our hiring inbox (SMTP must be
            configured on the server for delivery to succeed).
          </CardDescription>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleSubmit(onSubmit)} className="space-y-5">
            <div className="space-y-2">
              <Label htmlFor="fullName">Full name</Label>
              <Input id="fullName" autoComplete="name" {...register('fullName')} />
              {errors.fullName && (
                <p className="text-sm text-destructive">{errors.fullName.message}</p>
              )}
            </div>
            <div className="space-y-2">
              <Label htmlFor="email">Email</Label>
              <Input id="email" type="email" autoComplete="email" {...register('email')} />
              {errors.email && <p className="text-sm text-destructive">{errors.email.message}</p>}
            </div>
            <div className="space-y-2">
              <Label htmlFor="roleInterest">Role or area of interest (optional)</Label>
              <Input
                id="roleInterest"
                placeholder="e.g. Backend engineer, Internship"
                {...register('roleInterest')}
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="message">Message (optional)</Label>
              <textarea
                id="message"
                rows={4}
                className="flex w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50"
                placeholder="Tell us what you are looking for"
                {...register('message')}
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="resume">Resume</Label>
              <Input
                id="resume"
                ref={resumeRef}
                type="file"
                accept=".pdf,.doc,.docx,application/pdf,application/msword,application/vnd.openxmlformats-officedocument.wordprocessingml.document"
              />
            </div>
            <Button type="submit" className="w-full sm:w-auto" disabled={submitting}>
              {submitting && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
              Submit application
            </Button>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}
