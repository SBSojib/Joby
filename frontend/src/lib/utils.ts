import { type ClassValue, clsx } from 'clsx';
import { twMerge } from 'tailwind-merge';

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs));
}

export function formatDate(date: string | Date): string {
  return new Date(date).toLocaleDateString('en-US', {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
  });
}

export function formatDateTime(date: string | Date): string {
  return new Date(date).toLocaleDateString('en-US', {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
    hour: 'numeric',
    minute: '2-digit',
  });
}

export function formatRelativeTime(date: string | Date): string {
  const now = new Date();
  const then = new Date(date);
  const diffMs = now.getTime() - then.getTime();
  const diffSecs = Math.floor(diffMs / 1000);
  const diffMins = Math.floor(diffSecs / 60);
  const diffHours = Math.floor(diffMins / 60);
  const diffDays = Math.floor(diffHours / 24);

  if (diffDays > 7) {
    return formatDate(date);
  }
  if (diffDays > 0) {
    return `${diffDays}d ago`;
  }
  if (diffHours > 0) {
    return `${diffHours}h ago`;
  }
  if (diffMins > 0) {
    return `${diffMins}m ago`;
  }
  return 'Just now';
}

export function formatFileSize(bytes: number): string {
  if (bytes === 0) return '0 Bytes';
  const k = 1024;
  const sizes = ['Bytes', 'KB', 'MB', 'GB'];
  const i = Math.floor(Math.log(bytes) / Math.log(k));
  return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
}

export function getStatusColor(status: number): string {
  const colors: Record<number, string> = {
    0: 'status-saved',
    1: 'status-applied',
    2: 'status-recruiter-screen',
    3: 'status-tech-interview',
    4: 'status-onsite',
    5: 'status-offer',
    6: 'status-rejected',
    7: 'status-withdrawn',
  };
  return colors[status] || 'status-saved';
}

export function truncate(str: string, length: number): string {
  if (str.length <= length) return str;
  return str.slice(0, length) + '...';
}

/** Base URL (directory of the listing page) for resolving relative links in saved HTML. */
export function sourceListingBaseHref(sourceUrl: string | undefined): string {
  if (!sourceUrl) return '';
  try {
    const u = new URL(sourceUrl);
    const path = u.pathname.endsWith('/') ? u.pathname : u.pathname.replace(/\/[^/]*$/, '/') || '/';
    return `${u.origin}${path}`;
  } catch {
    return '';
  }
}

export function escapeHtmlAttribute(value: string): string {
  return value
    .replace(/&/g, '&amp;')
    .replace(/"/g, '&quot;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/'/g, '&#39;');
}

/**
 * Decodes JSON-style \\uXXXX sequences and common escapes. Some listings store JSON-LD description
 * text without proper JSON parsing, leaving literal \\u003c instead of "<".
 */
export function decodeJsonLikeString(input: string): string {
  let out = input;
  let prev = '';
  while (out !== prev) {
    prev = out;
    out = out.replace(/\\u([0-9a-fA-F]{4})/g, (_, hex: string) =>
      String.fromCharCode(parseInt(hex, 16))
    );
  }
  return out
    .replace(/\\n/g, '\n')
    .replace(/\\r/g, '\r')
    .replace(/\\t/g, '\t')
    .replace(/\\\//g, '/')
    .replace(/\\"/g, '"');
}

/** If a stored description is actually HTML (often after JSON unicode decode), return it for sandboxed rendering. */
export function listingBodyFromStoredDescription(description: string | undefined | null): string | null {
  if (!description?.trim()) return null;
  const once = decodeJsonLikeString(description);
  const twice = decodeJsonLikeString(once);
  const candidate = once === twice ? once : twice;
  const trimmed = candidate.trim();
  if (!trimmed.startsWith('<')) return null;
  if (!/<[a-zA-Z][\s\S]*>/.test(trimmed)) return null;
  return trimmed;
}

/** Plain text for display when content is not treated as an HTML document. */
export function plainTextFromMaybeEncodedHtml(text: string): string {
  const d = decodeJsonLikeString(text);
  return d
    .replace(/<[^>]+>/g, ' ')
    .replace(/\s+/g, ' ')
    .trim();
}

function pad2(n: number): string {
  return n.toString().padStart(2, '0');
}

/** Value for `<input type="date" />` in local timezone. */
export function toLocalDateInputValue(d: Date): string {
  return `${d.getFullYear()}-${pad2(d.getMonth() + 1)}-${pad2(d.getDate())}`;
}

/** Value for `<input type="time" step="60" />` in local timezone. */
export function toLocalTimeInputValue(d: Date): string {
  return `${pad2(d.getHours())}:${pad2(d.getMinutes())}`;
}

/** Default date/time for a new reminder (local), offset from now. */
export function getDefaultReminderDueParts(offsetMinutes = 30): { date: string; time: string } {
  const d = new Date();
  d.setMinutes(d.getMinutes() + offsetMinutes);
  return { date: toLocalDateInputValue(d), time: toLocalTimeInputValue(d) };
}

/**
 * Combines browser date + time fields into UTC ISO for the API.
 * Uses the Date(y, m, d, h, min) constructor so the instant is always in the user's **local**
 * timezone. Parsing `YYYY-MM-DDTHH:mm` as a string is not reliable (some engines treat it as UTC).
 */
export function localDateTimePartsToIso(dateStr: string, timeStr: string): string {
  const dm = dateStr.trim().match(/^(\d{4})-(\d{2})-(\d{2})$/);
  const tm = timeStr.trim().match(/^(\d{1,2}):(\d{2})(?::\d{2})?$/);
  if (!dm || !tm) {
    const fallback = new Date(`${dateStr.trim()}T${timeStr.trim()}`);
    return Number.isNaN(fallback.getTime()) ? '' : fallback.toISOString();
  }
  const y = Number(dm[1]);
  const mo = Number(dm[2]) - 1;
  const d = Number(dm[3]);
  const h = Number(tm[1]);
  const mi = Number(tm[2]);
  const local = new Date(y, mo, d, h, mi, 0, 0);
  if (Number.isNaN(local.getTime())) {
    return '';
  }
  return local.toISOString();
}


