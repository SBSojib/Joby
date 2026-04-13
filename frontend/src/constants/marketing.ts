/** Public contact email (footer, careers, press). */
export const CONTACT_EMAIL = 'sojib.24csedu.037@gmail.com';

export const mailtoHref = (subject?: string) => {
  const q = subject ? `?subject=${encodeURIComponent(subject)}` : '';
  return `mailto:${CONTACT_EMAIL}${q}`;
};
