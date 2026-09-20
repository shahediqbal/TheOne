"use server";

import type { Language, PublicationFields } from "./website-api";
import { listWebsiteRecords } from "./website-api";

/** Fetches one more page of publications for the Books & Reading catalogue's "Load more" button. */
export async function loadMorePublications(language: Language, page: number) {
  return listWebsiteRecords<PublicationFields>("Publication", language, page);
}
