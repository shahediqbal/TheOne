import { afterEach, beforeEach, describe, it, expect, vi } from "vitest";
import {
  render,
  screen,
  fireEvent,
  cleanup,
  waitFor,
} from "@testing-library/react";
import MembershipForm from "../components/MembershipForm";
import {
  validate,
  fields,
  consentVersion,
  conduct,
  declaration,
  oath,
  type Values,
} from "../../../packages/membership-form/schema";
const definition = {
  version: consentVersion,
  conductBn: conduct.bn,
  conductEn: conduct.en,
  declarationBn: declaration.bn,
  declarationEn: declaration.en,
  oathBn: oath.bn,
  oathEn: oath.en,
};
const response = (data: unknown, status = 200) => ({
  ok: status === 200,
  status,
  json: async () => ({
    success: status === 200,
    data,
    message: "Request failed",
  }),
});
function complete(): Values {
  const v: Values = { helpCategories: 1 };
  for (const f of fields.filter((f) => f.required && !f.otherOnly))
    v[f.key] = f.options
      ? f.options[0][0]
      : f.type === "number"
        ? f.min || 1
        : f.type === "date"
          ? "2026-01-01"
          : f.type === "email"
            ? "a@example.com"
            : /Number$/.test(f.key)
              ? "01700000000"
              : "Example";
  return v;
}
beforeEach(() => {
  vi.stubGlobal(
    "fetch",
    vi.fn(async () => response(definition)),
  );
});
afterEach(() => {
  cleanup();
  vi.unstubAllGlobals();
});
describe("submission validation", () => {
  it("accepts zero-valued gender/marital status and rejects empty or invalid required fields", () => {
    const v = complete();
    expect(validate(v)).toEqual([]);
    delete v.email;
    v.helpCategories = 0;
    expect(validate(v)).toEqual(
      expect.arrayContaining(["email", "helpCategories"]),
    );
  });
  it("requires Other details only when selected and rejects impossible dates", () => {
    const v = complete();
    v.helpCategories = 64;
    v.applicationDate = "2026-02-31";
    expect(validate(v)).toEqual(
      expect.arrayContaining(["otherHelpBn", "applicationDate"]),
    );
    v.otherHelpBn = "Other";
    v.applicationDate = "2026-01-01";
    expect(validate(v)).toEqual([]);
  });
});
describe("public applicant flows", () => {
  it("requires recovery details acknowledgement before entering the draft", async () => {
    vi.mocked(fetch).mockImplementation(
      async (_url, init) =>
        response(
          init?.method === "POST"
            ? { referenceCode: "SDR-2026-1", resumeToken: "private-secret" }
            : definition,
        ) as Response,
    );
    render(<MembershipForm language="en" />);
    for (const [name, value] of [
      ["Full name (Bangla)", "পরীক্ষা"],
      ["Full name (English)", "Test"],
      ["Contact number (English digits)", "01700000000"],
    ])
      fireEvent.change(
        screen.getByLabelText(new RegExp(name.replace(/[()]/g, "\\$&"))),
        { target: { value } },
      );
    fireEvent.click(screen.getByRole("button", { name: "Start application" }));
    const button = await screen.findByRole("button", {
      name: "Continue to form",
    });
    expect((button as HTMLButtonElement).disabled).toBe(true);
    fireEvent.click(
      screen.getByLabelText("I have saved these details securely."),
    );
    fireEvent.click(button);
    expect(
      await screen.findByRole("button", { name: "Save draft" }),
    ).toBeTruthy();
    expect(localStorage.length).toBe(0);
  });
  it("keeps draft values when a save fails and sends resume secrets only in the body", async () => {
    vi.mocked(fetch).mockImplementation(async (url, init) => {
      if (init?.method === "PATCH") return response(null, 503) as Response;
      if (init?.method === "POST")
        return response({
          ...complete(),
          referenceCode: "SDR-2026-1",
          status: 0,
          photoUrl: "uploaded",
        }) as Response;
      return response(definition) as Response;
    });
    render(<MembershipForm language="en" />);
    fireEvent.click(screen.getByRole("button", { name: "Resume application" }));
    for (const [label, value] of [
      ["Reference code", "SDR-2026-1"],
      ["Contact number", "01700000000"],
      ["Private resume token", "private-secret"],
    ])
      fireEvent.change(screen.getByLabelText(label), { target: { value } });
    fireEvent.click(screen.getByRole("button", { name: "Open application" }));
    await screen.findByRole("button", { name: "Save draft" });
    fireEvent.change(screen.getByLabelText(/Full name \(English\)/), {
      target: { value: "Retained" },
    });
    fireEvent.click(screen.getByRole("button", { name: "Save draft" }));
    await screen.findByRole("alert");
    expect(
      (screen.getByLabelText(/Full name \(English\)/) as HTMLInputElement)
        .value,
    ).toBe("Retained");
    const calls = vi.mocked(fetch).mock.calls;
    expect(
      calls.every(([url]) => !String(url).includes("private-secret")),
    ).toBe(true);
    expect(
      JSON.parse(String(calls.find(([, i]) => i?.method === "PATCH")![1]!.body))
        .resumeToken,
    ).toBe("private-secret");
  });
  it("does not permit submission until all three statements are accepted", async () => {
    vi.mocked(fetch).mockImplementation(
      async (url, init) =>
        response(
          init?.method === "POST" || init?.method === "PATCH"
            ? {
                ...complete(),
                referenceCode: "SDR-2026-1",
                status: String(url).endsWith("/submit") ? 1 : 0,
                photoUrl: "uploaded",
                submittedAtUtc: String(url).endsWith("/submit")
                  ? "2026-09-13T10:00:00Z"
                  : null,
              }
            : definition,
        ) as Response,
    );
    render(<MembershipForm language="en" />);
    fireEvent.click(screen.getByRole("button", { name: "Resume application" }));
    for (const [label, value] of [
      ["Reference code", "SDR-2026-1"],
      ["Contact number", "01700000000"],
      ["Private resume token", "private-secret"],
    ])
      fireEvent.change(screen.getByLabelText(label), { target: { value } });
    fireEvent.click(screen.getByRole("button", { name: "Open application" }));
    fireEvent.click(
      await screen.findByRole("button", { name: "6. Review and consent" }),
    );
    const button = screen.getByRole("button", {
      name: "Submit application",
    }) as HTMLButtonElement;
    expect(button.disabled).toBe(true);
    fireEvent.click(screen.getByLabelText(conduct.en.replace(/\s+/g, " ")));
    fireEvent.click(screen.getByLabelText(declaration.en));
    expect(button.disabled).toBe(true);
    fireEvent.click(screen.getByLabelText(oath.en));
    await waitFor(() => expect(button.disabled).toBe(false));
    fireEvent.click(button);
    expect(
      await screen.findByRole("heading", { name: "Submitted" }),
    ).toBeTruthy();
    const sent = vi
      .mocked(fetch)
      .mock.calls.filter(([url]) => String(url).endsWith("/submit"));
    expect(sent).toHaveLength(1);
    expect(JSON.parse(String(sent[0][1]!.body))).toMatchObject({
      codeOfConductAccepted: true,
      declarationAccepted: true,
      oathAccepted: true,
      consentVersion,
    });
  });
});
