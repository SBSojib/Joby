import { useState, useCallback } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useForm } from 'react-hook-form';
import { profileApi } from '@/lib/api';
import { useAuth } from '@/contexts/AuthContext';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Badge } from '@/components/ui/badge';
import { toast } from '@/components/ui/toaster';
import {
  Loader2,
  Upload,
  FileText,
  Plus,
  X,
  Download,
  CheckCircle,
  Trash2,
} from 'lucide-react';
import { formatDate, formatFileSize } from '@/lib/utils';
import type { Profile, Resume } from '@/types';

export default function ProfilePage() {
  const queryClient = useQueryClient();
  const { deleteAccount } = useAuth();
  const [newSkill, setNewSkill] = useState('');
  const [newKeyword, setNewKeyword] = useState('');
  const [deletePassword, setDeletePassword] = useState('');
  const [deleteConfirmText, setDeleteConfirmText] = useState('');

  const { data: profile, isLoading } = useQuery({
    queryKey: ['profile'],
    queryFn: profileApi.get,
  });

  const { data: resumes } = useQuery({
    queryKey: ['resumes'],
    queryFn: profileApi.getResumes,
  });

  const { register, handleSubmit } = useForm<Partial<Profile>>();

  const updateProfileMutation = useMutation({
    mutationFn: (data: Partial<Profile>) => profileApi.update(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['profile'] });
      toast({ title: 'Profile updated' });
    },
  });

  const uploadResumeMutation = useMutation({
    mutationFn: (file: File) => profileApi.uploadResume(file),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['resumes'] });
      queryClient.invalidateQueries({ queryKey: ['profile'] });
      toast({ title: 'Resume uploaded', description: 'Your resume has been processed.' });
    },
    onError: () => {
      toast({ variant: 'destructive', title: 'Upload failed', description: 'Please try again.' });
    },
  });

  const setActiveResumeMutation = useMutation({
    mutationFn: (id: string) => profileApi.setActiveResume(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['profile'] });
      toast({ title: 'Active resume updated' });
    },
  });

  const deleteResumeMutation = useMutation({
    mutationFn: (id: string) => profileApi.deleteResume(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['resumes'] });
      queryClient.invalidateQueries({ queryKey: ['profile'] });
      toast({ title: 'Resume deleted' });
    },
  });

  const handleFileUpload = useCallback(
    (e: React.ChangeEvent<HTMLInputElement>) => {
      const file = e.target.files?.[0];
      if (file) {
        uploadResumeMutation.mutate(file);
      }
    },
    [uploadResumeMutation]
  );

  const handleDownloadResume = async (resume: Resume) => {
    try {
      const blob = await profileApi.downloadResume(resume.id);
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = resume.fileName;
      a.click();
      window.URL.revokeObjectURL(url);
    } catch {
      toast({ variant: 'destructive', title: 'Download failed' });
    }
  };

  const addSkill = () => {
    if (newSkill.trim() && profile) {
      const skills = [...(profile.skills || []), newSkill.trim()];
      updateProfileMutation.mutate({ skills });
      setNewSkill('');
    }
  };

  const removeSkill = (skill: string) => {
    if (profile) {
      const skills = profile.skills.filter((s) => s !== skill);
      updateProfileMutation.mutate({ skills });
    }
  };

  const addKeyword = () => {
    if (newKeyword.trim() && profile) {
      const keywords = [...(profile.keywords || []), newKeyword.trim()];
      updateProfileMutation.mutate({ keywords });
      setNewKeyword('');
    }
  };

  const removeKeyword = (keyword: string) => {
    if (profile) {
      const keywords = profile.keywords.filter((k) => k !== keyword);
      updateProfileMutation.mutate({ keywords });
    }
  };

  const onSubmit = (data: Partial<Profile>) => {
    updateProfileMutation.mutate(data);
  };

  const canDeleteAccount = deletePassword.trim().length > 0 && deleteConfirmText === 'DELETE';

  const handleDeleteAccount = async () => {
    if (!canDeleteAccount) {
      return;
    }

    const confirmed = window.confirm(
      'This will permanently delete your account and all related data. This action cannot be undone. Continue?'
    );
    if (!confirmed) {
      return;
    }

    try {
      await deleteAccount(deletePassword);
      toast({ title: 'Account deleted', description: 'Your account and related data were removed.' });
    } catch {
      toast({
        variant: 'destructive',
        title: 'Delete failed',
        description: 'Could not delete your account. Check your password and try again.',
      });
    }
  };

  if (isLoading) {
    return (
      <div className="flex justify-center py-12">
        <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold">Profile</h1>
        <p className="text-muted-foreground">Manage your profile and resume</p>
      </div>

      <div className="grid gap-6 lg:grid-cols-2">
        {/* Basic Info */}
        <Card>
          <CardHeader>
            <CardTitle>Basic Information</CardTitle>
            <CardDescription>Update your personal details</CardDescription>
          </CardHeader>
          <CardContent>
            <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
              <div className="grid grid-cols-2 gap-4">
                <div className="space-y-2">
                  <Label htmlFor="fullName">Full Name</Label>
                  <Input
                    id="fullName"
                    defaultValue={profile?.fullName || ''}
                    {...register('fullName')}
                  />
                </div>
                <div className="space-y-2">
                  <Label htmlFor="currentTitle">Current Title</Label>
                  <Input
                    id="currentTitle"
                    defaultValue={profile?.currentTitle || ''}
                    {...register('currentTitle')}
                  />
                </div>
              </div>

              <div className="grid grid-cols-2 gap-4">
                <div className="space-y-2">
                  <Label htmlFor="email">Email</Label>
                  <Input
                    id="email"
                    type="email"
                    defaultValue={profile?.email || ''}
                    {...register('email')}
                  />
                </div>
                <div className="space-y-2">
                  <Label htmlFor="phone">Phone</Label>
                  <Input
                    id="phone"
                    defaultValue={profile?.phone || ''}
                    {...register('phone')}
                  />
                </div>
              </div>

              <div className="space-y-2">
                <Label htmlFor="location">Location</Label>
                <Input
                  id="location"
                  defaultValue={profile?.location || ''}
                  {...register('location')}
                />
              </div>

              <Button type="submit" disabled={updateProfileMutation.isPending}>
                {updateProfileMutation.isPending && (
                  <Loader2 className="h-4 w-4 animate-spin mr-2" />
                )}
                Save Changes
              </Button>
            </form>
          </CardContent>
        </Card>

        {/* Resume Upload */}
        <Card>
          <CardHeader>
            <CardTitle>Resume</CardTitle>
            <CardDescription>Upload your resume to auto-fill profile data</CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <div
              className="border-2 border-dashed rounded-lg p-6 text-center hover:border-primary/50 transition-colors cursor-pointer"
              onClick={() => document.getElementById('resume-upload')?.click()}
            >
              <input
                type="file"
                id="resume-upload"
                className="hidden"
                accept=".pdf,.docx"
                onChange={handleFileUpload}
                disabled={uploadResumeMutation.isPending}
              />
              {uploadResumeMutation.isPending ? (
                <Loader2 className="h-8 w-8 animate-spin mx-auto text-muted-foreground" />
              ) : (
                <>
                  <Upload className="h-8 w-8 mx-auto text-muted-foreground mb-2" />
                  <p className="text-sm text-muted-foreground">
                    Click to upload PDF or DOCX
                  </p>
                </>
              )}
            </div>

            {resumes && resumes.length > 0 && (
              <div className="space-y-2">
                <Label>Your Resumes</Label>
                {resumes.map((resume) => (
                  <div
                    key={resume.id}
                    className="flex items-center justify-between p-3 rounded-lg border"
                  >
                    <div className="flex items-center gap-3">
                      <FileText className="h-5 w-5 text-muted-foreground" />
                      <div>
                        <p className="text-sm font-medium">{resume.fileName}</p>
                        <p className="text-xs text-muted-foreground">
                          {formatFileSize(resume.fileSize)} • {formatDate(resume.createdAt)}
                        </p>
                      </div>
                    </div>
                    <div className="flex items-center gap-2">
                      {profile?.activeResumeId === resume.id ? (
                        <Badge variant="secondary">
                          <CheckCircle className="h-3 w-3 mr-1" />
                          Active
                        </Badge>
                      ) : (
                        <Button
                          variant="ghost"
                          size="sm"
                          onClick={() => setActiveResumeMutation.mutate(resume.id)}
                        >
                          Set Active
                        </Button>
                      )}
                      <Button
                        variant="ghost"
                        size="sm"
                        onClick={() => handleDownloadResume(resume)}
                      >
                        <Download className="h-4 w-4" />
                      </Button>
                      <Button
                        variant="ghost"
                        size="sm"
                        onClick={() => deleteResumeMutation.mutate(resume.id)}
                      >
                        <Trash2 className="h-4 w-4 text-destructive" />
                      </Button>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </CardContent>
        </Card>

        {/* Skills */}
        <Card>
          <CardHeader>
            <CardTitle>Skills</CardTitle>
            <CardDescription>Add skills for better job matching</CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="flex gap-2">
              <Input
                placeholder="Add a skill..."
                value={newSkill}
                onChange={(e) => setNewSkill(e.target.value)}
                onKeyDown={(e) => e.key === 'Enter' && (e.preventDefault(), addSkill())}
              />
              <Button onClick={addSkill} disabled={!newSkill.trim()}>
                <Plus className="h-4 w-4" />
              </Button>
            </div>
            <div className="flex flex-wrap gap-2">
              {profile?.skills.map((skill) => (
                <Badge key={skill} variant="secondary" className="pr-1">
                  {skill}
                  <button
                    onClick={() => removeSkill(skill)}
                    className="ml-1 hover:text-destructive"
                  >
                    <X className="h-3 w-3" />
                  </button>
                </Badge>
              ))}
              {(!profile?.skills || profile.skills.length === 0) && (
                <p className="text-sm text-muted-foreground">No skills added yet</p>
              )}
            </div>
          </CardContent>
        </Card>

        {/* Keywords */}
        <Card>
          <CardHeader>
            <CardTitle>Job Keywords</CardTitle>
            <CardDescription>Keywords to match against job descriptions</CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="flex gap-2">
              <Input
                placeholder="Add a keyword..."
                value={newKeyword}
                onChange={(e) => setNewKeyword(e.target.value)}
                onKeyDown={(e) => e.key === 'Enter' && (e.preventDefault(), addKeyword())}
              />
              <Button onClick={addKeyword} disabled={!newKeyword.trim()}>
                <Plus className="h-4 w-4" />
              </Button>
            </div>
            <div className="flex flex-wrap gap-2">
              {profile?.keywords.map((keyword) => (
                <Badge key={keyword} variant="outline" className="pr-1">
                  {keyword}
                  <button
                    onClick={() => removeKeyword(keyword)}
                    className="ml-1 hover:text-destructive"
                  >
                    <X className="h-3 w-3" />
                  </button>
                </Badge>
              ))}
              {(!profile?.keywords || profile.keywords.length === 0) && (
                <p className="text-sm text-muted-foreground">No keywords added yet</p>
              )}
            </div>
          </CardContent>
        </Card>

        {/* Danger Zone */}
        <Card className="border-destructive/40">
          <CardHeader>
            <CardTitle className="text-destructive">Danger Zone</CardTitle>
            <CardDescription>
              Permanently delete your account and all associated data (jobs, applications, reminders, profile, resumes,
              tokens). This cannot be undone.
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="space-y-2">
              <Label htmlFor="deletePassword">Confirm password</Label>
              <Input
                id="deletePassword"
                type="password"
                placeholder="Enter your password"
                value={deletePassword}
                onChange={(e) => setDeletePassword(e.target.value)}
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="deleteConfirmText">Type DELETE to confirm</Label>
              <Input
                id="deleteConfirmText"
                placeholder="DELETE"
                value={deleteConfirmText}
                onChange={(e) => setDeleteConfirmText(e.target.value)}
              />
            </div>
            <Button variant="destructive" disabled={!canDeleteAccount} onClick={handleDeleteAccount}>
              Delete account permanently
            </Button>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}


