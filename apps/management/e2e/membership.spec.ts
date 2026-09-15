import {
  fields,
  conduct,
  declaration,
  oath,
} from "../../../packages/membership-form/schema";
import { test, expect, type Page } from "@playwright/test";

async function fixture(
  page: Page,
  { read = true, fee = false, state = 1, failPayment = false } = {},
) {
  const calls: { path: string; body: any }[] = [];
  let status = state;
  const payments: any[] = fee
    ? [
        {
          id: "p1",
          type: 0,
          amount: 100,
          method: 1,
          transactionReference: null,
          note: null,
          recordedAtUtc: "2026-09-13T10:00:00Z",
        },
      ]
    : [];
  const application = () => ({
    referenceCode: "SDR-2026-000001",
    status,
    fullNameBn: "রহিম",
    fullNameEn: "Rahim",
    contactNumber: "+8801700000000",
    nidNumber: "1234567890",
    createdAtUtc: "2026-09-13T09:00:00Z",
    submittedAtUtc: "2026-09-13T09:00:00Z",
    codeOfConductAccepted: true,
  });
  const detail = () => ({
    application: application(),
    member: {
      id: "m1",
      membershipNumber: status === 3 ? "SDR-M-000001" : null,
      status: status === 3 ? 1 : 0,
      contributions: payments,
    },
    verifiedAtUtc: null,
    approvedAtUtc: null,
    rejectedAtUtc: null,
    rejectionReason: null,
    entryChannel: 0,
  });
  await page.route("**/api/v1/**", async (route) => {
    const request = route.request();
    const url = new URL(request.url());
    const path = url.pathname.replace("/api/v1", "");
    let data: unknown;
    let code = 200;
    if (path === "/browser/auth/refresh")
      data = { accessToken: "test", expiresAtUtc: "2099-01-01" };
    else if (path === "/auth/me")
      data = { userId: "staff", fullName: "Staff", roles: ["Admin"] };
    else if (path === "/me/permissions")
      data = read
        ? [
            "membership.read",
            "membership.review",
            "membership.approve",
            "membership.contribute",
            "membership.enter",
          ]
        : [];
    else if (path === "/me/menus") data = [];
    else if (path === "/admin/membership/applications")
      data = { items: [application()], totalCount: 1, page: 1, pageSize: 20 };
    else if (
      path === "/admin/membership/applications/SDR-2026-000001" &&
      request.method() === "GET"
    )
      data = detail();
    else if (
      request.method() === "POST" &&
      path.startsWith("/admin/membership/")
    ) {
      calls.push({ path, body: request.postDataJSON() });
      if (path.endsWith("/verify")) status = 2;
      if (path.endsWith("/approve")) status = 3;
      if (path.endsWith("/contributions")) {
        if (failPayment) code = 409;
        else
          payments.push({
            id: "p2",
            ...request.postDataJSON(),
            recordedAtUtc: "2026-09-13T11:00:00Z",
          });
      }
      data = detail();
    } else code = 404;
    await route.fulfill({
      status: code,
      contentType: "application/json",
      body: JSON.stringify({
        success: code === 200,
        data,
        message: code === 409 ? "Payment reference already exists" : "",
        errors: [],
      }),
    });
  });
  return calls;
}

test("queue opens details and hides NID until requested", async ({ page }) => {
  await fixture(page);
  await page.goto("/membership");
  await page.getByRole("link", { name: "Details", exact: true }).click();
  await expect(page.getByText("1234567890", { exact: true })).toHaveCount(0);
  await page.getByRole("button", { name: "Show NID" }).click();
  await expect(page.getByText("1234567890", { exact: true })).toBeVisible();
});
test("approval waits for membership fee", async ({ page }) => {
  await fixture(page, { state: 2 });
  await page.goto("/membership/SDR-2026-000001");
  await expect(
    page.getByRole("button", { name: "Approve membership", exact: true }),
  ).toBeDisabled();
});
test("approval requires confirmation and refreshes status", async ({
  page,
}) => {
  const calls = await fixture(page, { state: 2, fee: true });
  await page.goto("/membership/SDR-2026-000001");
  await page
    .getByRole("button", { name: "Approve membership", exact: true })
    .click();
  await expect(
    page.getByRole("button", { name: "Confirm", exact: true }),
  ).toBeDisabled();
  await page.getByLabel("I approve this membership application.").check();
  await page.getByRole("button", { name: "Confirm", exact: true }).click();
  await expect(page.getByText("SDR-M-000001", { exact: true })).toBeVisible();
  expect(calls.filter((x) => x.path.endsWith("/approve"))).toHaveLength(1);
});
test("payment failure preserves entered values for correction", async ({
  page,
}) => {
  await fixture(page, { failPayment: true });
  await page.goto("/membership/SDR-2026-000001");
  await page
    .getByRole("button", { name: "Record payment", exact: true })
    .click();
  await page.getByLabel("Amount (BDT)").fill("100");
  await page.getByLabel("Transaction reference").fill("ABC123");
  await page.getByLabel("I have verified receipt of this payment.").check();
  await page.getByRole("button", { name: "Confirm", exact: true }).click();
  await expect(
    page.getByText("Payment reference already exists"),
  ).toBeVisible();
  await expect(page.getByLabel("Transaction reference")).toHaveValue("ABC123");
});
test("paper entry is reviewed and submits with retry identifier", async ({
  page,
}) => {
  const calls = await fixture(page);
  await page.goto("/membership");
  await page.getByRole("button", { name: "Enter paper application" }).click();
  for (const field of fields.filter((f) => f.required && !f.otherOnly)) {
    if (field.options) {
      await page.getByRole("combobox", { name: field.en, exact: true }).click();
      await page
        .getByRole("option", { name: field.options[0][2], exact: true })
        .click();
    } else
      await page
        .locator(`[name="${field.key}"]`)
        .fill(
          field.type === "number"
            ? String(field.min || 1)
            : field.type === "date"
              ? "2026-01-01"
              : field.type === "email"
                ? "test@example.com"
                : /Number$/.test(field.key)
                  ? "01700000000"
                  : field.key === "fullNameBn"
                    ? "রহিম"
                    : "Example",
        );
  }
  await page.getByRole("checkbox", { name: "Education", exact: true }).check();
  for (const statement of [conduct, declaration, oath])
    await page
      .getByLabel(statement.en.replace(/\s+/g, " "), { exact: true })
      .check();
  await page
    .getByLabel(/Applicant photo/)
    .setInputFiles({
      name: "photo.png",
      mimeType: "image/png",
      buffer: Buffer.from(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+jRZkAAAAASUVORK5CYII=",
        "base64",
      ),
    });
  await page
    .getByRole("button", { name: "Review application", exact: true })
    .click();
  await page
    .getByRole("button", { name: "Submit application", exact: true })
    .click();
  await expect(page).toHaveURL(/SDR-2026-000001/);
  expect(calls[0].body.requestId).toMatch(/^[0-9a-f-]{36}$/);
  expect(calls[0].body.fields.fullNameBn).toBe("রহিম");
});
test("users without permission cannot open the membership route", async ({
  page,
}) => {
  const calls = await fixture(page, { read: false });
  await page.goto("/membership");
  await expect(page.getByText("Access denied", { exact: true })).toBeVisible();
  expect(calls).toHaveLength(0);
});
