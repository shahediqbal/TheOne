"use client";

import { useMemo, useState } from "react";
import type { Language, MediaItemFields, MediaType, PublicWebsiteRecord } from "../../lib/website-api";
import { assetUrl, distinctMediaCategories, filterMedia } from "../../lib/website-api";

const copy = {
  en: {
    all: "All",
    photo: "Photos",
    video: "Video",
    audio: "Audio",
    category: "Category",
    noResults: "No media matches this filter yet.",
    watch: "Watch",
  },
  bn: {
    all: "সব",
    photo: "ছবি",
    video: "ভিডিও",
    audio: "অডিও",
    category: "বিষয়শ্রেণী",
    noResults: "এই ফিল্টারে এখনও কোনো মিডিয়া নেই।",
    watch: "দেখুন",
  },
} as const;

const typeTabs: (MediaType | null)[] = [null, "Photo", "Video", "Audio"];

export function MediaGallery({
  language,
  items,
}: {
  language: Language;
  items: PublicWebsiteRecord<MediaItemFields>[];
}) {
  // null means "no filter on this dimension" — a distinct sentinel from any real type/category value.
  const [mediaType, setMediaType] = useState<MediaType | null>(null);
  const [category, setCategory] = useState<string | null>(null);
  const t = copy[language];
  const categories = useMemo(() => distinctMediaCategories(items), [items]);
  const filtered = filterMedia(items, mediaType, category);
  const tabLabel: Record<string, string> = { All: t.all, Photo: t.photo, Video: t.video, Audio: t.audio };

  return (
    <div>
      <div className="media-filters">
        {/* Plain toggle buttons, not ARIA tabs: these filter a shared grid rather than switching
            between separate tabpanels, so role="tab" (which implies an associated panel) would
            be the wrong semantic here. aria-pressed needs no custom keyboard handling — native
            button behavior (Tab, Enter, Space) is already correct. */}
        <div className="media-tabs" role="group" aria-label={t.category}>
          {typeTabs.map((type) => (
            <button
              key={type ?? "all"}
              type="button"
              aria-pressed={mediaType === type}
              className={`media-tab${mediaType === type ? " active" : ""}`}
              onClick={() => setMediaType(type)}
            >
              {tabLabel[type ?? "All"]}
            </button>
          ))}
        </div>
        {categories.length > 0 && (
          <select
            value={category ?? ""}
            onChange={(e) => setCategory(e.target.value === "" ? null : e.target.value)}
            aria-label={t.category}
            className="media-category-select"
          >
            <option value="">{t.all}</option>
            {categories.map((c) => (
              <option key={c} value={c}>
                {c}
              </option>
            ))}
          </select>
        )}
      </div>

      {filtered.length === 0 ? (
        <p style={{ color: "#6b6b66", marginTop: 16 }}>{t.noResults}</p>
      ) : (
        <div className="media-grid">
          {filtered.map((item) => {
            const f = item.document.fields;
            if (f.mediaType === "Photo") {
              const src = assetUrl(f.assetId) ?? f.url;
              return (
                <figure key={item.canonicalId} className="media-card">
                  {src ? (
                    <a href={src} target="_blank" rel="noopener noreferrer">
                      <img src={src} alt={f.alt} loading="lazy" referrerPolicy="no-referrer" />
                    </a>
                  ) : null}
                  <figcaption>{f.alt}</figcaption>
                </figure>
              );
            }
            if (f.mediaType === "Audio") {
              return (
                <figure key={item.canonicalId} className="media-card media-card-audio">
                  <figcaption>{f.alt}</figcaption>
                  {f.url ? <audio controls src={f.url} style={{ width: "100%" }} /> : null}
                </figure>
              );
            }
            return (
              <figure key={item.canonicalId} className="media-card media-card-video">
                <figcaption>{f.alt}</figcaption>
                {f.url ? (
                  <a href={f.url} target="_blank" rel="noopener noreferrer" className="media-watch-link">
                    ▶ {t.watch}
                  </a>
                ) : null}
              </figure>
            );
          })}
        </div>
      )}
    </div>
  );
}
