import { afterEach, expect, it, vi } from "vitest";
import { render, screen, cleanup, waitFor } from "@testing-library/react";
import BlogBlocks from "../components/BlogBlocks";
import BlogPreview from "../components/BlogPreview";
import BlogArticle from "../components/BlogArticle";
afterEach(() => {
  cleanup();
  vi.unstubAllGlobals();
  history.replaceState(null, "", "/");
});
it("renders untrusted text as text and refuses unsafe media URLs", () => {
  render(
    <BlogBlocks
      language="en"
      blocks={[
        { type: "Paragraph", text: "<script>bad()</script>" },
        { type: "Image", url: "javascript:bad()", alt: "Bad" },
      ]}
    />,
  );
  expect(screen.getByText("<script>bad()</script>")).toBeTruthy();
  expect(document.querySelector("script")).toBeNull();
  expect(document.querySelector("img")).toBeNull();
});
it("uses the private fragment only in the preview request body and removes it from history", async () => {
  history.replaceState(null, "", "/en/blog/preview#private-token");
  const fetch = vi.fn(async (_url: string, _init?: RequestInit) => ({
    ok: false,
  }));
  vi.stubGlobal("fetch", fetch);
  render(<BlogPreview language="en" />);
  await screen.findByRole("alert");
  expect(location.hash).toBe("");
  await waitFor(() => expect(fetch).toHaveBeenCalledTimes(1));
  expect(fetch.mock.calls[0][0]).toBe("/api/v1/blog/preview");
  expect(
    JSON.parse(
      String(
        (fetch.mock.calls[0] as unknown[])[1] &&
          ((fetch.mock.calls[0] as unknown[])[1] as RequestInit).body,
      ),
    ),
  ).toEqual({ token: "private-token" });
});
it("shows missing-translation guidance and safely encodes structured metadata", () => {
  render(
    <BlogArticle
      requestedLanguage="en"
      post={{
        postId: "post",
        language: "bn",
        publishedAtUtc: "2026-09-14T10:00:00Z",
        otherLanguageSlug: null,
        document: {
          title: "</script><script>bad()</script>",
          slug: "article",
          summary: "Summary",
          seoTitle: "Title",
          seoDescription: "Description",
          blocks: [],
          tags: [],
          provenance: "human",
        },
      }}
    />,
  );
  expect(screen.getByText(/English translation is not published/)).toBeTruthy();
  const scripts = document.querySelectorAll("script");
  expect(scripts.length).toBe(1);
  expect(JSON.parse(scripts[0].textContent!).headline).toContain("</script>");
});
