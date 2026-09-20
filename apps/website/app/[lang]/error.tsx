"use client";

/**
 * Catches genuine failures (network errors, 5xx responses) that propagate up because pages no
 * longer swallow them with .catch(() => null). A 404/not-yet-published record still resolves to
 * null without throwing (see getWebsiteRecord), so it never reaches this boundary — only a real
 * "the service is unavailable" condition does, and it gets a retryable message, not a fake
 * empty-content page.
 */
export default function LangError({ reset }: { error: Error; reset: () => void }) {
  return (
    <div className="home-section" style={{ marginTop: 60, textAlign: "center" }}>
      <p style={{ color: "var(--ink)", fontSize: "1.1rem", marginBottom: 12 }}>
        Something went wrong loading this page. Please try again.
      </p>
      <button
        type="button"
        onClick={() => reset()}
        style={{
          padding: "8px 20px",
          borderRadius: "0.4rem",
          border: "1px solid var(--ink)",
          background: "white",
          cursor: "pointer",
        }}
      >
        Retry
      </button>
    </div>
  );
}
