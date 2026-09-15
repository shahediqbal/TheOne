import { test, expect, type Page } from "@playwright/test";
const tokens = {
  accessToken: "test-access-only",
  expiresAtUtc: "2099-01-01",
  requiresTwoFactor: false,
  requiresAuthenticatorSetup: false,
  challengeId: null,
};
async function mock(page: Page, { member = false, enroll = false } = {}) {
  let signed = false,
    active = true;
  await page.route("**/api/v1/**", async (route) => {
    const req = route.request(),
      path = new URL(req.url()).pathname.replace("/api/v1", "");
    let data: unknown = null;
    let status = 200;
    const user = {
      id: "user-2",
      fullName: "Amina Rahman",
      email: "amina@example.test",
      mobileNumber: "01700000000",
      isActive: active,
      emailVerified: false,
      mobileVerified: true,
      authenticatorEnabled: false,
      roles: ["Member"],
    };
    if (path === "/browser/auth/refresh") {
      if (signed) data = tokens;
      else status = 401;
    } else if (path === "/browser/auth/login") {
      if (member) {
        signed = true;
        data = tokens;
      } else
        data = {
          ...tokens,
          accessToken: null,
          requiresTwoFactor: !enroll,
          requiresAuthenticatorSetup: enroll,
          challengeId: "challenge-1",
        };
    } else if (path === "/browser/auth/authenticator") {
      if (JSON.parse(req.postData()!).code === "123456") {
        signed = true;
        data = tokens;
      } else status = 401;
    } else if (path === "/browser/auth/logout") {
      signed = false;
      data = true;
    } else if (path === "/auth/authenticator/setup")
      data = {
        sharedKey: "EXAMPLESETUPKEY",
        qrCodeDataUri:
          "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+jRZkAAAAASUVORK5CYII=",
      };
    else if (path === "/auth/authenticator/confirm-setup")
      data = { recoveryCodes: ["EXAMPLE-ONE", "EXAMPLE-TWO"] };
    else if (path === "/auth/me")
      data = {
        userId: "user-1",
        fullName: "Workspace Owner",
        email: "owner@example.test",
        mobileNumber: "01700000001",
        mobileConfirmed: true,
        authenticatorEnabled: !member,
        roles: [member ? "Member" : "SuperAdmin"],
      };
    else if (path === "/me/permissions")
      data = member ? [] : ["users.read", "users.manage", "audit.read"];
    else if (path === "/me/menus")
      data = member
        ? []
        : [
            {
              id: "admin",
              labelEn: "Administration",
              children: [
                ["Users", "users"],
                ["Roles and Permissions", "roles"],
                ["Menu Management", "menus"],
                ["Audit History", "audit"],
              ].map(([labelEn, key]) => ({
                id: key,
                labelEn,
                route: "/administration/" + key,
                children: [],
              })),
            },
          ];
    else if (path === "/admin/users/user-2/status") {
      active = JSON.parse(req.postData()!).isActive;
      data = true;
    } else if (path === "/admin/users/user-2") data = user;
    else if (path === "/admin/users")
      data = { items: [user], totalCount: 1, page: 1, pageSize: 20 };
    else if (path === "/admin/roles" && req.method() === "POST") {
      status = 409;
    } else if (path === "/admin/roles")
      data = [
        {
          id: "r1",
          name: "SuperAdmin",
          isSystem: true,
          permissions: ["users.read", "users.manage", "audit.read"],
        },
        { id: "r2", name: "Member", isSystem: true, permissions: [] },
      ];
    else if (path === "/admin/permissions")
      data = ["users.read", "users.manage", "audit.read"];
    else if (path === "/auth/sessions") data = { items: [], totalCount: 0 };
    else {
      status = 404;
    }
    await route.fulfill({
      status,
      json: {
        success: status === 200,
        message:
          status === 409
            ? "Role already exists."
            : status === 401
              ? "Invalid or expired credentials."
              : "Request completed.",
        data,
        errors: [],
      },
    });
  });
}
async function login(page: Page, member = false) {
  await page.goto("/");
  await page.getByLabel("Email or mobile").fill("owner@example.test");
  await page
    .getByLabel("Password", { exact: true })
    .fill("Example-only-password");
  await page.getByRole("button", { name: "Sign in", exact: true }).click();
  if (!member) {
    await page
      .getByRole("textbox", { name: "Authenticator code", exact: true })
      .fill("123456");
    await page.getByRole("button", { name: "Continue", exact: true }).click();
  }
  await expect(
    page.getByRole("heading", { name: "Overview", exact: true }),
  ).toBeVisible();
}
test("MFA sign-in, account status confirmation, and no persisted tokens", async ({
  page,
}, info) => {
  await mock(page);
  await login(page);
  await page.goto("/administration/users");
  await expect(page.getByText("Amina Rahman")).toBeVisible();
  await page.getByRole("button", { name: "Deactivate", exact: true }).click();
  await expect(page.getByRole("dialog")).toBeVisible();
  await page.getByRole("button", { name: "Confirm", exact: true }).click();
  await expect(page.getByText("Inactive", { exact: true })).toBeVisible();
  expect(
    await page.evaluate(() =>
      JSON.stringify({ ...localStorage, ...sessionStorage }),
    ),
  ).not.toMatch(/test-access|refreshToken|password/);
  expect(
    await page.evaluate(
      () => document.documentElement.scrollWidth <= innerWidth,
    ),
  ).toBe(true);
  await page.screenshot({
    path: `test-results/users-${info.project.name}.png`,
    fullPage: true,
  });
});
test("members cannot open an administrative route", async ({ page }) => {
  await mock(page, { member: true });
  await login(page, true);
  await page.goto("/administration/users");
  await expect(page.getByText("Access denied", { exact: true })).toBeVisible();
  await expect(page.getByText("Amina Rahman")).toHaveCount(0);
});
test("role save errors stay in the editor", async ({ page }) => {
  await mock(page);
  await login(page);
  await page.goto("/administration/roles");
  await page.getByRole("button", { name: "New role", exact: true }).click();
  await page
    .getByRole("textbox", { name: "Name", exact: true })
    .fill("Editors");
  await page.getByRole("button", { name: "Save", exact: true }).click();
  await expect(page.getByRole("dialog")).toContainText("Role already exists.");
  await expect(
    page.getByRole("textbox", { name: "Name", exact: true }),
  ).toHaveValue("Editors");
});
test("required enrollment displays recovery codes before returning to login", async ({
  page,
}) => {
  await mock(page, { enroll: true });
  await page.goto("/");
  await page.getByLabel("Email or mobile").fill("owner@example.test");
  await page
    .getByLabel("Password", { exact: true })
    .fill("Example-only-password");
  await page.getByRole("button", { name: "Sign in", exact: true }).click();
  await expect(page.getByAltText("Authenticator setup QR code")).toBeVisible();
  await page
    .getByRole("textbox", { name: "Authenticator code", exact: true })
    .fill("123456");
  await page.getByRole("button", { name: "Enable authenticator" }).click();
  await expect(page.getByText("EXAMPLE-ONE")).toBeVisible();
  await page.getByRole("button", { name: "I saved my codes" }).click();
  await expect(
    page.getByRole("heading", { name: "Sign in", exact: true }),
  ).toBeVisible();
});
