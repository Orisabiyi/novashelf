import { fileURLToPath, URL } from "node:url";

import { defineConfig } from "vite";
import vue from "@vitejs/plugin-vue";
import vueDevTools from "vite-plugin-vue-devtools";
import tailwindcss from "@tailwindcss/vite";

// https://vite.dev/config/
export default defineConfig({
  plugins: [vue(), vueDevTools(), tailwindcss()],
  server: {
    // The ASP.NET Core API (server/Novashelf.Api) runs on port 5088 in development.
    // Set NOVASHELF_API_URL to point the dev server at another instance.
    proxy: {
      "/api": process.env.NOVASHELF_API_URL ?? "http://localhost:5088",
    },
  },
  resolve: {
    alias: {
      "@": fileURLToPath(new URL("./src", import.meta.url)),
    },
  },
});
