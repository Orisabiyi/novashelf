<script setup>
import { computed, onBeforeUnmount, onMounted, ref, watch } from "vue";
import { RouterLink, useRoute, useRouter } from "vue-router";
import logoImg from "@/assets/novashelf-logo-ord.svg";
import { useAuth } from "@/composables/useAuth";

const route = useRoute();
const router = useRouter();
const { account, status, isSignedIn, displayName, signOut } = useAuth();
const initial = computed(() => displayName.value.charAt(0).toUpperCase());
// Hide account links when accounts are switched off on the server (no database).
const accountsAvailable = computed(() => status.value !== "unavailable");
// Bring readers back to the page they were on after signing in.
const authQuery = computed(() =>
  ["/login", "/sign-up"].includes(route.path) ? {} : { redirect: route.fullPath },
);

const navLinks = [
  {
    name: "Explore",
    to: "/explore",
    isActive: () => route.path.startsWith("/explore"),
  },
  {
    name: "Picture of the day",
    to: "/apod",
    isActive: () => route.path.startsWith("/apod"),
  },
  {
    name: "Images",
    to: "/images",
    isActive: () => route.path.startsWith("/images"),
  },
  {
    name: "Learn",
    to: "/learn",
    isActive: () => route.path.startsWith("/learn"),
  },
];

const menuOpen = ref(false);

const accountOpen = ref(false);
const accountMenu = ref(null);

// Close menus after navigating.
watch(
  () => route.fullPath,
  () => {
    menuOpen.value = false;
    accountOpen.value = false;
  },
);

function onDocumentClick(event) {
  if (accountOpen.value && !accountMenu.value?.contains(event.target)) accountOpen.value = false;
}
function onKeydown(event) {
  if (event.key === "Escape") accountOpen.value = false;
}
onMounted(() => {
  document.addEventListener("click", onDocumentClick);
  document.addEventListener("keydown", onKeydown);
});
onBeforeUnmount(() => {
  document.removeEventListener("click", onDocumentClick);
  document.removeEventListener("keydown", onKeydown);
});

async function handleSignOut() {
  await signOut();
  accountOpen.value = false;
  menuOpen.value = false;
  router.push("/");
}
</script>

<template>
  <!-- A floating capsule: the wrapper is click-through so content scrolling past the
       pill's sides stays interactive. -->
  <header
    class="sticky top-0 z-20 px-4 pt-4 lg:px-10 2xl:px-48 pointer-events-none">
    <nav
      class="pointer-events-auto flex items-center gap-2 rounded-full border border-on-nav/10 bg-nav/95 backdrop-blur-md text-on-nav pl-4 pr-2 py-2 shadow-lg shadow-black/30">
      <RouterLink to="/" class="flex items-center gap-2 rounded-full pr-2">
        <img :src="logoImg" alt="" class="w-11 h-auto" />
        <span class="font-display text-xl font-medium text-brand">novashelf</span>
      </RouterLink>

      <ul class="hidden md:flex items-center gap-1 mx-auto">
        <li v-for="link in navLinks" :key="link.name">
          <RouterLink
            :to="link.to"
            :aria-current="link.isActive() ? 'page' : undefined"
            :class="[
              'flex items-center gap-2 rounded-full px-4 py-1.5 text-sm font-medium transition-colors',
              link.isActive()
                ? 'bg-on-nav/10 text-on-nav'
                : 'text-on-nav-muted hover:text-on-nav hover:bg-on-nav/5',
            ]">
            <span
              v-if="link.isActive()"
              class="size-1.5 rounded-full bg-nav-link shadow-[0_0_8px] shadow-nav-link"
              aria-hidden="true"></span>
            {{ link.name }}
          </RouterLink>
        </li>
      </ul>

      <!-- Signed in: account button with a small menu -->
      <div v-if="isSignedIn" ref="accountMenu" class="relative hidden md:block">
        <button
          type="button"
          class="flex items-center gap-2 rounded-full py-1 pl-1 pr-3 text-sm font-medium hover:bg-on-nav/10"
          :aria-expanded="accountOpen"
          aria-controls="account-menu"
          @click="accountOpen = !accountOpen">
          <span class="grid size-8 place-items-center rounded-full bg-action text-on-action font-display font-bold" aria-hidden="true">
            {{ initial }}
          </span>
          <span class="max-w-32 truncate">{{ displayName }}</span>
          <i class="pi pi-chevron-down text-on-nav-muted" style="font-size: 0.65rem" aria-hidden="true"></i>
        </button>
        <div
          v-if="accountOpen"
          id="account-menu"
          class="absolute right-0 top-full mt-3 w-64 rounded-2xl border border-on-nav/10 bg-nav p-2 shadow-2xl shadow-black/40">
          <p class="px-3 py-2 text-xs text-on-nav-muted">
            Signed in as<br />
            <span class="text-sm text-on-nav break-all">{{ account?.email }}</span>
          </p>
          <RouterLink
            to="/history"
            class="flex w-full items-center gap-2 rounded-xl px-3 py-2 text-sm font-medium hover:bg-on-nav/10"
            @click="accountOpen = false">
            <i class="pi pi-history" style="font-size: 0.8rem" aria-hidden="true"></i>
            Reading history
          </RouterLink>
          <RouterLink
            to="/settings"
            class="flex w-full items-center gap-2 rounded-xl px-3 py-2 text-sm font-medium hover:bg-on-nav/10"
            @click="accountOpen = false">
            <i class="pi pi-cog" style="font-size: 0.8rem" aria-hidden="true"></i>
            Settings
          </RouterLink>
          <button
            type="button"
            class="flex w-full items-center gap-2 rounded-xl px-3 py-2 text-left text-sm font-medium hover:bg-on-nav/10"
            @click="handleSignOut">
            <i class="pi pi-sign-out" style="font-size: 0.8rem" aria-hidden="true"></i>
            Sign out
          </button>
        </div>
      </div>

      <!-- Signed out -->
      <div v-else-if="accountsAvailable" class="hidden md:flex items-center gap-1">
        <RouterLink
          :to="{ path: '/login', query: authQuery }"
          class="rounded-full px-4 py-2 text-sm font-medium text-on-nav-muted hover:text-on-nav">
          Sign in
        </RouterLink>
        <RouterLink
          :to="{ path: '/sign-up', query: authQuery }"
          class="inline-flex items-center gap-2 rounded-full bg-action text-on-action px-4 py-2 text-sm font-medium transition-transform hover:-translate-y-px">
          Get started
          <i class="pi pi-arrow-right" style="font-size: 0.7rem" aria-hidden="true"></i>
        </RouterLink>
      </div>

      <button
        type="button"
        class="md:hidden ml-auto size-10 grid place-items-center rounded-full hover:bg-on-nav/10"
        :aria-expanded="menuOpen"
        aria-controls="mobile-nav"
        :aria-label="menuOpen ? 'Close menu' : 'Open menu'"
        @click="menuOpen = !menuOpen">
        <i :class="menuOpen ? 'pi pi-times' : 'pi pi-bars'" aria-hidden="true"></i>
      </button>
    </nav>

    <ul
      v-if="menuOpen"
      id="mobile-nav"
      class="md:hidden pointer-events-auto mt-2 flex flex-col gap-1 rounded-3xl border border-on-nav/10 bg-nav/95 backdrop-blur-md text-on-nav p-2 shadow-lg shadow-black/30">
      <li v-for="link in navLinks" :key="link.name">
        <RouterLink
          :to="link.to"
          :class="[
            'flex items-center gap-2 rounded-2xl px-4 py-3 font-medium',
            link.isActive() ? 'bg-on-nav/10' : 'text-on-nav-muted',
          ]">
          <span
            v-if="link.isActive()"
            class="size-1.5 rounded-full bg-nav-link"
            aria-hidden="true"></span>
          {{ link.name }}
        </RouterLink>
      </li>
      <template v-if="isSignedIn">
        <li class="mt-1 border-t border-on-nav/10 px-4 pt-3 pb-1 text-sm text-on-nav-muted">
          Signed in as <span class="text-on-nav break-all">{{ account?.email }}</span>
        </li>
        <li>
          <RouterLink to="/history" class="flex items-center gap-2 rounded-2xl px-4 py-3 font-medium text-on-nav-muted">
            <i class="pi pi-history" style="font-size: 0.85rem" aria-hidden="true"></i>
            Reading history
          </RouterLink>
        </li>
        <li>
          <RouterLink to="/settings" class="flex items-center gap-2 rounded-2xl px-4 py-3 font-medium text-on-nav-muted">
            <i class="pi pi-cog" style="font-size: 0.85rem" aria-hidden="true"></i>
            Settings
          </RouterLink>
        </li>
        <li>
          <button
            type="button"
            class="flex w-full items-center gap-2 rounded-2xl px-4 py-3 font-medium text-on-nav-muted"
            @click="handleSignOut">
            <i class="pi pi-sign-out" style="font-size: 0.85rem" aria-hidden="true"></i>
            Sign out
          </button>
        </li>
      </template>
      <template v-else-if="accountsAvailable">
        <li>
          <RouterLink
            :to="{ path: '/login', query: authQuery }"
            class="flex items-center rounded-2xl px-4 py-3 font-medium text-on-nav-muted">
            Sign in
          </RouterLink>
        </li>
        <li>
          <RouterLink
            :to="{ path: '/sign-up', query: authQuery }"
            class="flex items-center justify-center gap-2 rounded-2xl bg-action text-on-action px-4 py-3 font-medium">
            Get started
            <i class="pi pi-arrow-right" style="font-size: 0.75rem" aria-hidden="true"></i>
          </RouterLink>
        </li>
      </template>
    </ul>
  </header>
</template>
