import { afterEach, expect, it } from "vitest";
import { cleanup, fireEvent, render, screen } from "@testing-library/react";
import { ResponsiveNav } from "../components/site/ResponsiveNav";
import { portalUrl } from "../../../packages/website/portal";
afterEach(cleanup);
it("opens the menu, closes on Escape and restores button focus", () => {
  render(<ResponsiveNav language="en"><a href="/en/books">Books</a></ResponsiveNav>);
  const button = screen.getByRole("button", {name: "Menu"});
  expect(button.getAttribute("aria-expanded")).toBe("false");
  fireEvent.click(button);
  expect(button.getAttribute("aria-expanded")).toBe("true");
  fireEvent.keyDown(screen.getByRole("navigation"), {key: "Escape"});
  expect(button.getAttribute("aria-expanded")).toBe("false");
  expect(document.activeElement).toBe(button);
});
it("closes navigation when a destination is chosen", () => {
  render(<ResponsiveNav language="bn"><a href="#books">Books</a></ResponsiveNav>);
  const button = screen.getByRole("button", {name: "মেনু"});
  fireEvent.click(button);
  fireEvent.click(screen.getByText("Books"));
  expect(button.getAttribute("aria-expanded")).toBe("false");
});
it("allows configured web portals and rejects executable or credential URLs", () => {
  expect(portalUrl("https://staff.example.org", "/staff")).toBe("https://staff.example.org/");
  expect(portalUrl("javascript:alert(1)", "/staff")).toBe("/staff");
  expect(portalUrl("https://user:secret@example.org", "/staff")).toBe("/staff");
});

import { starterPage } from "../../../packages/website/starter-content";
it("creates independent reviewable bilingual drafts", () => {
  const draft = starterPage("bn", "life");
  expect(draft.provenance).toBe("ai_assisted_draft");
  expect(draft.title).toContain("মাওলা");
  draft.blocks[0].text = "Changed";
  expect(starterPage("bn", "life").blocks[0].text).not.toBe("Changed");
  expect(starterPage("en", "home").slug).toBe("home");
});
