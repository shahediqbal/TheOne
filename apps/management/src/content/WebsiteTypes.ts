import type { BlogDocument } from "../../../../packages/blog/types";
export type Field = {
  key: string;
  label: string;
  type: string;
  required: boolean;
  targetKind?: string;
  choices?: string[];
};
export type Schema = { kind: string; label: string; fields: Field[] };
export type Payload = {
  content: BlogDocument;
  fields: Record<string, unknown>;
};
export type RecordView = {
  id: string;
  canonicalId: string;
  kind: string;
  language: string;
  status: number;
  version: number;
  document: Payload;
  linkedStaffId?: string;
  publishedRevisionId?: string;
  approvedBy?: string;
  approvedAtUtc?: string;
};
export type Candidate = {
  id: string;
  title: string;
  language?: string;
  published?: boolean;
};
export type Asset = {
  id: string;
  originalName: string;
  contentType: string;
  bytes: number;
};
export const kindBn: Record<string, string> = {
  Page: "পাতা",
  SiteSettings: "সাইটের সেটিংস",
  PublicMenu: "পাবলিক মেনু",
  Place: "স্থান",
  LineageNode: "বংশপরম্পরা",
  Event: "অনুষ্ঠান",
  MediaItem: "মিডিয়া",
  Publication: "বই ও প্রকাশনা",
  AuthorProfile: "লেখক পরিচিতি",
  Series: "ব্লগ সিরিজ",
};
