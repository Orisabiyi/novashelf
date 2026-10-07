import "@/main.css";
import "primeicons/primeicons.css";
import { createApp } from "vue";
import App from "./App.vue";
import router from "./router";
import { applySavedTheme } from "@/composables/useReaderPreferences";

applySavedTheme();

const app = createApp(App);

app.use(router);

app.mount("#app");
