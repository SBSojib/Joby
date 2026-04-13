import { CONTACT_EMAIL } from '@/constants/marketing';

export default function TermsOfServicePage() {
  return (
    <div className="mx-auto max-w-3xl px-4 sm:px-6 lg:px-8 py-12 sm:py-16">
      <h1 className="text-3xl sm:text-4xl font-bold tracking-tight mb-2">Terms of Service</h1>
      <p className="text-sm text-muted-foreground mb-10">Last updated: April 2026</p>

      <div className="space-y-8 text-muted-foreground leading-relaxed">
        <section>
          <h2 className="text-lg font-semibold text-foreground mb-3">Agreement</h2>
          <p>
            By accessing or using Joby, you agree to these terms. If you do not agree, do not use the
            service. We may update these terms from time to time; continued use after changes means
            you accept the updated terms.
          </p>
        </section>
        <section>
          <h2 className="text-lg font-semibold text-foreground mb-3">The service</h2>
          <p>
            Joby provides tools to track job listings and applications. Features described on the
            marketing site as &quot;coming soon&quot; are not guaranteed by a date. We may modify,
            suspend, or discontinue parts of the service with reasonable notice where practicable.
          </p>
        </section>
        <section>
          <h2 className="text-lg font-semibold text-foreground mb-3">Accounts</h2>
          <p>
            You are responsible for your account credentials and for activity under your account. You
            must provide accurate information and not misuse the service (including scraping our
            systems, attempting unauthorized access, or interfering with other users).
          </p>
        </section>
        <section>
          <h2 className="text-lg font-semibold text-foreground mb-3">Subscriptions and trials</h2>
          <p>
            Trial and subscription terms (including pricing in BDT) are shown on the site at
            checkout or on the pricing page. Payment processing may be provided by third parties;
            their terms may also apply.
          </p>
        </section>
        <section>
          <h2 className="text-lg font-semibold text-foreground mb-3">Disclaimer</h2>
          <p>
            The service is provided &quot;as is&quot; without warranties of any kind, to the extent
            permitted by law. We are not responsible for hiring decisions by employers or the
            accuracy of third-party job listings you choose to save.
          </p>
        </section>
        <section>
          <h2 className="text-lg font-semibold text-foreground mb-3">Limitation of liability</h2>
          <p>
            To the maximum extent permitted by applicable law, Joby and its operators will not be
            liable for indirect, incidental, or consequential damages arising from your use of the
            service.
          </p>
        </section>
        <section>
          <h2 className="text-lg font-semibold text-foreground mb-3">Contact</h2>
          <p>
            Questions about these terms:{' '}
            <a className="text-primary hover:underline" href={`mailto:${CONTACT_EMAIL}`}>
              {CONTACT_EMAIL}
            </a>
            .
          </p>
        </section>
      </div>
    </div>
  );
}
