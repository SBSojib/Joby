import { useState, type FormEvent } from 'react';
import { Link } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { useAuth } from '@/contexts/AuthContext';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { toast } from '@/components/ui/toaster';
import { Briefcase, Loader2 } from 'lucide-react';
import { AxiosError } from 'axios';
import type { ApiError } from '@/types';

const registerSchema = z.object({
  firstName: z.string().min(1, 'First name is required').max(100),
  lastName: z.string().min(1, 'Last name is required').max(100),
  email: z.string().email('Invalid email address'),
  password: z
    .string()
    .min(8, 'Password must be at least 8 characters')
    .regex(/[A-Z]/, 'Password must contain at least one uppercase letter')
    .regex(/[a-z]/, 'Password must contain at least one lowercase letter')
    .regex(/[0-9]/, 'Password must contain at least one number'),
});

type RegisterForm = z.infer<typeof registerSchema>;

export default function RegisterPage() {
  const { register: registerUser, verifyEmail, resendVerificationCode } = useAuth();
  const [isLoading, setIsLoading] = useState(false);
  const [pendingEmail, setPendingEmail] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<RegisterForm>({
    resolver: zodResolver(registerSchema),
  });

  const onSubmit = async (data: RegisterForm) => {
    setIsLoading(true);
    try {
      const response = await registerUser(data);
      setPendingEmail(response.email);
      toast({ title: 'Verification code sent', description: response.message });
    } catch (error) {
      const axiosError = error as AxiosError<ApiError>;
      toast({
        variant: 'destructive',
        title: 'Registration failed',
        description: axiosError.response?.data?.message || 'Something went wrong',
      });
    } finally {
      setIsLoading(false);
    }
  };

  const onVerifySubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (!pendingEmail) {
      return;
    }

    const formData = new FormData(event.currentTarget);
    const code = String(formData.get('code') ?? '').trim();
    if (!code) {
      toast({
        variant: 'destructive',
        title: 'Verification failed',
        description: 'Verification code is required.',
      });
      return;
    }

    setIsLoading(true);
    try {
      await verifyEmail(pendingEmail, code);
      toast({ title: 'Email verified', description: 'Welcome to Joby!' });
    } catch (error) {
      const axiosError = error as AxiosError<ApiError>;
      toast({
        variant: 'destructive',
        title: 'Verification failed',
        description: axiosError.response?.data?.message || 'Invalid or expired code',
      });
    } finally {
      setIsLoading(false);
    }
  };

  const onResendCode = async () => {
    if (!pendingEmail) {
      return;
    }

    setIsLoading(true);
    try {
      await resendVerificationCode(pendingEmail);
      toast({ title: 'Code resent', description: 'Please check your email inbox.' });
    } catch (error) {
      const axiosError = error as AxiosError<ApiError>;
      toast({
        variant: 'destructive',
        title: 'Resend failed',
        description: axiosError.response?.data?.message || 'Could not resend verification code.',
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
              {pendingEmail ? 'Verify your email' : 'Create an account'}
            </CardTitle>
            <CardDescription className="text-center">
              {pendingEmail
                ? `Enter the 6-digit code sent to ${pendingEmail}`
                : 'Start tracking your job applications today'}
            </CardDescription>
          </CardHeader>
          <CardContent>
            {!pendingEmail ? (
              <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
                <div className="grid grid-cols-2 gap-4">
                  <div className="space-y-2">
                    <Label htmlFor="firstName">First name</Label>
                    <Input id="firstName" placeholder="John" {...register('firstName')} />
                    {errors.firstName && (
                      <p className="text-sm text-destructive">{errors.firstName.message}</p>
                    )}
                  </div>
                  <div className="space-y-2">
                    <Label htmlFor="lastName">Last name</Label>
                    <Input id="lastName" placeholder="Doe" {...register('lastName')} />
                    {errors.lastName && (
                      <p className="text-sm text-destructive">{errors.lastName.message}</p>
                    )}
                  </div>
                </div>

                <div className="space-y-2">
                  <Label htmlFor="email">Email</Label>
                  <Input
                    id="email"
                    type="email"
                    placeholder="name@example.com"
                    {...register('email')}
                  />
                  {errors.email && (
                    <p className="text-sm text-destructive">{errors.email.message}</p>
                  )}
                </div>

                <div className="space-y-2">
                  <Label htmlFor="password">Password</Label>
                  <Input
                    id="password"
                    type="password"
                    placeholder="••••••••"
                    {...register('password')}
                  />
                  {errors.password && (
                    <p className="text-sm text-destructive">{errors.password.message}</p>
                  )}
                </div>

                <Button type="submit" className="w-full" disabled={isLoading}>
                  {isLoading && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
                  Create account
                </Button>
              </form>
            ) : (
              <form onSubmit={onVerifySubmit} className="space-y-4">
                <div className="space-y-2">
                  <Label htmlFor="code">Verification code</Label>
                  <Input
                    id="code"
                    name="code"
                    placeholder="123456"
                    inputMode="numeric"
                    maxLength={6}
                    pattern="[0-9]{6}"
                  />
                </div>
                <Button type="submit" className="w-full" disabled={isLoading}>
                  {isLoading && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
                  Verify email
                </Button>
                <Button
                  type="button"
                  variant="outline"
                  className="w-full"
                  onClick={onResendCode}
                  disabled={isLoading}
                >
                  Resend code
                </Button>
              </form>
            )}

            <div className="mt-6 text-center text-sm">
              <span className="text-muted-foreground">Already have an account? </span>
              <Link to="/login" className="text-primary hover:underline font-medium">
                Sign in
              </Link>
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}




