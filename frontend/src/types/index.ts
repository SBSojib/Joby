// Auth types
export interface User {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  isAdmin: boolean;
  defaultFollowUpDays: number;
  /** Present when a root admin is viewing the app as another user. */
  impersonatorUserId?: string;
  impersonatorEmail?: string;
}

export interface AdminUser {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  isEmailVerified: boolean;
  isRootAdmin: boolean;
  createdAt: string;
  updatedAt: string;
  lastLoginAt?: string;
}

export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
  user: User;
}

export interface RegisterPendingResponse {
  email: string;
  message: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface DeleteAccountRequest {
  password: string;
}

export interface RegisterRequest {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
}

export interface VerifyEmailRequest {
  email: string;
  code: string;
}

export interface ForgotPasswordRequest {
  email: string;
}

export interface ResetPasswordRequest {
  email: string;
  code: string;
  newPassword: string;
}

// Profile types
export interface Profile {
  id: string;
  userId: string;
  fullName?: string;
  email?: string;
  phone?: string;
  location?: string;
  summary?: string;
  currentTitle?: string;
  yearsOfExperience?: number;
  skills: string[];
  keywords: string[];
  preferredLocations?: string;
  preferredJobTypes?: string;
  minSalary?: number;
  maxSalary?: number;
  workExperience: WorkExperience[];
  education: Education[];
  activeResumeId?: string;
  createdAt: string;
  updatedAt: string;
}

export interface WorkExperience {
  title?: string;
  company?: string;
  location?: string;
  startDate?: string;
  endDate?: string;
  description?: string;
  isCurrent: boolean;
}

export interface Education {
  degree?: string;
  school?: string;
  field?: string;
  startDate?: string;
  endDate?: string;
  description?: string;
}

export interface Resume {
  id: string;
  fileName: string;
  contentType: string;
  fileSize: number;
  isParsed: boolean;
  parseError?: string;
  parsedData?: ParsedResumeData;
  createdAt: string;
}

export interface ParsedResumeData {
  name?: string;
  email?: string;
  phone?: string;
  summary?: string;
  skills: string[];
  jobTitles: string[];
  workExperience: WorkExperience[];
  education: Education[];
}

// Job types
export interface Job {
  id: string;
  title: string;
  company: string;
  location?: string;
  description?: string;
  requirements?: string;
  salary?: string;
  jobType?: string;
  sourceUrl?: string;
  /** Full saved HTML from the listing URL; only returned when loading a single job. */
  sourcePageHtml?: string;
  sourcePlatform?: string;
  postedDate?: string;
  expiryDate?: string;
  isExtracted: boolean;
  hasApplication: boolean;
  applicationId?: string;
  reminders?: Reminder[];
  createdAt: string;
  updatedAt: string;
}

export interface JobWithRecommendation extends Job {
  recommendationScore?: number;
  matchedSkills: string[];
  missingSkills: string[];
  matchedKeywords: string[];
  missingKeywords: string[];
}

export interface CreateJobRequest {
  title: string;
  company: string;
  location?: string;
  description?: string;
  requirements?: string;
  salary?: string;
  jobType?: string;
  sourceUrl?: string;
  sourcePlatform?: string;
  postedDate?: string;
  expiryDate?: string;
}

// Application types
export enum ApplicationStatus {
  Saved = 0,
  Applied = 1,
  RecruiterScreen = 2,
  TechInterview = 3,
  Onsite = 4,
  Offer = 5,
  Rejected = 6,
  Withdrawn = 7,
}

export const ApplicationStatusLabels: Record<ApplicationStatus, string> = {
  [ApplicationStatus.Saved]: 'Saved',
  [ApplicationStatus.Applied]: 'Applied',
  [ApplicationStatus.RecruiterScreen]: 'Recruiter Screen',
  [ApplicationStatus.TechInterview]: 'Tech Interview',
  [ApplicationStatus.Onsite]: 'Onsite',
  [ApplicationStatus.Offer]: 'Offer',
  [ApplicationStatus.Rejected]: 'Rejected',
  [ApplicationStatus.Withdrawn]: 'Withdrawn',
};

export interface Application {
  id: string;
  jobId: string;
  job: Job;
  status: ApplicationStatus;
  statusDisplay: string;
  appliedAt?: string;
  notes?: string;
  contactName?: string;
  contactEmail?: string;
  contactPhone?: string;
  resumeId?: string;
  events: ApplicationEvent[];
  reminders: Reminder[];
  createdAt: string;
  updatedAt: string;
}

export interface ApplicationEvent {
  id: string;
  eventType: string;
  description?: string;
  oldStatus?: ApplicationStatus;
  newStatus?: ApplicationStatus;
  scheduledAt?: string;
  createdAt: string;
}

// Reminder types
export enum ReminderStatus {
  Pending = 0,
  Snoozed = 1,
  Completed = 2,
  Dismissed = 3,
}

export interface Reminder {
  id: string;
  applicationId?: string;
  jobId?: string;
  title: string;
  description?: string;
  dueAt: string;
  status: ReminderStatus;
  statusDisplay: string;
  completedAt?: string;
  snoozedUntil?: string;
  isAutoGenerated: boolean;
  reminderType?: string;
  emailSentAt?: string;
  createdAt: string;
}

// Pagination
export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasPrevious: boolean;
  hasNext: boolean;
}

// API Error
export interface ApiError {
  message: string;
  details?: string;
  errors?: { field?: string; message: string }[];
}




