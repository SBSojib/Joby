import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { AuthProvider, useAuth } from './AuthContext';

const apiMocks = vi.hoisted(() => ({
  refresh: vi.fn(),
  setAccessToken: vi.fn(),
}));

vi.mock('@/lib/api', () => ({
  authApi: {
    refresh: apiMocks.refresh,
  },
  adminApi: {},
  setAccessToken: apiMocks.setAccessToken,
}));

function AuthProbe() {
  const { isAuthenticated, isLoading, user } = useAuth();

  if (isLoading) {
    return <div>loading</div>;
  }

  return (
    <div>
      <span>{isAuthenticated ? 'authenticated' : 'anonymous'}</span>
      <span>{user?.email ?? 'no-user'}</span>
    </div>
  );
}

function renderAuthProvider() {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
    },
  });

  render(
    <MemoryRouter>
      <QueryClientProvider client={queryClient}>
        <AuthProvider>
          <AuthProbe />
        </AuthProvider>
      </QueryClientProvider>
    </MemoryRouter>
  );
}

describe('AuthProvider', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('restores an existing session from the refresh cookie', async () => {
    apiMocks.refresh.mockResolvedValue({
      accessToken: 'fresh-token',
      refreshToken: '',
      expiresAt: new Date().toISOString(),
      user: {
        id: 'user-1',
        email: 'user@example.com',
        firstName: 'Test',
        lastName: 'User',
        isAdmin: false,
        defaultFollowUpDays: 7,
      },
    });

    renderAuthProvider();

    await screen.findByText('authenticated');
    expect(screen.getByText('user@example.com')).toBeInTheDocument();
    expect(apiMocks.setAccessToken).toHaveBeenCalledWith('fresh-token');
  });

  it('clears the in-memory access token when refresh fails', async () => {
    apiMocks.refresh.mockRejectedValue(new Error('expired'));

    renderAuthProvider();

    await waitFor(() => expect(screen.getByText('anonymous')).toBeInTheDocument());
    expect(apiMocks.setAccessToken).toHaveBeenCalledWith(null);
  });
});
