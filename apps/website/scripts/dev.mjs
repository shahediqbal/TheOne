import { spawn, spawnSync } from "node:child_process";
import { mkdirSync } from "node:fs";
import { fileURLToPath } from "node:url";
import { resolve } from "node:path";

// Node needs an explicit trust anchor for ASP.NET's localhost certificate.
// Export only the public certificate; never disable TLS verification.
const root = fileURLToPath(new URL("../", import.meta.url));
const env = { ...process.env };
if (!env.NODE_EXTRA_CA_CERTS) {
  const cert = resolve(root, ".certs/aspnet-localhost.pem");
  const check = spawnSync("dotnet", ["dev-certs", "https", "--check"], { stdio: "ignore" });
  if (check.status === 0) {
    mkdirSync(resolve(root, ".certs"), { recursive: true });
    const exported = spawnSync("dotnet", ["dev-certs", "https", "--export-path", cert, "--format", "Pem"], { stdio: "inherit" });
    if (exported.status !== 0) process.exit(exported.status || 1);
    env.NODE_EXTRA_CA_CERTS = cert;
  } else {
    console.warn("No ASP.NET development certificate found. For a local HTTPS API, run dotnet dev-certs https and restart. For a remote API, configure MEMBERSHIP_API_ORIGIN.");
  }
}
const next = resolve(root, "node_modules/next/dist/bin/next");
const child = spawn(process.execPath, [next, "dev", "--hostname", "localhost", "--port", env.PORT || "3000"], { cwd: root, env, stdio: "inherit" });
child.on("error", error => { console.error(error.message); process.exitCode = 1; });
child.on("exit", code => { process.exitCode = code ?? 1; });
for (const signal of ["SIGINT", "SIGTERM"]) process.on(signal, () => child.kill(signal));
