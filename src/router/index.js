import ApodView from "@/views/ApodView.vue";
import ExploreView from "@/views/ExploreView.vue";
import HistoryView from "@/views/HistoryView.vue";
import HomeView from "@/views/HomeView.vue";
import ImagesView from "@/views/ImagesView.vue";
import LearnView from "@/views/LearnView.vue";
import LessonView from "@/views/LessonView.vue";
import LoginView from "@/views/LoginView.vue";
import NotFound from "@/views/NotFound.vue";
import Onboard from "@/views/Onboard.vue";
import ReaderView from "@/views/ReaderView.vue";
import SettingsView from "@/views/SettingsView.vue";
import TrackView from "@/views/TrackView.vue";
import { createRouter, createWebHistory } from "vue-router";
import { loadSession, useAuth } from "@/composables/useAuth";

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
      path: "/apod",
      name: "apod",
      component: ApodView,
    },
    {
      path: "/images",
      name: "images",
      component: ImagesView,
    },
    {
      path: "/learn",
      name: "learn",
      component: LearnView,
    },
    {
      path: "/learn/:track",
      name: "track",
      component: TrackView,
    },
    {
      path: "/learn/:track/:lesson",
      name: "lesson",
      component: LessonView,
    },
    {
      path: "/read/:id",
      name: "read",
      component: ReaderView,
    },
    {
      path: "/history",
      name: "history",
      component: HistoryView,
      meta: { requiresAuth: true },
    },
    {
      path: "/settings",
      name: "settings",
      component: SettingsView,
      meta: { requiresAuth: true },
    },
    {
      path: "/sign-up",
      name: "sign-up",
      component: Onboard,
      meta: { guestOnly: true },
    },
    {
      path: "/login",
      name: "login",
      component: LoginView,
      meta: { guestOnly: true },
    },
    {
      path: "/:pathMatch(.*)*",
      name: "not-found",
      component: NotFound,
    },
  ],
});

// Sign-in and sign-up are for signed-out readers; anyone else goes where they were heading.
// Pages marked requiresAuth send signed-out readers to sign in first.
router.beforeEach(async (to) => {
  if (!to.meta.guestOnly && !to.meta.requiresAuth) return true;
  await loadSession();
  const { isSignedIn } = useAuth();
  if (to.meta.guestOnly && isSignedIn.value) {
    const redirect = typeof to.query.redirect === "string" && to.query.redirect.startsWith("/");
    return redirect ? to.query.redirect : "/explore";
  }
  if (to.meta.requiresAuth && !isSignedIn.value) {
    return { path: "/login", query: { redirect: to.fullPath } };
  }
  return true;
});

// Pages that know their own title (the reader) set it; everything else gets the default.
router.afterEach((to) => {
  // These pages set their own title once their content loads.
  if (!["read", "track", "lesson"].includes(String(to.name))) document.title = "Novashelf";
});

export default router;
