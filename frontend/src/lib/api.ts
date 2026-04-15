import axios, { AxiosError, InternalAxiosRequestConfig } from 'axios';
import type {
  AuthResponse,
  LoginRequest,
  DeleteAccountRequest,
  RegisterRequest,
  RegisterPendingResponse,
  VerifyEmailRequest,
  ForgotPasswordRequest,
  ResetPasswordRequest,
  User,
  Profile,
  Resume,
  Job,
  JobWithRecommendation,
  CreateJobRequest,
  Application,
  ApplicationStatus,
  Reminder,
  PagedResult,
  AdminUser,
} from '@/types';

const API_BASE_URL = import.meta.env.VITE_API_URL || '/api';

const api = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
  withCredentials: true,
});

// Token management
let accessToken: string | null = null;

export const setAccessToken = (token: string | null) => {
  accessToken = token;
  if (token) {
    localStorage.setItem('accessToken', token);
  } else {
    localStorage.removeItem('accessToken');
  }
};

export const getAccessToken = () => {
  if (!accessToken) {
    accessToken = localStorage.getItem('accessToken');
  }
  return accessToken;
};

// Request interceptor
api.interceptors.request.use(
  (config: InternalAxiosRequestConfig) => {
    const token = getAccessToken();
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => Promise.reject(error)
);

// Response interceptor for token refresh
api.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const originalRequest = error.config as InternalAxiosRequestConfig & { _retry?: boolean };
    const requestPath = originalRequest?.url ?? '';
    const isAuthEndpoint =
      requestPath.includes('/auth/login') ||
      requestPath.includes('/auth/register') ||
      requestPath.includes('/auth/verify-email') ||
      requestPath.includes('/auth/resend-verification') ||
      requestPath.includes('/auth/refresh');
    const hasAccessToken = !!getAccessToken();

    if (error.response?.status === 401 && !originalRequest._retry && hasAccessToken && !isAuthEndpoint) {
      originalRequest._retry = true;

      try {
        const response = await api.post<AuthResponse>('/auth/refresh');
        setAccessToken(response.data.accessToken);
        originalRequest.headers.Authorization = `Bearer ${response.data.accessToken}`;
        return api(originalRequest);
      } catch {
        setAccessToken(null);
        window.location.href = '/login';
      }
    }

    return Promise.reject(error);
  }
);

// Auth API
export const authApi = {
  register: async (data: RegisterRequest): Promise<RegisterPendingResponse> => {
    const response = await api.post<RegisterPendingResponse>('/auth/register', data);
    return response.data;
  },

  verifyEmail: async (data: VerifyEmailRequest): Promise<AuthResponse> => {
    const response = await api.post<AuthResponse>('/auth/verify-email', data);
    return response.data;
  },

  resendVerification: async (email: string): Promise<void> => {
    await api.post('/auth/resend-verification', { email });
  },

  login: async (data: LoginRequest): Promise<AuthResponse> => {
    const response = await api.post<AuthResponse>('/auth/login', data);
    return response.data;
  },

  logout: async (): Promise<void> => {
    await api.post('/auth/logout');
  },

  deleteAccount: async (data: DeleteAccountRequest): Promise<void> => {
    await api.delete('/auth/account', { data });
  },

  refresh: async (): Promise<AuthResponse> => {
    const response = await api.post<AuthResponse>('/auth/refresh');
    return response.data;
  },

  me: async (): Promise<User> => {
    const response = await api.get<User>('/auth/me');
    return response.data;
  },

  stopImpersonation: async (): Promise<AuthResponse> => {
    const response = await api.post<AuthResponse>('/auth/stop-impersonation');
    return response.data;
  },

  forgotPassword: async (data: ForgotPasswordRequest): Promise<{ message: string }> => {
    const response = await api.post<{ message: string }>('/auth/forgot-password', data);
    return response.data;
  },

  resetPassword: async (data: ResetPasswordRequest): Promise<{ message: string }> => {
    const response = await api.post<{ message: string }>('/auth/reset-password', data);
    return response.data;
  },
};

// Profile API
export const profileApi = {
  get: async (): Promise<Profile> => {
    const response = await api.get<Profile>('/profile');
    return response.data;
  },

  update: async (data: Partial<Profile>): Promise<Profile> => {
    const response = await api.put<Profile>('/profile', data);
    return response.data;
  },

  uploadResume: async (file: File): Promise<Resume> => {
    const formData = new FormData();
    formData.append('file', file);
    const response = await api.post<Resume>('/profile/resumes', formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    });
    return response.data;
  },

  getResumes: async (): Promise<Resume[]> => {
    const response = await api.get<Resume[]>('/profile/resumes');
    return response.data;
  },

  getResume: async (id: string): Promise<Resume> => {
    const response = await api.get<Resume>(`/profile/resumes/${id}`);
    return response.data;
  },

  downloadResume: async (id: string): Promise<Blob> => {
    const response = await api.get(`/profile/resumes/${id}/download`, {
      responseType: 'blob',
    });
    return response.data;
  },

  setActiveResume: async (id: string): Promise<void> => {
    await api.put(`/profile/resumes/${id}/active`);
  },

  deleteResume: async (id: string): Promise<void> => {
    await api.delete(`/profile/resumes/${id}`);
  },
};

// Jobs API
export const jobsApi = {
  create: async (data: CreateJobRequest): Promise<Job> => {
    const response = await api.post<Job>('/jobs', data);
    return response.data;
  },

  createByUrl: async (url: string): Promise<Job> => {
    const response = await api.post<Job>('/jobs/url', { url });
    return response.data;
  },

  get: async (id: string): Promise<Job> => {
    const response = await api.get<Job>(`/jobs/${id}`);
    return response.data;
  },

  getRecommended: async (page = 1, pageSize = 20): Promise<PagedResult<JobWithRecommendation>> => {
    const response = await api.get<PagedResult<JobWithRecommendation>>('/jobs/recommended', {
      params: { page, pageSize },
    });
    return response.data;
  },

  getTopRecommendations: async (count = 10): Promise<JobWithRecommendation[]> => {
    const response = await api.get<JobWithRecommendation[]>('/jobs/top-recommendations', {
      params: { count },
    });
    return response.data;
  },

  search: async (params: {
    query?: string;
    location?: string;
    jobType?: string;
    company?: string;
    page?: number;
    pageSize?: number;
  }): Promise<PagedResult<Job>> => {
    const response = await api.get<PagedResult<Job>>('/jobs/search', { params });
    return response.data;
  },

  update: async (id: string, data: CreateJobRequest): Promise<Job> => {
    const response = await api.put<Job>(`/jobs/${id}`, data);
    return response.data;
  },

  delete: async (id: string): Promise<void> => {
    await api.delete(`/jobs/${id}`);
  },
};

// Applications API
export const applicationsApi = {
  create: async (data: {
    jobId: string;
    status?: ApplicationStatus;
    notes?: string;
    contactName?: string;
    contactEmail?: string;
    contactPhone?: string;
    resumeId?: string;
  }): Promise<Application> => {
    const response = await api.post<Application>('/applications', data);
    return response.data;
  },

  get: async (id: string): Promise<Application> => {
    const response = await api.get<Application>(`/applications/${id}`);
    return response.data;
  },

  getAll: async (): Promise<Application[]> => {
    const response = await api.get<Application[]>('/applications');
    return response.data;
  },

  getPipeline: async (): Promise<Record<ApplicationStatus, Application[]>> => {
    const response = await api.get<Record<ApplicationStatus, Application[]>>('/applications/pipeline');
    return response.data;
  },

  updateStatus: async (id: string, status: ApplicationStatus, note?: string): Promise<Application> => {
    const response = await api.put<Application>(`/applications/${id}/status`, { status, note });
    return response.data;
  },

  update: async (
    id: string,
    data: { notes?: string; contactName?: string; contactEmail?: string; contactPhone?: string }
  ): Promise<Application> => {
    const response = await api.put<Application>(`/applications/${id}`, data);
    return response.data;
  },

  addEvent: async (
    id: string,
    data: { eventType: string; description?: string; scheduledAt?: string }
  ): Promise<Application> => {
    const response = await api.post<Application>(`/applications/${id}/events`, data);
    return response.data;
  },

  delete: async (id: string): Promise<void> => {
    await api.delete(`/applications/${id}`);
  },
};

// Reminders API
export const remindersApi = {
  create: async (data: {
    applicationId?: string;
    jobId?: string;
    title: string;
    description?: string;
    dueAt: string;
    reminderType?: string;
  }): Promise<Reminder> => {
    const response = await api.post<Reminder>('/reminders', data);
    return response.data;
  },

  getAll: async (includePast = false): Promise<Reminder[]> => {
    const response = await api.get<Reminder[]>('/reminders', { params: { includePast } });
    return response.data;
  },

  getUpcoming: async (days = 7): Promise<Reminder[]> => {
    const response = await api.get<Reminder[]>('/reminders/upcoming', { params: { days } });
    return response.data;
  },

  snooze: async (id: string, snoozedUntil: string): Promise<Reminder> => {
    const response = await api.put<Reminder>(`/reminders/${id}/snooze`, { snoozedUntil });
    return response.data;
  },

  complete: async (id: string): Promise<Reminder> => {
    const response = await api.put<Reminder>(`/reminders/${id}/complete`);
    return response.data;
  },

  dismiss: async (id: string): Promise<Reminder> => {
    const response = await api.put<Reminder>(`/reminders/${id}/dismiss`);
    return response.data;
  },
};

// Admin API
export const adminApi = {
  getUsers: async (): Promise<AdminUser[]> => {
    const response = await api.get<AdminUser[]>('/admin/users');
    return response.data;
  },

  deleteUser: async (userId: string): Promise<void> => {
    await api.delete(`/admin/users/${userId}`);
  },

  impersonateUser: async (userId: string): Promise<AuthResponse> => {
    const response = await api.post<AuthResponse>(`/admin/users/${userId}/impersonate`);
    return response.data;
  },
};

/** Public careers form (multipart; do not set JSON Content-Type). */
export const careersApi = {
  submitApplication: async (formData: FormData): Promise<{ message: string }> => {
    const response = await axios.post<{ message: string }>(
      `${API_BASE_URL}/careers/applications`,
      formData,
      { withCredentials: true }
    );
    return response.data;
  },
};

export default api;




