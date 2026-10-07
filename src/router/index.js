import ExploreView from "@/views/ExploreView.vue";
import HeroOnboarding from "@/views/HeroOnboarding.vue";
import NotFound from "@/views/NotFound.vue";
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
      path: "/explore",
      name: "explore",
      component: ExploreView,
    },
    {
      path: "/sign-up",
      name: "sign up",
      component: Onboard,
    },
    {
      path: "/:pathMatch(.*)*",
      name: "not-found",
      component: NotFound,
    },
  ],
});

export default router;
