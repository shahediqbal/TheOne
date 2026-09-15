"use client";
import { useEffect, useRef, useState } from "react";
import type { BlogDocument } from "../../../packages/blog/types";
import BlogBlocks from "./BlogBlocks";
export default function BlogPreview({ language }: { language: string }) {
  const [document, setDocument] = useState<BlogDocument>(),
    [error, setError] = useState("");
  const started = useRef(false);
  useEffect(() => {
    if (started.current) return;
    started.current = true;
    const token = decodeURIComponent(window.location.hash.slice(1));
    history.replaceState(null, "", window.location.pathname);
    if (!token) {
      setError("Open the complete private preview link supplied by staff.");
      return;
    }
    fetch("/api/v1/blog/preview", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ token }),
      cache: "no-store",
    })
      .then(async (r) => {
        if (!r.ok)
          throw new Error(
            "This preview has expired or the draft has changed. Ask staff for a new link.",
          );
        const result = await r.json();
        setDocument(result.data);
      })
      .catch((e) => setError(e.message));
  }, []);
  return (
    <main className="blog-shell">
      <p className="notice">
        {language === "bn"
          ? "ব্যক্তিগত প্রিভিউ — প্রকাশিত নয়"
          : "Private preview — unpublished"}
      </p>
      {error && <p role="alert">{error}</p>}
      {document ? (
        <article>
          <h1>{document.title}</h1>
          <p className="blog-summary">{document.summary}</p>
          <BlogBlocks blocks={document.blocks} language={language} />
        </article>
      ) : (
        !error && <p>Loading…</p>
      )}
    </main>
  );
}
