import { beforeEach, afterEach, expect, it, vi } from "vitest";
import {
  render,
  screen,
  fireEvent,
  cleanup,
  waitFor,
} from "@testing-library/react";
import Website from "./Website";
import { api } from "../api";
import i18n from "../i18n";
const auth = vi.hoisted(() => ({
  permissions: ["website.read", "website.edit", "website.approve"],
  profile: { roles: ["Admin"] },
}));
vi.mock("../api", () => ({ api: vi.fn(), apiForm: vi.fn(), apiBlob: vi.fn() }));
vi.mock("../auth", () => ({ useAuth: () => auth }));
const record = {
  id: "record",
  canonicalId: "record",
  kind: "Page",
  language: "en",
  version: 3,
  status: 1,
  document: {
    content: {
      title: "About us",
      slug: "about",
      summary: "Summary",
      seoTitle: "About",
      seoDescription: "Description",
      blocks: [],
      tags: [],
      provenance: "human",
    },
    fields: {},
  },
};
let status = 1;
beforeEach(async () => {
  vi.resetAllMocks();
  status = 1;
  auth.profile.roles = ["Admin"];
  await i18n.changeLanguage("en");
  vi.mocked(api).mockImplementation(async (path, method) => {
    if (path.endsWith("/schema"))
      return [{ kind: "Page", label: "Pages", fields: [] }];
    const value = { ...record, status };
    if (path.includes("/records?")) return [value];
    if (method === "PUT") throw new Error("Content changed. Reload.");
    return value;
  });
});
afterEach(cleanup);
it("requires administrator confirmation and sends the selected version", async () => {
  render(<Website />);
  fireEvent.click(await screen.findByRole("button", { name: /About us · en/ }));
  fireEvent.click(await screen.findByRole("button", { name: "Approve" }));
  const confirm = screen.getByRole("button", {
    name: "Confirm",
  }) as HTMLButtonElement;
  expect(confirm.disabled).toBe(true);
  fireEvent.click(screen.getByLabelText("I confirm this action."));
  fireEvent.click(confirm);
  await waitFor(() =>
    expect(api).toHaveBeenCalledWith(
      "/admin/website/records/record/actions/approve",
      "POST",
      { expectedVersion: 3, revisionId: undefined },
    ),
  );
});
it("does not offer approval to an editor who merely has the approval permission", async () => {
  auth.profile.roles = ["Editor"];
  render(<Website />);
  fireEvent.click(await screen.findByRole("button", { name: /About us · en/ }));
  await screen.findByRole("button", { name: "Return to draft" });
  expect(screen.queryByRole("button", { name: "Approve" })).toBeNull();
});
it("preserves unsaved content after a conflicting save", async () => {
  status = 0;
  render(<Website />);
  fireEvent.click(await screen.findByRole("button", { name: /About us · en/ }));
  const title = await screen.findByLabelText("Title");
  fireEvent.change(title, { target: { value: "My revision" } });
  fireEvent.click(screen.getByRole("button", { name: "Save draft" }));
  await screen.findByText("Content changed. Reload.");
  expect((title as HTMLInputElement).value).toBe("My revision");
});

it("loads starter text into the draft without sending a save or publish request", async () => {
  status = 0;
  vi.spyOn(window, "confirm").mockReturnValue(true);
  render(<Website />);
  fireEvent.click(await screen.findByRole("button", { name: /About us · en/ }));
  fireEvent.mouseDown(await screen.findByLabelText("Use existing-site starter text / পুরোনো সাইটের প্রাথমিক লেখা"));
  fireEvent.click(await screen.findByRole("option", {name: "Mawla’s life / মাওলার জীবন"}));
  expect((screen.getByLabelText("Title") as HTMLInputElement).value).toBe("Mawla Sadar Uddin Ahmad Chisty");
  expect((screen.getByLabelText("Slug") as HTMLInputElement).value).toBe("life");
  expect(vi.mocked(api).mock.calls.filter(([,method]) => method === "PUT" || method === "POST")).toHaveLength(0);
  vi.restoreAllMocks();
});
