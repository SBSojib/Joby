import { createContext, useContext, useEffect, useState, ReactNode } from 'react';
import { useNavigate } from 'react-router-dom';
import { useQueryClient } from '@tanstack/react-query';
import { authApi, adminApi, setAccessToken, getAccessToken } from '@/lib/api';
import type { User, LoginRequest, RegisterRequest, RegisterPendingResponse } from '@/types';

interface AuthContextType {
  user: User | null;
  isLoading: boolean;
  isAuthenticated: boolean;
  login: (data: LoginRequest) => Promise<void>;
  register: (data: RegisterRequest) => Promise<RegisterPendingResponse>;
  verifyEmail: (email: string, code: string) => Promise<void>;
  resendVerificationCode: (email: string) => Promise<void>;
  deleteAccount: (password: string) => Promise<void>;
  logout: () => Promise<void>;
  stopImpersonation: () => Promise<void>;
  impersonateUser: (userId: string) => Promise<void>;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  useEffect(() => {
    const initAuth = async () => {
      const token = getAccessToken();
      if (token) {
        try {
          const userData = await authApi.me();
          setUser(userData);
        } catch {
          setAccessToken(null);
        }
      }
      setIsLoading(false);
    };

    initAuth();
  }, []);

  const login = async (data: LoginRequest) => {
    const response = await authApi.login(data);
    // Prevent cross-account stale UI from previously cached queries.
    queryClient.clear();
    setAccessToken(response.accessToken);
    setUser(response.user);
    navigate('/');
  };

  const register = async (data: RegisterRequest) => {
    return authApi.register(data);
  };

  const verifyEmail = async (email: string, code: string) => {
    const response = await authApi.verifyEmail({ email, code });
    // New session should start with an empty query cache.
    queryClient.clear();
    setAccessToken(response.accessToken);
    setUser(response.user);
    navigate('/');
  };

  const resendVerificationCode = async (email: string) => {
    await authApi.resendVerification(email);
  };

  const deleteAccount = async (password: string) => {
    await authApi.deleteAccount({ password });
    queryClient.clear();
    setAccessToken(null);
    setUser(null);
    navigate('/');
  };

  const logout = async () => {
    try {
      await authApi.logout();
    } catch {
      // Ignore errors
    }
    // Clear cached account-specific data after logout.
    queryClient.clear();
    setAccessToken(null);
    setUser(null);
    navigate('/');
  };

  const stopImpersonation = async () => {
    const response = await authApi.stopImpersonation();
    queryClient.clear();
    setAccessToken(response.accessToken);
    setUser(response.user);
    navigate('/');
  };

  const impersonateUser = async (userId: string) => {
    const response = await adminApi.impersonateUser(userId);
    queryClient.clear();
    setAccessToken(response.accessToken);
    setUser(response.user);
    navigate('/');
  };

  return (
    <AuthContext.Provider
      value={{
        user,
        isLoading,
        isAuthenticated: !!user,
        login,
        register,
        verifyEmail,
        resendVerificationCode,
        deleteAccount,
        logout,
        stopImpersonation,
        impersonateUser,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (context === undefined) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
}




