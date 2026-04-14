import { Routes, Route, Navigate } from 'react-router-dom';
import { useAuth } from '@/contexts/AuthContext';
import Layout from '@/components/Layout';
import MarketingLayout from '@/components/MarketingLayout';
import LandingPage from '@/pages/LandingPage';
import LoginPage from '@/pages/LoginPage';
import RegisterPage from '@/pages/RegisterPage';
import ForgotPasswordPage from '@/pages/ForgotPasswordPage';
import AboutPage from '@/pages/AboutPage';
import BlogPage from '@/pages/BlogPage';
import BlogPostPage from '@/pages/BlogPostPage';
import CareersPage from '@/pages/CareersPage';
import PrivacyPolicyPage from '@/pages/PrivacyPolicyPage';
import TermsOfServicePage from '@/pages/TermsOfServicePage';
import DashboardPage from '@/pages/DashboardPage';
import ProfilePage from '@/pages/ProfilePage';
import JobsPage from '@/pages/JobsPage';
import NewJobPage from '@/pages/NewJobPage';
import JobDetailPage from '@/pages/JobDetailPage';
import ApplicationsPage from '@/pages/ApplicationsPage';
import ApplicationDetailPage from '@/pages/ApplicationDetailPage';
import RemindersPage from '@/pages/RemindersPage';
import AdminPage from '@/pages/AdminPage';

function LoadingSpinner() {
  return (
    <div className="flex h-screen items-center justify-center">
      <div className="animate-spin rounded-full h-8 w-8 border-t-2 border-primary"></div>
    </div>
  );
}

function RootRoute() {
  const { isAuthenticated, isLoading } = useAuth();
  if (isLoading) return <LoadingSpinner />;
  if (isAuthenticated) return <Layout />;
  return <LandingPage />;
}

function PublicRoute({ children }: { children: React.ReactNode }) {
  const { isAuthenticated, isLoading } = useAuth();
  if (isLoading) return <LoadingSpinner />;
  if (isAuthenticated) return <Navigate to="/" replace />;
  return <>{children}</>;
}

function App() {
  return (
    <Routes>
      <Route
        path="/login"
        element={
          <PublicRoute>
            <LoginPage />
          </PublicRoute>
        }
      />
      <Route
        path="/forgot-password"
        element={
          <PublicRoute>
            <ForgotPasswordPage />
          </PublicRoute>
        }
      />
      <Route
        path="/register"
        element={
          <PublicRoute>
            <RegisterPage />
          </PublicRoute>
        }
      />
      <Route element={<MarketingLayout />}>
        <Route path="/about" element={<AboutPage />} />
        <Route path="/blog" element={<BlogPage />} />
        <Route path="/blog/:slug" element={<BlogPostPage />} />
        <Route path="/careers" element={<CareersPage />} />
        <Route path="/privacy" element={<PrivacyPolicyPage />} />
        <Route path="/terms" element={<TermsOfServicePage />} />
      </Route>
      <Route path="/" element={<RootRoute />}>
        <Route index element={<DashboardPage />} />
        <Route path="profile" element={<ProfilePage />} />
        <Route path="jobs" element={<JobsPage />} />
        <Route path="jobs/new" element={<NewJobPage />} />
        <Route path="jobs/:jobId" element={<JobDetailPage />} />
        <Route path="applications" element={<ApplicationsPage />} />
        <Route path="applications/:applicationId" element={<ApplicationDetailPage />} />
        <Route path="reminders" element={<RemindersPage />} />
        <Route path="admin" element={<AdminPage />} />
      </Route>
    </Routes>
  );
}

export default App;




