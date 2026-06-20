import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

// El front corre en :5173; el server .NET en :5050 (ver SERVER_URL en src/lib/api.ts).
export default defineConfig({
  plugins: [react()],
  server: { port: 5173 },
});
