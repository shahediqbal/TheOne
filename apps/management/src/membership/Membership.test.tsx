import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import {
  cleanup,
  fireEvent,
  render,
  screen,
  waitFor,
} from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { api } from "../api";
import i18n from "../i18n";
import Membership from "../pages/Membership";
import OperatorEntry from "./OperatorEntry";
import {
  fields,
  conduct,
  declaration,
  oath,
} from "../../../../packages/membership-form/schema";

vi.mock("../api", () => ({ api: vi.fn() }));
vi.mock("../auth", () => ({
  useAuth: () => ({
    permissions: [
      "membership.read",
      "membership.review",
      "membership.approve",
      "membership.contribute",
      "membership.enter",
    ],
  }),
}));
const ref = "SDR-2026-000001";
const base = "/admin/membership/applications/" + ref;
function detail(fee = false) {
  return {
    application: {
      referenceCode: ref,
      fullNameBn: "রহিম",
      fullNameEn: "Rahim",
      status: 2,
      nidNumber: "1234567890",
      codeOfConductAccepted: true,
    },
    member: {
      id: "m1",
      membershipNumber: null,
      status: 0,
      contributions: fee
        ? [
            {
              id: "p1",
              type: 0,
              amount: 100,
              method: 1,
              recordedAtUtc: "2026-09-13T10:00:00Z",
            },
          ]
        : [],
    },
    verifiedAtUtc: null,
    approvedAtUtc: null,
    rejectedAtUtc: null,
    entryChannel: 0,
  };
}
function view() {
  const client = new QueryClient({
    defaultOptions: { queries: { retry: false, gcTime: 0 } },
  });
  render(
    <QueryClientProvider client={client}>
      <MemoryRouter initialEntries={["/membership/" + ref]}>
        <Routes>
          <Route path="/membership/:reference" element={<Membership />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}
beforeEach(async () => {
  vi.resetAllMocks();
  await i18n.changeLanguage("en");
});
afterEach(cleanup);

describe("staff membership interactions", () => {
  it("keeps approval disabled until the membership fee is recorded", async () => {
    vi.mocked(api).mockResolvedValue(detail());
    view();
    const button = await screen.findByRole("button", {
      name: "Approve membership",
    });
    expect((button as HTMLButtonElement).disabled).toBe(true);
  });
  it("requires confirmation before sending approval", async () => {
    vi.mocked(api).mockResolvedValue(detail(true));
    view();
    fireEvent.click(
      await screen.findByRole("button", { name: "Approve membership" }),
    );
    expect(
      (screen.getByRole("button", { name: "Confirm" }) as HTMLButtonElement)
        .disabled,
    ).toBe(true);
    fireEvent.click(
      screen.getByLabelText("I approve this membership application."),
    );
    fireEvent.click(screen.getByRole("button", { name: "Confirm" }));
    await waitFor(() =>
      expect(api).toHaveBeenCalledWith(base + "/approve", "POST", {}),
    );
  });
  it("retains payment fields when the API rejects a duplicate reference", async () => {
    vi.mocked(api).mockImplementation(async (path, method) => {
      if (method === "POST")
        throw new Error("Payment reference already exists");
      return detail();
    });
    view();
    fireEvent.click(
      await screen.findByRole("button", { name: "Record payment" }),
    );
    fireEvent.change(screen.getByLabelText(/Amount \(BDT\)/), {
      target: { value: "100" },
    });
    fireEvent.change(screen.getByLabelText(/Transaction reference/), {
      target: { value: "ABC123" },
    });
    fireEvent.click(
      screen.getByLabelText("I have verified receipt of this payment."),
    );
    fireEvent.click(screen.getByRole("button", { name: "Confirm" }));
    await screen.findByText("Payment reference already exists");
    expect(
      (screen.getByLabelText(/Transaction reference/) as HTMLInputElement)
        .value,
    ).toBe("ABC123");
  });
  it("conceals the NID on initial display and reveals it only on request", async () => {
    vi.mocked(api).mockResolvedValue(detail());
    view();
    const show = await screen.findByRole("button", { name: "Show NID" });
    expect(screen.queryByText("1234567890")).toBeNull();
    fireEvent.click(show);
    expect(screen.getByText("1234567890")).toBeTruthy();
  });
  it("requires paper-form review and keeps the same request ID when retrying", async () => {
    vi.mocked(api).mockRejectedValue(new Error("Request interrupted"));
    render(<OperatorEntry onClose={vi.fn()} onSaved={vi.fn()} />);
    for (const field of fields.filter((f) => f.required && !f.otherOnly)) {
      const input = document.querySelector(`[name="${field.key}"]`)!;
      const value = field.options
        ? String(field.options[0][0])
        : field.type === "number"
          ? String(field.min || 1)
          : field.type === "date"
            ? "2026-01-01"
            : field.type === "email"
              ? "test@example.com"
              : /Number$/.test(field.key)
                ? "01700000000"
                : "Example";
      if (field.options) {
        fireEvent.mouseDown(
          screen.getByRole("combobox", {
            name: new RegExp(field.en.replace(/[.*+?^${}()|[\]\\]/g, "\\$&")),
          }),
        );
        fireEvent.click(
          await screen.findByRole("option", {
            name: field.options[0][2],
          }),
        );
      } else fireEvent.change(input, { target: { value } });
    }
    fireEvent.click(
      screen.getByLabelText("Education", {
        selector: 'input[type="checkbox"]',
      }),
    );
    for (const statement of [conduct, declaration, oath])
      fireEvent.click(screen.getByLabelText(statement.en.replace(/\s+/g, " ")));
    fireEvent.change(screen.getByLabelText(/Applicant photo/), {
      target: {
        files: [new File(["image"], "photo.png", { type: "image/png" })],
      },
    });
    await new Promise((resolve) => setTimeout(resolve, 30));
    fireEvent.submit(document.getElementById("paper-entry")!);
    fireEvent.click(screen.getByRole("button", { name: "Submit application" }));
    await screen.findByText("Request interrupted");
    const first = vi.mocked(api).mock.calls[0][2] as { requestId: string };
    fireEvent.click(screen.getByRole("button", { name: "Submit application" }));
    await waitFor(() => expect(api).toHaveBeenCalledTimes(2));
    expect(
      (vi.mocked(api).mock.calls[1][2] as { requestId: string }).requestId,
    ).toBe(first.requestId);
  }, 15000);
});
