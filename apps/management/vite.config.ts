import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import fs from "node:fs";
export default defineConfig(({ command }) => ({
  plugins: [react()],
  server: {
    host: "localhost",
    port: 5173,
    strictPort: true,
    https:
      command === "serve"
        ? {
            cert: fs.readFileSync(".certs/localhost.pem"),
            key: fs.readFileSync(".certs/localhost.key"),
          }
        : undefined,
    proxy: {
      "/api": {
        target: process.env.API_PROXY_TARGET || "https://localhost:7198",
        changeOrigin: true,
        secure: process.env.NODE_ENV === "production",
      },
    },
  },
  build: { outDir: "dist", sourcemap: false },
}));
