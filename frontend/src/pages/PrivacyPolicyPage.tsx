import { CONTACT_EMAIL } from '@/constants/marketing';

export default function PrivacyPolicyPage() {
  return (
    <div className="mx-auto max-w-3xl px-4 sm:px-6 lg:px-8 py-12 sm:py-16">
      <h1 className="text-3xl sm:text-4xl font-bold tracking-tight mb-2">Privacy Policy</h1>
      <p className="text-sm text-muted-foreground mb-10">Last updated: April 2026</p>

      <div className="space-y-8 text-muted-foreground leading-relaxed">
        <section>
          <h2 className="text-lg font-semibold text-foreground mb-3">Overview</h2>
          <p>
            Joby (&quot;we&quot;, &quot;us&quot;) respects your privacy. This policy describes how we
            handle information when you use our website and application. It is a general summary and
            does not replace legal advice tailored to your situation.
          </p>
        </section>
        <section>
          <h2 className="text-lg font-semibold text-foreground mb-3">Information we process</h2>
          <p>
            When you create an account, we process the details you provide (such as name and email)
            and data you add in the product (jobs, applications, reminders, profile and resume
            content where applicable). We use this to run the service you signed up for.
          </p>
        </section>
        <section>
          <h2 className="text-lg font-semibold text-foreground mb-3">How we use data</h2>
          <p>
            We use account and usage data to authenticate you, provide features, send operational
            emails (such as reminders you configure), improve reliability, and comply with law. We do
            not sell your personal data.
          </p>
        </section>
        <section>
          <h2 className="text-lg font-semibold text-foreground mb-3">Security</h2>
          <p>
            We apply reasonable technical and organizational measures to protect your information.
            No method of transmission or storage is completely secure; we encourage strong
            passwords and careful sharing of access to your account.
          </p>
        </section>
        <section>
          <h2 className="text-lg font-semibold text-foreground mb-3">Retention</h2>
          <p>
            We keep your information while your account is active and as needed to provide the
            service, resolve disputes, and meet legal obligations. You may request deletion of your
            account subject to any legal requirements we must satisfy.
          </p>
        </section>
        <section>
          <h2 className="text-lg font-semibold text-foreground mb-3">Contact</h2>
          <p>
            For privacy questions, contact{' '}
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
