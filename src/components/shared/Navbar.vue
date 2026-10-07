<script setup>
import { ref, watch } from "vue";
import { RouterLink, useRoute } from "vue-router";
import logoImg from "@/assets/novashelf-logo-ord.svg";

const route = useRoute();

const navLinks = [
  {
    name: "Explore",
    to: "/explore",
    isActive: () => route.path.startsWith("/explore"),
  },
  {
    name: "Today's picture",
    to: "/#today",
    isActive: () => route.path === "/" && route.hash === "#today",
  },
];

const menuOpen = ref(false);

// Close the mobile menu after navigating.
watch(
  () => route.fullPath,
  () => (menuOpen.value = false),
);
</script>

<template>
  <!-- A floating capsule: the wrapper is click-through so content scrolling past the
       pill's sides stays interactive. -->
  <header
    class="sticky top-0 z-20 px-4 pt-4 lg:px-10 2xl:px-48 pointer-events-none">
    <nav
      class="pointer-events-auto flex items-center gap-2 rounded-full border border-on-nav/10 bg-nav/80 backdrop-blur-md text-on-nav pl-4 pr-2 py-2 shadow-lg shadow-black/30">
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

      <RouterLink
        to="/sign-up"
        class="hidden md:inline-flex items-center gap-2 rounded-full bg-action text-on-action px-4 py-2 text-sm font-medium transition-transform hover:-translate-y-px">
        Get started
        <i class="pi pi-arrow-right" style="font-size: 0.7rem" aria-hidden="true"></i>
      </RouterLink>

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
      <li>
        <RouterLink
          to="/sign-up"
          class="flex items-center justify-center gap-2 rounded-2xl bg-action text-on-action px-4 py-3 font-medium">
          Get started
          <i class="pi pi-arrow-right" style="font-size: 0.75rem" aria-hidden="true"></i>
        </RouterLink>
      </li>
    </ul>
  </header>
</template>
