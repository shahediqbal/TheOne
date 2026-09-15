import { mkdirSync } from "node:fs";
import { spawnSync } from "node:child_process";
mkdirSync(".certs", { recursive: true });
const result = spawnSync(
  "dotnet",
  [
    "dev-certs",
    "https",
    "--export-path",
    ".certs/localhost.pem",
    "--format",
    "Pem",
    "--no-password",
  ],
  { stdio: "inherit" },
);
process.exit(result.status ?? 1);
