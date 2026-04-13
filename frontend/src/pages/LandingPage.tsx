import { useEffect } from 'react';
import { Link, useLocation } from 'react-router-dom';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import MarketingNav from '@/components/MarketingNav';
import MarketingFooter from '@/components/MarketingFooter';
import {
  Search,
  BarChart3,
  Bell,
  FileText,
  Sparkles,
  CheckCircle2,
  ArrowRight,
  ChevronDown,
  Star,
} from 'lucide-react';
import { useState } from 'react';
import { cn } from '@/lib/utils';

type FeatureItem = {
  icon: typeof Search;
  title: string;
  description: string;
  upcoming?: boolean;
};

const features: FeatureItem[] = [
  {
    icon: Search,
    title: 'Smart Job Scraping',
    description:
      'Paste any job listing URL and Joby instantly extracts title, company, requirements, salary, and more — no manual entry needed.',
  },
  {
    icon: BarChart3,
    title: 'Application Pipeline',
    description:
      'Track every application through stages: Saved, Applied, Recruiter Screen, Tech Interview, Onsite, Offer — all in one visual pipeline.',
  },
  {
    icon: Bell,
    title: 'Smart Reminders',
    description:
      'Never miss a follow-up. Joby auto-generates reminders based on your application timeline and sends email notifications.',
  },
  {
    icon: FileText,
    title: 'Resume Management',
    description:
      'Upload, store, and parse resumes in one place — coming soon to help you prep applications faster.',
    upcoming: true,
  },
  {
    icon: Sparkles,
    title: 'Job Recommendations',
    description:
      'Personalized match scores and gap insights based on your profile — on the roadmap after resume management.',
    upcoming: true,
  },
  {
    icon: BarChart3,
    title: 'Dashboard Analytics',
    description:
      'Visualize your job search with real-time stats: total applications, response rates, pipeline distribution, and upcoming deadlines.',
  },
];

const steps = [
  {
    step: '01',
    title: 'Add Job Listings',
    description:
      'Paste a URL from LinkedIn, Indeed, or any job board. Joby scrapes and organizes the details automatically.',
  },
  {
    step: '02',
    title: 'Track Applications',
    description:
      'Move jobs through your pipeline as you apply. Log notes, contacts, and events at every stage.',
  },
  {
    step: '03',
    title: 'Stay on Top',
    description:
      'Get smart reminders for follow-ups, deadlines, and interviews. Never let an opportunity slip through.',
  },
];

const faqs = [
  {
    q: 'How does the free trial work?',
    a: 'Every new account gets a full 30-day free trial with access to everything that is live today (scraping, pipeline, reminders, dashboard, and more). Resume management and job recommendations are marked as upcoming on the site. No credit card required. After your trial ends, you can subscribe for just ৳1.99/month to continue.',
  },
  {
    q: 'What job boards does Joby support?',
    a: 'Joby can scrape job listings from LinkedIn, Indeed, Glassdoor, and most major job boards. Just paste the URL and we handle the rest.',
  },
  {
    q: 'Can I cancel my subscription anytime?',
    a: 'Absolutely. You can cancel your subscription at any time from your profile settings. There are no long-term contracts or hidden fees.',
  },
  {
    q: 'Is my data secure?',
    a: 'Yes. All data is encrypted in transit and at rest. We never share your personal information or application data with third parties.',
  },
  {
    q: 'How do smart reminders work?',
    a: 'When you track an application, Joby automatically schedules follow-up reminders based on best practices. You also get email notifications so you never miss a deadline.',
  },
  {
    q: 'Can I export my data?',
    a: 'Yes. You own your data. You can export all your jobs, applications, and notes at any time from your dashboard.',
  },
];

const featuredReview = {
  name: 'MD Hasibul Islam',
  role: 'Software Engineer',
  quote:
    'I use Joby to keep listings, pipeline stages, and follow-ups in one place instead of scattered notes. It is the workflow I wanted for my own job search.',
  rating: 5,
};

function FAQItem({ q, a }: { q: string; a: string }) {
  const [open, setOpen] = useState(false);
  return (
    <div className="border border-border rounded-xl overflow-hidden">
      <button
        onClick={() => setOpen(!open)}
        className="flex w-full items-center justify-between px-6 py-5 text-left hover:bg-card/50 transition-colors"
      >
        <span className="text-base font-medium pr-4">{q}</span>
        <ChevronDown
          className={cn(
            'h-5 w-5 shrink-0 text-muted-foreground transition-transform duration-200',
            open && 'rotate-180'
          )}
        />
      </button>
      <div
        className={cn(
          'grid transition-all duration-200 ease-in-out',
          open ? 'grid-rows-[1fr] opacity-100' : 'grid-rows-[0fr] opacity-0'
        )}
      >
        <div className="overflow-hidden">
          <p className="px-6 pb-5 text-muted-foreground leading-relaxed">{a}</p>
        </div>
      </div>
    </div>
  );
}

export default function LandingPage() {
  const location = useLocation();

  useEffect(() => {
    const raw = location.hash.replace(/^#/, '');
    if (!raw) return;
    const t = window.setTimeout(() => {
      document.getElementById(raw)?.scrollIntoView({ behavior: 'smooth' });
    }, 80);
    return () => clearTimeout(t);
  }, [location.hash, location.pathname]);

  const scrollTo = (id: string) => {
    document.getElementById(id)?.scrollIntoView({ behavior: 'smooth' });
  };

  return (
    <div className="min-h-screen bg-background text-foreground overflow-x-hidden flex flex-col">
      <MarketingNav />
      <div className="h-16 shrink-0" aria-hidden />

      <main className="flex-1">
      {/* ─── Hero ─── */}
      <section className="relative overflow-hidden">
        {/* Background decoration */}
        <div className="absolute inset-0 -z-10">
          <div className="absolute top-0 left-1/2 -translate-x-1/2 w-[800px] h-[600px] bg-primary/10 rounded-full blur-[120px]" />
          <div className="absolute top-40 right-0 w-[400px] h-[400px] bg-primary/5 rounded-full blur-[80px]" />
        </div>

        <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8 pt-20 pb-24 sm:pt-28 sm:pb-32">
          <div className="mx-auto max-w-3xl text-center">
            <div className="mb-6 inline-flex items-center gap-2 rounded-full border border-primary/30 bg-primary/10 px-4 py-1.5 text-sm text-primary">
              <Sparkles className="h-4 w-4" />
              <span>30-day free trial — no credit card required</span>
            </div>

            <h1 className="text-4xl sm:text-5xl lg:text-6xl font-bold tracking-tight leading-[1.1]">
              Your Job Search,{' '}
              <span className="bg-gradient-to-r from-primary to-purple-400 bg-clip-text text-transparent">
                Finally Organized
              </span>
            </h1>

            <p className="mt-6 text-lg sm:text-xl text-muted-foreground leading-relaxed max-w-2xl mx-auto">
              Stop juggling spreadsheets and sticky notes. Joby scrapes job listings, tracks
              every application, sends smart reminders, and helps you land your next role faster.
            </p>

            <div className="mt-10 flex flex-col sm:flex-row items-center justify-center gap-4">
              <Button size="lg" asChild className="text-base px-8 h-12">
                <Link to="/register">
                  Start Free Trial
                  <ArrowRight className="ml-2 h-5 w-5" />
                </Link>
              </Button>
              <Button size="lg" variant="outline" className="text-base px-8 h-12" onClick={() => scrollTo('features')}>
                See How It Works
              </Button>
            </div>

            <p className="mt-4 text-sm text-muted-foreground">
              Free for 30 days, then just ৳1.99/month
            </p>
          </div>

          {/* Hero visual — dashboard mockup */}
          <div className="mt-16 sm:mt-20 mx-auto max-w-5xl">
            <div className="rounded-2xl border border-border/60 bg-card/50 backdrop-blur-sm p-2 shadow-2xl shadow-primary/5">
              <div className="rounded-xl border border-border/40 bg-card overflow-hidden">
                {/* Mock browser bar */}
                <div className="flex items-center gap-2 px-4 py-3 border-b border-border/40 bg-secondary/30">
                  <div className="flex gap-1.5">
                    <div className="w-3 h-3 rounded-full bg-red-500/60" />
                    <div className="w-3 h-3 rounded-full bg-yellow-500/60" />
                    <div className="w-3 h-3 rounded-full bg-green-500/60" />
                  </div>
                  <div className="flex-1 mx-4">
                    <div className="h-6 rounded-md bg-secondary/60 max-w-md mx-auto flex items-center justify-center">
                      <span className="text-xs text-muted-foreground/60">app.joby.com/dashboard</span>
                    </div>
                  </div>
                </div>
                {/* Mock dashboard — illustrative only; numbers are not live metrics */}
                <div className="p-6 sm:p-8">
                  <p className="text-xs text-muted-foreground mb-4 text-center">
                    Illustrative preview — your dashboard shows your own jobs and stages after you sign in.
                  </p>
                  <div className="grid grid-cols-2 sm:grid-cols-4 gap-4 mb-6">
                    {[
                      { label: 'Total Jobs' },
                      { label: 'Applied' },
                      { label: 'Interviews' },
                      { label: 'Offers' },
                    ].map((stat) => (
                      <div key={stat.label} className="rounded-xl bg-secondary/40 p-4">
                        <p className="text-xs text-muted-foreground">{stat.label}</p>
                        <p className="text-2xl font-bold mt-1 text-muted-foreground/50">—</p>
                      </div>
                    ))}
                  </div>
                  <div className="space-y-3">
                    {[1, 2, 3].map((i) => (
                      <div
                        key={i}
                        className="flex items-center justify-between rounded-lg bg-secondary/30 px-4 py-3"
                      >
                        <div className="flex-1 space-y-2 pr-4">
                          <div className="h-3.5 w-3/5 max-w-[200px] rounded bg-muted-foreground/15" />
                          <div className="h-3 w-2/5 max-w-[120px] rounded bg-muted-foreground/10" />
                        </div>
                        <div className="h-6 w-20 shrink-0 rounded-full bg-muted-foreground/15" />
                      </div>
                    ))}
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* ─── Features ─── */}
      <section id="features" className="scroll-mt-24 py-20 sm:py-28">
        <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
          <div className="mx-auto max-w-2xl text-center mb-16">
            <p className="text-sm font-medium text-primary mb-3">FEATURES</p>
            <h2 className="text-3xl sm:text-4xl font-bold tracking-tight">
              Everything You Need to Land Your Next Role
            </h2>
            <p className="mt-4 text-lg text-muted-foreground">
              From scraping job listings to tracking interviews — Joby handles the busywork so
              you can focus on what matters.
            </p>
          </div>

          <div className="grid sm:grid-cols-2 lg:grid-cols-3 gap-6">
            {features.map((feature) => (
              <Card
                key={feature.title}
                className={cn(
                  'group border-border/50 transition-all duration-300',
                  feature.upcoming
                    ? 'bg-card/30 opacity-95 hover:bg-card/40 border-dashed'
                    : 'bg-card/50 hover:bg-card hover:border-primary/30'
                )}
              >
                <CardHeader>
                  <div className="flex flex-wrap items-start justify-between gap-2 mb-2">
                    <div
                      className={cn(
                        'h-12 w-12 rounded-xl flex items-center justify-center',
                        feature.upcoming
                          ? 'bg-muted text-muted-foreground'
                          : 'bg-primary/10 group-hover:bg-primary/20 text-primary'
                      )}
                    >
                      <feature.icon className="h-6 w-6" />
                    </div>
                    {feature.upcoming && (
                      <Badge variant="warning" className="shrink-0">
                        Coming soon
                      </Badge>
                    )}
                  </div>
                  <CardTitle className="text-lg">{feature.title}</CardTitle>
                </CardHeader>
                <CardContent>
                  <p className="text-muted-foreground leading-relaxed">{feature.description}</p>
                </CardContent>
              </Card>
            ))}
          </div>
        </div>
      </section>

      {/* ─── How It Works ─── */}
      <section id="how-it-works" className="scroll-mt-24 py-20 sm:py-28 bg-card/30 border-y border-border/50">
        <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
          <div className="mx-auto max-w-2xl text-center mb-16">
            <p className="text-sm font-medium text-primary mb-3">HOW IT WORKS</p>
            <h2 className="text-3xl sm:text-4xl font-bold tracking-tight">
              Up and Running in Minutes
            </h2>
            <p className="mt-4 text-lg text-muted-foreground">
              Three simple steps to take control of your job search.
            </p>
          </div>

          <div className="grid md:grid-cols-3 gap-8 max-w-4xl mx-auto">
            {steps.map((item, i) => (
              <div key={item.step} className="relative text-center">
                {i < steps.length - 1 && (
                  <div className="hidden md:block absolute top-12 left-[60%] w-[80%] border-t-2 border-dashed border-border/60" />
                )}
                <div className="inline-flex h-20 w-20 items-center justify-center rounded-2xl bg-primary/10 border border-primary/20 mb-6">
                  <span className="text-2xl font-bold text-primary">{item.step}</span>
                </div>
                <h3 className="text-lg font-semibold mb-2">{item.title}</h3>
                <p className="text-muted-foreground leading-relaxed">{item.description}</p>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* ─── Testimonial ─── */}
      <section className="py-20 sm:py-28">
        <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
          <div className="mx-auto max-w-2xl text-center mb-12">
            <p className="text-sm font-medium text-primary mb-3">TESTIMONIAL</p>
            <h2 className="text-3xl sm:text-4xl font-bold tracking-tight">{featuredReview.name}</h2>
            <p className="mt-2 text-lg text-muted-foreground">{featuredReview.role}</p>
          </div>

          <div className="max-w-xl mx-auto">
            <Card className="border-border/50 bg-card/50">
              <CardContent className="pt-6">
                <div className="flex gap-0.5 mb-4">
                  {Array.from({ length: featuredReview.rating }).map((_, i) => (
                    <Star key={i} className="h-4 w-4 fill-amber-400 text-amber-400" />
                  ))}
                </div>
                <p className="text-muted-foreground leading-relaxed">
                  &ldquo;{featuredReview.quote}&rdquo;
                </p>
              </CardContent>
            </Card>
          </div>
        </div>
      </section>

      {/* ─── Pricing ─── */}
      <section id="pricing" className="scroll-mt-24 py-20 sm:py-28 bg-card/30 border-y border-border/50">
        <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
          <div className="mx-auto max-w-2xl text-center mb-16">
            <p className="text-sm font-medium text-primary mb-3">PRICING</p>
            <h2 className="text-3xl sm:text-4xl font-bold tracking-tight">
              Simple, Transparent Pricing
            </h2>
            <p className="mt-4 text-lg text-muted-foreground">
              Start free. Upgrade when you're ready. No hidden charges, ever.
            </p>
          </div>

          <div className="grid md:grid-cols-2 gap-8 max-w-3xl mx-auto">
            {/* Free Trial */}
            <Card className="border-border/50 bg-card/50 relative">
              <CardHeader className="pb-4">
                <div className="mb-2">
                  <span className="text-sm font-medium text-muted-foreground">Free Trial</span>
                </div>
                <div className="flex items-baseline gap-1">
                  <span className="text-5xl font-bold">৳0</span>
                  <span className="text-muted-foreground">/month</span>
                </div>
                <p className="text-sm text-muted-foreground mt-2">
                  Full access for your first 30 days
                </p>
              </CardHeader>
              <CardContent className="space-y-4">
                <ul className="space-y-3">
                  {[
                    'Everything available today (scraping, pipeline, reminders, dashboard)',
                    'Unlimited job tracking',
                    'Smart reminders & emails',
                    'Dashboard analytics',
                  ].map((item) => (
                    <li key={item} className="flex items-start gap-3 text-sm">
                      <CheckCircle2 className="h-5 w-5 text-green-400 shrink-0 mt-0.5" />
                      <span>{item}</span>
                    </li>
                  ))}
                  <li className="flex items-start gap-3 text-sm text-muted-foreground">
                    <span className="mt-0.5 shrink-0 w-5 text-center text-xs font-medium text-amber-400/90">
                      Soon
                    </span>
                    <span>Resume management & job recommendations (on the roadmap)</span>
                  </li>
                </ul>
                <Button variant="outline" asChild className="w-full mt-4">
                  <Link to="/register">Start Free Trial</Link>
                </Button>
              </CardContent>
            </Card>

            {/* Pro plan */}
            <Card className="border-primary/50 bg-card relative overflow-hidden">
              <div className="absolute top-0 right-0 bg-primary text-primary-foreground text-xs font-medium px-3 py-1 rounded-bl-lg">
                POPULAR
              </div>
              <CardHeader className="pb-4">
                <div className="mb-2">
                  <span className="text-sm font-medium text-primary">Pro</span>
                </div>
                <div className="flex items-baseline gap-1">
                  <span className="text-5xl font-bold">৳1.99</span>
                  <span className="text-muted-foreground">/month</span>
                </div>
                <p className="text-sm text-muted-foreground mt-2">
                  After your free trial ends
                </p>
              </CardHeader>
              <CardContent className="space-y-4">
                <ul className="space-y-3">
                  {[
                    'Everything in the free trial tier',
                    'Unlimited job tracking',
                    'Priority email reminders',
                    'Export your data anytime',
                    'Priority support',
                  ].map((item) => (
                    <li key={item} className="flex items-start gap-3 text-sm">
                      <CheckCircle2 className="h-5 w-5 text-primary shrink-0 mt-0.5" />
                      <span>{item}</span>
                    </li>
                  ))}
                  <li className="flex items-start gap-3 text-sm text-muted-foreground">
                    <span className="mt-0.5 shrink-0 w-5 text-center text-xs font-medium text-amber-400/90">
                      Soon
                    </span>
                    <span>Resume management & job recommendations when they ship</span>
                  </li>
                </ul>
                <Button asChild className="w-full mt-4">
                  <Link to="/register">
                    Get Started
                    <ArrowRight className="ml-2 h-4 w-4" />
                  </Link>
                </Button>
              </CardContent>
            </Card>
          </div>

          <p className="text-center text-sm text-muted-foreground mt-8">
            All prices in BDT (Bangladeshi Taka). Cancel anytime — no questions asked.
          </p>
        </div>
      </section>

      {/* ─── FAQ ─── */}
      <section id="faq" className="scroll-mt-24 py-20 sm:py-28">
        <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
          <div className="mx-auto max-w-2xl text-center mb-16">
            <p className="text-sm font-medium text-primary mb-3">FAQ</p>
            <h2 className="text-3xl sm:text-4xl font-bold tracking-tight">
              Frequently Asked Questions
            </h2>
            <p className="mt-4 text-lg text-muted-foreground">
              Got questions? We've got answers.
            </p>
          </div>

          <div className="mx-auto max-w-2xl space-y-3">
            {faqs.map((faq) => (
              <FAQItem key={faq.q} q={faq.q} a={faq.a} />
            ))}
          </div>
        </div>
      </section>

      {/* ─── CTA ─── */}
      <section className="py-20 sm:py-28 relative overflow-hidden">
        <div className="absolute inset-0 -z-10">
          <div className="absolute inset-0 bg-gradient-to-r from-primary/10 via-primary/5 to-transparent" />
          <div className="absolute bottom-0 left-1/2 -translate-x-1/2 w-[600px] h-[400px] bg-primary/10 rounded-full blur-[100px]" />
        </div>

        <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
          <div className="mx-auto max-w-2xl text-center">
            <h2 className="text-3xl sm:text-4xl font-bold tracking-tight">
              Ready to Take Control of Your Job Search?
            </h2>
            <p className="mt-4 text-lg text-muted-foreground">
              Start your free trial today — no credit card needed. You will see your own jobs and
              pipeline in the app as soon as you add listings.
            </p>
            <div className="mt-8 flex flex-col sm:flex-row items-center justify-center gap-4">
              <Button size="lg" asChild className="text-base px-8 h-12">
                <Link to="/register">
                  Start Your Free Trial
                  <ArrowRight className="ml-2 h-5 w-5" />
                </Link>
              </Button>
              <Button size="lg" variant="outline" asChild className="text-base px-8 h-12">
                <Link to="/login">Sign in to Your Account</Link>
              </Button>
            </div>
          </div>
        </div>
      </section>
      </main>

      <MarketingFooter />
    </div>
  );
}
