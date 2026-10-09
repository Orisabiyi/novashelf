import "@/main.css";
import "primeicons/primeicons.css";
import { createApp } from "vue";
import App from "./App.vue";
import router from "./router";
import { loadSession } from "@/composables/useAuth";
import { applySavedTheme } from "@/composables/useReaderPreferences";

applySavedTheme();
// Find out who's signed in without holding up the first render.
loadSession();

const app = createApp(App);

app.use(router);

app.mount("#app");
