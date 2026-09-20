import { describe, expect, it } from 'vitest';
import { pageMetadata } from '../lib/seo';
import type { PublicWebsiteRecord } from '../lib/website-api';

function record(content: Record<string, unknown>): PublicWebsiteRecord {
  return { canonicalId: 'fixture', kind: 'Page', language: 'en', publishedAtUtc: '',
    document: { content: { title: 'Page title', slug: 'home', summary: 'Page summary', blocks: [], ...content }, fields: {} } };
}

describe('CMS metadata contract', () => {
  it('uses the flat SEO fields returned by the backend', () => {
    const metadata = pageMetadata('en', record({ seoTitle: 'Search title', seoDescription: 'Search description' }), 'Fallback', 'Fallback description', '/en');
    expect(metadata.title).toBe('Search title');
    expect(metadata.description).toBe('Search description');
    expect(metadata.openGraph).toMatchObject({ title: 'Search title', description: 'Search description' });
  });
  it('falls back for blank SEO values and missing records', () => {
    const metadata = pageMetadata('en', record({ seoTitle: '   ', seoDescription: '' }), 'Fallback', 'Fallback description', '/en');
    expect(metadata.title).toBe('Page title');
    expect(metadata.description).toBe('Page summary');
    expect(pageMetadata('en', null, 'Fallback', 'Fallback description', '/en')).toMatchObject({ title: 'Fallback', description: 'Fallback description' });
  });
});
