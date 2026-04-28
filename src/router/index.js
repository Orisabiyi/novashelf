import HeroOnboarding from "@/views/HeroOnboarding.vue";
import Onboard from "@/views/Onboard.vue";
import { createRouter, createWebHistory } from "vue-router";

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: "/",
      name: "home",
      component: HeroOnboarding,
    },
    {
      path: "/sign-up",
      name: "sign up",
      component: Onboard,
    },
  ],
});

export default router;
