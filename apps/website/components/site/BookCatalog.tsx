"use client";

import { useRef, useState } from "react";
import type { Language, PublicationFields, PublicWebsiteRecord } from "../../lib/website-api";
import { assetUrl, filterPublications } from "../../lib/website-api";
import { loadMorePublications } from "../../lib/actions";

const copy = {
  en: {
    searchLabel: "Search by title or author",
    noResults: "No books match that search.",
    incompleteSearch: "Search incomplete — load more or retry.",
    available: "Read now",
    preparing: "Being prepared",
    by: "by",
    loadMore: "Load more",
    loading: "Loading…",
    searchingAll: "Loading the full catalogue to search…",
    loadError: "Couldn't load more books. Please try again.",
    retry: "Retry",
  },
  bn: {
    searchLabel: "শিরোনাম বা লেখক দিয়ে খুঁজুন",
    noResults: "এই খোঁজের সাথে কোনো বই মেলেনি।",
    incompleteSearch: "খোঁজা অসম্পূর্ণ — আরও লোড করুন বা আবার চেষ্টা করুন।",
    available: "এখনই পড়ুন",
    preparing: "প্রস্তুত করা হচ্ছে",
    by: "লেখক:",
    loadMore: "আরও দেখুন",
    loading: "লোড হচ্ছে…",
    searchingAll: "খোঁজার জন্য সম্পূর্ণ তালিকা লোড হচ্ছে…",
    loadError: "আরও বই লোড করা যায়নি। আবার চেষ্টা করুন।",
    retry: "আবার চেষ্টা করুন",
  },
} as const;

// Safety cap on how many pages a single "load everything to search" sequence will fetch.
const MAX_SEARCH_LOAD_PAGES = 50;

/** Appends a batch, deduping by canonicalId — defense in depth against any page ever being fetched twice. */
function mergeUnique(
  prev: PublicWebsiteRecord<PublicationFields>[],
  batch: PublicWebsiteRecord<PublicationFields>[],
): PublicWebsiteRecord<PublicationFields>[] {
  const seen = new Set(prev.map((p) => p.canonicalId));
  const newOnes = batch.filter((p) => !seen.has(p.canonicalId));
  return newOnes.length ? [...prev, ...newOnes] : prev;
}

export function BookCatalog({
  language,
  initialPublications,
  hasMore: initialHasMore,
}: {
  language: Language;
  initialPublications: PublicWebsiteRecord<PublicationFields>[];
  hasMore: boolean;
}) {
  const [query, setQuery] = useState("");
  const [publications, setPublications] = useState(initialPublications);
  const [hasMore, setHasMore] = useState(initialHasMore);
  const [nextPage, setNextPage] = useState(2);
  // A single coordination flag for BOTH entry points (manual Load More, and search auto-loading)
  // — whichever starts first runs to completion before the other can start, so the two can never
  // race on the same page or on the nextPage counter. A ref, not state: two calls fired in the
  // same tick (e.g. a fast double-click) would both still read a stale `false` from a useState
  // value, since React state updates aren't synchronous — a ref is read/written immediately.
  const busyRef = useRef(false);
  const [busy, setBusy] = useState(false); // drives the UI only; busyRef is the actual lock
  // False whenever loading stopped without confirming the catalogue is fully loaded — a failure,
  // or hitting the safety cap. Used to distinguish "genuinely searched everything, no match" from
  // "stopped early, so an empty result isn't a confirmed no-match."
  const [fullyLoaded, setFullyLoaded] = useState(!initialHasMore);
  const [loadError, setLoadError] = useState(false);
  const t = copy[language];
  const filtered = filterPublications(publications, query);

  /**
   * The one place either entry point actually fetches. Advances `nextPage` and merges results
   * after EACH successful page — not once at the end — so a mid-sequence failure leaves the
   * cursor correctly positioned at the first page that was never fetched, and a retry resumes
   * from there instead of re-requesting pages already downloaded.
   */
  async function loadPages(count: number) {
    if (busyRef.current) return;
    busyRef.current = true;
    setBusy(true);
    setLoadError(false);
    let page = nextPage;
    let fetchedAny = false;
    try {
      for (let i = 0; i < count; i++) {
        const batch = await loadMorePublications(language, page);
        if (batch.length === 0) {
          setHasMore(false);
          setFullyLoaded(true);
          break;
        }
        setPublications((prev) => mergeUnique(prev, batch));
        page += 1;
        setNextPage(page); // advanced immediately after this page succeeds, not after the loop
        fetchedAny = true;
        if (i === count - 1) {
          // Reached the requested count without an empty page — there may be more, but we don't
          // yet know the catalogue is fully loaded (relevant for the search-until-done case).
          setFullyLoaded(false);
        }
      }
    } catch {
      setLoadError(true);
      // fullyLoaded intentionally left false here — a failure never counts as "confirmed complete."
    } finally {
      busyRef.current = false;
      setBusy(false);
    }
    return fetchedAny;
  }

  function handleContinueLoading() {
    // While actively searching, "load more"/"retry" should resume the exhaustive search
    // sequence, not just fetch one page — otherwise a retry after a search failure would only
    // partially progress and require many repeated clicks to reach a genuinely complete search.
    void loadPages(searching ? MAX_SEARCH_LOAD_PAGES : 1);
  }

  function handleQueryChange(value: string) {
    setQuery(value);
    // A search must cover the whole catalogue, not just whatever's loaded so far — otherwise a
    // book that exists but isn't loaded yet looks like it doesn't exist. Only triggers once
    // (guarded by `busy`) and only while there's more to load.
    if (value.trim() && hasMore && !busy) {
      void loadPages(MAX_SEARCH_LOAD_PAGES);
    }
  }

  const searching = query.trim().length > 0;
  const showIncompleteNotice = searching && filtered.length === 0 && !busy && !fullyLoaded;
  const showNoResults = searching && filtered.length === 0 && !busy && fullyLoaded;

  return (
    <div>
      <input
        type="search"
        value={query}
        onChange={(e) => handleQueryChange(e.target.value)}
        placeholder={t.searchLabel}
        aria-label={t.searchLabel}
        className="book-search"
      />

      {busy && searching && (
        <p style={{ color: "#6b6b66", fontSize: "0.85rem", marginTop: 10 }} role="status">
          {t.searchingAll}
        </p>
      )}

      {showNoResults && <p style={{ color: "#6b6b66", marginTop: 16 }}>{t.noResults}</p>}
      {showIncompleteNotice && (
        <p style={{ color: "var(--brown)", marginTop: 16 }} role="status">
          {t.incompleteSearch}
        </p>
      )}

      {filtered.length > 0 && (
        <div className="book-grid">
          {filtered.map((pub) => {
            const f = pub.document.fields;
            return (
              <article key={pub.canonicalId} className="book-card">
                {assetUrl(f.coverAssetId) ? (
                  <img
                    src={assetUrl(f.coverAssetId)}
                    alt=""
                    loading="lazy"
                    referrerPolicy="no-referrer"
                    className="book-cover"
                  />
                ) : (
                  <div className="book-cover book-cover-empty" aria-hidden="true" />
                )}
                <div className="book-info">
                  <h3>{pub.document.content.title}</h3>
                  <p className="book-author">
                    {t.by} {f.author}
                    {f.publicationYear ? ` · ${f.publicationYear}` : ""}
                  </p>
                  <span className={`book-badge ${f.availability === "Available" ? "book-badge-available" : ""}`}>
                    {f.availability === "Available" ? t.available : t.preparing}
                  </span>
                  {f.availability === "Available" && f.readingUrl ? (
                    <a href={f.readingUrl} className="book-read-link">
                      {t.available} →
                    </a>
                  ) : null}
                </div>
              </article>
            );
          })}
        </div>
      )}

      {loadError && (
        <p style={{ color: "#a93232", fontSize: "0.88rem", marginTop: 16, textAlign: "center" }} role="alert">
          {t.loadError}
        </p>
      )}

      {hasMore && !busy && (
        <div style={{ textAlign: "center", marginTop: 28 }}>
          <button
            type="button"
            onClick={handleContinueLoading}
            disabled={busy}
            className="book-search"
            style={{ cursor: "pointer" }}
          >
            {loadError ? t.retry : t.loadMore}
          </button>
        </div>
      )}
      {busy && !searching && (
        <div style={{ textAlign: "center", marginTop: 28, color: "#6b6b66" }}>{t.loading}</div>
      )}
    </div>
  );
}
