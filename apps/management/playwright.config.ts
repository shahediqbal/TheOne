import { defineConfig } from "@playwright/test";
export default defineConfig({
  testDir: "e2e",
  fullyParallel: true,
  use: {
    baseURL: "https://localhost:5173",
    ignoreHTTPSErrors: true,
    headless: true,
    channel: "msedge",
  },
  reporter: "list",
  webServer: {
    command: "npm run dev",
    url: "https://localhost:5173",
    ignoreHTTPSErrors: true,
    reuseExistingServer: !process.env.CI,
  },
  projects: [
    { name: "desktop", use: { viewport: { width: 1440, height: 1000 } } },
    { name: "mobile", use: { viewport: { width: 390, height: 844 } } },
  ],
});
