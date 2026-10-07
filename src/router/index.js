import ExploreView from "@/views/ExploreView.vue";
import HomeView from "@/views/HomeView.vue";
import NotFound from "@/views/NotFound.vue";
import Onboard from "@/views/Onboard.vue";
import { createRouter, createWebHistory } from "vue-router";

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  scrollBehavior(to, from, savedPosition) {
    if (savedPosition) return savedPosition;
    // Leave room for the floating navbar when jumping to a section like /#today.
    if (to.hash) return { el: to.hash, top: 96, behavior: "smooth" };
    // Changing only the query (e.g. Explore's ?topic=) shouldn't jump to the top.
    if (to.path === from.path) return false;
    return { top: 0 };
  },
  routes: [
    {
      path: "/",
      name: "home",
      component: HomeView,
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
