import { CONTACT_EMAIL } from '@/constants/marketing';

export default function AboutPage() {
  return (
    <div className="mx-auto max-w-3xl px-4 sm:px-6 lg:px-8 py-12 sm:py-16">
      <h1 className="text-3xl sm:text-4xl font-bold tracking-tight mb-6">About Joby</h1>
      <div className="space-y-6 text-muted-foreground leading-relaxed">
        <p>
          Joby helps job seekers capture listings from the web, organize applications in a clear
          pipeline, and stay on top of follow-ups with reminders — so you spend less time
          administrating and more time interviewing.
        </p>
        <p>
          We focus on Bangladeshi Taka–friendly pricing and a simple workflow: paste a job URL,
          track your stage from saved to offer, and let the product nudge you when it is time to
          follow up.
        </p>
        <h2 className="text-xl font-semibold text-foreground pt-2">Mission</h2>
        <p>
          Make job search tracking accessible, honest, and affordable — with transparent pricing and
          features that match what we actually ship.
        </p>
        <h2 className="text-xl font-semibold text-foreground pt-2">Contact</h2>
        <p>
          Questions about the product or partnerships? Reach us at{' '}
          <a className="text-primary hover:underline" href={`mailto:${CONTACT_EMAIL}`}>
            {CONTACT_EMAIL}
          </a>
          .
        </p>
      </div>
    </div>
  );
}
