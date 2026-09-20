import type { Language, EventFields, PlaceFields } from "../../../lib/website-api";
import { listAllWebsiteRecords, assetUrl, sortEventsByStart } from "../../../lib/website-api";
import { TruncatedNotice } from "../../../components/site/TruncatedNotice";
import type { Metadata } from "next";

export function generateStaticParams() {
  return [{ lang: "bn" }, { lang: "en" }];
}

export async function generateMetadata({ params }: { params: Promise<{ lang: string }> }): Promise<Metadata> {
  const { lang } = await params;
  const language = lang as Language;
  return { title: copy[language].title, description: copy[language].intro };
}

const copy = {
  en: {
    title: "Programmes & Places",
    intro: "Upcoming programmes, and the branches and shrines that carry on this work.",
    programmes: "Upcoming Programmes",
    past: "Past Programmes",
    noEvents: "No upcoming programmes are published yet. Please check back soon.",
    noPast: "",
    places: "Branches & Shrines",
    noPlaces: "Places will be listed here once published.",
    cancelled: "Cancelled",
    ongoing: "Ongoing",
    visitingHours: "Visiting hours",
    mapNote:
      "An interactive map is planned for this page. In the meantime, addresses are listed below each place.",
  },
  bn: {
    title: "অনুষ্ঠান ও স্থান",
    intro: "আসন্ন অনুষ্ঠান এবং এই কাজ বহনকারী শাখা ও মাজারসমূহ।",
    programmes: "আসন্ন অনুষ্ঠান",
    past: "অতীত অনুষ্ঠান",
    noEvents: "এখনও কোনো আসন্ন অনুষ্ঠান প্রকাশিত হয়নি। শীঘ্রই আবার দেখুন।",
    noPast: "",
    places: "শাখা ও মাজার",
    noPlaces: "স্থানসমূহ প্রকাশিত হলে এখানে তালিকাভুক্ত হবে।",
    cancelled: "বাতিল",
    ongoing: "চলমান",
    visitingHours: "দর্শনের সময়",
    mapNote: "এই পাতার জন্য একটি ইন্টারঅ্যাক্টিভ মানচিত্র পরিকল্পনা করা হয়েছে। ইতিমধ্যে, ঠিকানা প্রতিটি স্থানের নিচে তালিকাভুক্ত।",
  },
} as const;

/**
 * Formats an event timestamp in a fixed, explicit timezone (Asia/Dhaka, this organization's own)
 * rather than the server's local time — otherwise the displayed time silently changes depending
 * on where the server happens to be deployed, which is exactly the kind of bug that's invisible
 * in local development and only shows up in production.
 */
function formatDateTime(iso: string, language: Language) {
  try {
    return new Intl.DateTimeFormat(language === "bn" ? "bn-BD" : "en-GB", {
      dateStyle: "medium",
      timeStyle: "short",
      timeZone: "Asia/Dhaka",
    }).format(new Date(iso));
  } catch {
    return iso;
  }
}

type EventStatus = "Upcoming" | "Ongoing" | "Past";

function classify(startsAt: string, endsAt: string, now: Date): EventStatus {
  const start = new Date(startsAt);
  const end = new Date(endsAt);
  if (now < start) return "Upcoming";
  if (now <= end) return "Ongoing";
  return "Past";
}

export default async function ProgrammesPage({ params }: { params: Promise<{ lang: string }> }) {
  const { lang } = await params;
  const language = lang as Language;
  const t = copy[language];

  // A genuine service failure propagates (real 500) rather than being caught here.
  const [{ records: events, truncated: eventsTruncated }, { records: places, truncated: placesTruncated }] =
    await Promise.all([
      listAllWebsiteRecords<EventFields>("Event", language),
      listAllWebsiteRecords<PlaceFields>("Place", language),
    ]);

  const placeById = new Map(places.map((p) => [p.canonicalId, p.document.content.title]));
  const sortedEvents = sortEventsByStart(events);
  const now = new Date();
  const upcomingOrOngoing = sortedEvents.filter(
    (e) => classify(e.document.fields.startsAt, e.document.fields.endsAt, now) !== "Past",
  );
  const past = sortedEvents
    .filter((e) => classify(e.document.fields.startsAt, e.document.fields.endsAt, now) === "Past")
    .reverse(); // most recent past event first

  const renderEvent = (event: (typeof sortedEvents)[number]) => {
    const f = event.document.fields;
    const status = classify(f.startsAt, f.endsAt, now);
    return (
      <li key={event.canonicalId} className="event-card">
        <div className="event-date">
          {formatDateTime(f.startsAt, language)}
          {f.hijriLabel ? <span className="event-hijri"> · {f.hijriLabel}</span> : null}
        </div>
        <h3>{event.document.content.title}</h3>
        {f.cancelled ? (
          <span className="event-cancelled">{t.cancelled}</span>
        ) : status === "Ongoing" ? (
          <span className="event-cancelled" style={{ background: "#e4efe9", color: "var(--emerald)" }}>
            {t.ongoing}
          </span>
        ) : null}
        {f.venue ? <p>{f.venue}</p> : null}
        {f.placeId && placeById.has(f.placeId) ? (
          <a href={`#place-${f.placeId}`}>{placeById.get(f.placeId)}</a>
        ) : null}
        {f.contact ? <p className="event-contact">{f.contact}</p> : null}
      </li>
    );
  };

  return (
    <div className="home-section" style={{ marginTop: 40 }}>
      <h1 style={{ fontFamily: "var(--font-display)", fontSize: "2.2rem", color: "var(--ink)" }}>
        {t.title}
      </h1>
      <p style={{ color: "#6b6b66", marginBottom: 32 }}>{t.intro}</p>

      <section>
        <h2 style={{ fontFamily: "var(--font-display)", fontSize: "1.5rem", color: "var(--ink)" }}>
          {t.programmes}
        </h2>
        {eventsTruncated && <TruncatedNotice language={language} />}
        {upcomingOrOngoing.length === 0 ? (
          <p style={{ color: "#6b6b66" }}>{t.noEvents}</p>
        ) : (
          <ul className="event-list">{upcomingOrOngoing.map(renderEvent)}</ul>
        )}
      </section>

      {past.length > 0 && (
        <section style={{ marginTop: 40 }}>
          <h2 style={{ fontFamily: "var(--font-display)", fontSize: "1.2rem", color: "#6b6b66" }}>
            {t.past}
          </h2>
          <ul className="event-list">{past.map(renderEvent)}</ul>
        </section>
      )}

      <section style={{ marginTop: 48 }}>
        <h2 style={{ fontFamily: "var(--font-display)", fontSize: "1.5rem", color: "var(--ink)" }}>
          {t.places}
        </h2>
        <p style={{ color: "#8b4a2b", fontSize: "0.88rem", marginBottom: 20 }}>{t.mapNote}</p>
        {placesTruncated && <TruncatedNotice language={language} />}
        {places.length === 0 ? (
          <p style={{ color: "#6b6b66" }}>{t.noPlaces}</p>
        ) : (
          <div className="place-grid">
            {places.map((place) => {
              const f = place.document.fields;
              return (
                <article key={place.canonicalId} id={`place-${place.canonicalId}`} className="place-card">
                  {assetUrl(f.imageAssetId) ? (
                    <img
                      src={assetUrl(f.imageAssetId)}
                      alt=""
                      loading="lazy"
                      referrerPolicy="no-referrer"
                      className="place-image"
                    />
                  ) : null}
                  <h3>{place.document.content.title}</h3>
                  <p>{f.address}</p>
                  {f.visitingHours ? (
                    <p className="place-meta">
                      <strong>{t.visitingHours}:</strong> {f.visitingHours}
                    </p>
                  ) : null}
                  {f.contact ? <p className="place-meta">{f.contact}</p> : null}
                </article>
              );
            })}
          </div>
        )}
      </section>
    </div>
  );
}
