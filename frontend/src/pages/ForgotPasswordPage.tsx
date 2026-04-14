import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { authApi } from '@/lib/api';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { toast } from '@/components/ui/toaster';
import { Briefcase, Loader2 } from 'lucide-react';
import { AxiosError } from 'axios';
import type { ApiError } from '@/types';

const requestSchema = z.object({
  email: z.string().email('Invalid email address'),
});

const resetSchema = z
  .object({
    email: z.string().email('Invalid email address'),
    code: z
      .string()
      .length(6, 'Code must be 6 digits')
      .regex(/^[0-9]{6}$/, 'Code must contain only digits'),
    newPassword: z
      .string()
      .min(8, 'Password must be at least 8 characters')
      .regex(/[A-Z]/, 'Password must contain at least one uppercase letter')
      .regex(/[a-z]/, 'Password must contain at least one lowercase letter')
      .regex(/[0-9]/, 'Password must contain at least one number'),
    confirmPassword: z.string().min(1, 'Please re-type your password'),
  })
  .refine((data) => data.newPassword === data.confirmPassword, {
    message: 'Passwords do not match',
    path: ['confirmPassword'],
  });

type RequestForm = z.infer<typeof requestSchema>;
type ResetForm = z.infer<typeof resetSchema>;

export default function ForgotPasswordPage() {
  const [isLoading, setIsLoading] = useState(false);
  const [pendingEmail, setPendingEmail] = useState<string | null>(null);

  const requestForm = useForm<RequestForm>({
    resolver: zodResolver(requestSchema),
  });

  const resetForm = useForm<ResetForm>({
    resolver: zodResolver(resetSchema),
    defaultValues: { email: '' },
  });

  const onRequestCode = async (data: RequestForm) => {
    setIsLoading(true);
    try {
      const response = await authApi.forgotPassword({ email: data.email });
      setPendingEmail(data.email);
      resetForm.setValue('email', data.email);
      toast({ title: 'Reset code sent', description: response.message });
    } catch (error) {
      const axiosError = error as AxiosError<ApiError>;
      toast({
        variant: 'destructive',
        title: 'Request failed',
        description: axiosError.response?.data?.message || 'Could not send reset code',
      });
    } finally {
      setIsLoading(false);
    }
  };

  const onResetPassword = async (data: ResetForm) => {
    setIsLoading(true);
    try {
      const response = await authApi.resetPassword({
        email: data.email,
        code: data.code,
        newPassword: data.newPassword,
      });
      toast({ title: 'Password reset successful', description: response.message });
    } catch (error) {
      const axiosError = error as AxiosError<ApiError>;
      toast({
        variant: 'destructive',
        title: 'Reset failed',
        description: axiosError.response?.data?.message || 'Invalid reset code',
      });
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="min-h-screen flex items-center justify-center p-4 bg-gradient-to-br from-background via-background to-primary/5">
      <div className="w-full max-w-md animate-fade-in">
        <div className="flex justify-center mb-8">
          <div className="flex items-center gap-3">
            <div className="h-12 w-12 rounded-xl bg-primary flex items-center justify-center">
              <Briefcase className="h-7 w-7 text-primary-foreground" />
            </div>
            <span className="text-3xl font-bold">Joby</span>
          </div>
        </div>

        <Card>
          <CardHeader className="space-y-1">
            <CardTitle className="text-2xl text-center">
              {pendingEmail ? 'Reset password' : 'Forgot password'}
            </CardTitle>
            <CardDescription className="text-center">
              {pendingEmail
                ? `Enter the code sent to ${pendingEmail}`
                : 'Enter your email to receive a password reset code'}
            </CardDescription>
          </CardHeader>
          <CardContent>
            {!pendingEmail ? (
              <form onSubmit={requestForm.handleSubmit(onRequestCode)} className="space-y-4">
                <div className="space-y-2">
                  <Label htmlFor="email">Email</Label>
                  <Input id="email" type="email" placeholder="name@example.com" {...requestForm.register('email')} />
                  {requestForm.formState.errors.email && (
                    <p className="text-sm text-destructive">{requestForm.formState.errors.email.message}</p>
                  )}
                </div>
                <Button type="submit" className="w-full" disabled={isLoading}>
                  {isLoading && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
                  Send reset code
                </Button>
              </form>
            ) : (
              <form onSubmit={resetForm.handleSubmit(onResetPassword)} className="space-y-4">
                <div className="space-y-2">
                  <Label htmlFor="reset-email">Email</Label>
                  <Input id="reset-email" type="email" {...resetForm.register('email')} />
                  {resetForm.formState.errors.email && (
                    <p className="text-sm text-destructive">{resetForm.formState.errors.email.message}</p>
                  )}
                </div>
                <div className="space-y-2">
                  <Label htmlFor="code">Verification code</Label>
                  <Input id="code" placeholder="123456" {...resetForm.register('code')} />
                  {resetForm.formState.errors.code && (
                    <p className="text-sm text-destructive">{resetForm.formState.errors.code.message}</p>
                  )}
                </div>
                <div className="space-y-2">
                  <Label htmlFor="newPassword">New password</Label>
                  <Input id="newPassword" type="password" {...resetForm.register('newPassword')} />
                  {resetForm.formState.errors.newPassword && (
                    <p className="text-sm text-destructive">{resetForm.formState.errors.newPassword.message}</p>
                  )}
                </div>
                <div className="space-y-2">
                  <Label htmlFor="confirmPassword">Confirm password</Label>
                  <Input id="confirmPassword" type="password" {...resetForm.register('confirmPassword')} />
                  {resetForm.formState.errors.confirmPassword && (
                    <p className="text-sm text-destructive">{resetForm.formState.errors.confirmPassword.message}</p>
                  )}
                </div>
                <Button type="submit" className="w-full" disabled={isLoading}>
                  {isLoading && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
                  Reset password
                </Button>
              </form>
            )}

            <div className="mt-6 text-center text-sm">
              <Link to="/login" className="text-primary hover:underline font-medium">
                Back to sign in
              </Link>
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
