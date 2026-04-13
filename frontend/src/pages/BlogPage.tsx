import { Link } from 'react-router-dom';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';

const posts = [
  {
    slug: 'why-we-built-joby',
    title: 'Why we built Joby',
    date: 'April 2026',
    excerpt:
      'Spreadsheets and browser bookmarks break down when you are juggling dozens of roles. Here is how we think about fixing that.',
  },
  {
    slug: 'pipeline-not-spreadsheet',
    title: 'Your pipeline is not a spreadsheet',
    date: 'April 2026',
    excerpt:
      'Stages like Applied and Tech Interview encode real decisions. A dedicated tracker keeps history and reminders aligned with that reality.',
  },
];

export default function BlogPage() {
  return (
    <div className="mx-auto max-w-3xl px-4 sm:px-6 lg:px-8 py-12 sm:py-16">
      <h1 className="text-3xl sm:text-4xl font-bold tracking-tight mb-2">Blog</h1>
      <p className="text-muted-foreground mb-10">
        Product notes and ideas from the Joby team. More posts will appear here over time.
      </p>
      <div className="space-y-6">
        {posts.map((post) => (
          <Card key={post.slug} className="border-border/50 bg-card/50">
            <CardHeader>
              <p className="text-xs text-muted-foreground mb-1">{post.date}</p>
              <CardTitle className="text-xl">
                <Link
                  to={`/blog/${post.slug}`}
                  className="hover:text-primary transition-colors"
                >
                  {post.title}
                </Link>
              </CardTitle>
            </CardHeader>
            <CardContent>
              <p className="text-sm text-muted-foreground leading-relaxed">{post.excerpt}</p>
              <Link
                to={`/blog/${post.slug}`}
                className="inline-block mt-4 text-sm font-medium text-primary hover:underline"
              >
                Read more
              </Link>
            </CardContent>
          </Card>
        ))}
      </div>
    </div>
  );
}
