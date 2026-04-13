import { Link, useParams } from 'react-router-dom';

const articles: Record<
  string,
  { title: string; date: string; body: string[] }
> = {
  'why-we-built-joby': {
    title: 'Why we built Joby',
    date: 'April 2026',
    body: [
      'Job searching rarely fails because people forget to apply — it fails because they lose track of where each opportunity stands, when to follow up, and what they said last time.',
      'We built Joby to give that work a proper home: scrape a listing from a URL, move it through a pipeline that matches how hiring actually works, and lean on reminders so nothing slips.',
      'We are keeping pricing in Bangladeshi Taka and being upfront about what is live versus coming soon, because trust matters for a tool you might use for months during a search.',
    ],
  },
  'pipeline-not-spreadsheet': {
    title: 'Your pipeline is not a spreadsheet',
    date: 'April 2026',
    body: [
      'A row in a spreadsheet does not capture a conversation with a recruiter, a technical screen that moved, or an offer deadline. Those events deserve a timeline, not a single cell.',
      'Joby treats each application as a first-class object with status, notes, and reminders. That structure makes it easier to report on your search and to act at the right moment.',
      'If you are still copying job text by hand, try pasting the listing URL instead — we would rather automate the boring parts so you can focus on preparation and interviews.',
    ],
  },
};

export default function BlogPostPage() {
  const { slug } = useParams<{ slug: string }>();
  const post = slug ? articles[slug] : undefined;

  if (!post) {
    return (
      <div className="mx-auto max-w-3xl px-4 sm:px-6 lg:px-8 py-12 sm:py-16">
        <h1 className="text-2xl font-bold mb-4">Post not found</h1>
        <Link to="/blog" className="text-primary hover:underline">
          Back to blog
        </Link>
      </div>
    );
  }

  return (
    <article className="mx-auto max-w-3xl px-4 sm:px-6 lg:px-8 py-12 sm:py-16">
      <Link to="/blog" className="text-sm text-muted-foreground hover:text-foreground mb-6 inline-block">
        ← Blog
      </Link>
      <p className="text-xs text-muted-foreground mb-2">{post.date}</p>
      <h1 className="text-3xl sm:text-4xl font-bold tracking-tight mb-8">{post.title}</h1>
      <div className="space-y-6 text-muted-foreground leading-relaxed">
        {post.body.map((p, i) => (
          <p key={i}>{p}</p>
        ))}
      </div>
    </article>
  );
}
