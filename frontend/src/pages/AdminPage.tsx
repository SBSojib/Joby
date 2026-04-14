import { useMemo } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Navigate } from 'react-router-dom';
import { adminApi } from '@/lib/api';
import { useAuth } from '@/contexts/AuthContext';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Loader2, Trash2 } from 'lucide-react';
import { toast } from '@/components/ui/toaster';
import { formatDate } from '@/lib/utils';

export default function AdminPage() {
  const { user } = useAuth();
  const queryClient = useQueryClient();

  const isRootAdmin = !!user?.isAdmin;

  const { data: users, isLoading } = useQuery({
    queryKey: ['admin-users'],
    queryFn: adminApi.getUsers,
    enabled: isRootAdmin,
  });

  const deleteUserMutation = useMutation({
    mutationFn: (userId: string) => adminApi.deleteUser(userId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin-users'] });
      toast({ title: 'User deleted', description: 'User and related data were removed.' });
    },
    onError: () => {
      toast({ variant: 'destructive', title: 'Delete failed', description: 'Could not delete user.' });
    },
  });

  const userCount = useMemo(() => users?.length ?? 0, [users]);

  if (!isRootAdmin) {
    return <Navigate to="/" replace />;
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold">Admin</h1>
        <p className="text-muted-foreground">Manage users and permanently delete accounts.</p>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Users</CardTitle>
          <CardDescription>{userCount} total users</CardDescription>
        </CardHeader>
        <CardContent>
          {isLoading ? (
            <div className="flex items-center justify-center py-12">
              <Loader2 className="h-6 w-6 animate-spin text-muted-foreground" />
            </div>
          ) : users && users.length > 0 ? (
            <div className="space-y-3">
              {users.map((u) => (
                <div key={u.id} className="rounded-lg border p-4 flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
                  <div>
                    <p className="font-medium">{u.firstName} {u.lastName}</p>
                    <p className="text-sm text-muted-foreground">{u.email}</p>
                    <p className="text-xs text-muted-foreground">
                      Joined {formatDate(u.createdAt)}{u.lastLoginAt ? ` • Last login ${formatDate(u.lastLoginAt)}` : ''}
                    </p>
                  </div>
                  <div className="flex items-center gap-2">
                    <Badge variant={u.isEmailVerified ? 'secondary' : 'outline'}>
                      {u.isEmailVerified ? 'Verified' : 'Unverified'}
                    </Badge>
                    {u.isRootAdmin ? (
                      <Badge>Root</Badge>
                    ) : (
                      <Button
                        variant="destructive"
                        size="sm"
                        disabled={deleteUserMutation.isPending}
                        onClick={() => {
                          const ok = window.confirm(
                            `Delete ${u.email}? This will permanently remove all related data.`
                          );
                          if (ok) {
                            deleteUserMutation.mutate(u.id);
                          }
                        }}
                      >
                        <Trash2 className="h-4 w-4 mr-1" />
                        Delete
                      </Button>
                    )}
                  </div>
                </div>
              ))}
            </div>
          ) : (
            <p className="text-sm text-muted-foreground">No users found.</p>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
