import { useState } from 'react';
import { Link, useLocation } from 'react-router-dom';
import { Button } from '@/components/ui/button';
import { Briefcase, ArrowRight, Menu, X } from 'lucide-react';
import { cn } from '@/lib/utils';

function scrollToId(id: string) {
  document.getElementById(id)?.scrollIntoView({ behavior: 'smooth' });
}

function SectionLink({
  id,
  label,
  isHome,
  onCloseMobile,
  className,
}: {
  id: string;
  label: string;
  isHome: boolean;
  onCloseMobile?: () => void;
  className?: string;
}) {
  const base = cn(
    'text-sm text-muted-foreground hover:text-foreground transition-colors',
    className
  );
  if (isHome) {
    return (
      <button
        type="button"
        className={cn(base, 'text-left w-full md:w-auto')}
        onClick={() => {
          onCloseMobile?.();
          scrollToId(id);
        }}
      >
        {label}
      </button>
    );
  }
  return (
    <Link
      to={{ pathname: '/', hash: id }}
      className={base}
      onClick={() => onCloseMobile?.()}
    >
      {label}
    </Link>
  );
}

export default function MarketingNav() {
  const location = useLocation();
  const [mobileMenuOpen, setMobileMenuOpen] = useState(false);
  const isHome = location.pathname === '/';

  return (
    <nav className="fixed top-0 left-0 right-0 z-50 border-b border-border/50 bg-background/85 backdrop-blur-xl shadow-sm shadow-black/5">
      <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
        <div className="flex h-16 items-center justify-between">
          <Link to="/" className="flex items-center gap-2.5">
            <div className="h-9 w-9 rounded-xl bg-primary flex items-center justify-center">
              <Briefcase className="h-5 w-5 text-primary-foreground" />
            </div>
            <span className="text-xl font-bold">Joby</span>
          </Link>

          <div className="hidden md:flex items-center gap-8">
            <SectionLink id="features" label="Features" isHome={isHome} />
            <SectionLink id="how-it-works" label="How It Works" isHome={isHome} />
            <SectionLink id="pricing" label="Pricing" isHome={isHome} />
            <SectionLink id="faq" label="FAQ" isHome={isHome} />
          </div>

          <div className="hidden md:flex items-center gap-3">
            <Button variant="ghost" asChild>
              <Link to="/login">Sign in</Link>
            </Button>
            <Button asChild>
              <Link to="/register">
                Get Started Free
                <ArrowRight className="ml-2 h-4 w-4" />
              </Link>
            </Button>
          </div>

          <button
            type="button"
            className="md:hidden p-2 hover:bg-accent rounded-lg"
            onClick={() => setMobileMenuOpen(!mobileMenuOpen)}
            aria-expanded={mobileMenuOpen}
            aria-label={mobileMenuOpen ? 'Close menu' : 'Open menu'}
          >
            {mobileMenuOpen ? <X className="h-5 w-5" /> : <Menu className="h-5 w-5" />}
          </button>
        </div>
      </div>

      {mobileMenuOpen && (
        <div className="md:hidden border-t border-border/50 bg-background/95 backdrop-blur-xl">
          <div className="px-4 py-4 space-y-3">
            <SectionLink
              id="features"
              label="Features"
              isHome={isHome}
              onCloseMobile={() => setMobileMenuOpen(false)}
            />
            <SectionLink
              id="how-it-works"
              label="How It Works"
              isHome={isHome}
              onCloseMobile={() => setMobileMenuOpen(false)}
            />
            <SectionLink
              id="pricing"
              label="Pricing"
              isHome={isHome}
              onCloseMobile={() => setMobileMenuOpen(false)}
            />
            <SectionLink
              id="faq"
              label="FAQ"
              isHome={isHome}
              onCloseMobile={() => setMobileMenuOpen(false)}
            />
            <div className="pt-3 border-t border-border/50 flex flex-col gap-2">
              <Button variant="ghost" asChild className="w-full">
                <Link to="/login" onClick={() => setMobileMenuOpen(false)}>
                  Sign in
                </Link>
              </Button>
              <Button asChild className="w-full">
                <Link to="/register" onClick={() => setMobileMenuOpen(false)}>
                  Get Started Free
                </Link>
              </Button>
            </div>
          </div>
        </div>
      )}
    </nav>
  );
}
