import { afterEach, expect, it, vi } from "vitest";
import { render, screen, cleanup, fireEvent } from "@testing-library/react";
import { swapLanguage, buildMenuTree, buildLineageTree, listPagesByTag, sortEventsByStart, filterPublications, distinctMediaCategories, filterMedia } from "../lib/website-api";
import { BookCatalog } from "../components/site/BookCatalog";
import { MediaGallery } from "../components/site/MediaGallery";
import { MissingTranslationBanner } from "../components/site/MissingTranslationBanner";
import { Footer } from "../components/site/Footer";
import { LineageTree } from "../components/site/LineageTree";

afterEach(() => {
  cleanup();
  vi.unstubAllGlobals();
});

it("swaps the language segment directly for routes with no staff-chosen slug", () => {
  expect(swapLanguage("/en/membership", "bn")).toBe("/bn/membership");
  expect(swapLanguage("/bn", "en")).toBe("/en");
  expect(swapLanguage("/bn/books", "en")).toBe("/en/books");
});

it("swapLanguage degrades to the section index for slug-driven detail routes, rather than guessing a slug that may not exist in the other language", () => {
  // organization/blog detail slugs are staff-chosen and not guaranteed to match across languages —
  // the precise per-record link belongs on the page itself (otherLanguageHref), not a path guess.
  expect(swapLanguage("/bn/blog/some-post", "en")).toBe("/en/blog");
  expect(swapLanguage("/en/organization/dorbar-institute", "bn")).toBe("/bn/organization");
  expect(swapLanguage("/bn/blog", "en")).toBe("/en/blog"); // the index itself has no slug, safe to swap directly
});

it("builds a nested menu tree from flat key/parentKey items and drops orphaned parent references", () => {
  const tree = buildMenuTree([
    { key: "books", label: "Books", href: "/en/books" },
    { key: "reading-list", label: "Reading list", href: "/en/books/list", parentKey: "books" },
    { key: "orphan", label: "Orphan", href: "/en/x", parentKey: "does-not-exist" },
  ]);
  expect(tree.map((t) => t.key)).toEqual(["books", "orphan"]);
  expect(tree[0].children.map((c) => c.key)).toEqual(["reading-list"]);
});

function lineageRecord(canonicalId: string, title: string, parentId: string | null, order: number) {
  return {
    canonicalId,
    kind: "LineageNode",
    language: "en" as const,
    publishedAtUtc: "2026-09-14T10:00:00Z",
    document: {
      content: { title, slug: canonicalId, blocks: [] },
      fields: { parentId, order },
    },
  };
}

it("builds a nested lineage tree from flat canonicalId/parentId records, ordered by fields.order", () => {
  const tree = buildLineageTree([
    lineageRecord("child-2", "Second Child", "root", 2),
    lineageRecord("root", "Founder", null, 0),
    lineageRecord("child-1", "First Child", "root", 1),
  ]);
  expect(tree.map((n) => n.canonicalId)).toEqual(["root"]);
  expect(tree[0].children.map((c) => c.canonicalId)).toEqual(["child-1", "child-2"]);
});

it("surfaces a lineage node with an unresolvable parent as an additional root instead of dropping it", () => {
  const tree = buildLineageTree([
    lineageRecord("root", "Founder", null, 0),
    lineageRecord("stray", "Undated Relative", "missing-parent", 0),
  ]);
  expect(tree.map((n) => n.canonicalId).sort()).toEqual(["root", "stray"]);
});

it("shows a language-appropriate missing-translation message with a working fallback link", () => {
  render(<MissingTranslationBanner requestedLanguage="en" fallbackHref="/bn/some-page" />);
  expect(screen.getByText(/isn't available in English yet/)).toBeTruthy();
  const link = screen.getByRole("link");
  expect(link.getAttribute("href")).toBe("/bn/some-page");
});

it("footer renders CMS-supplied contact details and social links when SiteSettings is published", async () => {
  const fetch = vi.fn(async (url: RequestInfo | URL) => {
    const isFirstPage = !String(url).includes("page=2");
    return {
      ok: true,
      status: 200,
      json: async () => ({
        data: isFirstPage
          ? [{
              canonicalId: "settings-1",
              kind: "SiteSettings",
              language: "en",
              publishedAtUtc: "2026-09-14T10:00:00Z",
              document: {
                content: { title: "Site settings", slug: "site-settings", blocks: [] },
                fields: {
                  organizationName: "Sadria Society",
                  contactEmail: "info@sadarmawla.org",
                  phone: "+880-000-0000",
                  address: "Dhaka, Bangladesh",
                  socialLinks: [{ label: "Facebook", href: "https://facebook.com/example" }],
                },
              },
            }]
          : [], // page 2+ empty, so getPublicMenu's underlying loop terminates realistically
      }),
    };
  });
  vi.stubGlobal("fetch", fetch);
  render(await Footer({ language: "en" }));
  expect(screen.getByText("info@sadarmawla.org")).toBeTruthy();
  expect(screen.getByRole("link", { name: "Facebook" })).toBeTruthy();
  expect(screen.getByText(new RegExp(String(new Date().getFullYear())))).toBeTruthy();
});

it("footer falls back to safe placeholder content when SiteSettings hasn't been published yet", async () => {
  const fetch = vi.fn(async () => ({ ok: false, status: 404 }));
  vi.stubGlobal("fetch", fetch);
  render(await Footer({ language: "en" }));
  expect(screen.getByText(/Content is being prepared/)).toBeTruthy();
});

it("footer degrades gracefully instead of throwing when the content service is unreachable", async () => {
  const fetch = vi.fn(async () => {
    throw new Error("network down");
  });
  vi.stubGlobal("fetch", fetch);
  render(await Footer({ language: "bn" }));
  expect(screen.getByText(/বিষয়বস্তু প্রস্তুত করা হচ্ছে/)).toBeTruthy();
});

it("renders a lineage tree with names, life dates and generational nesting, and nothing for an empty list", () => {
  const { container } = render(
    <LineageTree
      nodes={buildLineageTree([
        lineageRecord("root", "The Founder", null, 0),
        { ...lineageRecord("child", "A Successor", "root", 0), document: { content: { title: "A Successor", slug: "child", blocks: [] }, fields: { parentId: "root", order: 0, lifeDates: "1900–1970" } } },
      ])}
    />,
  );
  expect(screen.getByText("The Founder")).toBeTruthy();
  expect(screen.getByText("A Successor")).toBeTruthy();
  expect(screen.getByText("1900–1970")).toBeTruthy();
  expect(container.querySelectorAll(".lineage-tree").length).toBe(2); // outer list + one nested generation

  cleanup();
  const { container: emptyContainer } = render(<LineageTree nodes={[]} />);
  expect(emptyContainer.querySelector(".lineage-tree")).toBeNull();
});

function pageRecord(slug: string, title: string, tags: string[]) {
  return {
    canonicalId: slug,
    kind: "Page",
    language: "en",
    publishedAtUtc: "2026-09-14T10:00:00Z",
    document: { content: { title, slug, blocks: [], tags }, fields: {} },
  };
}

it("filters published Pages by content tag, ignoring pages without that tag", async () => {
  const fetch = vi.fn(async (url: RequestInfo | URL) => {
    const isFirstPage = String(url).includes("page=1");
    return {
      ok: true,
      status: 200,
      json: async () => ({
        data: isFirstPage
          ? [
              pageRecord("dorbar-institute", "Dorbar Institute", ["organization"]),
              pageRecord("some-blog-adjacent-page", "Unrelated", ["reading"]),
            ]
          : [], // subsequent pages empty, matching real backend pagination — stops the loop
      }),
    };
  });
  vi.stubGlobal("fetch", fetch);
  const { pages, truncated } = await listPagesByTag("en", "organization");
  expect(pages.map((r) => r.canonicalId)).toEqual(["dorbar-institute"]);
  expect(truncated).toBe(false);
});

it("listAllWebsiteRecords reports truncated:true when the safety cap is hit without an empty final page", async () => {
  const fetch = vi.fn(async () => ({
    ok: true,
    status: 200,
    // Every page returns data, never empty — simulates a pathological/misconfigured backend
    // that never terminates. The cap must still stop the loop and disclose the truncation.
    json: async () => ({ data: [pageRecord("x", "X", ["organization"])] }),
  }));
  vi.stubGlobal("fetch", fetch);
  const { truncated } = await listPagesByTag("en", "organization");
  expect(truncated).toBe(true);
});

function eventRecord(canonicalId: string, title: string, startsAt: string) {
  return {
    canonicalId,
    kind: "Event",
    language: "en" as const,
    publishedAtUtc: "2026-09-14T10:00:00Z",
    document: {
      content: { title, slug: canonicalId, blocks: [] },
      fields: { startsAt, endsAt: startsAt },
    },
  };
}

it("sorts events chronologically by start time regardless of input order", () => {
  const sorted = sortEventsByStart([
    eventRecord("later", "Later Event", "2027-03-01T10:00:00Z"),
    eventRecord("earliest", "Earliest Event", "2026-10-01T10:00:00Z"),
    eventRecord("middle", "Middle Event", "2026-12-01T10:00:00Z"),
  ]);
  expect(sorted.map((e) => e.canonicalId)).toEqual(["earliest", "middle", "later"]);
});

it("sortEventsByStart does not mutate the original array", () => {
  const original = [eventRecord("b", "B", "2027-01-01T00:00:00Z"), eventRecord("a", "A", "2026-01-01T00:00:00Z")];
  const originalOrder = original.map((e) => e.canonicalId);
  sortEventsByStart(original);
  expect(original.map((e) => e.canonicalId)).toEqual(originalOrder);
});

function publicationRecord(canonicalId: string, title: string, author: string, availability: "Preparing" | "Available") {
  return {
    canonicalId,
    kind: "Publication",
    language: "en" as const,
    publishedAtUtc: "2026-09-14T10:00:00Z",
    document: {
      content: { title, slug: canonicalId, blocks: [] },
      fields: { author, availability },
    },
  };
}

it("filterPublications matches on title or author, case-insensitively, and returns everything for an empty query", () => {
  const books = [
    publicationRecord("qd1", "Quran Darshan Part 1", "Mawla", "Available"),
    publicationRecord("qd2", "Quran Darshan Part 2", "Mawla", "Preparing"),
    publicationRecord("other", "A Different Work", "Someone Else", "Available"),
  ];
  expect(filterPublications(books, "").length).toBe(3);
  expect(filterPublications(books, "quran").map((b) => b.canonicalId)).toEqual(["qd1", "qd2"]);
  expect(filterPublications(books, "SOMEONE").map((b) => b.canonicalId)).toEqual(["other"]);
  expect(filterPublications(books, "nonexistent")).toEqual([]);
});

it("BookCatalog filters visible books as the visitor types, and shows a no-results message", () => {
  render(
    <BookCatalog
      language="en"
      initialPublications={[
        publicationRecord("qd1", "Quran Darshan", "Mawla", "Available"),
        publicationRecord("other", "A Different Work", "Someone Else", "Preparing"),
      ]}
      hasMore={false}
    />,
  );
  expect(screen.getByText("Quran Darshan")).toBeTruthy();
  expect(screen.getByText("A Different Work")).toBeTruthy();

  fireEvent.change(screen.getByRole("searchbox"), { target: { value: "Quran" } });
  expect(screen.getByText("Quran Darshan")).toBeTruthy();
  expect(screen.queryByText("A Different Work")).toBeNull();

  fireEvent.change(screen.getByRole("searchbox"), { target: { value: "nothing matches this" } });
  expect(screen.getByText(/No books match that search/)).toBeTruthy();
});

it("BookCatalog loads more publications on demand and appends them, rather than eagerly fetching everything upfront (finding #5)", async () => {
  const fetch = vi.fn(async () => ({
    ok: true,
    status: 200,
    json: async () => ({ data: [publicationRecord("page2-book", "Second Page Book", "Another Author", "Available")] }),
  }));
  vi.stubGlobal("fetch", fetch);

  render(
    <BookCatalog
      language="en"
      initialPublications={[publicationRecord("qd1", "Quran Darshan", "Mawla", "Available")]}
      hasMore={true}
    />,
  );
  expect(screen.queryByText("Second Page Book")).toBeNull();
  expect(screen.getByRole("button", { name: "Load more" })).toBeTruthy();

  fireEvent.click(screen.getByRole("button", { name: "Load more" }));
  await screen.findByText("Second Page Book");
  expect(screen.getByText("Quran Darshan")).toBeTruthy(); // original page still present, not replaced
});

it("BookCatalog shows a visible error and lets the reader retry when Load more fails (finding: load-more had no error handling)", async () => {
  const fetch = vi.fn(async () => {
    throw new Error("network down");
  });
  vi.stubGlobal("fetch", fetch);

  render(
    <BookCatalog
      language="en"
      initialPublications={[publicationRecord("qd1", "Quran Darshan", "Mawla", "Available")]}
      hasMore={true}
    />,
  );
  fireEvent.click(screen.getByRole("button", { name: "Load more" }));
  await screen.findByRole("alert");
  expect(screen.getByRole("alert").textContent).toMatch(/Couldn't load more books/);
  // The button stays present as a retry affordance, not silently disabled with no way forward.
  expect(screen.getByRole("button", { name: "Retry" })).toBeTruthy();
});

it("BookCatalog searches the whole catalogue, not just what's already loaded — a book on a later page must be findable, not appear to not exist (finding: search hid Load more)", async () => {
  const fetch = vi.fn(async (url: RequestInfo | URL) => {
    const isPage2 = String(url).includes("page=2");
    return {
      ok: true,
      status: 200,
      json: async () => ({
        data: isPage2
          ? [publicationRecord("page2-book", "A Book On Page Two", "Some Author", "Available")]
          : [], // page 3+ empty, so the search-load loop terminates realistically
      }),
    };
  });
  vi.stubGlobal("fetch", fetch);

  render(
    <BookCatalog
      language="en"
      initialPublications={[publicationRecord("qd1", "Quran Darshan", "Mawla", "Available")]}
      hasMore={true}
    />,
  );
  fireEvent.change(screen.getByRole("searchbox"), { target: { value: "Page Two" } });
  // The search must not silently report "no results" for a book that exists but isn't loaded yet.
  await screen.findByText("A Book On Page Two");
});

function mediaRecord(canonicalId: string, mediaType: "Photo" | "Video" | "Audio", category: string | undefined, alt: string) {
  return {
    canonicalId,
    kind: "MediaItem",
    language: "en" as const,
    publishedAtUtc: "2026-09-14T10:00:00Z",
    document: {
      content: { title: alt, slug: canonicalId, blocks: [] },
      fields: { mediaType, category, alt, url: "https://example.com/media" },
    },
  };
}

it("distinctMediaCategories returns unique, sorted, non-empty category values", () => {
  const items = [
    mediaRecord("a", "Photo", "Founder's Day", "a"),
    mediaRecord("b", "Video", "Branches", "b"),
    mediaRecord("c", "Audio", "Founder's Day", "c"),
    mediaRecord("d", "Photo", undefined, "d"),
  ];
  expect(distinctMediaCategories(items)).toEqual(["Branches", "Founder's Day"]);
});

it("filterMedia narrows by type and category independently, and null means no filtering on that dimension", () => {
  const items = [
    mediaRecord("photo-branches", "Photo", "Branches", "p"),
    mediaRecord("video-branches", "Video", "Branches", "v"),
    mediaRecord("photo-founders-day", "Photo", "Founder's Day", "p2"),
  ];
  expect(filterMedia(items, "Photo", null).map((i) => i.canonicalId).sort()).toEqual(["photo-branches", "photo-founders-day"]);
  expect(filterMedia(items, null, "Branches").map((i) => i.canonicalId).sort()).toEqual(["photo-branches", "video-branches"]);
  expect(filterMedia(items, "Photo", "Branches").map((i) => i.canonicalId)).toEqual(["photo-branches"]);
});

it("filterMedia trims stored category values before comparing, and a category literally named 'All' still filters correctly", () => {
  const items = [
    mediaRecord("padded", "Photo", " Branches ", "p"),
    mediaRecord("literal-all", "Photo", "All", "p2"),
  ];
  expect(filterMedia(items, null, "Branches").map((i) => i.canonicalId)).toEqual(["padded"]);
  expect(filterMedia(items, null, "All").map((i) => i.canonicalId)).toEqual(["literal-all"]);
});

it("MediaGallery lets a visitor filter by type via the tabs and updates the visible grid", () => {
  render(
    <MediaGallery
      language="en"
      items={[
        mediaRecord("photo-1", "Photo", "Branches", "A branch photograph"),
        mediaRecord("audio-1", "Audio", "Branches", "A recorded talk"),
      ]}
    />,
  );
  expect(screen.getByText("A branch photograph")).toBeTruthy();
  expect(screen.getByText("A recorded talk")).toBeTruthy();

  fireEvent.click(screen.getByRole("button", { name: "Audio" }));
  expect(screen.queryByText("A branch photograph")).toBeNull();
  expect(screen.getByText("A recorded talk")).toBeTruthy();
});

const siteSettingsRecord = {
  canonicalId: "settings-1",
  kind: "SiteSettings",
  language: "en",
  publishedAtUtc: "2026-09-14T10:00:00Z",
  document: {
    content: { title: "Site settings", slug: "site-settings", blocks: [] },
    fields: { organizationName: "Sadria Society", contactEmail: "info@sadarmawla.org", phone: "+880", address: "Dhaka" },
  },
};

const footerMenuRecord = {
  canonicalId: "menu-footer",
  kind: "PublicMenu",
  language: "en",
  publishedAtUtc: "2026-09-14T10:00:00Z",
  document: {
    content: { title: "Footer menu", slug: "menu-footer", blocks: [] },
    fields: {
      location: "footer",
      // Points at the actual generated route, not a placeholder that implies an unbuilt page.
      items: [{ key: "sitemap", label: "Sitemap", href: "/sitemap.xml" }],
    },
  },
};

it("footer renders the PublicMenu 'footer' location links when published, alongside SiteSettings contact info", async () => {
  const fetch = vi.fn(async (url: RequestInfo | URL) => {
    const href = String(url);
    const isFirstPage = !href.includes("page=2");
    if (href.includes("SiteSettings")) {
      return { ok: true, status: 200, json: async () => ({ data: isFirstPage ? [siteSettingsRecord] : [] }) };
    }
    if (href.includes("PublicMenu")) {
      return { ok: true, status: 200, json: async () => ({ data: isFirstPage ? [footerMenuRecord] : [] }) };
    }
    return { ok: false, status: 404 };
  });
  vi.stubGlobal("fetch", fetch);
  render(await Footer({ language: "en" }));
  expect(screen.getByRole("link", { name: "Sitemap" })).toBeTruthy();
  expect(screen.getByText("info@sadarmawla.org")).toBeTruthy();
});

it("footer keeps a successful result when the other request fails, instead of discarding both (finding #13)", async () => {
  const fetch = vi.fn(async (url: RequestInfo | URL) => {
    const href = String(url);
    if (href.includes("SiteSettings")) {
      return { ok: true, status: 200, json: async () => ({ data: [siteSettingsRecord] }) };
    }
    if (href.includes("PublicMenu")) {
      throw new Error("network down");
    }
    return { ok: false, status: 404 };
  });
  vi.stubGlobal("fetch", fetch);
  render(await Footer({ language: "en" }));
  // Settings succeeded even though the menu request failed — must still render.
  expect(screen.getByText("info@sadarmawla.org")).toBeTruthy();
  expect(screen.queryByRole("link", { name: "Sitemap" })).toBeNull();
});

it("BookCatalog: overlapping search triggers (e.g. rapid typing) never fetch the same page twice (regression: concurrent loading)", async () => {
  const pageRequests: string[] = [];
  let resolvePage2: (v: unknown) => void;
  const page2Promise = new Promise((resolve) => {
    resolvePage2 = resolve;
  });
  const fetch = vi.fn(async (url: RequestInfo | URL) => {
    const href = String(url);
    pageRequests.push(href);
    if (href.includes("page=2")) {
      await page2Promise; // held open, so a second attempt arriving while this is in flight is detectable
    }
    return {
      ok: true,
      status: 200,
      json: async () => ({
        data: href.includes("page=2")
          ? [publicationRecord("p2", "Page Two Book", "Author", "Available")]
          : [],
      }),
    };
  });
  vi.stubGlobal("fetch", fetch);

  render(
    <BookCatalog
      language="en"
      initialPublications={[publicationRecord("qd1", "Quran Darshan", "Mawla", "Available")]}
      hasMore={true}
    />,
  );

  // Genuinely fire two overlapping trigger attempts — not just check that a button is hidden.
  // The first starts the page-2 fetch (held open); the second, fired immediately after with a
  // different value (simulating rapid typing before the first load settles), independently
  // re-checks the same "should I load more to search?" condition. Whichever mechanism stops it
  // (the busy state check or the busyRef guard inside loadPages), the observable result must be
  // that page 2 is only ever requested once.
  fireEvent.change(screen.getByRole("searchbox"), { target: { value: "Page" } });
  fireEvent.change(screen.getByRole("searchbox"), { target: { value: "Page Two" } });

  resolvePage2!(undefined);
  await screen.findByText("Page Two Book");

  const page2Requests = pageRequests.filter((r) => r.includes("page=2"));
  expect(page2Requests.length).toBe(1); // never fetched twice despite two overlapping trigger attempts
});

it("BookCatalog: a failure after one successful page resumes from the right cursor, without re-fetching or duplicating the already-loaded page (regression: retry after partial failure)", async () => {
  let page2Attempts = 0;
  let page3Attempts = 0;
  const fetch = vi.fn(async (url: RequestInfo | URL) => {
    const href = String(url);
    if (href.includes("page=2")) {
      page2Attempts += 1;
      return { ok: true, status: 200, json: async () => ({ data: [publicationRecord("p2", "Page Two Book", "Author", "Available")] }) };
    }
    if (href.includes("page=3")) {
      page3Attempts += 1;
      if (page3Attempts === 1) throw new Error("network blip");
      return { ok: true, status: 200, json: async () => ({ data: [publicationRecord("p3", "Page Three Book", "Author", "Available")] }) };
    }
    return { ok: true, status: 200, json: async () => ({ data: [] }) };
  });
  vi.stubGlobal("fetch", fetch);

  render(
    <BookCatalog
      language="en"
      initialPublications={[publicationRecord("qd1", "Quran Darshan", "Mawla", "Available")]}
      hasMore={true}
    />,
  );

  fireEvent.change(screen.getByRole("searchbox"), { target: { value: "Book" } });
  // Page 2 succeeds (appended), page 3 fails — error shown, page 2's book must appear exactly once.
  await screen.findByRole("alert");
  expect(screen.getAllByText("Page Two Book").length).toBe(1);
  expect(page2Attempts).toBe(1); // the actual network call count, not just what got rendered —
  // a request-level assertion, so deduplication-on-render can't mask a wasted duplicate request.
  expect(screen.queryByText("Page Three Book")).toBeNull();

  // Retry: must resume at page 3, not re-request page 2.
  fireEvent.click(screen.getByRole("button", { name: "Retry" }));
  await screen.findByText("Page Three Book");
  expect(screen.getAllByText("Page Two Book").length).toBe(1); // still exactly one — not duplicated
  expect(page2Attempts).toBe(1); // explicit request-count assertion: page 2 was NOT re-requested on retry
  expect(page3Attempts).toBe(2); // page 3 was retried exactly once after the initial failure
});

it("BookCatalog: reaching the search cap shows 'incomplete search', not a false 'no results' (regression: search limit)", async () => {
  // Every page returns a non-matching, non-empty result, so the search-load loop runs all the
  // way to MAX_SEARCH_LOAD_PAGES without ever confirming the catalogue is fully loaded.
  const fetch = vi.fn(async () => ({
    ok: true,
    status: 200,
    json: async () => ({ data: [publicationRecord("never-matches", "Unrelated Title", "Someone", "Available")] }),
  }));
  vi.stubGlobal("fetch", fetch);

  render(
    <BookCatalog
      language="en"
      initialPublications={[publicationRecord("qd1", "Quran Darshan", "Mawla", "Available")]}
      hasMore={true}
    />,
  );

  fireEvent.change(screen.getByRole("searchbox"), { target: { value: "Nothing Matches This At All" } });
  await screen.findByText(/Search incomplete/);
  expect(screen.queryByText(/No books match that search/)).toBeNull();
});
