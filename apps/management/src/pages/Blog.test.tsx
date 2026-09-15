import { beforeEach, afterEach, expect, it, vi } from "vitest";
import {
  render,
  screen,
  fireEvent,
  cleanup,
  waitFor,
} from "@testing-library/react";
import Blog from "./Blog";
import { api } from "../api";
import i18n from "../i18n";
const auth = vi.hoisted(() => ({
  permissions: ["blog.read", "blog.edit", "blog.approve"],
  profile: { roles: ["Admin"] },
}));
vi.mock("../api", () => ({ api: vi.fn() }));
vi.mock("../auth", () => ({ useAuth: () => auth }));
const entry = {
  id: "translation",
  postId: "post",
  language: "en",
  version: 7,
  status: 1,
  publishedRevisionId: null,
  approvedBy: null,
  document: {
    title: "Article",
    slug: "article",
    summary: "Summary",
    seoTitle: "Title",
    seoDescription: "Description",
    blocks: [{ type: "Paragraph", text: "Text" }],
    tags: [],
    provenance: "human",
  },
};
beforeEach(async () => {
  vi.resetAllMocks();
  auth.profile.roles = ["Admin"];
  await i18n.changeLanguage("en");
  vi.mocked(api).mockImplementation(async (path) =>
    path.includes("?page=") ? [entry] : entry,
  );
});
afterEach(cleanup);
it("requires confirmation and sends the reviewed version with approval", async () => {
  render(<Blog />);
  fireEvent.click(await screen.findByRole("button", { name: /Article · en/ }));
  fireEvent.click(await screen.findByRole("button", { name: "Approve" }));
  const button = screen.getByRole("button", {
    name: "Confirm",
  }) as HTMLButtonElement;
  expect(button.disabled).toBe(true);
  fireEvent.click(screen.getByLabelText("I confirm this action."));
  fireEvent.click(button);
  await waitFor(() =>
    expect(api).toHaveBeenCalledWith(
      "/admin/blog/translation/actions/approve",
      "POST",
      { expectedVersion: 7, revisionId: undefined },
    ),
  );
});
it("does not show approval to a non-administrator even with the permission", async () => {
  auth.profile.roles = ["Editor"];
  render(<Blog />);
  fireEvent.click(await screen.findByRole("button", { name: /Article · en/ }));
  await screen.findByRole("button", { name: "Return to draft" });
  expect(screen.queryByRole("button", { name: "Approve" })).toBeNull();
});
it("keeps unsaved text when a stale-version save fails", async () => {
  vi.mocked(api).mockImplementation(async (path, method) => {
    if (method === "PUT") throw new Error("This post changed. Reload.");
    const draft = { ...entry, status: 0 };
    return path.includes("?page=") ? [draft] : draft;
  });
  render(<Blog />);
  fireEvent.click(await screen.findByRole("button", { name: /Article · en/ }));
  const input = await screen.findByLabelText("Title");
  fireEvent.change(input, { target: { value: "My unsaved title" } });
  fireEvent.click(screen.getByRole("button", { name: "Save draft" }));
  await screen.findByText("This post changed. Reload.");
  expect((input as HTMLInputElement).value).toBe("My unsaved title");
});
